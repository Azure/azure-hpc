// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Batch.Blast.Searches
{
    public class SearchQuery
    {
        public string Id { get; set; }

        public QueryState State { get; set; }

        public DateTime? StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public string Duration
        {
            get
            {
                if (StartTime == null || EndTime == null)
                {
                    return "";
                }
                return (EndTime.Value - StartTime.Value).GetFriendlyDuration();
            }
        }

        public string InputFilename { get; set; }

        public IEnumerable<QueryOutput> Outputs { get; set; }
    }
}
