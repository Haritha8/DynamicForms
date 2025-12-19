using System.Collections.ObjectModel;
using System.Linq;
using DynamicForms.Models.Data;
using DynamicForms.Models.Definitions;
namespace DynamicForms.ViewModels
{
    public class DynamicFormViewModel : ViewModelBase
    {
        public FormViewModel Root { get; }
        public DynamicFormViewModel(FormDefinition def, FormDataContext dataContext)
        {
            Root = new FormViewModel(def, dataContext);
        }
    }
}
