using System;
using System.Collections.Generic;
using Conan.VisualStudio.Core;
using Microsoft.VisualStudio.VCProjectEngine;

namespace Conan.VisualStudio
{
    internal static class VcProjectService
    {
        public static IEnumerable<ConanProject> ExtractConfigurations(VCProject project)
        {
            foreach (VCConfiguration2 configuration in project.Configurations)
            {
                yield return ExtractConfigurationInfo(project, configuration);
            }
        }

        internal static string GetArchitecture(string platformName)
        {
            switch (platformName)
            {
                case "Win32": return "x86";
                default: throw new NotSupportedException($"Platform {platformName} is not supported by the Conan plugin");
            }
        }

        internal static string GetBuildType(string configurationName) => configurationName;

        internal static string GetRuntime(runtimeLibraryOption runtimeType)
        {
            switch (runtimeType)
            {
                case runtimeLibraryOption.rtMultiThreaded: return "MT";
                case runtimeLibraryOption.rtMultiThreadedDebug: return "MTd";
                case runtimeLibraryOption.rtMultiThreadedDLL: return "MD";
                case runtimeLibraryOption.rtMultiThreadedDebugDLL: return "MDd";
                default: throw new NotSupportedException($"Runtime {runtimeType} is not supported by the Conan plugin");
            }
        }

        private static ConanProject ExtractConfigurationInfo(VCProject project, VCConfiguration2 configuration)
        {
            IVCCollection tools = configuration.Tools;
            VCCLCompilerTool compiler = tools.Item("VCCLCompilerTool");
            return new ConanProject
            {
                Path = project.ProjectDirectory,
                Architecture = GetArchitecture(configuration.Platform.Name),
                BuildType = GetBuildType(configuration.ConfigurationName),
                Compiler = "Visual Studio",
                CompilerRuntime = GetRuntime(compiler.RuntimeLibrary),
                CompilerVersion = "15"
            };
        }
    }
}
