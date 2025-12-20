using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicForms.Models.Definitions
{
    public class FieldDefinition : FormElementDefinition
    {
        public string BindingPath { get; set; }
        public string DataType { get; set; }      // "Int", "String", "Bool" etc.
        public string ControlType { get; set; }   // "TextDisplay", "Dropdown", "DialogInput"
        public bool IsVisible { get; set; } = true;
        public bool IsEnabled { get; set; } = true;
        public List<string> Options { get; set; } = [];

        public ValidationDefinition Validation { get; set; }

        public ChildLayoutDefinition Layout { get; set; } = new ChildLayoutDefinition();

    }
}
