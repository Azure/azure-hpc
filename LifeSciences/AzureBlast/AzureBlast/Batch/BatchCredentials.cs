// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Batch.Blast.Batch
{
    public class BatchCredentials
    {
        public BatchCredentials(string url, string account, string key)
        {
            Url = url;
            Account = account;
            Key = key;
        }

        public BatchCredentials()
        {
        }

        public string Url { get; set; }

        public string Account { get; set; }

        public string Key { get; set; }
    }
}
