// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Batch.Blast.Databases
{
    public class DatabaseFragment
    {
        public DatabaseFragment(string filename, long size)
        {
            Filename = filename;
            Size = size;
        }

        public string Filename { get; private set; }

        public long Size { get; private set; }
    }
}
