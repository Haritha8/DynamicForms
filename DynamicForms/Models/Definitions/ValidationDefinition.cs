using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicForms.Models.Definitions
{
    public class ValidationDefinition
    {
        public bool IsRequired { get; set; }
        public string ErrorMessage { get; set; }
        // You can extend later with min, max, maxLength, etc.
    }
}
