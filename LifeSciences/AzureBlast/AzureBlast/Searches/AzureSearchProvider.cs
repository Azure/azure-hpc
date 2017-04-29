// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Batch.Blast.Batch;
using Microsoft.Azure.Batch.Blast.Configuration;
using Microsoft.Azure.Batch.Blast.Databases;
using Microsoft.Azure.Batch.Blast.Storage;
using Microsoft.Azure.Batch.Blast.Storage.Entities;
using Microsoft.Azure.Batch.Common;

namespace Microsoft.Azure.Batch.Blast.Searches
{
    public class AzureSearchProvider : ISearchProvider
    {
        private readonly BlastConfiguration _configuration;
        private readonly IBlobStorageProvider _blobStorageProvider;
        private readonly ITableStorageProvider _tableStorageProvider;
        private readonly IDatabaseProvider _databaseProvider;
        private readonly BatchClient _batchClient;
        private readonly StorageCredentials _storageCredentials;
        private readonly BatchCredentials _batchCredentials;

        public AzureSearchProvider(BlastConfiguration configuration, IDatabaseProvider databaseProvider)
        {
            _configuration = configuration;
            _databaseProvider = databaseProvider;
            _tableStorageProvider = configuration.TableStorageProvider;
            _blobStorageProvider = configuration.BlobStorageProvider;
            _batchClient = configuration.BatchClient;
            _storageCredentials = configuration.StorageCredentials;
            _batchCredentials = configuration.BatchCredentials;
        }

        public Guid SubmitSearch(SearchSpecification search)
        {
            if (search == null)
            {
                throw new ArgumentNullException("search");
            }

            ValidateExecutableArgs(search.ExecutableArgs);

            var db = _databaseProvider.GetDatabase(search.DatabaseName);
            if (db == null)
            {
                throw new ArgumentException(string.Format("Cannot find database {0}", search.DatabaseName));
            }
            var fragments = _databaseProvider.GetDatabaseFragments(search.DatabaseName);
            if (fragments == null || fragments.Count == 0)
            {
                throw new ArgumentException(string.Format("Database has no fragments {0}", search.DatabaseName));
            }

            var searchEntity = CreateSearchEntity(search, db);

            try
            {
                // Upload all the inputs to storage
                Parallel.ForEach(search.SearchInputFiles, queryFile =>
                {
                    var filename = Path.GetFileName(queryFile.Filename);
                    _blobStorageProvider.UploadBlobFromStream(searchEntity.InputContainer, filename, queryFile.Content);
                });

                var queryIndex = 0;
                var searchQueries = new List<SearchQueryEntity>();
                foreach (var queryFile in search.SearchInputFiles)
                {
                    var query = new SearchQueryEntity(searchEntity.Id, queryIndex.ToString());
                    query.OutputContainer = searchEntity.OutputContainer;
                    query.QueryFilename = Path.GetFileName(queryFile.Filename);
                    query.State = QueryState.Waiting;
                    query.QueryOutputFilename = GetQueryOutputFilename(searchEntity.OutputfileFormat, queryIndex.ToString());
                    query.LogOutputFilename = GetLogFilename(searchEntity.OutputfileFormat, queryIndex.ToString());
                    searchQueries.Add(query);
                    queryIndex++;
                }

                _tableStorageProvider.InsertEntities(searchQueries);

                // Stage the generic batch scripts to storage
                var resourceFiles = InputFileStager.StageImportScripts(_blobStorageProvider);
                SubmitBatchJob(searchEntity, searchQueries, resourceFiles);

                searchEntity.State = SearchState.WaitingForResources;
                _tableStorageProvider.UpdateEntity(searchEntity);
            }
            catch (Exception e)
            {
                if (e is AggregateException)
                {
                    e = e.InnerException;
                }

                searchEntity.State = SearchState.Error;
                searchEntity.Errors = e.ToString();
                _tableStorageProvider.UpdateEntity(searchEntity);

                throw e;
            }

            return searchEntity.Id;
        }

        private void ValidateExecutableArgs(string arguments)
        {
            if (string.IsNullOrEmpty(arguments))
            {
                return;
            }

            var tokens = arguments.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);

            var reservedArgs = _configuration.ReservedExecutableArguments;

