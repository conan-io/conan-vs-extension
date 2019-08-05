using System;
using System.Threading.Tasks;
using Conan.VisualStudio.Core;
using Conan.VisualStudio.Core.VCInterfaces;
using EnvDTE;

namespace Conan.VisualStudio.Services
{
    public interface IVcProjectService
    {
        IVCProject GetActiveProject();
        ConanProject ExtractConanProject(IVCProject vcProject, ISettingsService settingsService);
        Task<ConanProject> ExtractConanProjectAsync(IVCProject vcProject, ISettingsService settingsService);
        bool IsConanProject(Project project);
        IVCProject AsVCProject(Project project);
        string GetInstallationDirectory(ISettingsService settingsService, IVCConfiguration configuration);
    }
}
