// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Azure.Batch.Blast.Databases.ExternalSources;

namespace Microsoft.Azure.Blast.Web.Models
{
    public class RespositoryViewModel
    {
        public string SelectedRepoId { get; set; }
        public List<ExternalRepository> Repositories { get; set; }
    }
}