using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace Conan.VisualStudio
{
    [Guid("794abe60-9a42-428b-8ba0-40ca96fa5e69")]
    public class PackageListToolWindow : ToolWindowPane
    {
        public PackageListToolWindow() : base(null) => InitializeWindow();

        private void InitializeWindow()
        {
            Caption = "Conan Package Management";
            Content = new PackageListControl();
        }
    }
}
