using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TaskRunnerExplorer;
using Task = System.Threading.Tasks.Task;

namespace Conan.VisualStudio.TaskRunner
{
    internal class TrimmingStringComparer : IEqualityComparer<string>
    {
        private char _toTrim;
        private IEqualityComparer<string> _basisComparison;

        public TrimmingStringComparer(char toTrim)
            : this(toTrim, StringComparer.Ordinal)
        {
        }

        public TrimmingStringComparer(char toTrim, IEqualityComparer<string> basisComparer)
        {
            _toTrim = toTrim;
            _basisComparison = basisComparer;
        }

        public bool Equals(string x, string y)
        {
            string realX = x?.TrimEnd(_toTrim);
            string realY = y?.TrimEnd(_toTrim);
            return _basisComparison.Equals(realX, realY);
        }

        public int GetHashCode(string obj)
        {
            string realObj = obj?.TrimEnd(_toTrim);
            return realObj != null ? _basisComparison.GetHashCode(realObj) : 0;
        }
    }

    [TaskRunnerExport("tasks.vs.json")]
    class TaskRunnerProvider : ITaskRunner
    {
        private ImageSource _icon;
        private HashSet<string> _dynamicNames = new HashSet<string>(new TrimmingStringComparer('\u200B'));

        [Import]
        internal SVsServiceProvider _serviceProvider = null;

        public void SetDynamicTaskName(string dynamicName)
        {
            _dynamicNames.Remove(dynamicName);
            _dynamicNames.Add(dynamicName);
        }

        public string GetDynamicName(string name)
        {
            IEqualityComparer<string> comparer = new TrimmingStringComparer('\u200B');
            return _dynamicNames.FirstOrDefault(x => comparer.Equals(name, x));
        }

        public TaskRunnerProvider()
        {
            _icon = new BitmapImage(new Uri(@"pack://application:,,,/Conan.VisualStudio;component/Resources/taskrunner.png"));
        }

        public List<ITaskRunnerOption> Options
        {
            get { return null; }
        }

        public async Task<ITaskRunnerConfig> ParseConfig(ITaskRunnerCommandContext context, string configPath)
        {
            return await Task.Run(() =>
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                ITaskRunnerNode hierarchy = LoadHierarchy(configPath);

                if (!hierarchy.Children.Any() && !hierarchy.Children.First().Children.Any())
                    return null;

                return new TaskRunnerConfig(this, context, hierarchy, _icon);
            });
        }

        private void ApplyVariable(string key, string value, ref string str)
        {
            str = str.Replace(key, value);
        }

        private string SetVariables(string str, string cmdsDir)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (str == null)
                return str;

            var dte = (DTE)_serviceProvider.GetService(typeof(DTE));
            if (dte == null)
                return str;

            Solution sln = dte.Solution;
            IList<Project> projs = GetProjects(dte);
            SolutionBuild build = sln.SolutionBuild;
            var slnCfg = (SolutionConfiguration2)build.ActiveConfiguration;

            Project proj = projs.Cast<Project>().FirstOrDefault(x => { ThreadHelper.ThrowIfNotOnUIThread(); return x.FileName.Contains(cmdsDir) && !x.FullName.EndsWith("vcxproj"); });

            ApplyVariable("$(ConfigurationName)", slnCfg.Name, ref str);
            ApplyVariable("$(DevEnvDir)", Path.GetDirectoryName(dte.FileName), ref str);
            ApplyVariable("$(PlatformName)", slnCfg.PlatformName, ref str);

            ApplyVariable("$(SolutionDir)", Path.GetDirectoryName(sln.FileName), ref str);
            ApplyVariable("$(SolutionExt)", Path.GetExtension(sln.FileName), ref str);
            ApplyVariable("$(SolutionFileName)", Path.GetFileName(sln.FileName), ref str);
            ApplyVariable("$(SolutionName)", Path.GetFileNameWithoutExtension(sln.FileName), ref str);
            ApplyVariable("$(SolutionPath)", sln.FileName, ref str);


