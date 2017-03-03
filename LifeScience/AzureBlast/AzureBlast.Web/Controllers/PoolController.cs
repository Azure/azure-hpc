// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Web.Mvc;

namespace Microsoft.Azure.Blast.Web.Controllers
{
    public class PoolController : AuthorizedController
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult New()
        {
            return View();
        }
    }
}