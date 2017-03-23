// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Batch.Blast.Databases.ExternalSources;

namespace Microsoft.Azure.Batch.Blast.Databases.Imports
{
    public interface IDatabaseImportManager
    {
        /// <summary>
        /// Imports the specified database from the repository and returns the import job Id
        /// </summary>
        /// <param name="externalRepository"></param>
        /// <param name="externalDatabase"></param>
        void SubmitImport(ExternalRepository externalRepository, ExternalDatabase externalDatabase);

        void ImportExisting(string databaseName, string description, string containerName);
    }
}
