// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Batch.Blast.Batch;
using Microsoft.Azure.Batch.Blast.Storage;

namespace Microsoft.Azure.Batch.Blast.Configuration
{
    public class BlastConfigurationManager
    {
        public static readonly Dictionary<string, int> VmSizesWithCores = new Dictionary<string, int>
        {
            {"Standard_A1", 1},
            {"Standard_A2", 2},
            {"Standard_A3", 4},
            {"Standard_A4", 8},
            {"Standard_A5", 2},
            {"Standard_A6", 4},
            {"Standard_A7", 8},

            {"Standard_A10", 8},
            {"Standard_A11", 16},

            {"Standard_D1_v2", 1},
            {"Standard_D2_v2", 2},
            {"Standard_D3_v2", 4},
            {"Standard_D4_v2", 8},
            {"Standard_D5_v2", 16},
            {"Standard_D11_v2", 2},
            {"Standard_D12_v2", 4},
            {"Standard_D13_v2", 8},
            {"Standard_D14_v2", 16},
            {"Standard_D15_v2", 20},

            {"Standard_F1", 1},
            {"Standard_F2", 2},
            {"Standard_F4", 4},
            {"Standard_F8", 8},
            {"Standard_F16", 16},

            {"Standard_G1", 2},
            {"Standard_G2", 4},
            {"Standard_G3", 8},
            {"Standard_G4", 16},
            {"Standard_G5", 32},
        };

        private readonly Dictionary<string, string> _defaultConfiguration =
            new Dictionary<string, string>
            {
                {"pool.keepalive", "false"},
                {"vm.sizes", string.Join(",", VmSizesWithCores.Select(kvp => kvp.Key + ":" + kvp.Value))},
                {"vm.image.publisher", "Canonical"},
                {"vm.image.offer", "UbuntuServer"},
                {"vm.image.sku", "16.04.0-LTS"},
                {"vm.image.version", "latest"},
                {"node.agent.sku", "batch.node.ubuntu 16.04"},
                {"import.vm.size", "Standard_A2"},
                {"import.vm.cores", "2"},
                {"import.vm.maxvms", "20"},
                {"import.vm.pool", ""},
            };

        private readonly StorageCredentials _storageCredentials;
        private readonly BatchCredentials _batchCredentials;

        public BlastConfigurationManager(
            StorageCredentials storageCredentials,
            BatchCredentials batchCredentials)
        {
            _storageCredentials = storageCredentials;
            _batchCredentials = batchCredentials;
        }

        public BlastConfiguration GetConfiguration()
        {
            return new BlastConfiguration(_defaultConfiguration, _storageCredentials, _batchCredentials);
        }
    }
}
