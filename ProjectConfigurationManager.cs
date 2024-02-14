using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;
using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;

namespace conan_vs_extension
{
    public class ProjectConfigurationManager
    {
        
        public ProjectConfigurationManager()
        {
        }

        public async Task InjectConanDepsAsync(VCProject vcProject, VCConfiguration vcConfig, string propsFilePath)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (File.Exists(propsFilePath))
            {
                bool isAlreadyIncluded = false;
                IVCCollection propertySheets = vcConfig.PropertySheets as IVCCollection;
                foreach (VCPropertySheet sheet in propertySheets)
                {
                    if (sheet.PropertySheetFile.Equals(propsFilePath, StringComparison.OrdinalIgnoreCase))
                    {
                        isAlreadyIncluded = true;
                        break;
                    }
                }

                if (!isAlreadyIncluded)
                {
                    vcConfig.AddPropertySheet(propsFilePath);
                    vcProject.Save();
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Properties file '{propsFilePath}' does not exist.");
            }
        }

        public async Task SaveConanPrebuildEventAsync(VCProject vcProject, VCConfiguration vcConfig, string conanCommand)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            IVCCollection tools = (IVCCollection)vcConfig.Tools;
            VCPreBuildEventTool preBuildTool = (VCPreBuildEventTool)tools.Item("VCPreBuildEventTool");

            if (preBuildTool != null)
            {
                string currentPreBuildEvent = preBuildTool.CommandLine;
                if (!currentPreBuildEvent.Contains("conan"))
                {
                    // FIXME: better do this with a script file?
                    preBuildTool.CommandLine = conanCommand + Environment.NewLine + currentPreBuildEvent;
                    vcProject.Save();
                }
            }
        }
    }
}
