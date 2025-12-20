using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicForms.Models.Definitions
{

    public class SectionDefinition : FormElementDefinition
    {
        public List<FormElementDefinition> Children { get; set; } = [];
        public PanelLayoutDefinition Layout { get; set; } = new PanelLayoutDefinition();
    }
}
