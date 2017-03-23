// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Azure.Batch.Blast.Batch;
using Microsoft.Azure.Batch.Blast.Configuration;
using Microsoft.Azure.Batch.Blast.Databases.ExternalSources;
using Microsoft.Azure.Batch.Blast.Storage;
using Microsoft.Azure.Batch.Blast.Storage.Entities;
using Microsoft.Azure.Batch.Common;

namespace Microsoft.Azure.Batch.Blast.Databases.Imports
{
    public class DatabaseImportManager : IDatabaseImportManager
    {
        private readonly BlastConfiguration _configuration;
        private readonly ITableStorageProvider _tableStorageProvider;
        private readonly IBlobStorageProvider _blobStorageProvider;
        private readonly BatchClient _batchClient;
        private readonly StorageCredentials _storageCredentials;
        private readonly BatchCredentials _batchCredentials;

        public DatabaseImportManager(BlastConfiguration configuration)
        {
            _configuration = configuration;
            _tableStorageProvider = configuration.TableStorageProvider;
            _blobStorageProvider = configuration.BlobStorageProvider;
            _batchClient = configuration.BatchClient;
            _storageCredentials = configuration.StorageCredentials;
            _batchCredentials = configuration.BatchCredentials;
        }

        public void SubmitImport(
            ExternalRepository externalRepository,
            ExternalDatabase externalDatabase)
        {
            ValidateNoImportsInProgress(externalDatabase.Name);

            var jobId = string.Format("import-{0}-{1}", externalDatabase.Name, Guid.NewGuid());
            var fragments = externalRepository.DatabaseSource.GetDatabaseFragments(externalDatabase.Name);
            var entity = CreateDatabaseEntity(SystemDatabaseProvider.DefaultContainerName, jobId, externalDatabase.Name, fragments.Count);

            try
            {
                // Stage all the job scripts
                var resourceFiles = InputFileStager.StageImportScripts(_blobStorageProvider);

                // Submit the job and tasks to batch
                SubmitBatchJob(jobId, externalDatabase.Name, fragments, resourceFiles);
            }
            catch (Exception)
            {
                CleanupAfterFailure(jobId, entity);
                throw;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="databaseName"></param>
        /// <param name="containerName"></param>
        public void ImportExisting(string databaseName, string databaseDescription, string containerName)
        {
            var container = _blobStorageProvider.GetContainer(containerName);

            if (container == null)
            {
                throw new Exception("Database container not found");
            }

            var blobs = _blobStorageProvider.ListBlobs(containerName).ToList();

            if (!blobs.Any())
            {
                throw new Exception("Container contains no database files");
            }

            var entity = CreateDatabaseEntity(containerName, null, databaseName, blobs.Count, false);
            entity.DisplayName = databaseDescription;
            entity.CompletedTasks = blobs.Count;
            entity.State = DatabaseState.Ready;
            entity.TotalSize = blobs.Sum(b => b.Length);
            entity.FileCount = blobs.Count;
            entity.DedicatedContainer = true;
            entity.Type = blobs.Any(b => Path.GetExtension(b.BlobName).StartsWith(".p"))
                ? DatabaseType.Protein
                : DatabaseType.Nucleotide;

            _tableStorageProvider.UpsertEntity(entity);
        }

        private DatabaseEntity CreateDatabaseEntity(string containerName, string jobId, string databaseName, int fragmentCount, bool insertEntity = true)
        {
            var entity = new DatabaseEntity(
                databaseName,
                containerName,
                0, // Zero file count for now as we don't know until extracted
                0, // Zero until extracted
                DatabaseState.ImportingWaitingForResources,
                DatabaseType.Unknown,
                jobId);

            // Total batch tasks for import
            entity.TotalTasks = fragmentCount;
            entity.CompletedTasks = 0;

            if (insertEntity)
            {
                // We allow upsert in case someone wants to re-import/overwrite
                _tableStorageProvider.UpsertEntity(entity);
            }

            return entity;
        }

        private void SubmitBatchJob(
            string jobId,
            string databaseName,
            IReadOnlyList<DatabaseFragment> fragments,
            List<ResourceFile> resourceFiles)
        {
            var poolInfo = new PoolInformation
            {
                AutoPoolSpecification = new AutoPoolSpecification
                {
                    PoolSpecification = new PoolSpecification
                    {
                        TargetDedicated = Math.Min(_configuration.GetImportVirtualMachineMaxCount(), fragments.Count), // We don't want to create too many VMs
                        MaxTasksPerComputeNode = _configuration.GetImportVirtualMachineCores(),
                        VirtualMachineSize = _configuration.GetImportVirtualMachineSize(),
                        VirtualMachineConfiguration = _configuration.GetVirtualMachineConfiguration(),
                        StartTask = GetStartTask(resourceFiles),
                    },
                    PoolLifetimeOption = PoolLifetimeOption.Job,
                    KeepAlive = false,
                }
            };

            var job = _batchClient.JobOperations.CreateJob(jobId, poolInfo);
            job.DisplayName = databaseName;
            job.JobPreparationTask = GetJobPreparationTask(resourceFiles);
            job.JobManagerTask = GetJobManagerTask(databaseName, resourceFiles);
            job.Commit();

            var tasks = GetTasks(databaseName, fragments);
            job.Refresh();
            job.AddTask(tasks);
            job.Refresh();
            job.OnAllTasksComplete = OnAllTasksComplete.TerminateJob;
        }

        private void ValidateNoImportsInProgress(string databaseName)
        {
            var existingEntity = _tableStorageProvider.GetEntity<DatabaseEntity>(DatabaseEntity.DefaultRepository, databaseName);
            if (existingEntity != null && existingEntity.ImportInProgress)
            {
                // Ensure we don't already have one
                throw new Exception("Import already in progress for database: " + databaseName);
            }
        }

        private string GetImportScriptsPath()
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            var assemblyPath = Path.GetDirectoryName(path);
            return Path.Combine(assemblyPath, "BatchScripts");
        }

        public JobManagerTask GetJobManagerTask(string databaseName, List<ResourceFile> resourceFiles)
        {
            var cmd =
                string.Format("/bin/bash -c 'python3 jobmanager.py {0} {1} {2} {3} {4} {5} {6} {7} {8} {9}'",
                _storageCredentials.Account,
                _storageCredentials.Key,
                _batchCredentials.Account,
                _batchCredentials.Key,
                _batchCredentials.Url,
                typeof(DatabaseEntity).Name,
                "$AZ_BATCH_JOB_ID",
                DatabaseEntity.DefaultRepository,
                databaseName,
                SystemDatabaseProvider.DefaultContainerName);

            return new JobManagerTask
            {
                Id = "JobManager",
                CommandLine = cmd,
                RunExclusive = false,
                UserIdentity = new UserIdentity(new AutoUserSpecification(elevationLevel: ElevationLevel.Admin)),
                ResourceFiles = resourceFiles,
                Constraints = new TaskConstraints(null, null, 3),
            };
        }

        public static StartTask GetStartTask(IList<ResourceFile> resourceFiles)
        {
            var startTask = new StartTask("/bin/bash starttask.sh");
            startTask.WaitForSuccess = true;
            startTask.UserIdentity = new UserIdentity(new AutoUserSpecification(elevationLevel: ElevationLevel.Admin));
            startTask.ResourceFiles = resourceFiles;
            return startTask;
        }

        public JobPreparationTask GetJobPreparationTask(List<ResourceFile> resourceFiles)
        {
            return new JobPreparationTask
            {
                CommandLine = "/bin/bash import-database-prep.sh",
                ResourceFiles = resourceFiles,
                UserIdentity = new UserIdentity(new AutoUserSpecification(elevationLevel: ElevationLevel.Admin)),
                WaitForSuccess = true,
            };
        }

        public IEnumerable<CloudTask> GetTasks(string databaseName, IEnumerable<DatabaseFragment> fragments)
        {
            var index = 0;
            foreach (var databaseFragment in fragments)
            {
                var task = new CloudTask(index.ToString(), GetTaskCommandLine(databaseName, databaseFragment.Filename));
                task.DisplayName = databaseFragment.Filename;
                task.UserIdentity = new UserIdentity(new AutoUserSpecification(elevationLevel: ElevationLevel.Admin));
                task.Constraints = new TaskConstraints(null, null, 2);
                index++;
                yield return task;
            }
        }

        private string GetTaskCommandLine(string databaseName, string filename)
        {
            return string.Format("/bin/bash -c '$AZ_BATCH_JOB_PREP_WORKING_DIR/import-database.sh {0} {1} {2} {3} {4}'",
                databaseName,
                _storageCredentials.Account,
                _storageCredentials.Key,
                SystemDatabaseProvider.DefaultContainerName,
                filename);
        }

        private void CleanupAfterFailure(string jobId, DatabaseEntity entity)
        {
            try
            {
                _tableStorageProvider.DeleteEntity(entity);
            }
            catch (Exception)
            {
            }
            try
            {
                _batchClient.JobOperations.DeleteJob(jobId);
            }
            catch (Exception)
            {
            }
        }
    }
}
