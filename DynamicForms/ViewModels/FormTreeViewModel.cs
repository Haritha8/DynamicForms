using System.Collections.ObjectModel;
using System.Linq;
using DynamicForms.Models.Data;
using DynamicForms.Models.Definitions;
namespace DynamicForms.ViewModels
{
    public class FormViewModel : ElementViewModel
    {
        public ObservableCollection<ElementViewModel> Children { get; } = new();
        public FormViewModel(FormDefinition def, FormDataContext ctx)
            : base(def, ctx)
        {
            foreach (var child in def.Children)
            {
                var vm = ElementViewModelFactory.Create(child, ctx);
                if (vm != null)
                    Children.Add(vm);
            }
        }
    }
    public class SectionViewModel : ElementViewModel
    {
        public ObservableCollection<ElementViewModel> Children { get; } = new();
        public SectionViewModel(SectionDefinition def, FormDataContext ctx)
            : base(def, ctx)
        {
            foreach (var child in def.Children)
            {
                var vm = ElementViewModelFactory.Create(child, ctx);
                if (vm != null)
                    Children.Add(vm);
            }
        }
    }
    // For now Repeater just knows its binding path; we’ll flesh it out when we render logs.
    public class RepeaterViewModel : ElementViewModel
    {
        public string BindingPath { get; }
        public RepeaterViewModel(RepeaterDefinition def, FormDataContext ctx)
            : base(def, ctx)
        {
            BindingPath = def.BindingPath;
        }
    }
}