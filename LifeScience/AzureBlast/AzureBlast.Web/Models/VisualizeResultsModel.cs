// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Blast.Web.Models
{
    public class VisualizeResultsModel
    {
        public string Id { get; set; }

        public int QueryId { get; set; }

        public string SearchName { get; set; }

        public string Filename { get; set; }
    }
}