using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicForms.Models.Definitions
{
    public class SaveConfigDefinition
    {
        public string SourceObjectPath { get; set; }       // "data.currentEntry"
        public string TargetCollectionPath { get; set; }   // "data.logs"
        public string AutoIncrementField { get; set; }     // "toolSetupNumber"
        public string CounterPath { get; set; }            // "meta.toolSetupCounter"
        public bool ResetAfterSave { get; set; }           // true/false
    }
}