            if (proj != null && proj.ConfigurationManager != null) // some types of projects (TwinCat) can have null ConfigurationManager
            {
                Configuration projCfg = proj.ConfigurationManager.ActiveConfiguration;

                if (projCfg.Properties != null) // website folder projects (File -> Add -> Existing Web Site) have null properties
                {
                    string outDir = (string)projCfg.Properties.Item("OutputPath").Value;

                    string projectDir = Path.GetDirectoryName(proj.FileName);
                    string targetFilename = (string)proj.Properties.Item("OutputFileName").Value;
                    string targetPath = Path.Combine(projectDir, outDir, targetFilename);
                    string targetDir = Path.Combine(projectDir, outDir);

                    ApplyVariable("$(OutDir)", outDir, ref str);

                    ApplyVariable("$(ProjectDir)", projectDir, ref str);
                    ApplyVariable("$(ProjectExt)", Path.GetExtension(proj.FileName), ref str);
                    ApplyVariable("$(ProjectFileName)", Path.GetFileName(proj.FileName), ref str);
                    ApplyVariable("$(ProjectName)", proj.Name, ref str);
                    ApplyVariable("$(ProjectPath)", proj.FileName, ref str);

                    ApplyVariable("$(TargetDir)", targetDir, ref str);
                    ApplyVariable("$(TargetExt)", Path.GetExtension(targetFilename), ref str);
                    ApplyVariable("$(TargetFileName)", targetFilename, ref str);
                    ApplyVariable("$(TargetName)", proj.Name, ref str);
                    ApplyVariable("$(TargetPath)", targetPath, ref str);
                }
            }

            return str;
        }

        private ITaskRunnerNode LoadHierarchy(string configPath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            ITaskRunnerNode root = new TaskRunnerNode("Commands");
            string rootDir = Path.GetDirectoryName(configPath);
            IEnumerable<CommandTask> commands = TaskParser.LoadTasks(configPath);

            if (commands == null)
                return root;

            var tasks = new TaskRunnerNode("Commands");
            tasks.Description = "A list of command to execute";
            root.Children.Add(tasks);

            foreach (CommandTask command in commands.OrderBy(k => k.taskName))
            {
                command.args.ForEach(a => SetVariables(a, rootDir));
                command.command = SetVariables(command.command, rootDir);
                command.taskName = SetVariables(command.taskName, rootDir);
                command.envVars.VSCMD_START_DIR = SetVariables(command.envVars.VSCMD_START_DIR, rootDir);

                string cwd = command.envVars.VSCMD_START_DIR ?? rootDir;

                // Add zero width space
                string commandName = command.taskName += "\u200B";
                SetDynamicTaskName(commandName);

                var task = new TaskRunnerNode(commandName, true)
                {
                    Command = new TaskRunnerCommand(cwd, command.command, string.Join(" ", command.args)),
                    Description = $"Filename:\t {command.command}\r\nArguments:\t {command.args}"
                };

                tasks.Children.Add(task);
            }

            return root;
        }
        private IList<Project> GetProjects(DTE dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Projects projects = dte.Solution.Projects;
            List<Project> list = new List<Project>();
            var item = projects.GetEnumerator();
            while (item.MoveNext())
            {
                var project = item.Current as Project;
                if (project == null)
                {
                    continue;
                }

                if (project.Kind == EnvDTE.Constants.vsProjectKindSolutionItems)
                {
                    list.AddRange(GetSolutionFolderProjects(project));
                }
                else
                {
                    list.Add(project);
                }
            }

            return list;
        }

        private IEnumerable<Project> GetSolutionFolderProjects(Project solutionFolder)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            List<Project> list = new List<Project>();
            for (var i = 1; i <= solutionFolder.ProjectItems.Count; i++)
            {
                var subProject = solutionFolder.ProjectItems.Item(i).SubProject;
                if (subProject == null)
                {
                    continue;
                }

                if (subProject.Kind == EnvDTE.Constants.vsProjectKindSolutionItems)
                {
                    list.AddRange(GetSolutionFolderProjects(subProject));
                }
                else
                {
                    list.Add(subProject);
                }
            }
            return list;
        }
    }
}

