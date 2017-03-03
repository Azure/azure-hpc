// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace Microsoft.Azure.Batch.Blast.Databases.ExternalSources
{
    public class ExternalRepository : TableEntity
    {
        public static string DefaultPk = "repository";

        public ExternalRepository(string id, string name, Uri uri, RepositoryType type, bool readOnly)
        {
            PartitionKey = DefaultPk;
            Id = id;
            Name = name;
            Uri = uri;
            Readonly = readOnly;
            Type = type;
        }

        public ExternalRepository()
        { }

        public string Id
        {
            get { return RowKey; }
            set { RowKey = value; }
        }

        public string Name { get; set; }

        public string _Uri { get; set; }

        [IgnoreProperty]
        public Uri Uri
        {
            get { return _Uri == null ? null : new Uri(_Uri); }
            set { _Uri = value?.ToString(); }
        }

        public bool Readonly { get; set; }

        public string _Type { get; set; }

        [IgnoreProperty]
        public RepositoryType Type
        {
            get { return (RepositoryType) Enum.Parse(typeof (RepositoryType), _Type); }
            set { _Type = value.ToString(); }
        }

        [IgnoreProperty]
        public IExternalDatabaseSource DatabaseSource { get; set; }
    }
}
