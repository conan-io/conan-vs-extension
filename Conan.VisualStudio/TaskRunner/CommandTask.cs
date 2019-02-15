using System.Collections.Generic;

namespace Conan.VisualStudio.TaskRunner
{
    public class CommandTask
    {
        public string taskName { get; set; }
        public string appliesTo { get; set; }
        public string type { get; set; }
        public string contextType { get; set; }
        public string command { get; set; }
        public List<string> args { get; set; }
        public EnvVars envVars { get; set; }
    }
    public class EnvVars
    {
        public string VSCMD_START_DIR { get; set; }
    }

    public class VsTasks
    {
        public string version { get; set; }
        public string outDir { get; set; }
        public List<CommandTask> tasks { get; set; }
    }
}
