// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Azure.Batch.Blast.Storage.Entities;

namespace Microsoft.Azure.Batch.Blast.Databases
{
    public interface IDatabaseProvider
    {
        IReadOnlyList<DatabaseEntity> ListDatabases();

        DatabaseEntity GetDatabase(string databaseName);

        IReadOnlyList<DatabaseFragment> GetDatabaseFragments(string databaseName);

        void DeleteDatabase(string databaseName);

        string ContainerName { get; }
    }
}
