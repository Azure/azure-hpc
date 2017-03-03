// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Web;
using System.Web.Mvc;

namespace Microsoft.Azure.Blast.Web.Controllers
{
    public class ConfigBasedMvcAuthorizeAttribute : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (ConfigBasedAuthorization.RequireAuthorization())
            {
                return base.AuthorizeCore(httpContext);
            }
            return true;
        }
    }
}