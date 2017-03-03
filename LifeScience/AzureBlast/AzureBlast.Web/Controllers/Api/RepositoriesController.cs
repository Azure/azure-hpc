// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using Microsoft.Azure.Batch.Blast.Databases;
using Microsoft.Azure.Batch.Blast.Databases.ExternalSources;
using Microsoft.Azure.Batch.Blast.Databases.Imports;

namespace Microsoft.Azure.Blast.Web.Controllers.Api
{
    [RoutePrefix("api/repositories")]
    public class RepositoriesController : BaseApiController
    {
        private readonly IExternalRepositoryManager _externalRepositoryManager;
        private readonly IDatabaseImportManager _databaseImportManager;

        public RepositoriesController(IExternalRepositoryManager externalRepositoryManager, IDatabaseImportManager databaseImportManager)
        {
            _externalRepositoryManager = externalRepositoryManager;
            _databaseImportManager = databaseImportManager;
        }

        [HttpGet]
        public IEnumerable<ExternalRepository> Get()
        {
            return _externalRepositoryManager.ListRepositories();
        }

        [Route("{repositoryId}/databases"), HttpGet]
        public IEnumerable<ExternalDatabase> Get(string repositoryId)
        {
            var databaseRespository = _externalRepositoryManager.GetRepository(repositoryId);

            if (databaseRespository == null)
            {
                return Enumerable.Empty<ExternalDatabase>();
            }

            return databaseRespository.DatabaseSource.ListDatabases().ToList();
        }

        [Route("{repositoryId}/databases/{databaseId}/import"), HttpPost]
        public void Post(string repositoryId, string databaseId)
        {
            var databaseRespository = _externalRepositoryManager.GetRepository(repositoryId);
            if (databaseRespository == null)
            {
                throw new Exception("No such repository");
            }

            var database = databaseRespository.DatabaseSource.GetDatabase(databaseId);
            if (database == null)
            {
                throw new Exception("No such database");
            }

            _databaseImportManager.SubmitImport(databaseRespository, database);
        }
    }
}
