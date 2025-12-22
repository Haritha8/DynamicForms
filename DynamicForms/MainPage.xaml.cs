using DynamicForms.Models.Data;
using DynamicForms.Models.Definitions;
using DynamicForms.ViewModels;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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

                    // For now, just render all children vertically
                    var rootPanel = new StackPanel
                    {
                        Orientation = Orientation.Vertical,
                        Spacing = 8
                    };

                    foreach (var child in formVm.Children)
                    {
                        RenderElement(child, rootPanel);
                    }
                    
                    parent.Children.Add(rootPanel);

                    break; 
                case SectionViewModel sectionVm:

                    // Outer wrapper: label + inner panel
                    var wrapper = new StackPanel
                    {
                        Orientation = Orientation.Vertical,
                        Margin = new Thickness(4)
                    };
                    if (!string.IsNullOrEmpty(sectionVm.Label))
                    {
                        wrapper.Children.Add(new TextBlock
                        {
                            Text = sectionVm.Label,
                            FontWeight = Windows.UI.Text.FontWeights.Bold,
                            Margin = new Thickness(0, 0, 0, 4)
                        });
                    }
                    // Inner panel based on JSON layout (Grid or StackPanel)
                    var innerPanel = CreatePanelForLayout(sectionVm.PanelLayout);
                    wrapper.Children.Add(innerPanel);
                    foreach (var child in sectionVm.Children)
                    {
                        RenderElement(child, innerPanel);
                    }
                    ApplyChildLayout(sectionVm, parent, wrapper);
                    parent.Children.Add(wrapper);
                    break;

                case RepeaterViewModel repeaterVm:
                    var repStack = new StackPanel
                    {
                        Orientation = Orientation.Vertical,
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
                            Orientation = Orientation.Vertical,
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
                    parent.Children.Add(repStack);
                    break;

                case FieldViewModel fieldVm:

                    control = CreateFieldControl(fieldVm);
                    if (control != null)
                    {
                        ApplyChildLayout(fieldVm, parent, control);
                    }
                    break;

                case ActionViewModel actionVm:

                    control = CreateActionControl(actionVm);
                    if (control != null)
                    {
                        ApplyChildLayout(actionVm, parent, control);
                    }
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
            bool hasBinding = !string.IsNullOrEmpty(vm.BindingPath);
            if (vm.ControlType == "TextDisplay" && !hasBinding)
            {
                return new TextBlock
                {
                    Text = vm.Label,
                    FontSize = 16,
                    Margin = new Thickness(8, 0, 8, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
            }
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
                switch (vm.ActionType)
                {
                    case "SaveNewEntry":
                        // existing entry-row save
                        await HandleSaveNewEntryAsync(vm);
                        break;
                    case "EnterEditMode":
                        // Row-level Edit: set isEditing = true for this row's JSON object
                        vm.DataContext.SetValue("isEditing", true);
                        // Rebuild VM + rerender so VisibleWhen / edit controls take effect
                        ViewModel = new DynamicFormViewModel(_formDefinition, _dataContext);
                        RenderForm();
                        break;
                    case "SaveRowEdit":
                        await HandleSaveRowEditAsync(vm);
                        break;
                    default:
                        // fallback: use Command if you later want
                        if (vm.Command != null && vm.Command.CanExecute(null))
                            vm.Command.Execute(null);
                        break;
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
            else if (root is RepeaterViewModel repeaterVm)
            {
                foreach (var item in repeaterVm.Items)
                {
                    foreach (var child in item.Children)
                    {
                        foreach (var c in GetAllFields(child))
                            yield return c;
                    }
                }
            }
        }

        private IEnumerable<FieldViewModel> GetFieldsForSameContext(FormDataContext ctx)
        {
            return GetAllFields(ViewModel.Root)
                .Where(f => ReferenceEquals(f.DataContext, ctx));
        }

        private async System.Threading.Tasks.Task HandleSaveRowEditAsync(ActionViewModel actionVm)
        {
            // 1. Get all fields that belong to this row (same FormDataContext)
            var rowFields = GetFieldsForSameContext(actionVm.DataContext).ToList();
            if (rowFields.Count == 0)
                return;
            // 2. Optional: validate required fields in this row
            foreach (var f in rowFields)
            {
                if (!f.IsRequired)
                    continue;
                var val = f.Value?.ToString();
                if (string.IsNullOrWhiteSpace(val))
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"Row validation failed: {f.Label} is required. Message: {f.ValidationErrorMessage}");
                    return; // cancel save for this row
                }
            }
            // 3. Flip isEditing = false for this row's JSON object
            // Because actionVm.DataContext is a row context (basePath "data.logs[i]"),
            // "isEditing" resolves to "data.logs[i].isEditing"
            actionVm.DataContext.SetValue("isEditing", false);
            // 4. Rebuild VM + rerender so:
            // - display fields become visible again
            // - edit controls & row Save button hide
            ViewModel = new DynamicFormViewModel(_formDefinition, _dataContext);
            RenderForm();
            // Note: no need to "copy" values, the edit controls wrote directly into this row's JSON
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

        private Panel CreatePanelForLayout(PanelLayoutDefinition layout)
        {
            // Default: horizontal stack
            if (layout == null || string.IsNullOrEmpty(layout.PanelType) ||
                layout.PanelType.Equals("StackPanel", StringComparison.OrdinalIgnoreCase))
            {
                return new StackPanel
                {
                    Orientation = Orientation.Horizontal
                };
            }
            if (layout.PanelType.Equals("Grid", StringComparison.OrdinalIgnoreCase))
            {
                var grid = new Grid();
                if (layout.RowDefinitions != null)
                {
                    foreach (var r in layout.RowDefinitions)
                    {
                        grid.RowDefinitions.Add(new RowDefinition
                        {
                            Height = ParseGridLength(r.Height)
                        });
                    }
                }
                if (layout.ColumnDefinitions != null)
                {
                    foreach (var c in layout.ColumnDefinitions)
                    {
                        grid.ColumnDefinitions.Add(new ColumnDefinition
                        {
                            Width = ParseGridLength(c.Width)
                        });
                    }
                }
                return grid;
            }
            // Fallback
            return new StackPanel
            {
                Orientation = Orientation.Horizontal
            };
        }
        private GridLength ParseGridLength(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return GridLength.Auto;
            value = value.Trim();
            if (value.Equals("Auto", StringComparison.OrdinalIgnoreCase))
                return GridLength.Auto;
            if (value.EndsWith("*"))
            {
                var starPart = value.Substring(0, value.Length - 1);
                if (double.TryParse(starPart, out double weight))
                    return new GridLength(weight, GridUnitType.Star);
                // "*" or invalid number → treat as 1*
                return new GridLength(1, GridUnitType.Star);
            }
            if (double.TryParse(value, out double pixel))
                return new GridLength(pixel);
            // fallback
            return GridLength.Auto;
        }

        private void ApplyChildLayout(ElementViewModel vm, Panel parent, FrameworkElement element)
        {
            if (vm?.ChildLayout == null || element == null)
                return;

            ApplyLayoutToElement(element, vm.ChildLayout, parent);
        }

        private static void ApplyLayoutToElement(
    FrameworkElement element,
    ChildLayoutDefinition layout,
    Panel parent)
        {
            if (layout == null || element == null)
                return;

            // --- Grid row/column/span ---
            if (parent is Grid)
            {
                if (layout.GridRow.HasValue)
                    Grid.SetRow(element, layout.GridRow.Value);

                if (layout.GridColumn.HasValue)
                    Grid.SetColumn(element, layout.GridColumn.Value);

                if (layout.GridRowSpan.HasValue)
                    Grid.SetRowSpan(element, layout.GridRowSpan.Value);

                if (layout.GridColumnSpan.HasValue)
                    Grid.SetColumnSpan(element, layout.GridColumnSpan.Value);
            }

            // --- Alignment ---
            if (!string.IsNullOrWhiteSpace(layout.HorizontalAlignment) &&
                Enum.TryParse(layout.HorizontalAlignment, true, out HorizontalAlignment hAlign))
            {
                element.HorizontalAlignment = hAlign;
            }

            if (!string.IsNullOrWhiteSpace(layout.VerticalAlignment) &&
                Enum.TryParse(layout.VerticalAlignment, true, out VerticalAlignment vAlign))
            {
                element.VerticalAlignment = vAlign;
            }

            // --- Margin ("left,top,right,bottom") ---
            if (!string.IsNullOrWhiteSpace(layout.Margin))
            {
                var parts = layout.Margin.Split(',');
                if (parts.Length == 4 &&
                    double.TryParse(parts[0], out double left) &&
                    double.TryParse(parts[1], out double top) &&
                    double.TryParse(parts[2], out double right) &&
                    double.TryParse(parts[3], out double bottom))
                {
                    element.Margin = new Thickness(left, top, right, bottom);
                }
            }

            // --- Explicit width/height ---
            if (layout.Width.HasValue)
                element.Width = layout.Width.Value;

            if (layout.Height.HasValue)
                element.Height = layout.Height.Value;
        }

    }
}