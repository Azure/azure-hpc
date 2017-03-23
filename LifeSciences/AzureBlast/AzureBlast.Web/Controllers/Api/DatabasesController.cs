// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Microsoft.Azure.Batch.Blast.Configuration;
using Microsoft.Azure.Batch.Blast.Databases;
using Microsoft.Azure.Batch.Blast.Databases.Imports;
using Microsoft.Azure.Batch.Blast.Searches;
using Microsoft.Azure.Batch.Blast.Storage.Entities;
using Microsoft.Azure.Blast.Web.Models;

namespace Microsoft.Azure.Blast.Web.Controllers.Api
{
    [RoutePrefix("api/databases")]
    public class DatabasesController : BaseApiController
    {
        private readonly IDatabaseProvider _databaseProvider;
        private readonly IDatabaseImportManager _databaseImportManager;

        public DatabasesController(IDatabaseProvider databaseProvider, IDatabaseImportManager databaseImportManager)
        {
            _databaseProvider = databaseProvider;
            _databaseImportManager = databaseImportManager;
        }

        [HttpGet]
        public IEnumerable<DatabaseEntity> Get()
        {
            return _databaseProvider.ListDatabases();
        }

        [Route("{databaseName}"), HttpDelete]
        public void Delete(string databaseName)
        {
            _databaseProvider.DeleteDatabase(databaseName);
        }


        [HttpPost]
        public HttpResponseMessage Add(AddDatabaseSpec spec)
        {
            if (spec.Name == null)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Database name cannot be null");
            }

            if (spec.Name.Contains(" "))
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Database name cannot contain spaces");
            }

            if (spec.ContainerName == null)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Container name cannot be null");
            }

            if (spec.ContainerName.Contains(" "))
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Container name cannot contains invalid characters");
            }

            try
            {
                _databaseImportManager.ImportExisting(spec.Name, spec.Description, spec.ContainerName);
                return Request.CreateResponse(HttpStatusCode.OK, spec.Name);
            }
            catch (Exception e)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, e.ToString());
            }
        }
    }
}
