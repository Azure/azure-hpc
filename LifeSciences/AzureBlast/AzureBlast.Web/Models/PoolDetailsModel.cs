using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Azure.Batch;

namespace Microsoft.Azure.Blast.Web.Models
{
    public class PoolDetailsModel
    {
        public CloudPool Pool { get; set; }
        public List<ComputeNode> ComputeNodes { get; set; }
    }
}