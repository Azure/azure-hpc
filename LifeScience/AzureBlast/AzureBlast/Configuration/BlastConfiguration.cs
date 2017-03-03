// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Batch.Auth;
using Microsoft.Azure.Batch.Blast.Storage;
using Microsoft.WindowsAzure.Storage;
using BatchCredentials = Microsoft.Azure.Batch.Blast.Batch.BatchCredentials;

namespace Microsoft.Azure.Batch.Blast.Configuration
{
    public class BlastConfiguration
    {
        private Dictionary<string, string> _defaultConfiguration;
        private Dictionary<string, string> _configuration;
        private readonly StorageCredentials _storageCredentials;
        private readonly BatchCredentials _batchCredentials;
        private readonly BatchClient _batchClient;
        private readonly ITableStorageProvider _tableStorageProvider;
        private readonly IBlobStorageProvider _blobStorageProvider;

        public BlastConfiguration(
            Dictionary<string, string> defaultConfiguration,
            StorageCredentials storageCredentials,
            BatchCredentials batchCredentials)
        {
            _defaultConfiguration = defaultConfiguration;
            _configuration = defaultConfiguration;
            _storageCredentials = storageCredentials;
            _batchCredentials = batchCredentials;
            _batchClient = BatchClient.Open(
                new BatchSharedKeyCredentials(
                    batchCredentials.Url,
                    batchCredentials.Account,
                    batchCredentials.Key));
            var storageAccount = new CloudStorageAccount(
                new WindowsAzure.Storage.Auth.StorageCredentials(
                    storageCredentials.Account,
                    storageCredentials.Key), true);
            _tableStorageProvider = new AzureTableStorageProvider(storageAccount);
            _blobStorageProvider = new AzureBlobStorageProvider(storageAccount);
            LoadConfiguration();
        }

        public BatchCredentials BatchCredentials { get { return _batchCredentials; } }

        public StorageCredentials StorageCredentials { get { return _storageCredentials; } }

        public BatchClient BatchClient { get { return _batchClient; } }

        public ITableStorageProvider TableStorageProvider { get { return _tableStorageProvider; } }

        public IBlobStorageProvider BlobStorageProvider { get { return _blobStorageProvider; } }

        public VirtualMachineConfiguration GetVirtualMachineConfiguration()
        {
            return new VirtualMachineConfiguration(
                new ImageReference(
                    GetStringValue("vm.image.offer"),
                    GetStringValue("vm.image.publisher"),
                    GetStringValue("vm.image.sku")),
                GetStringValue("node.agent.sku"));
        }

        private static List<string> _reservedExecutableArguments = new List<string>(
            new [] { "-db", "-query" });

        public List<string> ReservedExecutableArguments { get { return _reservedExecutableArguments; } }

        public List<string> GetVirtualMachineSizes()
        {
            return BlastConfigurationManager.VmSizesWithCores.Select(kvp => kvp.Key).ToList();
        }

        public int GetCoresForVirtualMachineSize(string virtualMachineSize)
        {
            var cores = 2;

            var vmSpec =
                BlastConfigurationManager.VmSizesWithCores.Where(e => string.Equals(e.Key, virtualMachineSize, StringComparison.InvariantCulture))
                .Select(e => (KeyValuePair<string, int>?)e)
                .FirstOrDefault();

            if (vmSpec != null)
            {
                cores = vmSpec.Value.Value;
            }

            return cores;
        }

        public string GetImportVirtualMachineSize()
        {
            return GetStringValue("import.vm.size");
        }

        public int GetImportVirtualMachineCores()
        {
            return GetIntValue("import.vm.cores");
        }

        public int GetImportVirtualMachineMaxCount()
        {
            return GetIntValue("import.vm.maxvms");
        }

        private string GetStringValue(string property)
        {
            return _configuration[property];
        }

        private int GetIntValue(string property)
        {
            int value;
            if (int.TryParse(_configuration[property], out value))
            {
                return value;
            }
            return int.Parse(_defaultConfiguration[property]);
        }

        private void LoadConfiguration()
        {
            try
            {
                var entities = _tableStorageProvider.ListEntities<ConfigurationEntity>();
                if (entities != null)
                {
                    foreach (var configurationEntity in entities)
                    {
                        _configuration[configurationEntity.Key] = configurationEntity.Value;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to load config {0}", e);
                _configuration = _defaultConfiguration;
            }
        }

        public void ResetConfiguration()
        {
            try
            {
                var entities = _tableStorageProvider.ListEntities<ConfigurationEntity>();
                if (entities != null)
                {
                    Parallel.ForEach(entities, entity => _tableStorageProvider.DeleteEntity(entity));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to reset config {0}", e);
            }
            _configuration = _defaultConfiguration;
        }
    }
}
