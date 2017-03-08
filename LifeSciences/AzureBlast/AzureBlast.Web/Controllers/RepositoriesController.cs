// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Linq;
using System.Web.Mvc;
using Microsoft.Azure.Batch.Blast.Databases;
using Microsoft.Azure.Blast.Web.Models;

namespace Microsoft.Azure.Blast.Web.Controllers
{
    public class RepositoriesController : AuthorizedController
    {
        private readonly IExternalRepositoryManager _externalRepositoryManager;

        public RepositoriesController(IExternalRepositoryManager externalRepositoryManager)
        {
            _externalRepositoryManager = externalRepositoryManager;
        }

        [Route("repositories")]
        public ActionResult Index(string id)
        {
            var repos = _externalRepositoryManager.ListRepositories().ToList();
            var repoModel = new RespositoryViewModel
            {
                SelectedRepoId = string.IsNullOrEmpty(id) ? "ncbi" : id,
                Repositories = repos,
            };
            return View(repoModel);
        }
    }
}
