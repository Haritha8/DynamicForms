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
               else if(dep.Type == "EnabledWhen")
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