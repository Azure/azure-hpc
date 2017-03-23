// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.Batch.Blast.Databases;
using Microsoft.WindowsAzure.Storage.Table;

namespace Microsoft.Azure.Batch.Blast.Storage.Entities
{
    public class DatabaseEntity : TableEntity
    {
        public const string DefaultRepository = "default";

        public DatabaseEntity(string databaseId, string containerName, int fileCount, long totalSize, DatabaseState state, DatabaseType type, string importJobId)
        {
            PartitionKey = DefaultRepository;
            RowKey = databaseId;
            ContainerName = containerName;
            FileCount = fileCount;
            TotalSize = totalSize;
            State = state;
            Type = type;
            ImportJobId = importJobId;
        }

        public DatabaseEntity(string databaseId, string containerName, int fileCount, long totalSize, DatabaseState state, DatabaseType type) :
            this(databaseId, containerName, fileCount, totalSize, state, type, null)
        { }

        public DatabaseEntity()
        {
        }

        public string Name { get { return RowKey; } }

        public string DisplayName { get; set; }

        public string ContainerName { get; set; }

        public bool? _DedicatedContainer { get; set; }

        [IgnoreProperty]
        public bool DedicatedContainer
        {
            get { return _DedicatedContainer.HasValue ? _DedicatedContainer.Value : false; }
            set { _DedicatedContainer = value; }
        }

        public string _Type { get; set; }

        public Int64 FileCount { get; set; }

        public long TotalSize { get; set; }

        public string _State { get; set; }

        public string ImportJobId { get; set; }

        public Int64 TotalTasks { get; set; }

        public Int64 CompletedTasks { get; set; }

        [IgnoreProperty]
        public int ImportPercent { get { return Math.Min(100, (int) (CompletedTasks*100/TotalTasks)); } }

        public string ImportErrors { get; set; }

        #region non-persisted helpers

        [IgnoreProperty]
        public DatabaseState State
        {
            get { return (DatabaseState) Enum.Parse(typeof (DatabaseState), _State); }
            set { _State = value.ToString(); }
        }

        [IgnoreProperty]
        public DatabaseType Type
        {
            get { return (DatabaseType)Enum.Parse(typeof(DatabaseType), _Type); }
            set { _Type = value.ToString(); }
        }

        [IgnoreProperty]
        public bool ImportInProgress
        {
            get { return State < DatabaseState.ImportingFailed; }
        }

        [IgnoreProperty]
        public string FriendlySize
        {
            get { return GetFriendlySize(TotalSize); }
        }

        [IgnoreProperty]
        public string FriendlyStatus
        {
            get { return GetFriendlyStatus(); }
        }

        public static string GetFriendlySize(long size)
        {
            if (size > 1073741824) //GB
            {
                return Math.Round((double)size / (double)1073741824, 1) + " GB";
            }
            if (size > 1048576) // MB
            {
                return Math.Round((double)size / (double)1048576, 1) + " MB";
            }
            return Math.Round((double)size / (double)1024, 1) + " KB";
        }

        public string GetFriendlyStatus()
        {
            switch (State)
            {
                case DatabaseState.ImportingNotStarted:
                    return "Importing: Not started";
                case DatabaseState.ImportingWaitingForResources:
                    return "Importing: Waiting for resources";
                case DatabaseState.ImportingRunning:
                    return string.Format("Importing: {0}%", ImportPercent);
                case DatabaseState.ImportingFailed:
                    return string.Format("Import Failed {0}", ImportErrors ?? "");
                case DatabaseState.Ready:
                    return "Ready";
            }
            return "";
        }

        #endregion
    }
}
