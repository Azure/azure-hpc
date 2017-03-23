// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.RetryPolicies;

namespace Microsoft.Azure.Batch.Blast.Storage
{
    public class AzureBlobStorageProvider : IBlobStorageProvider
    {
        private readonly CloudBlobClient _cloudBlobClient;

        public AzureBlobStorageProvider(CloudStorageAccount storageAccount)
        {
            _cloudBlobClient = storageAccount.CreateCloudBlobClient();
            _cloudBlobClient.DefaultRequestOptions.RetryPolicy = new LinearRetry(TimeSpan.FromSeconds(1), 15);
        }

        public IEnumerable<string> ListContainers()
        {
            try
            {
                return _cloudBlobClient.ListContainers().Select(c => c.Name);
            }
            catch (Exception e)
            {
                throw HandleException(e);
            }
        }

        public void DeleteContainer(string containerName)
        {
            try
            {
                var containerRef = _cloudBlobClient.GetContainerReference(containerName);

                if (containerRef.Exists())
                {
                    containerRef.Delete();
                }
            }
            catch (Exception e)
            {
                throw HandleException(e);
            }
        }

        public void DeleteBlob(string containerName, string blobName)
        {
            var containerRef = _cloudBlobClient.GetContainerReference(containerName);
            if (containerRef.Exists())
            {
                var blobRef = containerRef.GetBlockBlobReference(blobName);
                blobRef.DeleteIfExists();
            }
        }

        public IEnumerable<Blob> ListBlobs(string containerName, string prefix = null)
        {
            if (string.IsNullOrEmpty(containerName))
            {
                throw new ArgumentNullException("containerName");
            }

            try
            {
                var containerRef = _cloudBlobClient.GetContainerReference(containerName);

                if (!containerRef.Exists())
                {
                    return Enumerable.Empty<Blob>();
                }

                var blobs = new List<Blob>();

                foreach (var item in containerRef.ListBlobs(prefix, true))
                {
                    var blockBlob = item as CloudBlockBlob;
                    if (blockBlob != null)
                    {
                        blobs.Add(new Blob(blockBlob.Name, blockBlob.Properties.Length));
                    }
                }

                return blobs;
            }
            catch (Exception e)
            {
                throw HandleException(e);
            }
        }

        public string GetBlobSAS(string containerName, string blobName)
        {
            if (string.IsNullOrEmpty(containerName))
            {
                throw new ArgumentNullException("containerName");
            }

            if (string.IsNullOrEmpty(blobName))
            {
                throw new ArgumentNullException("blobName");
            }

            var containerRef = _cloudBlobClient.GetContainerReference(containerName);
            var blobRef = containerRef.GetBlockBlobReference(blobName);

            var sharedAccessPolicy = new SharedAccessBlobPolicy
            {
                Permissions = SharedAccessBlobPermissions.Read,
                SharedAccessStartTime = DateTime.UtcNow.AddHours(-1),
                SharedAccessExpiryTime = DateTime.UtcNow.AddMonths(6),
            };

            return new Uri(blobRef.Uri, blobRef.GetSharedAccessSignature(sharedAccessPolicy)).AbsoluteUri;
        }

        public void UploadBlobFromText(string containerName, string blobName, string content)
        {
            if (string.IsNullOrEmpty(containerName))
            {
                throw new ArgumentNullException("containerName");
            }

            if (string.IsNullOrEmpty(blobName))
            {
                throw new ArgumentNullException("blobName");
            }

            var containerRef = _cloudBlobClient.GetContainerReference(containerName);

            containerRef.CreateIfNotExists();

            var blobRef = containerRef.GetBlockBlobReference(blobName);

            blobRef.UploadText(content);
        }

        public void UploadBlobFromStream(string containerName, string blobName, Stream stream)
        {
            if (string.IsNullOrEmpty(containerName))
            {
                throw new ArgumentNullException("containerName");
            }

            if (string.IsNullOrEmpty(blobName))
            {
                throw new ArgumentNullException("blobName");
            }

            var containerRef = _cloudBlobClient.GetContainerReference(containerName);

            containerRef.CreateIfNotExists();

            var blobRef = containerRef.GetBlockBlobReference(blobName);

            blobRef.UploadFromStream(stream);
        }

        public BlobLease AcquireBlobLease(string containerName, string blobName)
        {
            var lease = new RenewableBlobLease(_cloudBlobClient);
            return lease.AcquireLease(containerName, blobName);
        }

        public CloudBlobContainer GetContainer(string containerName)
        {
            if (string.IsNullOrEmpty(containerName))
            {
                throw new ArgumentNullException("containerName");
            }

            var containerRef = _cloudBlobClient.GetContainerReference(containerName);

            if (!containerRef.Exists())
            {
                return null;
            }

            return containerRef;
        }

        public Stream GetBlobAsStream(string containerName, string blobName)
        {
            if (string.IsNullOrEmpty(containerName))
            {
                throw new ArgumentNullException("containerName");
            }

            if (string.IsNullOrEmpty(blobName))
            {
                throw new ArgumentNullException("blobName");
            }

            var containerRef = _cloudBlobClient.GetContainerReference(containerName);

            if (!containerRef.Exists())
            {
                return null;
            }

            var blobRef = containerRef.GetBlockBlobReference(blobName);

            if (!blobRef.Exists())
            {
                return null;
            }

            return blobRef.OpenRead();
        }

        public string GetBlobAsText(string containerName, string blobName)
        {
            if (string.IsNullOrEmpty(containerName))
            {
                throw new ArgumentNullException("containerName");
            }

            if (string.IsNullOrEmpty(blobName))
            {
                throw new ArgumentNullException("blobName");
            }

            var containerRef = _cloudBlobClient.GetContainerReference(containerName);

            if (!containerRef.Exists())
            {
                return null;
            }

            var blobRef = containerRef.GetBlockBlobReference(blobName);

            if (!blobRef.Exists())
            {
                return null;
            }

            return blobRef.DownloadText();
        }

        private Exception HandleException(Exception e)
        {
            return e;
        }
    }
}
