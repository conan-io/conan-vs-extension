using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;
using Conan.VisualStudio.Services;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace Conan.VisualStudio.Menu
{
    /// <summary>Command handler.</summary>
    internal sealed class ConanAbout : MenuCommandBase
    {
        protected override int CommandId => PackageIds.ConanAboutId;

        public ConanAbout(
            IMenuCommandService commandService,
            Core.IErrorListService errorListService)
            : base(commandService, errorListService)
        {
            
        }

        protected internal override async Task MenuItemCallbackAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var assembly = Assembly.GetExecutingAssembly();
            string version = assembly.GetName().Version.ToString();
            string fileVersionInfo = assembly.GetCustomAttribute<AssemblyDescriptionAttribute>().Description.ToString();
            Logger.Log($"About Conan Extension for Visual Studio: {version} ({fileVersionInfo})");

            string message = String.Format("Conan Extension for Visual Studio\n" +
                $"Version\t: {version}\n" +
                $"Commit\t: {fileVersionInfo}");
            MessageBox.Show(message, "About Conan Extension", MessageBoxButtons.OK);
        }
    }
}
