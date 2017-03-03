// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.WindowsAzure.Storage.Table;

namespace Microsoft.Azure.Batch.Blast.Configuration
{
    public class ConfigurationEntity : TableEntity
    {
        public ConfigurationEntity(string key, string value)
        {
            PartitionKey = key;
            RowKey = value;
        }

        public ConfigurationEntity()
        {
        }

        public string Key {  get { return PartitionKey; } }
        public string Value {  get { return RowKey; } }
    }
}
