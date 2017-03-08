// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Web.Mvc;
using Microsoft.Azure.Batch.Blast.Configuration;
using Microsoft.Azure.Batch.Blast.Searches;
using Microsoft.Azure.Blast.Web.Models;

namespace Microsoft.Azure.Blast.Web.Controllers
{
    public class SearchesController : AuthorizedController
    {
        private readonly BlastConfiguration _configuration;
        private readonly ISearchProvider _searchProvider;

        public SearchesController(BlastConfiguration configuration, ISearchProvider searchProvider)
        {
            _configuration = configuration;
            _searchProvider = searchProvider;
        }

        [Route("searches")]
        public ActionResult Index()
        {
            return View();
        }

        [Route("searches/new")]
        public ActionResult New()
        {
            var model = new NewSearchModel
            {
                BlastExecutables = new List<string>(new string[] {"blastp", "blastn", "blastx", "tblastn", "tblastx"}),
                VirtualMachineSizes = _configuration.GetVirtualMachineSizes(),
            };
            return View(model);
        }

        [Route("searches/show/{searchId}")]
        public ActionResult Show(string searchId)
        {
            Guid id;
            if (!Guid.TryParse(searchId, out id))
            {
                return HttpNotFound();
            }

            var search = _searchProvider.GetSearch(id);
            if (search == null)
            {
                return HttpNotFound();
            }

            return View(search);
        }

        [Route("searches/show/{searchId}/{queryId}/visualize/{resultXmlFile}")]
        public ActionResult Visualize(string searchId, string queryId, string resultXmlFile)
        {
            Guid parsedSearchId;
            if (!Guid.TryParse(searchId, out parsedSearchId))
            {
                return HttpNotFound(string.Format("Unable to parse searchId: {0}", searchId));
            }

            int parsedQueryId;
            if (!Int32.TryParse(queryId, out parsedQueryId))
            {
                return HttpNotFound(string.Format("Unable to parse queryId: {0}", queryId));
            }

            var search = _searchProvider.GetSearch(parsedSearchId);
            if (search == null)
            {
                return HttpNotFound();
            }

            return View(new VisualizeResultsModel
            {
                Id = searchId,
                QueryId = parsedQueryId,
                SearchName = search.Name,
                Filename = resultXmlFile
            });
        }
    }
}