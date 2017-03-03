// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Batch.Blast.Databases.ExternalSources;
using Microsoft.Azure.Batch.Blast.Storage;

namespace Microsoft.Azure.Batch.Blast.Databases
{
    public class ExternalRepositoryManager : IExternalRepositoryManager
    {
        private readonly ITableStorageProvider _tableStorageProvider;

        public ExternalRepositoryManager(Microsoft.Azure.Batch.Blast.Configuration.BlastConfiguration configuration)
        {
            _tableStorageProvider = configuration.TableStorageProvider;
        }

        public IEnumerable<ExternalRepository> ListRepositories()
        {
            return _tableStorageProvider.ListEntities<ExternalRepository>(ExternalRepository.DefaultPk).Select(HydrateEntity);
        }

        public ExternalRepository GetRepository(string repoId)
        {
            return HydrateEntity(_tableStorageProvider.GetEntity<ExternalRepository>(ExternalRepository.DefaultPk, repoId));
        }

        public void AddRepository(ExternalRepository repository)
        {
            _tableStorageProvider.UpsertEntity(repository);
        }

        public void DeleteRepository(string repoId)
        {
            var entity =
                _tableStorageProvider.GetEntity<ExternalRepository>(ExternalRepository.DefaultPk, repoId);

            if (entity == null)
            {
                throw new Exception("Repository not found: " + repoId);
            }

            if (entity.Readonly)
            {
                throw new Exception("Repository is readonly: " + repoId);
            }

            _tableStorageProvider.DeleteEntity(entity);
        }

        public static ExternalRepository GetNCBIRepository()
        {
            return new ExternalRepository("ncbi", "NCBI", new Uri("ftp://ftp.ncbi.nlm.nih.gov/blast/db"), RepositoryType.Ftp, true);
        }

        private ExternalRepository HydrateEntity(ExternalRepository externalRepository)
        {
            if (externalRepository == null)
            {
                return null;
            }

            switch (externalRepository.Type)
            {
                case RepositoryType.Ftp: externalRepository.DatabaseSource = new FtpDatabaseProvider(_tableStorageProvider, externalRepository.Uri);
                    break;
                default:
                    throw new NotSupportedException("Unknown repository type");
            }

            return externalRepository;
        }
    }
}
