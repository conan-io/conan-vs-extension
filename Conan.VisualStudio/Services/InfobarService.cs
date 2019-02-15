using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.VCProjectEngine;
using System;

namespace Conan.VisualStudio.Services
{
    public class InfobarService
    {
        private IServiceProvider _serviceProvider;

        public InfobarService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        private InfoBarModel CreateInfoBarModel()
        {
            var model = new InfoBarModel
            (
                textSpans: new[]
                {
                    new InfoBarTextSpan("Conan dependencies have been installed successfully. "),
                    new InfoBarTextSpan("Please "),
                    new InfoBarHyperlink("reload"),
                    new InfoBarTextSpan(" the project.")
                },
                actionItems: new[]
                {
                    new InfoBarButton("Reload")
                },
                image: KnownMonikers.StatusInformation,
                isCloseButtonVisible: true
            );

            return model;
        }

        public void ShowInfoBar(VCProject project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var shell = _serviceProvider.GetService(typeof(SVsShell)) as IVsShell;
            if (shell != null)
            {
                // Get the main window handle to host our InfoBar
                shell.GetProperty((int)__VSSPROPID7.VSSPROPID_MainWindowInfoBarHost, out var obj);
                var host = (IVsInfoBarHost)obj;

                //If we cannot find the handle, we cannot do much, so return.
                if (host == null)
                {
                    return;
                }

                InfoBarModel infoBarModel = CreateInfoBarModel();

                //Get the factory object from IVsInfoBarUIFactory, create it and add it to host.
                var factory = _serviceProvider.GetService(typeof(SVsInfoBarUIFactory)) as IVsInfoBarUIFactory;
                if (factory == null)
                {
                    return;
                }
                IVsInfoBarUIElement element = factory.CreateInfoBar(infoBarModel);

                var infoBarEventsHandler = new InfoBarEventsHandler(project);

                element.Advise(infoBarEventsHandler, out var _cookie);
                host.AddInfoBar(element);
            }
        }
    }
}
