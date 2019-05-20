using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Conan.VisualStudio.Core
{
    public class ConanRunner
    {
        private readonly ConanSettings _conanSettings;
        private readonly string _executablePath;

        public ConanRunner(ConanSettings conanSettings, string executablePath)
        {
            _conanSettings = conanSettings;
            _executablePath = executablePath;
        }

        private string Escape(string arg) =>
            arg.Contains(" ") ? $"\"{arg}\"" : arg;

        public Task<Process> Install(ConanProject project, ConanConfiguration configuration, ConanGeneratorType generator, ConanBuildType build, bool update)
        {
            string ProcessArgument(string name, string value) => $"-s {name}={Escape(value)}";

            var path = Path.GetDirectoryName(project.Path);

            var arguments = string.Empty;
            if (_conanSettings != null)
            {
                var installConfig = _conanSettings.ConanCommands.FirstOrDefault(c => c.Name.Equals("install"));
                arguments = installConfig.Args;
            }
            else
            {
                string generatorName = generator.ToString();
                var settingValues = new[]
                {
                    ("arch", configuration.Architecture),
                    ("build_type", configuration.BuildType),
                    ("compiler.toolset", configuration.CompilerToolset),
                    ("compiler.version", configuration.CompilerVersion),
                };
                if (configuration.RuntimeLibrary != null)
                {
                    settingValues = settingValues.Concat(new[] { ("compiler.runtime", configuration.RuntimeLibrary) }).ToArray();
                }
                string options = "";
                if (build != ConanBuildType.none)
                {
                    options += "--build " + build.ToString();
                }

                if (update)
                {
                    options += " --update";
                }

                var settings = string.Join(" ", settingValues.Where(pair => pair.Item2 != null).Select(pair =>
                {
                    var (key, value) = pair;
                    return ProcessArgument(key, value);
                }));
                arguments = $"install {Escape(path)} " +
                            $"-g {generatorName} " +
                            $"--install-folder {Escape(configuration.InstallPath)} " +
                            $"{settings} {options}";
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = _executablePath,
                Arguments = arguments,
                UseShellExecute = false,
                WorkingDirectory = path,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            return Task.Run(() => Process.Start(startInfo));
        }

        public Task<Process> Inspect(ConanProject project)
        {
            var path = project.Path;
            var arguments = $"inspect {Escape(path)} -a name -j";

            var startInfo = new ProcessStartInfo
            {
                FileName = _executablePath,
                Arguments = arguments,
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(path),
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            return Task.Run(() => Process.Start(startInfo));
        }
    }
}
