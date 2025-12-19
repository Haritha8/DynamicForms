using System;
using System.Collections.ObjectModel;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DynamicForms.Models.Data;
using DynamicForms.Models.Definitions;
using DynamicForms.ViewModels;
using Newtonsoft.Json.Linq;
using Windows.UI.Xaml.Data;
namespace DynamicForms
{
    public sealed partial class MainPage : Page
    {
        public DynamicFormViewModel ViewModel { get; private set; }
        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded;
        }
        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Load DATA json
                var dataFile = await StorageFile.GetFileFromApplicationUriAsync(
                    new Uri("ms-appx:///Assets/ToolSetupForm.data.json"));
                var dataJson = await FileIO.ReadTextAsync(dataFile);
                var dataRoot = JObject.Parse(dataJson);
                var dataContext = new FormDataContext(dataRoot);
                // Load STRUCTURE json
                var structFile = await StorageFile.GetFileFromApplicationUriAsync(
                    new Uri("ms-appx:///Assets/ToolSetupForm.structure.json"));
                var structJson = await FileIO.ReadTextAsync(structFile);
                var formDef = FormDefinitionFactory.FromJson(structJson);
                // Create VM
                ViewModel = new DynamicFormViewModel(formDef, dataContext);
                // Render entry row (4 elements) into EntryRowPanel
                RenderEntryRow();
            }
            catch (Exception ex)
            {

            }
        }
        private void RenderEntryRow()
        {
            EntryRowPanel.Children.Clear();
            foreach (var element in ViewModel.EntryElements)
            {
                FrameworkElement control = null;
                if (element is FieldViewModel fieldVm)
                {
                    control = CreateFieldControl(fieldVm);
                }
                else if (element is ActionViewModel actionVm)
                {
                    control = CreateActionControl(actionVm);
                }
                if (control != null)
                {
                    EntryRowPanel.Children.Add(control);
                }
            }
        }

        private FrameworkElement CreateFieldControl(FieldViewModel vm)
        {
            // Container for label + editor
            var container = new StackPanel
            {
                Margin = new Thickness(8, 0, 8, 0)
            };
            // Label
            var label = new TextBlock
            {
                Text = vm.Label,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 2)
            };
            container.Children.Add(label);
            FrameworkElement editor;
            switch (vm.ControlType)
            {
                case "TextDisplay":
                    // Read-only display of the current value
                    var textBlock = new TextBlock
                    {
                        FontSize = 16,
                        Text = vm.Value?.ToString()
                    };
                    // If you want it to auto-update later, you can hook vm.PropertyChanged here.
                    editor = textBlock;
                    break;
                case "Dropdown":
                    // Simple ComboBox: Options list + selection events
                    var combo = new ComboBox
                    {
                        Width = 200
                    };

                    if(vm.Options != null)
                    {
                        foreach (var option in vm.Options)
                        {
                            combo.Items.Add(option);
                        }
                    }
                    // Push changes back into VM when user picks a different item
                    combo.SelectionChanged += (s, e) =>
                    {
                        vm.Value = combo.SelectedItem?.ToString();
                    };
                    editor = combo;
                    break;
                default:
                    // Default: editable TextBox
                    var textBox = new TextBox
                    {
                        Width = 200,
                        Text = vm.Value?.ToString()
                    };
                    // Push text changes back into VM
                    textBox.TextChanged += (s, e) =>
                    {
                        vm.Value = textBox.Text;
                    };
                    editor = textBox;
                    break;
            }
            container.Children.Add(editor);
            return container;
        }

        private FrameworkElement CreateActionControl(ActionViewModel vm)
        {
            var container = new StackPanel
            {
                Margin = new Thickness(8, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Bottom
            };
            var button = new Button
            {
                Content = vm.Label,
                Width = 80,
                Height = 32
            };
            // Click executes the VM command (if present)
            button.Click += (s, e) =>
            {
                if (vm.Command != null && vm.Command.CanExecute(null))
                {
                    vm.Command.Execute(null);
                }
            };
            container.Children.Add(button);
            return container;
        }
    }
}