﻿using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using EnvDTE80;
using System.IO;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.VCProjectEngine;
using System.Collections;
using System.Diagnostics;


namespace conan_vs_extension
{
    public class Component
    {
        public string cmake_target_name { get; set; }
    }

    public class Library
    {
        public string cmake_file_name { get; set; }
        public string cmake_target_name { get; set; }
        public string description { get; set; }
        public List<string> license { get; set; }
        public bool v2 { get; set; }
        public List<string> versions { get; set; }
        public Dictionary<string, Component> components { get; set; } = new Dictionary<string, Component>();
    }

    public class RootObject
    {
        public long date { get; set; }
        public Dictionary<string, Library> libraries { get; set; }
    }

    /// <summary>
    /// Interaction logic for ConanToolWindowControl.
    /// </summary>
    public partial class ConanToolWindowControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConanToolWindowControl"/> class.
        /// </summary>
        public ConanToolWindowControl()
        {
            this.InitializeComponent();
            LibraryHeader.Visibility = Visibility.Collapsed;
            myWebBrowser.Visibility = Visibility.Collapsed;
            Task.Run(() => LoadLibrariesFromJsonAsync());
        }

        private RootObject jsonData;

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterListView(SearchTextBox.Text);
        }

        private async Task LoadLibrariesFromJsonAsync()
        {
            string url = "https://raw.githubusercontent.com/conan-io/conan-clion-plugin/develop2/src/main/resources/conan/targets-data.json";
            using (var httpClient = new HttpClient())
            {
                var json = await httpClient.GetStringAsync(url);
                jsonData = JsonConvert.DeserializeObject<RootObject>(json);

                Dispatcher.Invoke(() =>
                {
                    PackagesListView.Items.Clear();
                    foreach (var library in jsonData.libraries.Keys)
                    {
                        PackagesListView.Items.Add(library);
                    }
                });
            }
        }

        private void FilterListView(string searchText)
        {
            if (jsonData == null || jsonData.libraries == null) return;

            PackagesListView.Items.Clear();

            var filteredLibraries = jsonData.libraries
                .Where(kv => kv.Key.Contains(searchText))
                .ToList();

            foreach (var library in filteredLibraries)
            {
                PackagesListView.Items.Add(library.Key);
            }
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PackagesListView.SelectedItem is string selectedItem)
            {
                var htmlContent = GenerateHtml(selectedItem);
                myWebBrowser.NavigateToString(htmlContent);
            }
        }

        public void UpdatePanel(string name, string description, string licenses, List<string> versions)
        {
            LibraryNameLabel.Content = name;
            VersionsComboBox.ItemsSource = versions;
            VersionsComboBox.SelectedIndex = 0;

            DescriptionTextBlock.Text = description ?? "No description available.";
            LicenseText.Text = licenses ?? "No description available.";

            InstallButton.Visibility = Visibility.Visible;
            RemoveButton.Visibility = Visibility.Collapsed;

            LibraryHeader.Visibility = Visibility.Visible;
            myWebBrowser.Visibility = Visibility.Visible;

        }

        private void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedLibrary = LibraryNameLabel.Content.ToString();
            var selectedVersion = VersionsComboBox.SelectedItem.ToString();

            MessageBox.Show($"Installing {selectedLibrary} version {selectedVersion}");

            InstallButton.Visibility = Visibility.Collapsed;
            RemoveButton.Visibility = Visibility.Visible;
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedLibrary = LibraryNameLabel.Content.ToString();
            var selectedVersion = VersionsComboBox.SelectedItem.ToString();

            MessageBox.Show($"Removing {selectedLibrary} version {selectedVersion}");

            InstallButton.Visibility = Visibility.Visible;
            RemoveButton.Visibility = Visibility.Collapsed;
        }


        private string GenerateHtml(string name)
        {
            if (jsonData == null || !jsonData.libraries.ContainsKey(name)) return "";

            var library = jsonData.libraries[name];
            var versions = library.versions;
            var description = library.description ?? "No description available.";
            var licenses = library.license != null ? string.Join(", ", library.license) : "No license information.";
            var cmakeFileName = library.cmake_file_name ?? name;
            var cmakeTargetName = library.cmake_target_name ?? $"{name}::{name}";
            var warningSection = !library.v2 ? "<div class='warning'>Warning: This library is not compatible with Conan v2.</div>" : string.Empty;

            UpdatePanel(name, description, licenses, versions);

            var additionalInfo = $@"
        <p>Please, be aware that this information is generated automatically and it may contain some mistakes. If you have any problem, you can check the <a href='https://github.com/conan-io/conan-center-index/tree/master/recipes/{name}' target='_blank'>upstream recipe</a> to confirm the information. Also, for more detailed information on how to consume Conan packages, please <a href='https://docs.conan.io/2/tutorial/consuming_packages.html' target='_blank'>check the Conan documentation</a>.</p>";

            var componentsSection = string.Empty;
            if (library.components != null && library.components.Count > 0)
            {
                componentsSection += "<h2>Declared components for " + name + "</h2>";
                componentsSection += "<p>This library declares components, so you can use the components targets in your project instead of the global target. There are the declared CMake target names for the library's components:<br><ul>";
                foreach (var component in library.components)
                {
                    var componentCmakeTargetName = component.Value.cmake_target_name ?? $"{name}::{component.Key}";
                    componentsSection += $"<li>{component.Key}: <code>{componentCmakeTargetName}</code></li>";
                }
                componentsSection += "</ul></p>";
            }

            var htmlTemplate = $@"
<html>
<head>
    <style>
        body {{ font-family: 'Roboto', sans-serif; }}
        .code {{ background-color: lightgray; padding: 10px; border-radius: 5px; overflow: auto; white-space: pre; }}
        .warning {{ background-color: yellow; padding: 10px; }}
    </style>
</head>
<body>
    {warningSection}
    <h2>Using {name} with CMake</h2>
<pre class='code'>
# First, tell CMake to find the package.
find_package({cmakeFileName})

# Then, link your executable or library with the package target.
target_link_libraries(your_target_name PRIVATE {cmakeTargetName})
</pre>
    {additionalInfo}
    {componentsSection}
</body>
</html>";
            return htmlTemplate;
        }




        /// <summary>
        /// Handles click on the button by displaying a message box.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions", Justification = "Sample code")]
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Default event handler naming pattern")]

        private void ShowConfigurationDialog()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            DTE dte = Package.GetGlobalService(typeof(DTE)) as DTE;

            if (dte != null)
            {
                dte.ExecuteCommand("Tools.Options", GuidList.strConanOptionsPage);
            }

        }

        private void Configuration_Click(object sender, RoutedEventArgs e)
        {
            ShowConfigurationDialog();
        }

        private void ShowPackages_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DTE2 dte = (DTE2)ServiceProvider.GlobalProvider.GetService(typeof(DTE));
                if (dte != null && dte.Solution != null && dte.Solution.Projects != null)
                {
                    string projectDirectory = System.IO.Path.GetDirectoryName(dte.Solution.FullName);
                    string conanProjectDirectory = System.IO.Path.Combine(projectDirectory, ".conan");

                    if (!Directory.Exists(conanProjectDirectory))
                    {
                        Directory.CreateDirectory(conanProjectDirectory);
                    }

                    string fileName = "conan.config.json";
                    string filePath = System.IO.Path.Combine(conanProjectDirectory, fileName);

                    if (!File.Exists(filePath))
                    {
                        String toolset = "";
                        foreach (Project project in dte.Solution.Projects)
                        {
                            if (project.Object is VCProject vcProject)
                            {

                                foreach (VCConfiguration vcConfig in (IEnumerable)vcProject.Configurations)
                                {
                                    // TODO: Make the proper conversion from toolset to msvc conan versioning
                                    toolset = vcConfig.Evaluate("$(PlatformToolset)").ToString();
                                    // var cppstd = vcConfig.Evaluate("$(ClCompile-LanguageStandard)");
                                    break;
                                }
                            }
                        }

                        var conan_config = new Dictionary<string, Dictionary<string, string>>();
                        string releaseProfileName = "release_x86_64";
                        string debugProfileName = "debug_x86_64";
                        conan_config["configurations"] = new Dictionary<string, string>();
                        conan_config["configurations"]["Release|x86_64"] = releaseProfileName;
                        conan_config["configurations"] = new Dictionary<string, string>();
                        conan_config["configurations"]["Debug|x86_64"] = debugProfileName;
                        var jsonContent = JsonConvert.SerializeObject(conan_config, Formatting.Indented);
                        File.WriteAllText(filePath, jsonContent);

                        string releaseProfilePath = System.IO.Path.Combine(conanProjectDirectory, releaseProfileName);
                        string releaseProfileContent = "[settings]\narch=x86_64\nbuild_type=Release\ncompiler=msvc\ncompiler.cppstd=14\ncompiler.runtime=dynamic\n" +
                            $"compiler.runtime_type=Release\ncompiler.version={toolset}\nos=Windows";
                        File.WriteAllText(releaseProfilePath, releaseProfileContent);

                        string debugProfilePath = System.IO.Path.Combine(conanProjectDirectory, debugProfileName);
                        string debugProfileContent = "[settings]\narch=x86_64\nbuild_type=Debug\ncompiler=msvc\ncompiler.cppstd=14\ncompiler.runtime=dynamic\n" +
                            $"compiler.runtime_type=Release\ncompiler.version={toolset}\nos=Windows";
                        File.WriteAllText(debugProfilePath, debugProfileContent);

                        MessageBox.Show($"Generated '{fileName}' for actual project.", "Conan config file generated", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show($"Conan config file '{fileName}' found for actual project.", "Conan config file found", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    MessageBox.Show("Could not get current active project", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"There was a problem generating the file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
    string.Format(System.Globalization.CultureInfo.CurrentUICulture, "Invoked '{0}'", this.ToString()),
    "Conan C/C++ Package Manager");
        }
    }
}