using System.Collections.Generic;
namespace DynamicForms.Models.Definitions
{
    // Base class for all elements in the form JSON
    
    public class FormDefinition : FormElementDefinition
    {
        public string SchemaVersion { get; set; }
        public List<FormElementDefinition> Children { get; set; } = [];
    }
    public class RepeaterDefinition : FormElementDefinition
    {
        public string BindingPath { get; set; }   // "data.logs"
        public List<FormElementDefinition> ItemTemplate { get; set; } = [];
    }
}