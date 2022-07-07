using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

using Ookii.Dialogs.Wpf;

namespace MarlinEasyConfig
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<ConfigParameter> configParameters = new ObservableCollection<ConfigParameter>();

        string[] parameterIgnoreList = new string[] { "CONFIGURATION_H_VERSION", "CONFIGURATION_ADV_H_VERSION" };
        string[] externalParameterList = new string[] { "HIGH", "LOW", "DISABLE", "ENABLE",
            "X_CENTER", "Y_CENTER", "XY_CENTER",
            "JAPANESE", "WESTERN", "CYRILLIC",
            "NEO_GRBW", "NEO_GRB",
            "DXC_AUTO_PARK_MODE",
            "_XMAX_", "_YMAX_", "_ZMAX_",
            "F_CPU", "SDSS", "SD_DETECT_PIN", "SV_SD_ONBOARD", "SV_USB_FLASH_DRIVE",
            "CHOPPER_DEFAULT_12V", "CHOPPER_DEFAULT_19V", "CHOPPER_DEFAULT_24V", "CHOPPER_DEFAULT_36V", "CHOPPER_09STEP_24V", "CHOPPER_PRUSAMK3_24V", "CHOPPER_MARLIN_11"
        };

        string currentMarlinConfig;
        string currentMarlinConfigAdv;

        public MainWindow()
        {
            InitializeComponent();
            CompareColumn.Visibility = Visibility.Collapsed;
        }

        private void Menu_OpenMarlin(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog
            {
                Description = "Please select a Marlin folder.",
                UseDescriptionForTitle = true
            };

            if ((bool)dialog.ShowDialog(this))
            {
                bool doCompare = ((MenuItem)sender).Name == "MenuItem_Compare";
                var configFolder = GetMarlinConfigFolder(dialog.SelectedPath);
                var configFile = CheckConfigFile(configFolder);
                if (configFile != null)
                {
                    if (doCompare) CompareConfig(configFile);
                    else ParseConfig(configFile);

                    var configFileAdv = CheckConfigFile(configFolder, true);
                    if (configFileAdv != null)
                    {
                        if (doCompare) CompareConfig(configFileAdv, true);
                        else ParseConfig(configFileAdv, true);
                    }

                    // update source if needed
                    if (!doCompare)
                    {
                        CompareColumn.Visibility = Visibility.Collapsed;
                        ConfigTable.ItemsSource = null;
                        ConfigTable.ItemsSource = configParameters;
                        MenuItem_Compare.IsEnabled = true;
                        MenuItem_Transfer.IsEnabled = false;

                        currentMarlinConfig = configFile;
                        currentMarlinConfigAdv = configFileAdv;
                    }
                    else
                    {
                        MenuItem_Compare.IsEnabled = true;
                        MenuItem_Transfer.IsEnabled = true;
                        CompareColumn.Visibility = Visibility.Visible;
                    }
                }
            }
        }

        private string GetMarlinConfigFolder(string path)
        {
            var platformFile = path + @"\platformio.ini";
            if (File.Exists(platformFile) && Directory.Exists(path + @"\Marlin"))
            {
                path += @"\Marlin";
            }

            return path;
        }

        private string CheckConfigFile(string path, bool advanced = false)
        {
            var configFile = @"\Configuration" + (advanced ? "_adv" : null) + @".h";
            var configFilePath = path + configFile;
            if (File.Exists(configFilePath)) return configFilePath;

            MessageBox.Show(this, $"Sorry! This does not seem to be a Marlin directory:{Environment.NewLine}{path} does not include \"{configFile}\".", "No Marlin Folder");
            return null;
        }

        private string ReadConfigFile(string file)
        {
            Regex regexMultiline = new Regex(@"\\\n\s*");
            return regexMultiline.Replace(File.ReadAllText(file), "");
        }
        private string[] ReadConfigFileLines(string file)
        {
            Regex regexMultiline = new Regex(@"\\\n\s*");
            return regexMultiline.Replace(File.ReadAllText(file), "").Split('\n');
        }

        private void ParseConfig(string configFile, bool advanced = false)
        {
            string[] configLines = ReadConfigFileLines(configFile);
            foreach (var line in configLines)
            {
                var cleanedLine = line.TrimStart();
                if (!cleanedLine.StartsWith("#define")) continue;

                var configLine = new ConfigParameter(cleanedLine, advanced);
                if (!parameterIgnoreList.Contains(configLine.Name))
                {
                    configLine.Index = configParameters.Count;
                    configParameters.Add(configLine);
                }
            }
        }

        private void CompareConfig(string configFile, bool advanced = false)
        {
            string[] configLines = ReadConfigFileLines(configFile);
            foreach (var line in configLines)
            {
                var cleanedLine = line.TrimStart();
                if (!cleanedLine.StartsWith("#define")) continue;

                var compareConfigParameter = new ConfigParameter(cleanedLine, advanced);
                var existingConfigParameterIndex = configParameters.FindIndex(c => c.Name == compareConfigParameter.Name);
                
                if (existingConfigParameterIndex >= 0 && configParameters[existingConfigParameterIndex].CompareTo(compareConfigParameter) == -1)
                {
                    var existingConfigParameter = configParameters[existingConfigParameterIndex];
                    existingConfigParameter.IsDifferent = true;
                    existingConfigParameter.DifferentValue = compareConfigParameter.CleanValue(compareConfigParameter.Value);
                    configParameters[existingConfigParameterIndex] = existingConfigParameter;
                }
            }

        }

        private bool ParameterExists(string parameterName)
        {
            return configParameters.FindIndex(p => p.Name == parameterName) >= 0 || externalParameterList.Contains(parameterName);
        }

        public bool IsMathFormula(string expr)
        {
            var split = expr.Split(" ");

            Regex regexVariable = new Regex(@"^[A-Z0-9_]*$");
            Regex regexMathAndNum = new Regex(@"^[\d\(\)\+\-\*\/\.]*$");

            foreach (var item in split)
            {
                var cleanedItem = item.ReplaceAll(new[] { "(", ")", "+", "-", "*", "/" }, "");
                if (regexMathAndNum.IsMatch(cleanedItem)) continue;
                if (regexVariable.IsMatch(cleanedItem) && !ParameterExists(cleanedItem))
                {
                    return false;
                }
            }

            return true;
        }
        private bool IsFloatOrInt(string value) => int.TryParse(value, out int intValue) || float.TryParse(value, out float floatValue) || IsMathFormula(value);

        private void ConfigTable_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                var column = e.Column as DataGridBoundColumn;
                if (column != null)
                {
                    var bindingSource = (column.Binding as Binding).Path;
                    var bindingPath = (column.Binding as Binding).Path.Path;
                    if (bindingPath == "Value")
                    {
                        int rowIndex = ((ConfigParameter)e.Row.Item).Index;//e.Row.GetIndex();
                        var el = e.EditingElement as TextBox;
                        var elText = el.Text.Trim();
                        var parameter = configParameters[rowIndex];
                        switch (parameter.Type)
                        {
                            case ParameterType.String:
                                //el.Text = elText.Trim(new[] { '"', '\'' });
                                break;
                            case ParameterType.Array:
                                if (!new Regex(@"^{.+}").Match(elText).Success && !ParameterExists(elText))
                                {
                                    MessageBox.Show(this, $"Sorry! {parameter.Name} is of type array:{Environment.NewLine}Value needs to be surrounded by \"{{\" and \"}}\"  or another parameter.", "Value Error");
                                    el.Text = parameter.Value;
                                    e.Cancel = true;
                                }
                                else
                                {
                                    el.Text = parameter.CleanValue(elText);
                                }
                                break;
                            case ParameterType.Boolean:
                                var lowerVal = elText.ToLower();
                                if (lowerVal != "false" && lowerVal != "true" && !ParameterExists(elText))
                                {
                                    MessageBox.Show(this, $"Sorry! {parameter.Name} is of type bool:{Environment.NewLine}Only \"true\", \"false\" or another parameter accepted.", "Value Error");
                                    el.Text = parameter.Value;
                                    e.Cancel = true;
                                }
                                else
                                {
                                    el.Text = parameter.CleanValue(lowerVal);
                                }
                                break;
                            case ParameterType.Definition:
                                if (!string.IsNullOrEmpty(elText))
                                {
                                    MessageBox.Show(this, $"Sorry! {parameter.Name} is only a definition:{Environment.NewLine}No value allowed.", "Value Error");
                                    el.Text = null;
                                    e.Cancel = true;
                                }
                                break;
                            case ParameterType.Number:
                                var newVal = elText.Replace(",", ".");
                                Regex regexMathSpacers = new Regex(@"(\s*([\(\)\+\-\*\/])\s*)");
                                newVal = regexMathSpacers.Replace(newVal.Trim(), " $2 ").Trim();
                                if (!IsFloatOrInt(newVal) && !ParameterExists(elText))
                                {
                                    MessageBox.Show(this, $"Sorry! {parameter.Name} is of type Number:{Environment.NewLine}Only integers, floats, other parameters and mathematic expressions accepted.", "Value Error");
                                    el.Text = parameter.Value;
                                    e.Cancel = true;
                                }
                                else
                                {
                                    // remove whitespace if value is a single number with a sign (like a negative integer)
                                    if (newVal.Count(char.IsWhiteSpace) == 1) newVal = newVal.Replace(" ", "");
                                    // finally set new value
                                    el.Text = parameter.CleanValue(newVal);
                                }
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
        }

        private void ContextMenuCopyClicked(object sender, RoutedEventArgs e, ConfigParameter config)
        {
            if (config != null) Clipboard.SetData("MarlinEasyConfigParameter", config);
        }

        private void ContextMenuPasteClicked(object sender, RoutedEventArgs e, DataGridRow gridRow)
        {
            if (Clipboard.ContainsData("MarlinEasyConfigParameter"))
            {
                var rowIndex = gridRow.GetIndex();
                if (rowIndex < 0) return;

                var configNamePasted = (Clipboard.GetData("MarlinEasyConfigParameter") as ConfigParameter).Name;
                var configParamByIndex = configParameters[rowIndex];
                if (configParamByIndex.Name == configNamePasted) return;

                configParamByIndex.Value = configNamePasted;
                configParameters[rowIndex] = configParamByIndex;
                gridRow.Item = configParamByIndex;
                ConfigTable.Items.Refresh();
            }
        }

        private void ConfigTable_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            DataGridCell cell;
            DataGridRow row;

            var dep = DataGridMiscHelpers.FindVisualParentAsDataGridSubComponent((DependencyObject)e.OriginalSource);
            if (dep == null)
            {
                e.Handled = true;
                return;
            }

            DataGridMiscHelpers.FindCellAndRow(dep, out cell, out row);
            string header = cell?.Column?.Header.ToString();
            if (dep is DataGridColumnHeader || dep is DataGridRow || (header != "Name" && header != "Value"))
            {
                e.Handled = true;
                return;
            }

            ContextCopy.IsEnabled = header == "Name";
            ContextPaste.IsEnabled = header == "Value" && Clipboard.ContainsData("MarlinEasyConfigParameter");

            ContextCopy.Click += (sender, e) => ContextMenuCopyClicked(sender, e, row.Item as ConfigParameter);
            ContextPaste.Click += (sender, e) => ContextMenuPasteClicked(sender, e, row);
        }

        private void Menu_Transfer(object sender, RoutedEventArgs e)
        {
            foreach (var param in configParameters)
            {
                if (!string.IsNullOrEmpty(param.DifferentValue) && param.Value != param.DifferentValue)
                {
                    param.Value = param.DifferentValue;
                }
            }
            ConfigTable.Items.Refresh();
        }

        private bool ReplaceInConfig(string configFile)
        {
            if (!string.IsNullOrEmpty(configFile))
            {
                string conf = ReadConfigFile(configFile);
                foreach (var parameter in configParameters)
                {
                    // ignore multiline commented defines (* in front), match name of variable, value and comment (if available)
                    string pattern = @"^(\s*#define\s+)(" + parameter.Name + @")(?:\s+)(.*?)(?: *(/{2}.+)|\n)"; // {1} = define, {2} = variable, {3} = value, {4} = comment
                    var newValue = parameter.Value;
                    if (parameter.Type == ParameterType.Definition) continue;
                    if (parameter.Type == ParameterType.String) newValue = '"' + parameter.CleanValue(newValue).TrimEnd('\\') + '"';
                    conf = Regex.Replace(conf, pattern, m => string.IsNullOrEmpty(m.Groups[3].Value) ? m.Value : m.Value?.ReplaceFirst(m.Groups[3].Value, newValue ), RegexOptions.Multiline);
                }
                File.WriteAllText(configFile.Replace(".h", "_new.h"), conf);
                return true;
            }
            return false;
        }

        private void MenuItem_Save(object sender, RoutedEventArgs e)
        {
            // regular config file
            ReplaceInConfig(currentMarlinConfig);

            // advanced config file
            ReplaceInConfig(currentMarlinConfigAdv);
        }

        private void Input_Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            var txt = (TextBox)sender;
            var txtSearch = txt.Text.ToLower();
            foreach (var param in configParameters)
            {
                param.IsFiltered = !param.Name.ToLower().StartsWith(txtSearch);
            }
            ConfigTable.Items.Refresh();
        }

        private void MenuItem_Info(object sender, RoutedEventArgs e)
        {
            string messageBoxCaption = "Information";
            string messageBoxText = string.Join(Environment.NewLine + Environment.NewLine,
                            $"MarlinEasyConfig is created by 48DESIGN GmbH - New Media Agency, Karlsruhe, Germany. More information about us can be found at vierachtdesign.com",
                            "The source code can be found at github.com/48design/marlineasyconfig",
                            "MarlinEasyConfig uses the class library \"Ookii.Dialogs.Wpf\" for the directory dialog which is licensed under BSDv3",
                            "The open source firmware Marlin is licensed under GPLv3");

            if (TaskDialog.OSSupportsTaskDialogs)
            {
                using (TaskDialog dialog = new TaskDialog())
                {
                    dialog.WindowTitle = messageBoxCaption;
                    dialog.MainInstruction = "This is an example task dialog.";
                    dialog.Content = messageBoxText;
                    
                    dialog.ExpandedControlText = "Show licences";
                    dialog.ExpandedInformation = "Ookii.org's Task Dialog doesn't just provide a wrapper for the native Task Dialog API; it is designed to provide a programming interface that is natural to .Net developers.";
                    
                    dialog.Footer = "The source code can be found at <a href=\"https://github.com/48design\">github.com/48design/marlineasyconfig</a>.";
                    dialog.FooterIcon = TaskDialogIcon.Information;
                    
                    dialog.EnableHyperlinks = true;
                    TaskDialogButton customButton = new TaskDialogButton("A custom button");
                    TaskDialogButton okButton = new TaskDialogButton(ButtonType.Ok);
                    TaskDialogButton cancelButton = new TaskDialogButton(ButtonType.Cancel);
                    dialog.Buttons.Add(customButton);
                    dialog.Buttons.Add(okButton);
                    
                    dialog.HyperlinkClicked += new EventHandler<HyperlinkClickedEventArgs>(TaskDialog_HyperLinkClicked);
                    
                    TaskDialogButton button = dialog.ShowDialog(this);
                    if (button == customButton)
                        MessageBox.Show(this, "You clicked the custom button", "Task Dialog Sample");
                    /*
                    else if (button == okButton)
                        MessageBox.Show(this, "You clicked the OK button.", "Task Dialog Sample");
                    */
                }
            }
            else
            {
                MessageBox.Show(messageBoxText, messageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.Yes);
            }
            
        }

        private void TaskDialog_HyperLinkClicked(object sender, HyperlinkClickedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Href,
                UseShellExecute = true
            });
        }

        private void Menu_Exit(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show($"Do you want to close MarlinEasyConfig?{Environment.NewLine}Unsaved changes may be lost.", "Exit", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
        }
    }
}
