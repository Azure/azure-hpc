// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Batch.Blast.Searches
{
    public enum QueryState
    {
        Waiting,
        Running,
        Success,
        Error,
        Unmapped,
    }
}
