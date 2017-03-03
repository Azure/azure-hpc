// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Microsoft.Azure.Blast.Web.Models
{
    public class NewSearchModel
    {
        public List<string> VirtualMachineSizes { get; set; }
        public List<string> BlastExecutables { get; set; }
    }
}