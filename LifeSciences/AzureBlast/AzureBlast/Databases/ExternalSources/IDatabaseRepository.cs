// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Batch.Blast.Databases.ExternalSources
{
    public interface IDatabaseRepository
    {
        string Id { get; set; }

        string Name { get; set; }

        Uri Uri { get; set; }

        string ContainerName { get; set; }

        bool Readonly { get; set; }

        RepositoryType Type { get; set; }

        IDatabaseProvider GetDatabaseProvider();
    }
}
