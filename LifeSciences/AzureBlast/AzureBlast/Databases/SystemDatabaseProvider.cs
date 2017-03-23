// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Batch.Blast.Storage;
using Microsoft.Azure.Batch.Blast.Storage.Entities;

namespace Microsoft.Azure.Batch.Blast.Databases
{
    public class SystemDatabaseProvider : IDatabaseProvider
    {
        public const string DefaultContainerName = "blast-databases";

        private readonly ITableStorageProvider _tableStorageProvider;
        private readonly IBlobStorageProvider _blobStorageProvider;
        private readonly BlobBackedDatabaseProvider _blobBackedDatabaseProvider;

        public SystemDatabaseProvider(Microsoft.Azure.Batch.Blast.Configuration.BlastConfiguration configuration, string containerName)
        {
            _tableStorageProvider = configuration.TableStorageProvider;
            _blobStorageProvider = configuration.BlobStorageProvider;
            _blobBackedDatabaseProvider = new BlobBackedDatabaseProvider(configuration.BlobStorageProvider, containerName);
        }

        public IReadOnlyList<DatabaseEntity> ListDatabases()
        {
            var databaseEntities = _tableStorageProvider.ListEntities<DatabaseEntity>();
            return databaseEntities.ToList();
        }

        public DatabaseEntity GetDatabase(string databaseName)
        {
            return _tableStorageProvider.GetEntity<DatabaseEntity>(DatabaseEntity.DefaultRepository, databaseName);
        }

        public IReadOnlyList<DatabaseFragment> GetDatabaseFragments(string databaseName)
        {
            var db = GetDatabase(databaseName);

            if (db == null)
            {
                throw new Exception("No such database " + databaseName);
            }

            var databaseProvider = GetDatabaseProvider(db);
            return databaseProvider.GetDatabaseFragments(databaseName);
        }

        public void DeleteDatabase(string databaseName)
        {
            var db = GetDatabase(databaseName);

            if (db != null)
            {
                try
                {
                    var databaseProvider = GetDatabaseProvider(db);
                    databaseProvider.DeleteDatabase(databaseName);
                }
                catch (Exception)
                {
                    Console.WriteLine("Error deleteing database " + databaseName);
                }

                _tableStorageProvider.DeleteEntity(db);
            }
        }

        private IDatabaseProvider GetDatabaseProvider(DatabaseEntity db)
        {
            if (db != null && db.DedicatedContainer)
            {
                return new BlobBackedDatabaseProvider(_blobStorageProvider, db.ContainerName, true);
            }
            return _blobBackedDatabaseProvider;
        }

        public string ContainerName { get { return DefaultContainerName; } }
    }
}
