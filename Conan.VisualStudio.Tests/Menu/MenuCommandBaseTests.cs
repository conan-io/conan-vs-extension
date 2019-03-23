using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Threading.Tasks;
using Conan.VisualStudio.Menu;
using Conan.VisualStudio.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Conan.VisualStudio.Tests.Menu
{
    public class MenuCommandBaseTests
    {
        private class TestMenuCommand : MenuCommandBase
        {
            protected override int CommandId => 0;

            public TestMenuCommand(IMenuCommandService commandService, IErrorListService errorListService)
                : base(commandService, errorListService)
            {
            }

            protected internal override Task MenuItemCallbackAsync() =>
                throw new Exception("Test menu command exception");
        }

        [Ignore("failing because of ThreadHelper, we're not going to test GUI")]
        [TestMethod]
        public void MenuCommandBaseShowsAnExceptionDialog()
        {
            var commands = new List<MenuCommand>();
            var commandService = new Mock<IMenuCommandService>();
            commandService.Setup(x => x.AddCommand(It.IsAny<MenuCommand>()))
                .Callback((Action<MenuCommand>)commands.Add);

            var errorListService = new Mock<IErrorListService>();
            var command = new TestMenuCommand(commandService.Object, errorListService.Object);

            commands.Single().Invoke();
            errorListService.Verify(x => x.WriteError(It.IsAny<string>(), It.IsAny<string>()));
        }
    }
}
