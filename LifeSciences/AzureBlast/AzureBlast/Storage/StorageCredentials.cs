// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Batch.Blast.Storage
{
    public class StorageCredentials
    {
        public StorageCredentials(string account, string key)
        {
            Account = account;
            Key = key;
        }

        public StorageCredentials()
        {

        }

        public string Account { get; set; }
        public string Key { get; set; }
    }
}
