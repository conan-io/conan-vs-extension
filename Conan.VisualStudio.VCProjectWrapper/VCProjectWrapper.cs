using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Conan.VisualStudio.Core.VCInterfaces;
using EnvDTE;
using Microsoft.VisualStudio.VCProjectEngine;

namespace Conan.VisualStudio.VCProjectWrapper
{
    public class VCProjectWrapper : IVCProject
    {
        private readonly VCProject _vcProject;
        private readonly Project _project;
        public VCProjectWrapper(object project)
        {
            _project = project as Project;
            _vcProject = _project.Object as VCProject;
        }
        public string ProjectDirectory => _vcProject.ProjectDirectory;

        public List<IVCConfiguration> Configurations
        {
            get
            {
                List<IVCConfiguration> configurations = new List<IVCConfiguration>();
                foreach (VCConfiguration configuration in _vcProject.Configurations)
                    configurations.Add(new VCConfigurationWrapper(configuration));
                return configurations;
            }
        }

        public IVCConfiguration ActiveConfiguration => new VCConfigurationWrapper(_vcProject.ActiveConfiguration);

        string IVCProject.Guid => _vcProject.ProjectGUID;

        string IVCProject.FullPath {
            get {
                return Path.Combine(_vcProject.ProjectDirectory, _vcProject.ProjectFile);
            }
        }

        bool IVCProject.Saved => _project.Saved;

        void IVCProject.Save()
        {
            _project.Save();
        }
    }
}
