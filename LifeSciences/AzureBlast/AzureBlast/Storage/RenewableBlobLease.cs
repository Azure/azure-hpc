// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.RetryPolicies;

namespace Microsoft.Azure.Batch.Blast.Storage
{
    public class RenewableBlobLease
    {
        private readonly CloudBlobClient _cloudBlobClient;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public RenewableBlobLease(CloudBlobClient cloudBlobClient)
        {
            _cloudBlobClient = cloudBlobClient;
            _cloudBlobClient.DefaultRequestOptions.RetryPolicy = new LinearRetry(TimeSpan.FromSeconds(1), 15);
        }

        public BlobLease AcquireLease(string containerName, string blobName)
        {
            var cts = new CancellationTokenSource();
            var container = _cloudBlobClient.GetContainerReference(containerName);
            container.CreateIfNotExists();
            var blob = container.GetBlockBlobReference(blobName);
            if (!blob.Exists())
            {
                blob.UploadText("");
            }

            var leaseId = blob.AcquireLease(TimeSpan.FromSeconds(60), null);

            // Success
            Task.Run(() => RenewLeaseTask(blob, leaseId, cts), _cancellationTokenSource.Token);

            return new BlobLease(cts);
        }

        private void RenewLeaseTask(CloudBlockBlob blob, string leaseId, CancellationTokenSource cts)
        {
            AccessCondition acc = new AccessCondition();
            acc.LeaseId = leaseId;

            while (true)
            {
                Thread.Sleep(TimeSpan.FromSeconds(15));

                try
                {
                    // Exit if disposing
                    _cancellationTokenSource.Token.ThrowIfCancellationRequested();

                    // Exit if client disposing of lease
                    cts.Token.ThrowIfCancellationRequested();

                    blob.RenewLease(acc);
                }
                catch (OperationCanceledException)
                {
                    // Tell client lease is gone
                    cts.Cancel();
                    blob.ReleaseLease(acc);
                    break;
                }
                catch (Exception)
                {
                    // Tell client lease is gone
                    cts.Cancel();
                    blob.ReleaseLease(acc);
                    break;
                }
            }
        }

        public void Dispose()
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel(false);
            }
        }
    }

    public class BlobLease : IDisposable
    {
        private readonly CancellationTokenSource _ct;

        public BlobLease(CancellationTokenSource ct)
        {
            _ct = ct;
        }

        public void Dispose()
        {
            if (_ct != null && !_ct.IsCancellationRequested)
            {
                _ct.Cancel(false);
            }
        }

        public void ThrowIfLeaseLost()
        {
            _ct.Token.ThrowIfCancellationRequested();
        }
    }
}
