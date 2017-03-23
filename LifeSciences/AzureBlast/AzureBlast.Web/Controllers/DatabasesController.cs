// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Web.Mvc;

namespace Microsoft.Azure.Blast.Web.Controllers
{
    public class DatabasesController : AuthorizedController
    {
        [Route("databases")]
        public ActionResult Index()
        {
            return View();
        }

        [Route("databases/new")]
        public ActionResult New()
        {
            return View();
        }
    }
}
