using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicForms.Models.Definitions
{
    public class ActionDefinition : FormElementDefinition
    {
        public string ControlType { get; set; }   // "Button"
        public string ActionType { get; set; }    // "SaveNewEntry", "EnterEditMode", etc.
        public bool IsVisible { get; set; } = true;
        public bool IsEnabled { get; set; } = true;
        public SaveConfigDefinition SaveConfig { get; set; }

        public ChildLayoutDefinition Layout { get; set; } = new ChildLayoutDefinition();
    }
}
