using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.VCProjectEngine;

namespace Conan.VisualStudio.Services
{
    public class InfoBarEventsHandler : IVsInfoBarUIEvents
    {
        private readonly VCProject _project;
        private readonly IVcProjectService _vcProjectService;

        public InfoBarEventsHandler(VCProject project)
        {
            _project = project;
            _vcProjectService = new VcProjectService();
        }

        public void OnClosed(IVsInfoBarUIElement infoBarUIElement)
        {
            // User does not want to reload now...
        }

        public void OnActionItemClicked(IVsInfoBarUIElement infoBarUIElement, IVsInfoBarActionItem actionItem)
        {
            var projectGuid = _vcProjectService.UnloadProject(_project);
            _vcProjectService.ReloadProject(projectGuid);
            ThreadHelper.ThrowIfNotOnUIThread();
            infoBarUIElement.Close();
        }
    }
}
