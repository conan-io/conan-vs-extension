using System;
using Microsoft.VisualStudio.VCProjectEngine;
using Xunit;

namespace Conan.VisualStudio.Tests
{
    public class VcProjectServiceTests
    {
        [Fact]
        public void GetArchitrectureSupportsTheNecessaryArchitectures() =>
            Assert.Equal("x86", VcProjectService.GetArchitecture("Win32"));

        [Fact]
        public void GetBuildTypeReturnsItsArgument()
        {
            var configurationName = Guid.NewGuid().ToString();
            Assert.Equal(configurationName, VcProjectService.GetBuildType(configurationName));
        }

        [Fact]
        public void GetRuntimeMapsVisualCppRuntimesToConanOptions()
        {
            Assert.Equal("MT", VcProjectService.GetRuntime(runtimeLibraryOption.rtMultiThreaded));
            Assert.Equal("MTd", VcProjectService.GetRuntime(runtimeLibraryOption.rtMultiThreadedDebug));
            Assert.Equal("MD", VcProjectService.GetRuntime(runtimeLibraryOption.rtMultiThreadedDLL));
            Assert.Equal("MDd", VcProjectService.GetRuntime(runtimeLibraryOption.rtMultiThreadedDebugDLL));
        }
    }
}
