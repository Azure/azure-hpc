// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Web.Mvc;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Blast.Configuration;
using Microsoft.Azure.Blast.Web.Models;

namespace Microsoft.Azure.Blast.Web.Controllers
{
    public class PoolsController : AuthorizedController
    {
        private readonly BlastConfiguration _configuration;
        private readonly BatchClient _batchClient;

        public PoolsController(BlastConfiguration configuration)
        {
            _configuration = configuration;
            _batchClient = configuration.BatchClient;
        }

        [Route("pools")]
        public ActionResult Index()
        {
            return View();
        }

        [Route("pools/new")]
        public ActionResult New()
        {
            var model = new NewPoolModel
            {
                VirtualMachineSizes = _configuration.GetVirtualMachineSizes(),
            };
            return View(model);
        }

        [Route("pools/{poolId}")]
        public ActionResult Show(string poolId)
        {
            var pool = _batchClient.PoolOperations.GetPool(poolId);

            if (pool == null)
            {
                return new HttpNotFoundResult("No such pool");
            }

            var model = new PoolDetailsModel
            {
                Pool = pool,
                ComputeNodes = _batchClient.PoolOperations.ListComputeNodes(poolId).ToList(),
            };

            return View(model);
        }

        [Route("pools/{poolId}/computenodes/{computeNodeId}/files/{fileName}/{fileExtension}")]
        public ActionResult DownloadStartTaskFile(string poolId, string computeNodeId, string fileName, string fileExtension)
        {
            var pool = _batchClient.PoolOperations.GetPool(poolId);

            if (pool == null)
            {
                return new HttpNotFoundResult("No such pool");
            }

            var node = _batchClient.PoolOperations.GetComputeNode(poolId, computeNodeId);

            if (node == null)
            {
                return new HttpNotFoundResult("No such node");
            }

            fileName = fileName + "." + fileExtension;
            var filePath = "startup/" + fileName;

            var nodeFile = _batchClient.PoolOperations.GetNodeFile(poolId, computeNodeId, filePath);

            if (nodeFile == null)
            {
                return new HttpNotFoundResult("No such node file");
            }

            var content = nodeFile.ReadAsString();

            string filename = Path.GetFileName(fileName);
            byte[] filedata = Encoding.UTF8.GetBytes(content);

            var cd = new ContentDisposition
            {
                FileName = filename,
                Inline = true,
            };

            Response.AppendHeader("Content-Disposition", cd.ToString());

            return File(filedata, MediaTypeNames.Text.Plain, fileName);
        }
    }
}