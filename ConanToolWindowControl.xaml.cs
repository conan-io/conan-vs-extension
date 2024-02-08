using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;

namespace conan_vs_extension
{
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
            LoadItemsIntoListView();
            LoadHtmlContent();
        }

        private void LoadItemsIntoListView()
        {
            var items = new List<string>
            {
                "open-dis-cpp", "open-simulation-interface", "open62541", "openal-soft", "openapi-generator", "openassetio", "openblas", "opencascade",
                "opencl-clhpp-headers", "opencl-headers", "opencl-icd-loader", "opencolorio", "opencore-amr", "opencv", "openddl-parser",
                "openebs", "openexr", "openfbx", "openfst", "openfx", "opengl-registry", "opengr", "opengv", "openh264", "openimageio",
                "openjdk", "openjpeg"
            };

            foreach (var item in items)
            {
                PackagesListView.Items.Add(item);
            }
        }

        public void LoadHtmlContent()
        {
            string htmlContent = "<html><body><h1>Hello World!</h1>/body></html>";
            myWebBrowser.NavigateToString(htmlContent);
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PackagesListView.SelectedItem is string selectedItem)
            {
                LoadHtmlContentForItem(selectedItem);
            }
        }

        private void LoadHtmlContentForItem(string item)
        {
            string htmlTemplate = @"
        <html>
        <head>
            <style>
                body {{ font-family: 'Roboto', sans-serif; }}
            </style>
        </head>
        <body>
            <h1>{0}</h1>
            <p>Hi! {0}.</p>
        </body>
        </html>";
            string htmlContent = string.Format(htmlTemplate, item);
            myWebBrowser.NavigateToString(htmlContent);
        }


        /// <summary>
        /// Handles click on the button by displaying a message box.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions", Justification = "Sample code")]
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Default event handler naming pattern")]

        private void Configuration_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
    string.Format(System.Globalization.CultureInfo.CurrentUICulture, "Invoked '{0}'", this.ToString()),
    "Conan C/C++ Package Manager");
        }

        private void ShowPackages_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
    string.Format(System.Globalization.CultureInfo.CurrentUICulture, "Invoked '{0}'", this.ToString()),
    "Conan C/C++ Package Manager");
        }

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
    string.Format(System.Globalization.CultureInfo.CurrentUICulture, "Invoked '{0}'", this.ToString()),
    "Conan C/C++ Package Manager");
        }
    }
}