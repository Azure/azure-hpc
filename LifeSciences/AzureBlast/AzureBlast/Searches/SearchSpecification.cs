// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Azure.Batch.Blast.Searches
{
    public class SearchSpecification
    {
        public string Name { get; set; }

        public string DatabaseName { get; set; }

        public string Executable { get; set; }

        public string ExecutableArgs { get; set; }

        public IEnumerable<SearchInputFile> SearchInputFiles { get; set; }

        public bool SplitSequenceFile { get; set; }

        public int SequencesPerQuery { get; set; }

        public string PoolId { get; set; }

        public int? TargetDedicated { get; set; }

        public string VirtualMachineSize { get; set; }

        public string PoolDisplayName { get; set; }

    }
}
