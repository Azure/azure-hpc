// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Web.Http;
using System.Web.Mvc;

namespace Microsoft.Azure.Blast.Web.Controllers.Api
{
    [RequireHttps]
    [ConfigBasedWebApiAuthorize]
    public class AuthorizedController : ApiController
    {
    }
}