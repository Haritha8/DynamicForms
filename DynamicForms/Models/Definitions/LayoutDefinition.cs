using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicForms.Models.Definitions
{
    // Panel layout: used by sections / form / repeater item template
    public class PanelLayoutDefinition
    {
        // "Grid", "StackPanel" (for now)
        public string PanelType { get; set; }
        public List<RowLayoutDefinition> RowDefinitions { get; set; }
        public List<ColumnLayoutDefinition> ColumnDefinitions { get; set; }

        public int? GridRow { get; set; }
        public int? GridColumn { get; set; }
    }
    public class RowLayoutDefinition
    {
        // "Auto", "1*", "2*", "100" etc.
        public string Height { get; set; }
    }
    public class ColumnLayoutDefinition
    {
        // "Auto", "1*", "2*", "120" etc.
        public string Width { get; set; }
    }
    // Child layout inside a panel: used by fields / actions (and later sections)
    public class ChildLayoutDefinition
    {
        public int? GridRow { get; set; }
        public int? GridColumn { get; set; }
    }
}
