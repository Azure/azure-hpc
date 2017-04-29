// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Web.Http;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Blast.Configuration;
using Microsoft.Azure.Blast.Web.Models;

namespace Microsoft.Azure.Blast.Web.Controllers.Api
{
    [RoutePrefix("api/pools")]
    public class PoolsController : BaseApiController
    {
        private readonly BlastConfiguration _configuration;
        private readonly BatchClient _batchClient;

        public PoolsController(BlastConfiguration configuration)
        {
            _configuration = configuration;
            _batchClient = configuration.BatchClient;
        }

        [Route(""), HttpGet]
        public IEnumerable<CloudPool> List()
        {
            return _batchClient.PoolOperations.ListPools();
        }

        [Route("{poolId}"), HttpGet]
        public CloudPool Get(string poolId)
        {
            return _batchClient.PoolOperations.GetPool(poolId);
        }

        [Route(""), HttpPost]
        public void Add(PoolSpec poolSpec)
        {
            var pool = _batchClient.PoolOperations.CreatePool(
                poolSpec.Id,
                poolSpec.VirtualMachineSize,
                _configuration.GetVirtualMachineConfiguration(),
                poolSpec.TargetDedicated);
            pool.MaxTasksPerComputeNode = _configuration.GetCoresForVirtualMachineSize(poolSpec.VirtualMachineSize);

            if (pool.TargetDedicated == 1 && pool.MaxTasksPerComputeNode == 1)
            {
                // Need to always ensure a JM can run
                pool.MaxTasksPerComputeNode = 2;
            }

            pool.Commit();
        }

        [Route("{poolId}"), HttpDelete]
        public void Delete(string poolId)
        {
            _batchClient.PoolOperations.DeletePool(poolId);
        }
    }
}