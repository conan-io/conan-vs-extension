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
using System.IO;
using System.Reflection;
using EnvDTE;
using Microsoft.VisualStudio.Threading;
using System.Windows.Navigation;

namespace conan_vs_extension
{
    public class Library
    {
        public string Description { get; set; }
        public List<string> License { get; set; }
        public List<string> Versions { get; set; }
    }

    public class RootObject
    {
        public Dictionary<string, Library> Libraries { get; set; }
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

        /// <summary>
        /// Initializes a new instance of the <see cref="ConanToolWindowControl"/> class.
        /// </summary>
        public ConanToolWindowControl()
        {
            this.InitializeComponent();
            LibraryHeader.Visibility = Visibility.Collapsed;

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
            ThreadHelper.ThrowIfNotOnUIThread();
            FilterListView(LibrarySearchTextBox.Text, ShowPackagesCheckbox.IsChecked ?? false);
        }

        private async Task LoadLibrariesFromJsonAsync()
        {
            string userConanFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".conan-vs-extension");
            string jsonFilePath = Path.Combine(userConanFolder, "targets-data.json");

            string json = await Task.Run(() => File.ReadAllText(jsonFilePath));
            _jsonData = JsonConvert.DeserializeObject<RootObject>(json);

            await ThreadHelper.JoinableTaskFactory.RunAsync(async delegate {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                PackagesListView.Items.Clear();
                foreach (var library in _jsonData.Libraries.Keys)
                {
                    PackagesListView.Items.Add(library);
                }
            });
        }

