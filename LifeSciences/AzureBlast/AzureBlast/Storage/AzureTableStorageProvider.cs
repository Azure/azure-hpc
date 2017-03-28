// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Microsoft.WindowsAzure.Storage.Table;

namespace Microsoft.Azure.Batch.Blast.Storage
{
    public class AzureTableStorageProvider : ITableStorageProvider
    {
        private readonly CloudTableClient _cloudTableClient;

        public AzureTableStorageProvider(CloudStorageAccount storageAccount)
        {
            _cloudTableClient = storageAccount.CreateCloudTableClient();
            _cloudTableClient.DefaultRequestOptions.RetryPolicy = new LinearRetry(TimeSpan.FromSeconds(1), 15);
        }

        public void InsertEntity<T>(T entity) where T : TableEntity
        {
            CloudTable table = _cloudTableClient.GetTableReference(entity.GetType().Name);
            table.CreateIfNotExists();
            TableOperation insertOperation = TableOperation.Insert(entity);
            table.Execute(insertOperation);
        }

        public void InsertEntities<T>(IEnumerable<T> entities) where T : TableEntity
        {
            CloudTable table = _cloudTableClient.GetTableReference(typeof(T).Name);
            table.CreateIfNotExists();

            TableBatchOperation batchInsert = new TableBatchOperation();

            foreach (var entity in entities)
            {
                if (batchInsert.Count == 99)
                {
                    table.ExecuteBatch(batchInsert);
                    batchInsert = new TableBatchOperation();
                }
                batchInsert.Insert(entity);
            }

            if (batchInsert.Count > 0)
            {
                table.ExecuteBatch(batchInsert);
            }
        }

        public void UpdateEntity<T>(T entity) where T : TableEntity
        {
            CloudTable table = _cloudTableClient.GetTableReference(entity.GetType().Name);
            TableOperation insertOperation = TableOperation.Replace(entity);
            table.Execute(insertOperation);
        }

        public void UpsertEntity<T>(T entity) where T : TableEntity
        {
            CloudTable table = _cloudTableClient.GetTableReference(entity.GetType().Name);
            table.CreateIfNotExists();
            TableOperation insertOperation = TableOperation.InsertOrReplace(entity);
            table.Execute(insertOperation);
        }

        public void DeleteEntity<T>(T entity) where T : TableEntity
        {
            CloudTable table = _cloudTableClient.GetTableReference(entity.GetType().Name);
            if (table.Exists())
            {
                TableOperation deleteOperation = TableOperation.Delete(entity);
                table.Execute(deleteOperation);
            }
        }

        public T GetEntity<T>(string partitionKey, string rowKey) where T : TableEntity, new()
        {
            CloudTable table = _cloudTableClient.GetTableReference(typeof(T).Name);
            TableOperation retrieveOperation = TableOperation.Retrieve<T>(partitionKey, rowKey);
            TableResult retrievedResult = table.Execute(retrieveOperation);
            return (T) retrievedResult.Result;
        }

        public IEnumerable<T> ListEntities<T>(string partitionKey = null) where T : TableEntity, new()
        {
            CloudTable table = _cloudTableClient.GetTableReference(typeof(T).Name);
            table.CreateIfNotExists();

            TableQuery<T> query;
            if (partitionKey != null)
            {
                query =
                    new TableQuery<T>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal,
                        partitionKey));
            }
            else
            {
                query = new TableQuery<T>();
            }

            return table.ExecuteQuery(query);
        }
    }
}
