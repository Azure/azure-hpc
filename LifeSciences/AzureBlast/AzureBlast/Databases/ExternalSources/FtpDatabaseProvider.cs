// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Batch.Blast.Storage;
using Microsoft.Azure.Batch.Blast.Storage.Entities;

namespace Microsoft.Azure.Batch.Blast.Databases.ExternalSources
{
    public class FtpDatabaseProvider : IExternalDatabaseSource
    {
        private readonly ITableStorageProvider _tableStorageProvider;
        private readonly string _uri;
        private readonly List<string> _fileExtensions = new List<string> { "tar.gz" };

        public FtpDatabaseProvider(ITableStorageProvider tableStorageProvider, Uri uri)
        {
            _tableStorageProvider = tableStorageProvider;
            // We must ensure there's a trailing '/', otherwise we get the current dir in the results.
            _uri = uri.ToString().EndsWith("/") ? uri.ToString() : uri + "/";
        }

        public IReadOnlyList<ExternalDatabase> ListDatabases()
        {
            var listFtpDatabasesTask = Task.Run(() => ListFtpDatabases(), CancellationToken.None);
            var listSystemDatabasesTask = Task.Run(() => ListSystemDatabases(), CancellationToken.None);

            var ftpDatabases = listFtpDatabasesTask.Result;
            var systemDatabaseEntities = listSystemDatabasesTask.Result;
            var importingDatabases =
                systemDatabaseEntities.Where(entity => entity.ImportInProgress).ToList();

            foreach (var ftpDatabase in ftpDatabases)
            {
                var importingEntity = importingDatabases.FirstOrDefault(entity => entity.Name == ftpDatabase.Name);
                if (importingEntity != null)
                {
                    ftpDatabase.ImportInProgress = true;
                }
            }

            return ftpDatabases;
        }

        public ExternalDatabase GetDatabase(string databaseName)
        {
            return ListFtpDatabases().FirstOrDefault(db => db.Name == databaseName);
        }

        private IReadOnlyList<ExternalDatabase> ListFtpDatabases()
        {
            var allSegments = ListDatabaseFiles().Where(
                filetuple => IsDatabaseFile(filetuple.Item1)).ToList();

            var databaseNames = new HashSet<string>(
                allSegments.Select(
                    segmentTuple => segmentTuple.Item1.Substring(0, segmentTuple.Item1.IndexOf(".", StringComparison.Ordinal))));

            var databases = new List<ExternalDatabase>();

            foreach (var databaseName in databaseNames)
            {
                var databaseSegmentTuples = allSegments.Where(
                    segmentTuple => segmentTuple.Item1.StartsWith(databaseName + ".")).ToList();

                var fragments = databaseSegmentTuples.Select(segmentTuple => new DatabaseFragment(
                    segmentTuple.Item1, // filename
                    segmentTuple.Item2 // size
                    )).ToList();

                var totalCompressedSize = fragments.Sum(fragment => fragment.Size);

                databases.Add(new ExternalDatabase(
                    databaseName, totalCompressedSize, fragments.Count));
            }

            return databases.OrderBy(db => db.Name).ToList();
        }

        private IReadOnlyList<DatabaseEntity> ListSystemDatabases()
        {
            return _tableStorageProvider.ListEntities<DatabaseEntity>().ToList();
        }

        private IEnumerable<Tuple<string, long>> ListDatabaseFiles()
        {
            FtpWebRequest ftpRequest = (FtpWebRequest)WebRequest.Create(_uri);
            ftpRequest.Credentials = new NetworkCredential("anonymous", "janedoe@contoso.com");
            ftpRequest.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
            FtpWebResponse response = (FtpWebResponse)ftpRequest.GetResponse();

            // Enumerable of filename, size in bytes
            var databaseFiles = new List<Tuple<string, long>>();

            using (var streamReader = new StreamReader(response.GetResponseStream()))
            {
                string line = streamReader.ReadLine();
                while (!string.IsNullOrEmpty(line))
                {
                    var tokens = line.Split(new [] {' '}, 9, StringSplitOptions.RemoveEmptyEntries);
                    if (tokens.Length != 9)
                    {
                        continue;
                    }

                    long sizeInBytes;
                    if (!Int64.TryParse(tokens[4], out sizeInBytes))
                    {
                        // Something bad happened
                        continue;
                    }

                    var filename = tokens[8];
                    databaseFiles.Add(Tuple.Create(filename, sizeInBytes));
                    line = streamReader.ReadLine();
                }
            }

            return databaseFiles;
        }

        private bool IsDatabaseFile(string file)
        {
            if (!string.IsNullOrEmpty(file))
            {
                foreach (var fileExtension in _fileExtensions)
                {
                    if (file.ToLower().EndsWith(fileExtension))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public IReadOnlyList<DatabaseFragment> GetDatabaseFragments(string databaseName)
        {
            var allSegments = ListDatabaseFiles().Where(
                filetuple => IsDatabaseFile(filetuple.Item1)).ToList();

            var databaseSegmentTuples = allSegments.Where(
                segmentTuple => segmentTuple.Item1.StartsWith(databaseName + ".")).ToList();

            return databaseSegmentTuples.Select(segmentTuple => new DatabaseFragment(
                segmentTuple.Item1, // filename
                segmentTuple.Item2 // size
                )).ToList();
        }
    }
}
