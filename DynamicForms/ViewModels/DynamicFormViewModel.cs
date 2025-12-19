using System.Collections.ObjectModel;
using System.Linq;
using DynamicForms.Models.Data;
using DynamicForms.Models.Definitions;
namespace DynamicForms.ViewModels
{
    public class DynamicFormViewModel : ViewModelBase
    {
        private readonly FormDefinition _definition;
        private readonly FormDataContext _dataContext;
        public ObservableCollection<ElementViewModel> EntryElements { get; }
            = new ObservableCollection<ElementViewModel>();
        public DynamicFormViewModel(FormDefinition def, FormDataContext dataContext)
        {
            _definition = def;
            _dataContext = dataContext;
            BuildEntryElements();
        }
        private void BuildEntryElements()
        {
            // Try to find EntryRowSection by id
            var entrySection = _definition.Children
                .OfType<SectionDefinition>()
                .FirstOrDefault(s => s.Id == "EntryRowSection");
            // Fallback: just take the first Section if id doesn't match for some reason
            if (entrySection == null)
            {
                entrySection = _definition.Children
                    .OfType<SectionDefinition>()
                    .FirstOrDefault();
            }
            if (entrySection == null)
                return;
            foreach (var child in entrySection.Children)
            {
                ElementViewModel vm = null;
                if (child is FieldDefinition fd)
                {
                    vm = new FieldViewModel(fd, _dataContext);
                }
                else if (child is ActionDefinition ad)
                {
                    vm = new ActionViewModel(ad, _dataContext, () =>
                    {
                        System.Diagnostics.Debug.WriteLine("Save clicked (placeholder).");
                    });
                }
                if (vm != null)
                    EntryElements.Add(vm);
            }
        }
    }
}