            foreach (var reservedExecutableArgument in _configuration.ReservedExecutableArguments)
            {
                if (tokens.Contains(reservedExecutableArgument))
                {
                    throw new Exception("Arguments cannot contain reserved arguments " + string.Join(",", reservedArgs));
                }
            }
        }

        private SearchEntity CreateSearchEntity(SearchSpecification search, DatabaseEntity database)
        {
            var queryId = Guid.NewGuid();
            var searchEntity = new SearchEntity(queryId);
            searchEntity.Name = search.Name;
            searchEntity.JobId = queryId.ToString();
            searchEntity.InputContainer = queryId.ToString();
            searchEntity.OutputContainer = queryId.ToString();
            searchEntity.DatabaseId = search.DatabaseName;
            searchEntity.DatabaseType = database.Type;
            searchEntity.DatabaseContainer = database.ContainerName;
            searchEntity.Executable = search.Executable;
            searchEntity.ExecutableArgs = search.ExecutableArgs;
            searchEntity.ExecutableArgsSanitised = search.ExecutableArgs;
            searchEntity.State = SearchState.StagingData;
            searchEntity.StartTime = DateTime.UtcNow;
            searchEntity.PoolId = search.PoolId;
            searchEntity.PoolDisplayName = search.PoolDisplayName;
            searchEntity.TargetDedicated = search.TargetDedicated;
            searchEntity.VirtualMachineSize = search.VirtualMachineSize;
            searchEntity.CompletedTasks = 0;
            searchEntity.TotalTasks = search.SearchInputFiles.Count();
            ParseExecutableArgs(search, searchEntity);
            _tableStorageProvider.InsertEntity(searchEntity);
            return searchEntity;
        }

        private void SubmitBatchJob(SearchEntity searchEntity, IEnumerable<SearchQueryEntity> queries, List<ResourceFile> resourceFiles)
        {
            PoolInformation poolInfo;

            if (!string.IsNullOrEmpty(searchEntity.PoolId))
            {
                poolInfo = new PoolInformation
                {
                    PoolId = searchEntity.PoolId,
                };
            }
            else
            {
                var maxTasksPerNode = _configuration.GetCoresForVirtualMachineSize(searchEntity.VirtualMachineSize);

                if (searchEntity.TargetDedicated == 1 && maxTasksPerNode == 1)
                {
                    // Need to always ensure a JM can run
                    maxTasksPerNode = 2;
                }

                poolInfo = new PoolInformation
                {
                    AutoPoolSpecification = new AutoPoolSpecification
                    {
                        PoolSpecification = new PoolSpecification
                        {
                            TargetDedicated = searchEntity.TargetDedicated,
                            MaxTasksPerComputeNode = maxTasksPerNode,
                            VirtualMachineSize = searchEntity.VirtualMachineSize,
                            VirtualMachineConfiguration = _configuration.GetVirtualMachineConfiguration(),
                        },
                        PoolLifetimeOption = PoolLifetimeOption.Job,
                        KeepAlive = false,
                    }
                };
            }

            var job = _batchClient.JobOperations.CreateJob(searchEntity.JobId, poolInfo);
            job.DisplayName = searchEntity.DatabaseId;
            job.JobPreparationTask = GetJobPreparationTask(
                searchEntity.DatabaseId,
                searchEntity.DatabaseContainer,
                resourceFiles);
            job.JobManagerTask = GetJobManagerTask(
                searchEntity.Id.ToString(),
                resourceFiles);
            job.Commit();

            var tasks = GetTasks(searchEntity, queries);
            job.Refresh();
            job.AddTask(tasks);

//            job.Refresh();
//            job.OnAllTasksComplete = OnAllTasksComplete.TerminateJob;
        }

