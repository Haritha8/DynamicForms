using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicForms.Models.Definitions
{
    public class DependencyDefinition
    {
        public string Type { get; set; } // "Visibility", "Enablement"
        public string SourcePath { get; set; } // e.g., "data.isAdmin"
        public string Operator { get; set; } // e.g., "Equals", "NotEquals"
        public JToken Value { get; set; } // e.g., true, "someValue"
    }
}
