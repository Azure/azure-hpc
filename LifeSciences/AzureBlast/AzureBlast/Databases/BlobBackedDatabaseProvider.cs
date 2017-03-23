// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Batch.Blast.Storage;
using Microsoft.Azure.Batch.Blast.Storage.Entities;

namespace Microsoft.Azure.Batch.Blast.Databases
{
    public class BlobBackedDatabaseProvider : IDatabaseProvider
    {
        private readonly IBlobStorageProvider _blobStorageProvider;
        private readonly string _databaseContainerName;
        private readonly bool _dedicatedContainer;

        public BlobBackedDatabaseProvider(IBlobStorageProvider blobStorageProvider, string databaseContainerName, bool dedicatedContainer = false)
        {
            if (blobStorageProvider == null)
            {
                throw new ArgumentNullException("blobStorageProvider");
            }
            if (databaseContainerName == null)
            {
                throw new ArgumentNullException("databaseContainerName");
            }
            _blobStorageProvider = blobStorageProvider;
            _databaseContainerName = databaseContainerName;
            _dedicatedContainer = dedicatedContainer;
        }

        public IReadOnlyList<DatabaseEntity> ListDatabases()
        {
            var databases = new List<DatabaseEntity>();
            var blobs = _blobStorageProvider.ListBlobs(_databaseContainerName).ToList();
            var databaseNames = new HashSet<string>(GetDatabaseNamesFromBlobs(blobs));
            foreach (var databaseName in databaseNames)
            {
                var databaseBlobs =
                    blobs.Where(blob => blob.BlobName.StartsWith(databaseName)).ToList();
                databases.Add(GetDatabase(databaseName, databaseBlobs));
            }
            return databases.AsReadOnly();
        }

        private IEnumerable<string> GetDatabaseNamesFromBlobs(IEnumerable<Blob> blobs)
        {
            foreach (var blob in blobs)
            {
                if (blob.BlobName.Contains("."))
                {
                    yield return blob.BlobName.Substring(0, blob.BlobName.IndexOf("."));
                }
            }
        }

        public DatabaseEntity GetDatabase(string databaseName)
        {
            if (databaseName == null)
            {
                throw new ArgumentNullException("databaseName");
            }
            var databaseBlobs =
                _blobStorageProvider.ListBlobs(_databaseContainerName, databaseName + ".").ToList();
            return GetDatabase(databaseName, databaseBlobs);
        }

        public IReadOnlyList<DatabaseFragment> GetDatabaseFragments(string databaseName)
        {
            string prefix = _dedicatedContainer ? null : databaseName + ".";

            var databaseBlobs =
                _blobStorageProvider.ListBlobs(_databaseContainerName, prefix).ToList();

            return databaseBlobs.Select(
                    blob =>
                        new DatabaseFragment(blob.BlobName, blob.Length)).ToList();
        }

        public void DeleteDatabase(string databaseName)
        {
            var fragments = GetDatabaseFragments(databaseName);
            Parallel.ForEach(fragments, fragment =>
            {
                _blobStorageProvider.DeleteBlob(ContainerName, fragment.Filename);
            });
        }

        private DatabaseEntity GetDatabase(string databaseName, IEnumerable<Blob> databaseBlobs)
        {
            var aliasBlobName = GetDatabaseAliasBlobs(databaseBlobs).FirstOrDefault();
            if (aliasBlobName != null)
            {
                var content = _blobStorageProvider.GetBlobAsText(_databaseContainerName, aliasBlobName);
                if (!string.IsNullOrEmpty(content))
                {
                    var alias = DatabaseAlias.FromContent(content);
                    if (!string.IsNullOrEmpty(alias.Title) && alias.Length > 0 && alias.DatabaseList != null)
                    {
                        return GetDatabaseFromAliasBlob(databaseName, alias, aliasBlobName, databaseBlobs);
                    }
                }
            }

            // Create a db fragment for each file in the segment, e.g. nt.00.nhd, nt.00.nhi,
            // nt.00.nhr, etc.
            var segmentFragments =
                databaseBlobs.Select(
                    blob =>
                        new DatabaseFragment(blob.BlobName, blob.Length));

            var fragments = new List<DatabaseFragment>(segmentFragments);

            if (fragments.Any())
            {
                var type = Path.GetExtension(databaseBlobs.First().BlobName).StartsWith("n")
                    ? DatabaseType.Nucleotide
                    : DatabaseType.Protein;
                var length =
                    fragments.Where(frag => !frag.Filename.EndsWith(".nal") && !frag.Filename.EndsWith(".pal"))
                        .Sum(frag => frag.Size);

                return new DatabaseEntity(databaseName, ContainerName, fragments.Count, length, DatabaseState.Ready, type);
            }

            return null;
        }

        private IEnumerable<string> GetDatabaseAliasBlobs(IEnumerable<Blob> blobs)
        {
            return blobs.Where(blob => blob.BlobName.EndsWith(".nal") || blob.BlobName.EndsWith(".pal")).Select(blob => blob.BlobName);
        }

        private DatabaseEntity GetDatabaseFromAliasBlob(string databaseName, DatabaseAlias dbalias, string aliasBlobName, IEnumerable<Blob> allBlobs)
        {
            var fragments = new List<DatabaseFragment>();

            foreach (var dbSegment in dbalias.DatabaseList)
            {
                // Get all blobs prefixed with this db segment name, e.g. nt.00*
                var segmentBlobs = allBlobs.Where(blob => blob.BlobName.StartsWith(dbSegment));

                // Create a db fragment for each file in the segment, e.g. nt.00.nhd, nt.00.nhi,
                // nt.00.nhr, etc.
                var segmentFragments =
                    segmentBlobs.Select(
                        blob =>
                            new DatabaseFragment(blob.BlobName, blob.Length));

                fragments.AddRange(segmentFragments);
            }

            var aliasBlob = allBlobs.First(blob => blob.BlobName == aliasBlobName);

            fragments.Add(new DatabaseFragment(aliasBlob.BlobName, aliasBlob.Length));

            var length =
                fragments.Where(frag => !frag.Filename.EndsWith(".nal") && !frag.Filename.EndsWith(".pal"))
                    .Sum(frag => frag.Size);

            return new DatabaseEntity(
                databaseName,
                ContainerName,
                fragments.Count,
                length,
                DatabaseState.Ready,
                aliasBlobName.EndsWith(".nal") ? DatabaseType.Nucleotide : DatabaseType.Protein);
        }

        public string ContainerName { get { return _databaseContainerName; } }
    }
}