        private IEnumerable<CloudTask> GetTasks(SearchEntity searchEntity, IEnumerable<SearchQueryEntity> queries)
        {
            var taskId = 0;
            foreach (var query in queries)
            {
                var cmd = string.Format("/bin/bash -c '{0}; result=$?; {1}; exit $result'",
                    GetBlastCommandLine(searchEntity.DatabaseId, searchEntity.Executable, searchEntity.ExecutableArgsSanitised, query.QueryFilename, query.QueryOutputFilename, query.LogOutputFilename),
                    GetUploadCommandLine(searchEntity.OutputContainer));

                var task = new CloudTask(taskId.ToString(), cmd);
                task.DisplayName = query.QueryFilename;
                task.UserIdentity = new UserIdentity(new AutoUserSpecification(elevationLevel: ElevationLevel.Admin));
                task.ResourceFiles = new List<ResourceFile>
                {
                    new ResourceFile(_blobStorageProvider.GetBlobSAS(searchEntity.InputContainer, query.QueryFilename), query.QueryFilename)
                };

                task.EnvironmentSettings = new[]
                {
                    new EnvironmentSetting("BLOBXFER_STORAGEACCOUNTKEY", _storageCredentials.Key),
                };

                yield return task;

                taskId++;
            }
        }

        private static string GetQueryOutputFilename(string outputFilenameFormat, string taskId)
        {
            // outputFilenameFormat is only used here to determine if we should use the old legacy names.
            if (string.IsNullOrEmpty(outputFilenameFormat))
            {
                return string.Format("queryoutput-{0}.xml", taskId);
            }
            return string.Format(outputFilenameFormat, taskId);
        }

        private static string GetLogFilename(string outputFilenameFormat, string taskId)
        {
            if (string.IsNullOrEmpty(outputFilenameFormat))
            {
                return string.Format("blastoutput-{0}.log", taskId);
            }
            return string.Format("log-{0}.txt", taskId);
        }

        private string GetBlastCommandLine(string databaseName, string executable, string executableArgs,
            string queryFilename, string queryOutputFilename, string logOutputFilename)
        {
            var outputFormat = "-outfmt 5"; // XML
            if (!string.IsNullOrEmpty(executableArgs) && executableArgs.Contains(" -outfmt "))
            {
                outputFormat = ""; // let it be overriden by exec args
            }

            return string.Format(
                "{0} -db /dev/shm/{1}/{1} -query {2} {3} -out {4} {5} > {6} 2>&1",
                executable.ToLower(),
                databaseName,
                queryFilename,
                outputFormat,
                queryOutputFilename,
                executableArgs,
                logOutputFilename);
        }

        /// <summary>
        /// Deal with any arguments and possible output filename.
        /// If a output filename is specified, create a string 'format' based
        /// on that and string out the arg.
        /// </summary>
        private void ParseExecutableArgs(SearchSpecification searchSpec, SearchEntity searchEntity)
        {
            var outputFormat = "output-{0}.xml";
            var executableArgs = searchSpec.ExecutableArgs;

            if (!string.IsNullOrEmpty(executableArgs))
            {
                var tokens = executableArgs.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries).ToList();

                if (tokens.Any(token => token == "-out"))
                {
                    var indexOfOutArg = tokens.IndexOf("-out");
                    var indexOfOutFilename = indexOfOutArg + 1;
                    if (indexOfOutFilename > tokens.Count - 1)
                    {
                        throw new Exception("No filename specified for -out argument");
                    }
                    var outputFilename = tokens[indexOfOutArg + 1];
                    var name = Path.GetFileNameWithoutExtension(outputFilename);
                    var extension = Path.GetExtension(outputFilename);
                    outputFormat = name + "-{0}" + extension;

                    // Strip out the -out filename args as we already set it later
                    executableArgs = executableArgs.Replace(" -out " + outputFilename, " ");
                }
            }

