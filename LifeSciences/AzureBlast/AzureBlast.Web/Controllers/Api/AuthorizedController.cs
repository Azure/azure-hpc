// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Web.Http;

namespace Microsoft.Azure.Blast.Web.Controllers.Api
{
    [ConfigBasedWebApiAuthorize]
    public class AuthorizedController : ApiController
    {
    }
}