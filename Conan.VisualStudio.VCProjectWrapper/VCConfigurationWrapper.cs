using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Conan.VisualStudio.Core;
using Conan.VisualStudio.Core.VCInterfaces;
using Microsoft.VisualStudio.VCProjectEngine;
using Microsoft.Build.Construction;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Conan.VisualStudio.VCProjectWrapper
{
    public class VCConfigurationWrapper : IVCConfiguration
    {
        private readonly VCConfiguration _configuration;
        public VCConfigurationWrapper(VCConfiguration configuration)
        {
            _configuration = configuration;
        }

        private static string RuntimeLibraryToString(runtimeLibraryOption RuntimeLibrary)
        {
            switch (RuntimeLibrary)
            {
                case runtimeLibraryOption.rtMultiThreaded:
                    return "MT";
                case runtimeLibraryOption.rtMultiThreadedDebug:
                    return "MTd";
                case runtimeLibraryOption.rtMultiThreadedDLL:
                    return "MD";
                case runtimeLibraryOption.rtMultiThreadedDebugDLL:
                    return "MDd";
                default:
                    throw new NotSupportedException($"Runtime Library {RuntimeLibrary} is not supported by the Conan plugin");
            }
        }

        public string ProjectDirectory => _configuration.project.ProjectDirectory;

        public string ProjectFileName => _configuration.project.ProjectFile;

        public string ProjectName => _configuration.project.Name;

        public string Name => _configuration.Name;

        public string ConfigurationName => _configuration.ConfigurationName;

        public string PlatformName => _configuration.Platform.Name;

        public string RuntimeLibrary
        {
            get
            {
                var VCCLCompilerTool = _configuration.Tools.Item("VCCLCompilerTool");
                return VCCLCompilerTool != null ? RuntimeLibraryToString(VCCLCompilerTool.RuntimeLibrary) : null;
            }
        }

        public string Toolset
        {
            get
            {
                IVCRulePropertyStorage generalSettings = _configuration.Rules.Item("ConfigurationGeneral");
                return generalSettings.GetEvaluatedPropertyValue("PlatformToolset");
            }
        }

        public string Evaluate(string value)
        {
            return _configuration.Evaluate(value);
        }

        public bool IsPropertySheetPresent(string sheet)
        {
            string vcProjectFilename = Path.Combine(_configuration.project.ProjectDirectory, _configuration.project.ProjectFile);
            ProjectRootElement project = ProjectRootElement.Open(vcProjectFilename);
            string configCondition = "'$(Configuration)|$(Platform)'=='" + _configuration.Name + "'";
            bool bIsInVcxproj = false;
            bool bIsLoaded = false;

            foreach (ProjectImportGroupElement importGroup in project.ImportGroups)
                if (importGroup.Label == "PropertySheets" && importGroup.Condition == configCondition)
                    foreach (ProjectImportElement importElement in importGroup.Imports)
                        if (importElement.Project == sheet && importElement.Condition == "Exists('" + sheet + "')")
                            bIsInVcxproj = true;

            foreach (VCPropertySheet VCsheet in _configuration.PropertySheets)
            {
                if (ConanPathHelper.GetRelativePath(ProjectDirectory, VCsheet.PropertySheetFile) == sheet)
                    bIsLoaded = true;
            }

            return bIsLoaded && bIsInVcxproj;
        }

        public void AddPropertySheet(string sheet, string projectFileName)
        {
            ProjectRootElement project = ProjectRootElement.Open(projectFileName);
            string configCondition = "'$(Configuration)|$(Platform)'=='" + _configuration.Name + "'";
            bool bMustBeSaved = false;
            foreach (ProjectImportGroupElement importGroup in project.ImportGroups)
            {
                if (importGroup.Label == "PropertySheets" && importGroup.Condition == configCondition)
                {
                    bool bFound = false;
                    foreach (ProjectImportElement importElement in importGroup.Imports)
                        if (importElement.Project == sheet)
                        {
                            bFound = true;
                            //Ensure that condition is present
                            if(importElement.Condition != "Exists('" + sheet + "')")
                            {
                                importElement.Condition = "Exists('" + sheet + "')";
                                bMustBeSaved = true;
                            }
                        }
                    if (!bFound)
                    {
                        importGroup.AddImport(sheet).Condition = "Exists('" + sheet + "')";
                        bMustBeSaved = true;
                    }
                }
            }
            if (bMustBeSaved)
                project.Save();
        }

        public void CollectIntelliSenseInfo()
        {
#if VS15
            _configuration.CollectIntelliSenseInfo();
#endif
        }

        public List<IVCPropertySheet> PropertySheets
        {
            get
            {
                List<IVCPropertySheet> propertySheets = new List<IVCPropertySheet>();
                foreach (VCPropertySheet propertySheet in _configuration.PropertySheets)
                    propertySheets.Add(new VCPropertySheetWrapper(propertySheet));
                return propertySheets;
            }
        }

        private VCLinkerTool LinkerTool
        {
            get
            {
                IVCCollection tools = _configuration.Tools as IVCCollection;
                return tools != null ? tools.Item("VCLinkerTool") as VCLinkerTool : null;
            }
        }

        public string AdditionalDependencies
        {
            get
            {
                return LinkerTool != null ? LinkerTool.AdditionalDependencies : "";
            }
            set
            {
                if (LinkerTool != null)
                    LinkerTool.AdditionalDependencies = value;
            }
        }

    }
}
