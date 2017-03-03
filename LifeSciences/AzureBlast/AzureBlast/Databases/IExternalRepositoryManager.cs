// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Azure.Batch.Blast.Databases.ExternalSources;

namespace Microsoft.Azure.Batch.Blast.Databases
{
    public interface IExternalRepositoryManager
    {
        IEnumerable<ExternalRepository> ListRepositories();

        ExternalRepository GetRepository(string repoId);

        void AddRepository(ExternalRepository repository);

        void DeleteRepository(string repoId);
    }
}
