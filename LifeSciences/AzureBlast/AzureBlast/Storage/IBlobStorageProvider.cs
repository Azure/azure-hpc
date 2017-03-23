// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Microsoft.Azure.Batch.Blast.Storage
{
    public interface IBlobStorageProvider
    {
        IEnumerable<string> ListContainers();

        void DeleteContainer(string containerName);

        void DeleteBlob(string containerName, string blobName);

        IEnumerable<Blob> ListBlobs(string container, string prefix = null);

        Stream GetBlobAsStream(string containerName, string blobName);

        string GetBlobAsText(string containerName, string blobName);

        string GetBlobSAS(string containerName, string blobName);

        void UploadBlobFromText(string containerName, string blobName, string content);

        void UploadBlobFromStream(string containerName, string blobName, Stream stream);

        BlobLease AcquireBlobLease(string containerName, string blobName);

        CloudBlobContainer GetContainer(string containerName);
    }
}
