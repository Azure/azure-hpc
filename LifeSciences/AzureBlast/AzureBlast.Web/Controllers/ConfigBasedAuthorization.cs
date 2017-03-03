// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Configuration;

namespace Microsoft.Azure.Blast.Web.Controllers
{
    public static class ConfigBasedAuthorization
    {
        public static bool RequireAuthorization()
        {
            var disableAuthn = ConfigurationManager.AppSettings["ida:disableAuthn"];

            bool disabled = false;
            bool.TryParse(disableAuthn, out disabled);

            if (disabled)
            {
                return false;
            }

            return true;
        }
    }
}