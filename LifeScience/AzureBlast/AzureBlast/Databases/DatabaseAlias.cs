// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Batch.Blast.Databases
{
    public class DatabaseAlias
    {
        public DatabaseAlias(string title, IEnumerable<string> dbList, long numberSequences, long length)
        {
            Title = title;
            DatabaseList = dbList;
            NumberOfSequences = numberSequences;
            Length = length;
        }

        public string Title { get; private set; }

        public IEnumerable<string> DatabaseList { get; private set; }

        public long NumberOfSequences { get; private set; }

        public long Length { get; private set; }

        public static DatabaseAlias FromContent(string content)
        {
            long length = -1;
            long numSeq = -1;
            string title = null;
            string[] dbList = null;

            var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            foreach (var line in lines)
            {
                if (line.StartsWith("title", StringComparison.InvariantCultureIgnoreCase))
                {
                    title = line.Split(new char[] { ' ' }, 2)[1].Trim();
                }

                if (line.StartsWith("dblist", StringComparison.InvariantCultureIgnoreCase))
                {
                    dbList = line.Split(new[] { ' ' }, 2)[1].Trim().Replace("\"", "").Split(new[] { ' ' });
                }

                if (line.StartsWith("nseq", StringComparison.InvariantCultureIgnoreCase))
                {
                    numSeq = long.Parse(line.Split(new char[] { ' ' }, 2)[1].Trim());
                }

                if (line.StartsWith("length", StringComparison.InvariantCultureIgnoreCase))
                {
                    length = long.Parse(line.Split(new char[] { ' ' }, 2)[1].Trim());
                }
            }

            return new DatabaseAlias(title, dbList, numSeq, length);
        }
    }
}
