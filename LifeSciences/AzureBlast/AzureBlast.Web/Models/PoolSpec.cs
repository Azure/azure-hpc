// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Blast.Web.Models
{
    public class PoolSpec
    {
        public string Id { get; set; }
        public int TargetDedicated { get; set; }
        public int MaxTasksPerComputeNode { get; set; }
        public string VirtualMachineSize { get; set; }
    }
}