using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;
using Conan.VisualStudio.Services;
using Xunit;

namespace Conan.VisualStudio.Tests.Services
{
    public class VcProjectServiceTests
    {
        [Theory]
        [InlineData(@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project DefaultTargets=""Build"" ToolsVersion=""15.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
</Project>")]
        [InlineData(@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project DefaultTargets=""Build"" ToolsVersion=""15.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
    <Import Project=""foo\other.props"" />
</Project>")]
        [InlineData(@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project DefaultTargets=""Build"" ToolsVersion=""15.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
    <Import Project=""foo\bar.props"" />
</Project>")]
        public async Task IntegrateAddsImportIfNecessary(string projectText)
        {
            var path = Path.GetTempFileName();
            File.WriteAllText(path, projectText);

            const string propFilePath = @"foo\bar.props";
            await VcProjectService.AddPropsImport(path, propFilePath);

            var document = XDocument.Load(path);
            XNamespace ns = "http://schemas.microsoft.com/developer/msbuild/2003";
            var imports = document.Root.Descendants(ns + "Import");
            Assert.Single(imports, import => import.Attribute("Project").Value == propFilePath);
        }
    }
}
