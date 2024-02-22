using System;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace conan_vs_extension
{
    public static class ConanFileManager
    {
        private static readonly string _modifyCommentGuard = "# This file is managed by the Conan Visual Studio Extension, contents will be overwritten.\n# To keep your changes, remove these comment lines, but the plugin won't be able to modify your requirements";

        private static bool IsFileCommentGuarded(string path)
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

        public static string[] GetConandataRequirements(string projectDirectory)
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

        private static void WriteConanfileIfNecessary(string projectDirectory)
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

        private static void WriteConandataIfNecessary(string projectDirectory)
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

        public static void RemoveRequirement(string projectDirectory, string oldRequirement)
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

    }
}
