using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Threading.Tasks;
using Conan.VisualStudio.Menu;
using Conan.VisualStudio.Services;
using Moq;
using Xunit;

namespace Conan.VisualStudio.Tests.Menu
{
    public class MenuCommandBaseTests
    {
        private class TestMenuCommand : MenuCommandBase
        {
            protected override int CommandId => 0;

            public TestMenuCommand(IMenuCommandService commandService, IDialogService dialogService)
                : base(commandService, dialogService)
            {
            }

            protected override Task MenuItemCallback() =>
                throw new Exception("Test menu command exception");
        }

        [Fact]
        public void MenuCommandBaseShowsAnExceptionDialog()
        {
            var commands = new List<MenuCommand>();
            var commandService = new Mock<IMenuCommandService>();
            commandService.Setup(x => x.AddCommand(It.IsAny<MenuCommand>()))
                .Callback((Action<MenuCommand>)commands.Add);

            var dialogServiceMock = new Mock<IDialogService>();
            var command = new TestMenuCommand(commandService.Object, dialogServiceMock.Object);

            commands.Single().Invoke();
            dialogServiceMock.Verify(x => x.ShowPluginError(It.IsAny<string>()));
        }
    }
}
