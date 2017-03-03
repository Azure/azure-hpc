// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Batch.Blast.Storage.Entities;
using Microsoft.WindowsAzure.Storage.Table;

namespace Microsoft.Azure.Batch.Blast.Databases.ExternalSources
{
    public class ExternalDatabase
    {
        public ExternalDatabase(string name, long size, int fileCount)
        {
            Name = name;
            Size = size;
            FileCount = fileCount;
        }

        public string Name { get; set; }

        public long Size { get; set; }

        public int FileCount { get; set; }

        public bool ImportInProgress { get; set; }

        [IgnoreProperty]
        public string FriendlySize
        {
            get { return DatabaseEntity.GetFriendlySize(Size); }
        }
    }
}
