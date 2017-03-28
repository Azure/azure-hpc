// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Azure.Batch.Blast.Storage.Entities;

namespace Microsoft.Azure.Batch.Blast.Searches
{
    public interface ISearchProvider
    {
        Guid SubmitSearch(SearchSpecification searchSpec);

        SearchEntity GetSearch(Guid searchId);

        void DeleteSearch(Guid searchId);

        void CancelSearch(Guid searchId);

        IEnumerable<SearchEntity> ListSearches();

        IEnumerable<SearchQueryEntity> ListSearchQueries(Guid searchId);

        string GetSearchQueryOutput(Guid searchId, string queryId, string filename);
    }
}
