using System.Collections.Generic;
using System.Collections.ObjectModel;
using DynamicForms.Models.Data;
using DynamicForms.Models.Definitions;
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
        // for now we won't use Options yet; we’ll add later
        public List<string> Options => _def.Options;
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
        public RelayCommand Command { get; }
    }
}