            searchEntity.ExecutableArgsSanitised = executableArgs;
            searchEntity.OutputfileFormat = outputFormat;
        }

        private string GetUploadCommandLine(string outputContainer)
        {
            return string.Format(
                "blobxfer {0} {1} . --upload --include \"*\"",
                _storageCredentials.Account,
                outputContainer);
        }

        public JobManagerTask GetJobManagerTask(string searchId, List<ResourceFile> resourceFiles)
        {
            var cmd =
                string.Format("/bin/bash -c 'python3 SearchJobManager.py {0} {1} {2} {3} {4} {5} {6} {7}'",
                _storageCredentials.Account,
                _storageCredentials.Key,
                _batchCredentials.Account,
                _batchCredentials.Key,
                _batchCredentials.Url,
                "$AZ_BATCH_JOB_ID",
                SearchEntity.AllUsersPk, // PK for JobMananger
                searchId); // RK for JobManager

            return new JobManagerTask
            {
                Id = "JobManager",
                CommandLine = cmd,
                RunExclusive = false,
                UserIdentity = new UserIdentity(new AutoUserSpecification(elevationLevel: ElevationLevel.Admin)),
                ResourceFiles = resourceFiles,
                Constraints = new TaskConstraints(null, null, 5),
                KillJobOnCompletion = true,
            };
        }

        public JobPreparationTask GetJobPreparationTask(string databaseName, string databaseContainer, List<ResourceFile> resourceFiles)
        {
            var cmd = string.Format("/bin/bash -c 'query-job-prep.sh {0} {1} {2} {3}'",
                databaseName,
                _storageCredentials.Account,
                _storageCredentials.Key,
                databaseContainer);

            return new JobPreparationTask
            {
                CommandLine = cmd,
                ResourceFiles = resourceFiles,
                UserIdentity = new UserIdentity(new AutoUserSpecification(elevationLevel: ElevationLevel.Admin)),

            };
        }

        public SearchEntity GetSearch(Guid queryId)
        {
            var entity = _tableStorageProvider.GetEntity<SearchEntity>(SearchEntity.AllUsersPk, queryId.ToString());

            if (entity == null)
            {
                return null;
            }

            try
            {
                var job = _batchClient.JobOperations.GetJob(entity.JobId);

                if (job.ExecutionInformation != null && job.ExecutionInformation.PoolId != null)
                {
                    entity.PoolId = job.ExecutionInformation.PoolId;
                }
            }
            catch (BatchException be)
            {
                if (be.RequestInformation.HttpStatusCode != HttpStatusCode.NotFound)
                {
                    throw;
                }
            }

            return entity;
        }

        public void DeleteSearch(Guid searchId)
        {
            var entity = _tableStorageProvider.GetEntity<SearchEntity>(SearchEntity.AllUsersPk, searchId.ToString());

            if (entity == null)
            {
                throw new Exception("No such search " + searchId);
            }

            if (entity.InputContainer != null)
            {
                _blobStorageProvider.DeleteContainer(entity.InputContainer);
            }

            if (entity.OutputContainer != null)
            {
                _blobStorageProvider.DeleteContainer(entity.OutputContainer);
            }

            if (entity.JobId != null)
            {
                try
                {
                    _batchClient.JobOperations.DeleteJob(entity.JobId);
                }
                catch (BatchException be)
                {
                    if (be.RequestInformation.HttpStatusCode != HttpStatusCode.NotFound)
                    {
                        throw;
                    }
                }
            }

            _tableStorageProvider.DeleteEntity(entity);
        }

        public void CancelSearch(Guid searchId)
        {
            var entity = _tableStorageProvider.GetEntity<SearchEntity>(SearchEntity.AllUsersPk, searchId.ToString());

            if (entity == null)
            {
                throw new Exception("No such search " + searchId);
            }

            _batchClient.JobOperations.TerminateJob(entity.JobId);

            entity.State = SearchState.Canceled;

            _tableStorageProvider.UpdateEntity(entity);
        }

        public IEnumerable<SearchEntity> ListSearches()
        {
            return _tableStorageProvider.ListEntities<SearchEntity>(SearchEntity.AllUsersPk);
        }

        public IEnumerable<SearchQueryEntity> ListSearchQueries(Guid searchId)
        {
            var entity = _tableStorageProvider.GetEntity<SearchEntity>(SearchEntity.AllUsersPk, searchId.ToString());

            if (entity == null)
            {
                throw new Exception("No such search " + searchId);
            }

            if (entity.Version == 0)
            {
                return ListLegacySearchQueries(entity);
            }

            if (entity.Version == 1)
            {
                return ListV1SearchQueries(entity);
            }

            throw new ArgumentException("Unknown search version: " + entity.Version);
        }

        private IEnumerable<SearchQueryEntity> ListLegacySearchQueries(SearchEntity entity)
        {
            IEnumerable<QueryOutput> queryOutputs = GetAllQueryOutputs(entity).ToList();

            List<SearchQueryEntity> searchQueries = new List<SearchQueryEntity>();

            try
            {
                var tasks = _batchClient.JobOperations.ListTasks(entity.JobId).Where(
                    task => task.Id != "JobManager").ToList();

                foreach (var task in tasks)
                {
                    var queryOutput = GetQueryOutputFilename(entity.OutputfileFormat, task.Id);
                    var logOutput = GetLogFilename(entity.OutputfileFormat, task.Id);
                    var outputs =
                        queryOutputs.Where(output => output.Filename == queryOutput || output.Filename == logOutput)
                            .ToList();

                    searchQueries.Add(new SearchQueryEntity
                    {
                        Id = task.Id,
                        QueryFilename = task.DisplayName,
                        Outputs = outputs,
                        State = BatchToQueryState(task),
                        StartTime = task.ExecutionInformation?.StartTime,
                        EndTime = task.ExecutionInformation?.EndTime,
                    });
                }
            }
            catch (Exception)
            {
                var inputFiles = entity.Files;
                foreach (var queryNumber in Enumerable.Range(0, (int)entity.TotalTasks))
                {
                    var queryOutput = GetQueryOutputFilename(entity.OutputfileFormat, queryNumber.ToString());
                    var logOutput = GetLogFilename(entity.OutputfileFormat, queryNumber.ToString());
                    var outputs =
                        queryOutputs.Where(output => output.Filename == queryOutput || output.Filename == logOutput)
                            .ToList();

                    try
                    {
                        searchQueries.Add(new SearchQueryEntity
                        {
                            Id = queryNumber.ToString(),
                            QueryFilename = inputFiles[queryNumber],
                            Outputs = outputs,
                            State = QueryState.Success,
                            StartTime = null,
                            EndTime = null,
                        });
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error getting search query " + e);
                    }
                }
            }

            return searchQueries;
        }

        private IEnumerable<SearchQueryEntity> ListV1SearchQueries(SearchEntity entity)
        {
            IEnumerable<QueryOutput> queryOutputs = GetAllQueryOutputs(entity).ToList();

            var queries = _tableStorageProvider.ListEntities<SearchQueryEntity>(entity.Id.ToString()).ToList();

            foreach (var searchQueryEntity in queries)
            {
                var queryOutput = GetQueryOutputFilename(entity.OutputfileFormat, searchQueryEntity.Id);
                var logOutput = GetLogFilename(entity.OutputfileFormat, searchQueryEntity.Id);
                var outputs =
                    queryOutputs.Where(output => output.Filename == queryOutput || output.Filename == logOutput)
                        .ToList();
                searchQueryEntity.Outputs = outputs;
            }

            return queries;
        }

        private QueryState BatchToQueryState(CloudTask task)
        {
            switch (task.State)
            {
                case TaskState.Active: return QueryState.Waiting;
                case TaskState.Preparing: return QueryState.Waiting;
                case TaskState.Running: return QueryState.Running;
                case TaskState.Completed:
                    if (task.ExecutionInformation.ExitCode.HasValue && task.ExecutionInformation.ExitCode == 0)
                    {
                        return QueryState.Success;
                    }
                    return QueryState.Error;
                default: return QueryState.Unmapped;
            }
        }

        public string GetSearchQueryOutput(Guid searchId, string queryId, string filename)
        {
            var entity = _tableStorageProvider.GetEntity<SearchEntity>(SearchEntity.AllUsersPk, searchId.ToString());

            if (entity == null)
            {
                throw new Exception("No such search " + searchId);
            }

            var blobs = _blobStorageProvider.ListBlobs(entity.OutputContainer);

            var blob = blobs.FirstOrDefault(b => b.BlobName == filename);

            if (blob == null)
            {
                throw new Exception("No such query output " + filename);
            }

            return _blobStorageProvider.GetBlobAsText(entity.OutputContainer, blob.BlobName);
        }

        private IEnumerable<QueryOutput> GetAllQueryOutputs(SearchEntity searchEntity)
        {
            return
                _blobStorageProvider.ListBlobs(searchEntity.OutputContainer)
                    .Select(
                        blob =>
                            new QueryOutput
                            {
                                Filename = blob.BlobName,
                                Url = _blobStorageProvider.GetBlobSAS(searchEntity.OutputContainer, blob.BlobName),
                                Length = blob.Length,
                            });
        }
    }
}
