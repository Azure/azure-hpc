// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Azure.Batch.Blast.Databases.ExternalSources
{
    public interface IExternalDatabaseSource
    {
        IReadOnlyList<ExternalDatabase> ListDatabases();

        ExternalDatabase GetDatabase(string databaseName);

        IReadOnlyList<DatabaseFragment> GetDatabaseFragments(string databaseName);
    }
}
