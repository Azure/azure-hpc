// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Web.Mvc;
using Microsoft.Azure.Batch.Blast.Configuration;
using Microsoft.Azure.Blast.Web.Models;

namespace Microsoft.Azure.Blast.Web.Controllers
{
    public class PoolController : AuthorizedController
    {
        private readonly BlastConfiguration _configuration;

        public PoolController(BlastConfiguration configuration)
        {
            _configuration = configuration;
        }

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult New()
        {
            var model = new NewPoolModel
            {
                VirtualMachineSizes = _configuration.GetVirtualMachineSizes(),
            };
            return View(model);
        }
    }
}