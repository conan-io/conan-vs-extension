using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;
using System;
using System.Collections;
using System.IO;

namespace conan_vs_extension
{
    public class BuildEventsHandler
    {
        private readonly DTE _dte;
        private readonly BuildEvents _buildEvents;
        private ConanProfilesManager _profiles_manager;

        public BuildEventsHandler(DTE dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _dte = dte;
            _buildEvents = _dte.Events.BuildEvents;
            _profiles_manager = new ConanProfilesManager();

            // Subscribe to compilation events
            _buildEvents.OnBuildDone += OnBuildDone;
            _buildEvents.OnBuildProjConfigBegin += OnBuildProjConfigBegin;
            _buildEvents.OnBuildProjConfigDone += OnBuildProjConfigDone;
        }

        private void OnBuildProjConfigBegin(string Project, string ProjectConfig, string Platform, string SolutionConfig)
        {
            // here we generate profiles for all projects but we probably should only generate profiles for 
            // the project marked as startup project
            Project invokedProject = ProjectConfigurationManager.GetProjectByName(_dte, Project);
            _profiles_manager.GenerateProfilesForProject(invokedProject);
        }

        private void OnBuildProjConfigDone(string Project, string ProjectConfig, string Platform, string SolutionConfig, bool Success)
        {
            //ThreadHelper.ThrowIfNotOnUIThread();
            var message = "OnBuildProjConfigDone";
            System.Diagnostics.Debug.WriteLine(message);
            Project invokedProject = ProjectConfigurationManager.GetProjectByName(_dte, Project);
            VCConfiguration config = ProjectConfigurationManager.GetVCConfig(invokedProject, ProjectConfig, Platform);
            // FIXME: the problem with this is that the first time you build
            // the build is started before this step finishes
            // maybe we can inject the dependency to the project via a prebuild event? 
            // with a script?
            _ = ProjectConfigurationManager.InjectConanDepsAsync(invokedProject, config);
        }

        private void OnBuildDone(vsBuildScope Scope, vsBuildAction Action)
        {
            var message = "OnBuildDone";
            System.Diagnostics.Debug.WriteLine(message);
        }

        public void Dispose()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _buildEvents.OnBuildDone -= OnBuildDone;
            _buildEvents.OnBuildProjConfigBegin -= OnBuildProjConfigBegin;
            _buildEvents.OnBuildProjConfigDone -= OnBuildProjConfigDone;        }
    }
}
