using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace conan_vs_extension
{
    public static class ConanFileManager
    {
        private static readonly string[] _modifyCommentGuard = new[]
        {
            "# This file is managed by the Conan Visual Studio Extension, contents will be overwritten.",
            "# To keep your changes, remove these comment lines, but the plugin won't be able to modify your requirements"
        };

        private static bool IsFileCommentGuarded(string path)
        {
            if (!File.Exists(path)) return false;

            var guardCommentLines = new List<string>(_modifyCommentGuard.Length);

            using (var reader = new StreamReader(path))
            {
                for (int i = 0; i < _modifyCommentGuard.Length; i++)
                {
                    if (reader.EndOfStream) return false;
                    guardCommentLines.Add(reader.ReadLine());
                }
            }

            return guardCommentLines.SequenceEqual(_modifyCommentGuard);
        }

        public static string[] GetConandataRequirements(string projectDirectory)
        {
            string path = Path.Combine(projectDirectory, "conandata.yml");
            if (IsFileCommentGuarded(path))
            {
                string[] conandataContents = File.ReadAllLines(path);

                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .Build();

                var result = deserializer.Deserialize<Requirements>(string.Join(Environment.NewLine, conandataContents));

                if (result.requirements != null)
                {
                    return result.requirements;
                }
            }
            return new string[] { };
        }

        private static void WriteConanfileIfNecessary(string projectDirectory)
        {
            string conanfilePath = Path.Combine(projectDirectory, "conanfile.py");
            if (!IsFileCommentGuarded(conanfilePath))
            {
                string conanfileContents = string.Join(Environment.NewLine,
                    _modifyCommentGuard.Concat(new string[]
                    {
                        "",
                        "from conan import ConanFile",
                        "from conan.tools.microsoft import vs_layout, MSBuildDeps",
                        "class ConanApplication(ConanFile):",
                        "    package_type = \"application\"",
                        "    settings = \"os\", \"compiler\", \"build_type\", \"arch\"",
                        "",
                        "    def layout(self):",
                        "        vs_layout(self)",
                        "",
                        "    def generate(self):",
                        "        deps = MSBuildDeps(self)",
                        "        deps.generate()",
                        "",
                        "    def requirements(self):",
                        "        requirements = self.conan_data.get('requirements', [])",
                        "        for requirement in requirements:",
                        "            self.requires(requirement)"
                    })
                );

                File.WriteAllText(conanfilePath, conanfileContents);
            }
        }

        private static void WriteConandataIfNecessary(string projectDirectory)
        {
            string conandataPath = Path.Combine(projectDirectory, "conandata.yml");
            if (!IsFileCommentGuarded(conandataPath))
            {
                string conandataContents = string.Join(Environment.NewLine,
                    _modifyCommentGuard.Concat(new string[] { "requirements:" })
                );

                File.WriteAllText(conandataPath, conandataContents);
            }
        }

        public static void WriteNecessaryConanGuardedFiles(string projectDirectory)
        {
            WriteConanfileIfNecessary(projectDirectory);
            WriteConandataIfNecessary(projectDirectory);
        }

        public static void WriteNewRequirement(string projectDirectory, string newRequirement)
        {
            string path = Path.Combine(projectDirectory, "conandata.yml");
            if (IsFileCommentGuarded(path))
            {
                string[] requirements = GetConandataRequirements(projectDirectory);
                if (!requirements.Contains(newRequirement))
                {
                    var newRequirements = requirements.Append(newRequirement).ToArray();
                    var serializer = new SerializerBuilder()
                        .WithNamingConvention(UnderscoredNamingConvention.Instance)
                        .Build();
                    var yaml = serializer.Serialize(new Requirements(newRequirements));

                    // Combine guard comments and YAML contents.
                    string fileContents = string.Join(Environment.NewLine, _modifyCommentGuard) + Environment.NewLine + yaml;
                    
                    // Write the combined contents to the file.
                    File.WriteAllText(path, fileContents);
                }
            }
        }

        public static void RemoveRequirement(string projectDirectory, string oldRequirement)
        {
            string path = Path.Combine(projectDirectory, "conandata.yml");
            if (IsFileCommentGuarded(path))
            {
                string[] requirements = GetConandataRequirements(projectDirectory);
                if (requirements.Contains(oldRequirement))
                {
                    var newRequirements = requirements.Where(req => req != oldRequirement).ToArray();
                    var serializer = new SerializerBuilder()
                        .WithNamingConvention(UnderscoredNamingConvention.Instance)
                        .Build();
                    var yaml = serializer.Serialize(new Requirements(newRequirements));

                    // Combine guard comments and YAML contents.
                    string fileContents = string.Join(Environment.NewLine, _modifyCommentGuard) + Environment.NewLine + yaml;
                    
                    // Write the combined contents to the file.
                    File.WriteAllText(path, fileContents);
                }
            }
        }
    }
}
