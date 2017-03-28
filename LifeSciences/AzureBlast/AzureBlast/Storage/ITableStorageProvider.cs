// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage.Table;

namespace Microsoft.Azure.Batch.Blast.Storage
{
    public interface ITableStorageProvider
    {
        void InsertEntity<T>(T entity) where T : TableEntity;

        void InsertEntities<T>(IEnumerable<T> entities) where T : TableEntity;

        void UpdateEntity<T>(T entity) where T : TableEntity;

        void UpsertEntity<T>(T entity) where T : TableEntity;

        void DeleteEntity<T>(T entity) where T : TableEntity;

        T GetEntity<T>(string partitionKey, string rowKey) where T : TableEntity, new();

        IEnumerable<T> ListEntities<T>(string partitionKey = null) where T : TableEntity, new();
    }
}
