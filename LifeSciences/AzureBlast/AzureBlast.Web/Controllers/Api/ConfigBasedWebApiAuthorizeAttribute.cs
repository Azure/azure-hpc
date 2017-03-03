// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Web.Http;
using System.Web.Http.Controllers;

namespace Microsoft.Azure.Blast.Web.Controllers.Api
{
    public class ConfigBasedWebApiAuthorizeAttribute : AuthorizeAttribute
    {
        protected override bool IsAuthorized(HttpActionContext httpContext)
        {
            if (ConfigBasedAuthorization.RequireAuthorization())
            {
                return base.IsAuthorized(httpContext);
            }
            return true;
        }
    }
}