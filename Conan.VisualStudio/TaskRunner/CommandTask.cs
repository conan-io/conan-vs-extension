using System.Collections.Generic;

namespace Conan.VisualStudio.TaskRunner
{
    public class CommandTask
    {
        public string TaskName { get; set; }
        public string AppliesTo { get; set; }
        public string Type { get; set; }
        public string ContextType { get; set; }
        public string Command { get; set; }
        public List<string> Args { get; set; }
        public EnvVars EnvVars { get; set; }
    }
    public class EnvVars
    {
        public string VSCMD_START_DIR { get; set; }
    }

    public class VsTasks
    {
        public string Version { get; set; }
        public string OutDir { get; set; }
        public List<CommandTask> Tasks { get; set; }
    }
}
