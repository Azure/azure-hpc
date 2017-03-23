using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Microsoft.Azure.Blast.Web.Models
{
    public class AddDatabaseSpec
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string ContainerName { get; set; }
    }
}