        private void FilterListView(string searchText, bool onlyInstalled)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_jsonData == null || _jsonData.Libraries == null) return;

            PackagesListView.Items.Clear();

            var filteredLibraries = _jsonData.Libraries
                .Where(kv => kv.Key.Contains(searchText))
                .ToList();
            Project startupProject = ProjectConfigurationManager.GetStartupProject(_dte);

            if (onlyInstalled && startupProject != null && startupProject.Object is VCProject vcProject)
            {
                string projectFilePath = startupProject.FullName;
                string projectDirectory = Path.GetDirectoryName(projectFilePath);

                var requirements = ConanFileManager.GetConandataRequirements(projectDirectory);
                foreach (var requirement in requirements)
                {
                    string key = requirement.Split('/')[0];
                    if (filteredLibraries.Any(library => library.Key == key))
                    {
                        PackagesListView.Items.Add(key);
                    }
                }
            }
            else
            {
                foreach (var library in filteredLibraries)
                {
                    PackagesListView.Items.Add(library.Key);
                }
            }

        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (PackagesListView.SelectedItem is string selectedItem)
            {
                UpdateLibraryInfo(selectedItem);
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Uri.AbsoluteUri);
            e.Handled = true;
        }

        public void UpdatePanel(string name, string description, string licenses, List<string> versions)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            LibraryNameLabel.Content = name;
            VersionsComboBox.ItemsSource = versions;
            VersionsComboBox.SelectedIndex = 0;

            DescriptionTextBlock.Text = description ?? "No description available.";
            LicenseText.Text = licenses ?? "No description available.";

            MoreInfoHyperlink.NavigateUri = new Uri($"https://conan.io/center/recipes/{name}");
            GitHubRecipeLink.NavigateUri = new Uri($"https://github.com/conan-io/conan-center-index/tree/master/recipes/{name}");

            Project startupProject = ProjectConfigurationManager.GetStartupProject(_dte);

            if (startupProject != null && startupProject.Object is VCProject vcProject)
            {
                string projectFilePath = startupProject.FullName;
                string projectDirectory = Path.GetDirectoryName(projectFilePath);

                var requirements = ConanFileManager.GetConandataRequirements(projectDirectory);
                bool isInstalled = requirements.Any(e => e.StartsWith(name + "/"));

                InstallButton.Visibility = isInstalled ? Visibility.Collapsed : Visibility.Visible;
                RemoveButton.Visibility = isInstalled ? Visibility.Visible : Visibility.Collapsed;
                VersionsComboBox.IsEnabled = !isInstalled;
                LibraryHeader.Visibility = Visibility.Visible;
                UnsupportedProjectType.Visibility = Visibility.Collapsed;
            }
            else
            {
                LibraryHeader.Visibility = Visibility.Collapsed;
                UnsupportedProjectType.Visibility = Visibility.Visible;
            }
        }

        private void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedLibrary = LibraryNameLabel.Content.ToString();
            var selectedVersion = VersionsComboBox.SelectedItem.ToString();

            MessageBox.Show($"Requirement {selectedLibrary}/{selectedVersion} added to conandata.yml", "Conan C/C++ Package Manager");

            InstallButton.Visibility = Visibility.Collapsed;
            RemoveButton.Visibility = Visibility.Visible;

            ThreadHelper.ThrowIfNotOnUIThread();

            Project startupProject = ProjectConfigurationManager.GetStartupProject(_dte);

            if (startupProject.Object is VCProject)
            {
                string projectFilePath = startupProject.FullName;
                string projectDirectory = Path.GetDirectoryName(projectFilePath);

                ConanFileManager.WriteNecessaryConanGuardedFiles(projectDirectory);
                ConanFileManager.WriteNewRequirement(projectDirectory, selectedLibrary + "/" + selectedVersion);

                _ = ProjectConfigurationManager.SaveConanPrebuildEventsAllConfigAsync(startupProject);
                VersionsComboBox.IsEnabled = false;
                FilterListView(LibrarySearchTextBox.Text, ShowPackagesCheckbox.IsChecked ?? false);
            }
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedLibrary = LibraryNameLabel.Content.ToString();
            var selectedVersion = VersionsComboBox.SelectedItem.ToString();

            MessageBox.Show($"Removing {selectedLibrary} version {selectedVersion}", "Conan C/C++ Package Manager");

            InstallButton.Visibility = Visibility.Visible;
            RemoveButton.Visibility = Visibility.Collapsed;

            ThreadHelper.ThrowIfNotOnUIThread();

            Array activeSolutionProjects = _dte.ActiveSolutionProjects as Array;
            Project activeProject = activeSolutionProjects.GetValue(0) as Project;

            string projectFilePath = activeProject.FullName;
            string projectDirectory = Path.GetDirectoryName(projectFilePath);

            ConanFileManager.RemoveRequirement(projectDirectory, selectedLibrary + "/" + selectedVersion);
            VersionsComboBox.IsEnabled = true;
            FilterListView(LibrarySearchTextBox.Text, ShowPackagesCheckbox.IsChecked ?? false);
        }


        private void UpdateLibraryInfo(string name)
        {
            if (_jsonData != null && _jsonData.Libraries.ContainsKey(name))
            {
                var library = _jsonData.Libraries[name];
                var versions = library.Versions;
                var description = library.Description ?? "No description available.";
                var licenses = library.License != null ? string.Join(", ", library.License) : "No license information.";

                UpdatePanel(name, description, licenses, versions);
            }
        }

        /// <summary>
        /// Handles click on the button by displaying a message box.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>

        private void ShowConfigurationDialog()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _dte.ExecuteCommand("Tools.Options", GuidList.strConanOptionsPage);
        }

        private bool IsConanInitialized()
        {
            return !string.IsNullOrEmpty(GlobalSettings.ConanExecutablePath);
        }

        private void ToggleUIEnableState(bool enabled)
        {
            LibrarySearchTextBox.IsEnabled = enabled;

            ShowPackagesCheckbox.IsEnabled = enabled;
            UpdateButton.IsEnabled = enabled;

            PackagesListView.IsEnabled = enabled;
            LibraryHeader.IsEnabled = enabled;

            if (!enabled)
            {
                LibrarySearchTextBox.Text = "Click 'configure' to set the Conan path -->";
            }
            else
            {
                LibrarySearchTextBox.Text = "";
            }
        }


        private void Configuration_Click(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            ShowConfigurationDialog();
            ToggleUIEnableState(IsConanInitialized());
        }

        private void ShowPackagesCheckbox_Click(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            FilterListView(LibrarySearchTextBox.Text, ShowPackagesCheckbox.IsChecked ?? false);
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

                    MessageBox.Show("ConanCenter libraries data file updated.", "Conan C/C++ Package Manager", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating: {ex.Message}", "Error - Conan C/C++ Package Manager", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            _ = UpdateJsonDataAsync();
        }
    }
}
