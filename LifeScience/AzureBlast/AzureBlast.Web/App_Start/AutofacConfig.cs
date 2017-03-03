// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Configuration;
using System.Reflection;
using System.Web.Http;
using System.Web.Mvc;
using Autofac;
using Autofac.Integration.Mvc;
using Autofac.Integration.WebApi;
using Microsoft.Azure.Batch.Blast.Configuration;
using Microsoft.Azure.Batch.Blast.Databases;
using Microsoft.Azure.Batch.Blast.Databases.ExternalSources;
using Microsoft.Azure.Batch.Blast.Databases.Imports;
using Microsoft.Azure.Batch.Blast.Searches;
using Microsoft.Azure.Batch.Blast.Storage;
using BatchCredentials = Microsoft.Azure.Batch.Blast.Batch.BatchCredentials;

namespace Microsoft.Azure.Blast.Web
{
    public static class AutofacConfig
    {
        public static void RegisterDependencies()
        {
            var builder = new ContainerBuilder();

            var storageCredentials = GetPrimaryStorageCredentials();
            var batchCredentials = GetBatchCredentials();

            builder.Register(c => new Microsoft.Azure.Batch.Blast.Configuration.BlastConfigurationManager(
                storageCredentials, batchCredentials).GetConfiguration())
            .As<BlastConfiguration>();

            builder.Register(c => new SystemDatabaseProvider(
                c.Resolve<BlastConfiguration>(),
                SystemDatabaseProvider.DefaultContainerName))
                .As<IDatabaseProvider>();

            builder.Register(c =>
            {
                var databaseRepoManager = new ExternalRepositoryManager(
                    c.Resolve<BlastConfiguration>());
                databaseRepoManager.AddRepository(ExternalRepositoryManager.GetNCBIRepository());
                return databaseRepoManager;
            }).As<IExternalRepositoryManager>().SingleInstance();

            builder.Register(c =>
            {
                var importManager = new DatabaseImportManager(
                    c.Resolve<BlastConfiguration>());
                return importManager;
            })
                .As<IDatabaseImportManager>().SingleInstance();

            builder.Register(c => new AzureSearchProvider(
                c.Resolve<BlastConfiguration>(),
                c.Resolve<IDatabaseProvider>()))
                .As<ISearchProvider>();

            // autowire
            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                   .As<IDatabaseRepository>()
                   .AsImplementedInterfaces()
                   .InstancePerDependency();

            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                   .As<ISearchProvider>()
                   .AsImplementedInterfaces()
                   .InstancePerRequest();

            // make controllers use constructor injection
            builder.RegisterControllers(Assembly.GetExecutingAssembly());
            builder.RegisterApiControllers(Assembly.GetExecutingAssembly()).InstancePerRequest();

            var container = builder.Build();

            DependencyResolver.SetResolver(new AutofacDependencyResolver(container));
            GlobalConfiguration.Configuration.DependencyResolver = new AutofacWebApiDependencyResolver(container);
        }

        private static StorageCredentials GetPrimaryStorageCredentials()
        {
            return
                new StorageCredentials()
                {
                    Account = ConfigurationManager.AppSettings["storage.account"],
                    Key = ConfigurationManager.AppSettings["storage.key"],
                };
        }

        private static BatchCredentials GetBatchCredentials()
        {
            return new BatchCredentials
            {
                Url = ConfigurationManager.AppSettings["batch.url"],
                Account = ConfigurationManager.AppSettings["batch.account"],
                Key = ConfigurationManager.AppSettings["batch.key"],
            };
        }
    }
}