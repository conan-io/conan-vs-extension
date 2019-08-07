using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Conan.VisualStudio.Core.VCInterfaces;
using Microsoft.VisualStudio.VCProjectEngine;

namespace Conan.VisualStudio.VCProjectWrapper
{
    public class VCProjectWrapper : IVCProject
    {
        private readonly VCProject _project;
        public VCProjectWrapper(object project)
        {
            _project = project as VCProject;
        }
        public string ProjectDirectory => _project.ProjectDirectory;

        public List<IVCConfiguration> Configurations
        {
            get
            {
                List<IVCConfiguration> configurations = new List<IVCConfiguration>();
                foreach (VCConfiguration configuration in _project.Configurations)
                    configurations.Add(new VCConfigurationWrapper(configuration));
                return configurations;
            }
        }

        public IVCConfiguration ActiveConfiguration => new VCConfigurationWrapper(_project.ActiveConfiguration);
    }
}
