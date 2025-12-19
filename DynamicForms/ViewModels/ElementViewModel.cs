using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DynamicForms.Models.Data;
using DynamicForms.Models.Definitions;
using Newtonsoft.Json.Linq;
namespace DynamicForms.ViewModels
{
    public abstract class ElementViewModel : ViewModelBase
    {
        protected ElementViewModel(FormElementDefinition def, FormDataContext dataContext)
        {
            Id = def.Id;
            Label = def.Label;
            ElementType = def.ElementType;
            DataContext = dataContext;
            Dependencies = def.Dependencies;

            EvaluateDependencies();
        }

        private void EvaluateDependencies()
        {
           bool visible = true;
           bool enabled = true;

            foreach (var dep in Dependencies)
            {
               bool result = EvaluateDependency(dep);
               
               if(dep.Type == "VisibleWhen")
               {
                   visible = visible && result;
               }
               else if(dep.Type == "EnableWhen")
               {
                   enabled = enabled && result;
                }
            }
            IsVisible = visible;
            IsEnabled = enabled;
        }

        private bool EvaluateDependency(DependencyDefinition dep)
        {
            var token = DataContext.GetToken(dep.SourcePath);
            switch (dep.Operator)
            {
                case "NotEmpty":
                    if (token == null || token.Type == JTokenType.Null)
                        return false;
                    if (token.Type == JTokenType.Array)
                        return token.HasValues;
                    var s = token.ToString();
                    return !string.IsNullOrWhiteSpace(s);
                case "HasAny":
                    return token is JArray arr && arr.Count > 0;
                case "Equals":
                    if (token == null && dep.Value == null)
                        return true;
                    if (token == null || dep.Value == null)
                        return false;
                    return JToken.DeepEquals(token, dep.Value);
                default:
                    // Unknown operator: do not block visibility/enabled
                    return true;
            }
        }

        public string Id { get; }
        public string Label { get; }
        public string ElementType { get; }
        public FormDataContext DataContext { get; }
        private bool _isVisible = true;
        public bool IsVisible
        {
            get => _isVisible;
            set { _isVisible = value; RaisePropertyChanged(); }
        }
        private bool _isEnabled = true;
        public bool IsEnabled
        {
            get => _isEnabled;
            set { _isEnabled = value; RaisePropertyChanged(); }
        }

        public List<DependencyDefinition> Dependencies { get; }

        // Factory to build the right VM type for a given definition
        public static ElementViewModel Create(FormElementDefinition def, FormDataContext ctx)
        {
            switch (def)
            {
                case FormDefinition f:
                    return new FormViewModel(f, ctx);
                case SectionDefinition s:
                    return new SectionViewModel(s, ctx);
                case RepeaterDefinition r:
                    return new RepeaterViewModel(r, ctx);
                case FieldDefinition fd:
                    return new FieldViewModel(fd, ctx);
                case ActionDefinition ad:
                    return new ActionViewModel(ad, ctx, null);
                default:
                    return null;
            }
        }
    }

    // ---------- FORM & SECTION ----------
    public class FormViewModel : ElementViewModel
    {
        public ObservableCollection<ElementViewModel> Children { get; } = new ObservableCollection<ElementViewModel>();
        public FormViewModel(FormDefinition def, FormDataContext ctx)
            : base(def, ctx)
        {
            foreach (var child in def.Children)
            {
                var vm = Create(child, ctx);
                if (vm != null)
                    Children.Add(vm);
            }
        }
    }
    public class SectionViewModel : ElementViewModel
    {
        public ObservableCollection<ElementViewModel> Children { get; } = new ObservableCollection<ElementViewModel>();
        public SectionViewModel(SectionDefinition def, FormDataContext ctx)
            : base(def, ctx)
        {
            foreach (var child in def.Children)
            {
                var vm = Create(child, ctx);
                if (vm != null)
                    Children.Add(vm);
            }
        }
    }

    // ---------- REPEATER & REPEATER ITEM ----------
    public class RepeaterViewModel : ElementViewModel
    {
        private readonly RepeaterDefinition _def;
        public RepeaterViewModel(RepeaterDefinition def, FormDataContext ctx)
            : base(def, ctx)
        {
            _def = def;
            Items = new ObservableCollection<RepeaterItemViewModel>();
            BuildItems();
        }
        public string BindingPath => _def.BindingPath;
        public IReadOnlyList<FormElementDefinition> ItemTemplate => _def.ItemTemplate;
        public ObservableCollection<RepeaterItemViewModel> Items { get; }
        private void BuildItems()
        {
            var arrayToken = DataContext.GetToken(BindingPath) as Newtonsoft.Json.Linq.JArray;
            if (arrayToken == null)
                return;
            for (int i = 0; i < arrayToken.Count; i++)
            {
                // Each row has its own child context: e.g. "data.logs[0]"
                var rowContext = DataContext.CreateChildContext($"{BindingPath}[{i}]");
                var itemVm = new RepeaterItemViewModel(this, rowContext);
                Items.Add(itemVm);
            }
        }
    }
    public class RepeaterItemViewModel : ViewModelBase
    {
        public RepeaterItemViewModel(RepeaterViewModel parent, FormDataContext ctx)
        {
            Parent = parent;
            DataContext = ctx;
            Children = new ObservableCollection<ElementViewModel>();
            foreach (var def in parent.ItemTemplate)
            {
                // For now we only support fields/actions in the item template
                ElementViewModel vm = null;
                if (def is FieldDefinition fd)
                    vm = new FieldViewModel(fd, ctx);
                else if (def is ActionDefinition ad)
                    vm = new ActionViewModel(ad, ctx, null);
                if (vm != null)
                    Children.Add(vm);
            }
        }
        public RepeaterViewModel Parent { get; }
        public FormDataContext DataContext { get; }
        public ObservableCollection<ElementViewModel> Children { get; }
    }
    public class FieldViewModel : ElementViewModel
    {
        private readonly FieldDefinition _def;
        private object _value;
        public FieldViewModel(FieldDefinition def, FormDataContext dataContext)
            : base(def, dataContext)
        {
            _def = def;
            if (!string.IsNullOrEmpty(def.BindingPath))
            {
                _value = dataContext.GetValue(def.BindingPath);
            }
        }
        public string ControlType => _def.ControlType;
        public string DataType => _def.DataType;
        public string BindingPath => _def.BindingPath;
        public List<string> Options => _def.Options;
        public bool IsRequired => _def.Validation?.IsRequired == true;
        public string ValidationErrorMessage => _def.Validation?.ErrorMessage;
        public object Value
        {
            get => _value;
            set
            {
                if (!Equals(_value, value))
                {
                    _value = value;
                    RaisePropertyChanged();
                    if (!string.IsNullOrEmpty(BindingPath))
                        DataContext.SetValue(BindingPath, value);
                }
            }
        }
    }
    public class ActionViewModel : ElementViewModel
    {
        private readonly ActionDefinition _def;
        public ActionViewModel(ActionDefinition def, FormDataContext dataContext, System.Action onClick)
            : base(def, dataContext)
        {
            _def = def;
            Command = new RelayCommand(onClick ?? (() => { }));
        }
        public string ControlType => _def.ControlType;
        public string ActionType => _def.ActionType;
        public SaveConfigDefinition SaveConfig => _def.SaveConfig;
        public RelayCommand Command { get; }
    }
}