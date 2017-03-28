// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Batch.Blast.Databases;
using Microsoft.Azure.Batch.Blast.Searches;
using Microsoft.WindowsAzure.Storage.Table;

namespace Microsoft.Azure.Batch.Blast.Storage.Entities
{
    public class SearchEntity : TableEntity
    {
        public static string AllUsersPk = "allusers";

        public SearchEntity()
        {

        }

        public SearchEntity(Guid queryId)
        {
            PartitionKey = AllUsersPk;
            RowKey = queryId.ToString();
            Version = 1;
        }

        public Guid Id { get { return Guid.Parse(RowKey); } }

        public string Name { get; set; }

        public Int64 TotalTasks { get; set; }

        public Int64 CompletedTasks { get; set; }

        public int Version { get; set; }

        public string InputContainer { get; set; }

        public string OutputContainer { get; set; }

        public string JobId { get; set; }

        public string Executable { get; set; }

        public string ExecutableArgs { get; set; }

        public string ExecutableArgsSanitised { get; set; }

        public string OutputfileFormat { get; set; }

        public string DatabaseId { get; set; }

        public string DatabaseContainer { get; set; }

        public string _DatabaseType { get; set; }

        [IgnoreProperty]
        public DatabaseType DatabaseType
        {
            get { return (DatabaseType)Enum.Parse(typeof(DatabaseType), _DatabaseType); }
            set { _DatabaseType = value.ToString(); }
        }

        public string _State { get; set; }

        [IgnoreProperty]
        public SearchState State
        {
            get { return (SearchState)Enum.Parse(typeof(SearchState), _State); }
            set { _State = value.ToString(); }
        }

        public string Errors { get; set; }

        public string PoolId { get; set; }

        public string PoolDisplayName { get; set; }

        public int? TargetDedicated { get; set; }

        public string VirtualMachineSize { get; set; }

        public string _Files
        {
            get
            {
                if (Files == null)
                {
                    return null;
                }
                return string.Join(",", Files);
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    Files = null;
                }
                else
                {
                    Files = value.Split(new[] { ',' }).ToList();
                }
            }
        }

        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        [IgnoreProperty]
        public List<string> Files { get; set; }

        [IgnoreProperty]
        public string Duration {
            get { return EndTime == null ? "" : (EndTime.Value - StartTime).GetFriendlyDuration(); }
        }
    }
}
