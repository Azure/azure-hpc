// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Batch.Blast.Storage
{
    public class Blob
    {
        public Blob(string blobName, long length)
        {
            BlobName = blobName;
            Length = length;
        }

        public string BlobName { get; private set; }

        public long Length { get; private set; }
    }
}
