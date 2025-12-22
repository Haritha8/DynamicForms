using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicForms.Models.Definitions
{
    public abstract class FormElementDefinition
    {
        public string Id { get; set; }
        public string Label { get; set; }
        public string ElementType { get; set; }  // "Form", "Section", "Field", "Action", "Repeater"
        public List<DependencyDefinition> Dependencies { get; set; } = [];
        public string StyleKey { get; set; }
    }
}
