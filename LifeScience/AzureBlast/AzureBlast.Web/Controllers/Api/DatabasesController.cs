// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Web.Http;
using Microsoft.Azure.Batch.Blast.Databases;
using Microsoft.Azure.Batch.Blast.Storage.Entities;

namespace Microsoft.Azure.Blast.Web.Controllers.Api
{
    [RoutePrefix("api/databases")]
    public class DatabasesController : BaseApiController
    {
        private readonly IDatabaseProvider _databaseProvider;

        public DatabasesController(IDatabaseProvider databaseProvider)
        {
            _databaseProvider = databaseProvider;
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
    }
}
