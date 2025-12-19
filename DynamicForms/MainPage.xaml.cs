using DynamicForms.Models.Data;
using DynamicForms.Models.Definitions;
using DynamicForms.ViewModels;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
namespace DynamicForms
{
    public sealed partial class MainPage : Page
    {
        public DynamicFormViewModel ViewModel { get; private set; }
        private FormDefinition _formDefinition;
        private FormDataContext _dataContext;
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
                _dataContext = new FormDataContext(dataRoot);
                // Load STRUCTURE json
                var structFile = await StorageFile.GetFileFromApplicationUriAsync(
                    new Uri("ms-appx:///Assets/ToolSetupForm.structure.json"));
                var structJson = await FileIO.ReadTextAsync(structFile);
                _formDefinition = FormDefinitionFactory.FromJson(structJson);
                // Create VM
                ViewModel = new DynamicFormViewModel(_formDefinition, _dataContext);
                // Render entry row (4 elements) into EntryRowPanel
                RenderForm();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading form: {ex}");
            }
        }

        private void RenderForm()
        {
            FormRootPanel.Children.Clear();

            if (ViewModel?.Root == null)
                return;

            RenderElement(ViewModel.Root, FormRootPanel);
        }

        private void RenderElement(ElementViewModel vm, Panel parent)
        {
            if (!vm.IsVisible)
                return;

            FrameworkElement control = null;

            switch (vm)
            {
                case FormViewModel formVm:

                    // Form: just render its children

                    foreach (var child in formVm.Children)

                    {

                        RenderElement(child, parent);

                    }

                    return; // nothing to add directly

                case SectionViewModel sectionVm:

                    // Section container with label

                    var sectionStack = new StackPanel
                    {

                        Margin = new Thickness(0, 4, 0, 4)

                    };

                    if (!string.IsNullOrEmpty(sectionVm.Label))

                    {

                        sectionStack.Children.Add(new TextBlock

                        {

                            Text = sectionVm.Label,

                            FontSize = 16,

                            FontWeight = Windows.UI.Text.FontWeights.SemiBold,

                            Margin = new Thickness(0, 0, 0, 4)

                        });

                    }

                    // Horizontal row for child fields/actions (for this prototype)

                    var rowPanel = new StackPanel

                    {

                        Orientation = Orientation.Horizontal,

                        Spacing = 8

                    };

                    sectionStack.Children.Add(rowPanel);

                    foreach (var child in sectionVm.Children)

                    {

                        RenderElement(child, rowPanel);

                    }

                    control = sectionStack;

                    break;

                case RepeaterViewModel repeaterVm:
                    var repStack = new StackPanel
                    {
                        Margin = new Thickness(0, 8, 0, 0)
                    };
                    if (!string.IsNullOrEmpty(repeaterVm.Label))
                    {
                        repStack.Children.Add(new TextBlock
                        {
                            Text = repeaterVm.Label,
                            FontSize = 16,
                            FontWeight = Windows.UI.Text.FontWeights.SemiBold,
                            Margin = new Thickness(0, 0, 0, 4)
                        });
                    }
                    // One horizontal row per item
                    foreach (var item in repeaterVm.Items)
                    {
                        var itemRow = new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Spacing = 8,
                            Background = new SolidColorBrush(Windows.UI.Colors.Honeydew),
                            Margin = new Thickness(0, 2, 0, 2)
                        };
                        foreach (var child in item.Children)
                        {
                            RenderElement(child, itemRow);
                        }
                        repStack.Children.Add(itemRow);
                    }
                    control = repStack;
                    break;

                case FieldViewModel fieldVm:

                    control = CreateFieldControl(fieldVm);

                    break;

                case ActionViewModel actionVm:

                    control = CreateActionControl(actionVm);

                    break;

            }

            if (control != null)
            {

                // Visibility respects vm.IsVisible (dependencies)

                control.Visibility = vm.IsVisible ? Visibility.Visible : Visibility.Collapsed;

                parent.Children.Add(control);

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

                    if (vm.Options != null)
                    {
                        foreach (var option in vm.Options)
                        {
                            combo.Items.Add(option);
                        }
                    }
                    if (vm.Value != null)
                    {
                        combo.SelectedItem = vm.Value.ToString();
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
            button.Click += async (s, e) =>
            {
                if (vm.ActionType == "SaveNewEntry")
                {
                    await HandleSaveNewEntryAsync(vm);
                }
                else
                {
                    // fallback: if you later set other commands
                    if (vm.Command != null && vm.Command.CanExecute(null))
                        vm.Command.Execute(null);
                }
            };
            container.Children.Add(button);
            return container;
        }

        private System.Collections.Generic.IEnumerable<FieldViewModel> GetAllFields(ElementViewModel root)
        {
            if (root is FieldViewModel fv)
                yield return fv;
            if (root is FormViewModel formVm)
            {
                foreach (var child in formVm.Children)
                {
                    foreach (var c in GetAllFields(child))
                        yield return c;
                }
            }
            else if (root is SectionViewModel sectionVm)
            {
                foreach (var child in sectionVm.Children)
                {
                    foreach (var c in GetAllFields(child))
                        yield return c;
                }
            }
            // we will extend this when Repeater items get their own VMs
        }

        private async Task HandleSaveNewEntryAsync(ActionViewModel actionVm)
        {
            // Find all fields whose BindingPath is under "data.currentEntry"
            var fields = GetAllFields(ViewModel.Root)
               .Where(f => !string.IsNullOrEmpty(f.BindingPath) &&
                           f.BindingPath.StartsWith("data.currentEntry."))
               .ToList();
            if (fields.Count == 0)
                return;
            // 2. VALIDATION: check required fields
            foreach (var f in fields)
            {
                if (!f.IsRequired)
                    continue;
                var val = f.Value?.ToString();
                if (string.IsNullOrWhiteSpace(val))
                {
                    // For now, just debug output. You can replace this with a ContentDialog later.
                    System.Diagnostics.Debug.WriteLine(
                        $"Validation failed: {f.Label} is required. Message: {f.ValidationErrorMessage}");
                    // Early return – cancel save
                    return;
                }
            }
            var saveCfg = actionVm.SaveConfig;
            if (saveCfg == null)
            {
                System.Diagnostics.Debug.WriteLine("SaveNewEntry clicked but saveConfig is null.");
                return;
            }
            // 3. Build a new log entry JObject from the current entry fields
            var logEntry = new JObject();
            foreach (var f in fields)
            {
                if (string.IsNullOrEmpty(f.BindingPath))
                    continue;
                // BindingPath like "data.currentEntry.toolingSetUsed" → take last segment
                var segments = f.BindingPath.Split('.');
                var propName = segments[segments.Length - 1];
                var valueToken = f.Value != null
                    ? JToken.FromObject(f.Value)
                    : JValue.CreateNull();
                logEntry[propName] = valueToken;
            }
            // default flag for edit mode in the log
            logEntry["isEditing"] = false;
            // 4. Append to data.logs
            var logsToken = actionVm.DataContext.GetToken(saveCfg.TargetCollectionPath) as JArray;
            if (logsToken == null)
            {
                System.Diagnostics.Debug.WriteLine($"Target collection '{saveCfg.TargetCollectionPath}' is not a JArray.");
                return;
            }
            // Determine current ToolSetup# from the corresponding field
            var numberField = fields.FirstOrDefault(f =>
                f.BindingPath?.EndsWith("." + saveCfg.AutoIncrementField) == true);
            int currentNumber = 1;
            if (numberField?.Value != null)
            {
                // handle int/long/string
                if (numberField.Value is int i) currentNumber = i;
                else if (numberField.Value is long l) currentNumber = (int)l;
                else int.TryParse(numberField.Value.ToString(), out currentNumber);
            }
            // Ensure the log entry uses the current number
            logEntry[saveCfg.AutoIncrementField] = currentNumber;
            logsToken.Add(logEntry);
            // 5. Auto-increment counter and currentEntry.toolSetupNumber for the NEXT row
            int nextNumber = currentNumber + 1;
            // Update the meta counter in JSON
            actionVm.DataContext.SetValue(saveCfg.CounterPath, nextNumber);
            // Update the currentEntry field via the ViewModel (this also updates JSON)
            if (numberField != null)
            {
                numberField.Value = nextNumber;
            }
            // 6. Reset other entry fields if resetAfterSave == true
            if (saveCfg.ResetAfterSave)
            {
                foreach (var f in fields)
                {
                    if (f == numberField)
                        continue; // don't reset the auto-increment field
                    switch (f.DataType)
                    {
                        case "Int":
                            f.Value = null;
                            break;
                        case "Bool":
                            f.Value = false;
                            break;
                        default:
                            f.Value = string.Empty;
                            break;
                    }
                }
            }
            // 7. Re-render the entry row so the UI reflects updated VM values
            ViewModel = new DynamicFormViewModel(_formDefinition, _dataContext);
            RenderForm();
            // (Next step: render the log section and re-evaluate dependencies so it becomes visible)
        }
    }
}