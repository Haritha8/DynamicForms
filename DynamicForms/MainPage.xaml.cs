using DynamicForms.Models.Definitions;
using DynamicForms.Services;
using DynamicForms.ViewModels;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace DynamicForms
{
    public sealed partial class MainPage : Page
    {
        public MainPageViewModel ViewModel { get; private set; }

        private FormActionService _formActions = new FormActionService();

        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel = new MainPageViewModel();
            DataContext = ViewModel;

            await ViewModel.InitializeAsync();

            // Existing behavior: render form from ViewModel.DynamicForm.Root
            RenderForm();
        }

        private void RenderForm()
        {
            FormRootPanel.Children.Clear();

            if (ViewModel?.DynamicForm?.Root == null)
                return;

            RenderElement(ViewModel.DynamicForm.Root, FormRootPanel);
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
                            Background = new SolidColorBrush(Windows.UI.Colors.WhiteSmoke),
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
                Height = 32,
                Background = new SolidColorBrush(Windows.UI.Colors.Teal)
            };
            button.Click += async (s, e) =>
            {
                switch (vm.ActionType)
                {
                    case "SaveNewEntry":
                        await ViewModel.HandleSaveNewEntryAsync(vm);
                        // Rebuild VM from same data & re-render
                        ViewModel.RebuildDynamicForm();
                        RenderForm();
                        break;

                    case "EnterEditMode":
                        await ViewModel.HandleEnterEditModeAsync(vm);
                        ViewModel.RebuildDynamicForm();
                        RenderForm();
                        break;

                    case "SaveRowEdit":
                        await ViewModel.HandleSaveRowEditAsync(vm);
                        ViewModel.RebuildDynamicForm();
                        RenderForm();
                        break;

                    default:
                        // optional: vm.Command, if you decide to keep it
                        break;
                }
            };
            container.Children.Add(button);
            return container;
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