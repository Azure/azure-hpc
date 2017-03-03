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
        private readonly BlobBackedDatabaseProvider _blobBackedDatabaseProvider;

        public SystemDatabaseProvider(Microsoft.Azure.Batch.Blast.Configuration.BlastConfiguration configuration, string containerName)
        {
            _tableStorageProvider = configuration.TableStorageProvider;
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
            return _blobBackedDatabaseProvider.GetDatabaseFragments(databaseName);
        }

        public void DeleteDatabase(string databaseName)
        {
            try
            {
                _blobBackedDatabaseProvider.DeleteDatabase(databaseName);
            }
            catch (Exception)
            {
            }

            var entity = _tableStorageProvider.GetEntity<DatabaseEntity>(DatabaseEntity.DefaultRepository, databaseName);

            if (entity != null)
            {
                _tableStorageProvider.DeleteEntity(entity);
            }
        }

        public string ContainerName { get { return DefaultContainerName; } }
    }
}
