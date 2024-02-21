using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;
using System.Collections;
using System.IO;
using System.Reflection;
using EnvDTE;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Threading;
using YamlDotNet.Core;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using VSLangProj;
using System.Windows.Navigation;

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

    public class Requirements
    {
        public Requirements(string[] requirements)
        {
            this.requirements = requirements;
        }
        public Requirements()
        {
            this.requirements = new string[] { };
        }
        public string[] requirements { get; set; }
    }

    /// <summary>
    /// Interaction logic for ConanToolWindowControl.
    /// </summary>
    public partial class ConanToolWindowControl : UserControl
    {
        private DTE _dte;
        private RootObject _jsonData;

        private string _modifyCommentGuard = "# This file is managed by the Conan Visual Studio Extension, contents will be overwritten.\n# To keep your changes, remove these comment lines, but the plugin won't be able to modify your requirements";

        /// <summary>
        /// Initializes a new instance of the <see cref="ConanToolWindowControl"/> class.
        /// </summary>
        public ConanToolWindowControl()
        {
            this.InitializeComponent();
            LibraryHeader.Visibility = Visibility.Collapsed;
            myWebBrowser.Visibility = Visibility.Collapsed;

            ToggleUIEnableState(IsConanInitialized());

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            _dte = (DTE)ServiceProvider.GlobalProvider.GetService(typeof(DTE));
            if (_dte == null)
            {
                throw new InvalidOperationException("Cannot access DTE service.");
            }

            await CopyJsonFileFromResourceIfNeededAsync();
            await LoadLibrariesFromJsonAsync();
        }
        private async Task CopyJsonFileFromResourceIfNeededAsync()
        {
            string userConanFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".conan-vs-extension");
            string jsonFilePath = Path.Combine(userConanFolder, "targets-data.json");

            if (!File.Exists(jsonFilePath))
            {
                if (!Directory.Exists(userConanFolder))
                {
                    Directory.CreateDirectory(userConanFolder);
                }

                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "conan_vs_extension.Resources.targets-data.json";
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                using (var reader = new StreamReader(stream))
                {
                    string jsonContent = await reader.ReadToEndAsync();
                    using (var writer = new StreamWriter(jsonFilePath))
                    {
                        await writer.WriteAsync(jsonContent);
                    }
                }
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterListView(LibrarySearchTextBox.Text);
        }

        private async Task LoadLibrariesFromJsonAsync()
        {
            string userConanFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".conan-vs-extension");
            string jsonFilePath = Path.Combine(userConanFolder, "targets-data.json");

            string json = await Task.Run(() => File.ReadAllText(jsonFilePath));
            _jsonData = JsonConvert.DeserializeObject<RootObject>(json);

            Dispatcher.Invoke(() =>
            {
                PackagesListView.Items.Clear();
                foreach (var library in _jsonData.libraries.Keys)
                {
                    PackagesListView.Items.Add(library);
                }
            });
        }

        private void FilterListView(string searchText)
        {
            if (_jsonData == null || _jsonData.libraries == null) return;

            PackagesListView.Items.Clear();

            var filteredLibraries = _jsonData.libraries
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

            ThreadHelper.ThrowIfNotOnUIThread();

            Array activeSolutionProjects = _dte.ActiveSolutionProjects as Array;
            Project activeProject = activeSolutionProjects.GetValue(0) as Project;

            string projectFilePath = activeProject.FullName;
            string projectDirectory = Path.GetDirectoryName(projectFilePath);

            var requirements = GetConandataRequirements(projectDirectory);
            bool isInstalled = requirements.Any(e => e.StartsWith(name + "/"));

            InstallButton.Visibility = isInstalled ? Visibility.Collapsed : Visibility.Visible;
            RemoveButton.Visibility = isInstalled ? Visibility.Visible : Visibility.Collapsed;

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

            ThreadHelper.ThrowIfNotOnUIThread();

            Project startupProject = ProjectConfigurationManager.GetStartupProject(_dte);

            if (startupProject.Object is VCProject vcProject)
            {
                string projectFilePath = startupProject.FullName;
                string projectDirectory = Path.GetDirectoryName(projectFilePath);

                WriteNecessaryConanGuardedFiles(projectDirectory);
                WriteNewRequirement(projectDirectory, selectedLibrary + "/" + selectedVersion);

                ProjectConfigurationManager.SaveConanPrebuildEventsAllConfig(startupProject);
            }
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedLibrary = LibraryNameLabel.Content.ToString();
            var selectedVersion = VersionsComboBox.SelectedItem.ToString();

            MessageBox.Show($"Removing {selectedLibrary} version {selectedVersion}");

            InstallButton.Visibility = Visibility.Visible;
            RemoveButton.Visibility = Visibility.Collapsed;

            ThreadHelper.ThrowIfNotOnUIThread();

            Array activeSolutionProjects = _dte.ActiveSolutionProjects as Array;
            Project activeProject = activeSolutionProjects.GetValue(0) as Project;

            string projectFilePath = activeProject.FullName;
            string projectDirectory = Path.GetDirectoryName(projectFilePath);

            RemoveRequirement(projectDirectory, selectedLibrary + "/" + selectedVersion);
        }


        private string GenerateHtml(string name)
        {
            if (_jsonData == null || !_jsonData.libraries.ContainsKey(name)) return "";

            var library = _jsonData.libraries[name];
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
            _dte.ExecuteCommand("Tools.Options", GuidList.strConanOptionsPage);
        }

        private bool IsConanInitialized()
        {
            bool initialized = GlobalSettings.ConanExecutablePath != null && GlobalSettings.ConanExecutablePath.Length > 0;
            return initialized;
        }

        private bool IsFileCommentGuarded(string path)
        {
            if (File.Exists(path))
            {
                string[] guardComment = _modifyCommentGuard.Split('\n');
                string[] fileContents = File.ReadAllLines(path);
                if (fileContents.Length > guardComment.Length && fileContents.AsSpan(0, guardComment.Length).SequenceEqual(guardComment))
                {
                    return true;
                }
            }
            return false;
        }

        private void WriteConanfileIfNecessary(string projectDirectory)
        {
            string path = Path.Combine(projectDirectory, "conanfile.py");
            if (!IsFileCommentGuarded(path))
            {
                StreamWriter conanfileWriter = File.CreateText(path);
                conanfileWriter.Write(_modifyCommentGuard +  "\n");

                conanfileWriter.Write(@"
from conan import ConanFile
from conan.tools.microsoft import vs_layout, MSBuildDeps
class ConanApplication(ConanFile):
    package_type = ""application""
    settings = ""os"", ""compiler"", ""build_type"", ""arch""

    def layout(self):
        vs_layout(self)

    def generate(self):
        deps = MSBuildDeps(self)
        deps.generate()

    def requirements(self):
        requirements = self.conan_data.get('requirements', [])
        for requirement in requirements:
            self.requires(requirement)");
                conanfileWriter.Close();
            }
        }

        private void WriteConandataIfNecessary(string projectDirectory)
        {
            string path = Path.Combine(projectDirectory, "conandata.yml");
            if (!IsFileCommentGuarded(path))
            {
                StreamWriter conandataWriter = File.CreateText(path);
                conandataWriter.Write(_modifyCommentGuard + "\n");

                conandataWriter.Write("requirements:\n");

                conandataWriter.Close();
            }
        }

        private void WriteNecessaryConanGuardedFiles(string projectDirectory)
        {
            WriteConanfileIfNecessary(projectDirectory);
            WriteConandataIfNecessary(projectDirectory);
        }

        private string[] GetConandataRequirements(string projectDirectory)
        {
            string path = Path.Combine(projectDirectory, "conandata.yml");
            if (IsFileCommentGuarded(path))
            {
                string[] conandataContents = File.ReadAllLines(path);

                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .Build();

                var result = deserializer.Deserialize<Requirements>(string.Join("\n", conandataContents));

                if (result.requirements != null)
                {
                    return result.requirements;
                }
            }
            return new string[] { };
        }

        private void WriteNewRequirement(string projectDirectory, string newRequirement)
        {
            string path = Path.Combine(projectDirectory, "conandata.yml");
            if (IsFileCommentGuarded(path))
            {
                string[] requirements = GetConandataRequirements(projectDirectory);
                if (!requirements.Contains(newRequirement))
                {
                    var newRequirements = requirements.Append(newRequirement);
                    var conandata = File.CreateText(path);
                    conandata.Write(_modifyCommentGuard + "\n");
                    var serializer = new SerializerBuilder()
                        .WithNamingConvention(UnderscoredNamingConvention.Instance)
                        .Build();
                    var yaml = serializer.Serialize(new Requirements(newRequirements.ToArray()));
                    conandata.Write(yaml);
                    conandata.Close();
                }
            }
        }

        private void RemoveRequirement(string projectDirectory, string oldRequirement)
        {
            string path = Path.Combine(projectDirectory, "conandata.yml");
            if (IsFileCommentGuarded(path))
            {
                string[] requirements = GetConandataRequirements(projectDirectory);
                if (requirements.Contains(oldRequirement))
                {
                    var newRequirements = requirements.Where(req => req != oldRequirement).ToArray();
                    var conandata = File.CreateText(path);
                    conandata.Write(_modifyCommentGuard + "\n");
                    var serializer = new SerializerBuilder()
                        .WithNamingConvention(UnderscoredNamingConvention.Instance)
                        .Build();
                    var yaml = serializer.Serialize(new Requirements(newRequirements));
                    conandata.Write(yaml);
                    conandata.Close();
                }
            }
        }

        private void ToggleUIEnableState(bool enabled)
        {
            LibrarySearchTextBox.IsEnabled = enabled;

            ShowPackagesButton.IsEnabled = enabled;
            UpdateButton.IsEnabled = enabled;

            PackagesListView.IsEnabled = enabled;
            LibraryHeader.IsEnabled = enabled;
            myWebBrowser.IsEnabled = enabled;
        }


        private void Configuration_Click(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            ShowConfigurationDialog();
            ToggleUIEnableState(true);

        }

        private void ShowPackages_Click(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

        }

        private async Task UpdateJsonDataAsync()
        {
            string jsonUrl = "https://raw.githubusercontent.com/conan-io/conan-clion-plugin/develop2/src/main/resources/conan/targets-data.json";

            string userConanFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".conan-vs-extension");
            string jsonFilePath = Path.Combine(userConanFolder, "targets-data.json");

            try
            {
                using (var httpClient = new HttpClient())
                {
                    string jsonContent = await httpClient.GetStringAsync(jsonUrl);

                    File.WriteAllText(jsonFilePath, jsonContent);

                    MessageBox.Show("Libraries data file updated.", "Libraries data file updated.", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating: {ex.Message}", "Error updating", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            _ = UpdateJsonDataAsync();
        }
    }

}