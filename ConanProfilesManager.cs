using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace conan_vs_extension
{
    public class ConanProfilesManager
    {
        
        public ConanProfilesManager()
        {
        }

        public static string getProfileName(VCConfiguration vcConfig)
        {
            return vcConfig.Name.Replace("|", "_");
        }

        private string getConanArch(string platform)
        {
            var archMap = new Dictionary<string, string>();
            archMap["x64"] = "x86_64";
            archMap["Win32"] = "x86";
            archMap["ARM64"] = "armv8";
            return archMap[platform];
        }

        private string getConanCompilerVersion(string platformToolset)
        {
            var msvcVersionMap = new Dictionary<string, string>();
            msvcVersionMap["v143"] = "193";
            msvcVersionMap["v142"] = "192";
            msvcVersionMap["v141"] = "191";
            return msvcVersionMap[platformToolset];
        }

        private string getConanCppstd(string languageStandard)
        {
            // https://learn.microsoft.com/en-us/cpp/build/reference/std-specify-language-standard-version?view=msvc-170

            if (languageStandard.ToLower().Contains("default"))
            {
                return "14";
            }

            List<string> cppStdValues = new List<string>() { "14", "17", "20", "23" };

            foreach (string cppStdValue in cppStdValues)
            {
                if (languageStandard.Contains(cppStdValue))
                {
                    return cppStdValue;
                }
            }
            return "null";
        }

        public void GenerateProfilesForProject(Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                if (project != null && project.Object is VCProject vcProject)
                {
                    string projectDirectory = System.IO.Path.GetDirectoryName(project.FullName);
                    string conanProjectDirectory = System.IO.Path.Combine(projectDirectory, ".conan");
                    if (!Directory.Exists(conanProjectDirectory))
                    {
                        Directory.CreateDirectory(conanProjectDirectory);
                    }

                    foreach (VCConfiguration vcConfig in (IEnumerable)vcProject.Configurations)
                    {
                        string profileName = getProfileName(vcConfig);
                        string profilePath = System.IO.Path.Combine(conanProjectDirectory, profileName);

                        if (!File.Exists(profilePath))
                        {
                            string toolset = vcConfig.Evaluate("$(PlatformToolset)").ToString();
                            string compilerVersion = getConanCompilerVersion(toolset);
                            string arch = getConanArch(vcConfig.Evaluate("$(PlatformName)").ToString());
                            IVCRulePropertyStorage generalRule = vcConfig.Rules.Item("ConfigurationGeneral") as IVCRulePropertyStorage;
                            string languageStandard = generalRule == null ? null : generalRule.GetEvaluatedPropertyValue("LanguageStandard");
                            string cppStd = getConanCppstd(languageStandard);
                            string buildType = vcConfig.ConfigurationName;
                            string profileContent = 
$@"
[settings]
arch={arch}
build_type={buildType}
compiler=msvc
compiler.cppstd={cppStd}
compiler.runtime=dynamic
" +
$@"
compiler.runtime_type={buildType}
compiler.version={compilerVersion}
os=Windows
";
                            File.WriteAllText(profilePath, profileContent);
                        }
                    }
                }
                //MessageBox.Show($"Generated profiles for actual project.", "Conan profiles generated", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"There was a problem generating the file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}
