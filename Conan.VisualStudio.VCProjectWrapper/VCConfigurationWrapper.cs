using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Conan.VisualStudio.Core.VCInterfaces;
using Microsoft.VisualStudio.VCProjectEngine;

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

        private static string LanguageStandardToCppStd(string LanguageStandard)
        {
            switch (LanguageStandard?.ToLower())
            {
                case "stdcpplatest":
                    return "20";
                case "stdcpp17":
                    return "17";
                case "stdcpp14":
                    return "14";
                case "default":
                case null:
                    return null;
                default:
                    throw new NotSupportedException(
                        $"Language Standard {LanguageStandard} is not supported by the Conan plugin");
            }
        }

        private string GetEvaluatedPropertyValueFromTool(string propertyName, string toolName)
        {
            var properties = _configuration.Tools.Item(toolName) as IVCRulePropertyStorage;

            try
            {
                return properties?.GetEvaluatedPropertyValue(propertyName);
            }
            catch
            {
                return null;
            }
        }

        private string GetEvaluatedPropertyValueFromRule(string propertyName, string ruleName)
        {
            var properties = _configuration.Rules.Item(ruleName) as IVCRulePropertyStorage;

            try
            {
                return properties?.GetEvaluatedPropertyValue(propertyName);
            }
            catch
            {
                return null;
            }
        }

        public string ProjectDirectory => _configuration.project.ProjectDirectory;

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

        public string CppStd
        {
            get
            {
                var LanguageStandard = GetEvaluatedPropertyValueFromTool("LanguageStandard",
                    "VCCLCompilerTool");

                if (LanguageStandard != null)
                {
                    return LanguageStandardToCppStd(LanguageStandard);
                }

                LanguageStandard = GetEvaluatedPropertyValueFromRule("LanguageStandard", "CL");

                if (LanguageStandard != null)
                {
                    return LanguageStandardToCppStd(LanguageStandard);
                }

                LanguageStandard = GetEvaluatedPropertyValueFromRule("LanguageStandard",
                    "ConfigurationGeneral");

                return LanguageStandardToCppStd(LanguageStandard);
            }
        }

        public string Evaluate(string value)
        {
            return _configuration.Evaluate(value);
        }

        public void AddPropertySheet(string sheet)
        {
            _configuration.AddPropertySheet(sheet);
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
