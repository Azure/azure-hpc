// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Azure.Batch.Blast.Databases;

namespace Microsoft.Azure.Batch.Blast.Searches
{
    public class Search
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Database { get; set; }

        public Int64 NumberOfQueries { get; set; }

        public Int64 CompletedQueries { get; set; }

        public string DatabaseDisplayName { get; set; }

        public string PoolId { get; set; }

        public SearchState State { get; set; }

        public DatabaseType DatabaseType { get; set; }

        public string Program { get; set; }

        public IEnumerable<string> QueryInputFiles { get; set; }

        public string LastError { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }
    }
}
