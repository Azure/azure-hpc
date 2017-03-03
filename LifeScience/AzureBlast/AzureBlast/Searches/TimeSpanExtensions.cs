// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Batch.Blast.Searches
{
    public static class TimeSpanExtensions
    {
        public static string GetFriendlyDuration(this TimeSpan timespan)
        {
            return string.Format("{0}h {1}m {2}s", timespan.Hours, timespan.Minutes, timespan.Seconds);
        }
    }
}
