using Microsoft.VisualStudio.TestTools.UnitTesting;
using VisualStudio.OpenFolder;

namespace Conan.VisualStudio.Tests.OpenFolder
{
    [TestClass]
    public class TaskVsTests
    {
        [TestMethod]
        public void ParseTasksFile()
        {
            string body = FileSystemUtils.ReadTestFile(@"OpenFolder\TestData\tasks.vs.json");

            var tasksObject = JsonUtils.ReadToObject<TasksVs>(body);


            Assert.AreEqual(tasksObject.Version, "0.2.1");
        }

    }
}
