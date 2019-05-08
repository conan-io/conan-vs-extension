using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace VisualStudio.OpenFolder
{
    [DataContract]
    [GeneratedCode("Microsoft.Json.Schema.ToDotNet", "0.63.0.0")]
    public partial class TasksVs
    {
        public static IEqualityComparer<TasksVs> ValueComparer => TasksVsEqualityComparer.Instance;

        public bool ValueEquals(TasksVs other) => ValueComparer.Equals(this, other);
        public int ValueGetHashCode() => ValueComparer.GetHashCode(this);

        [DataMember(Name = "version", IsRequired = false, EmitDefaultValue = false)]
        public string Version { get; set; }
        [DataMember(Name = "variables", IsRequired = false, EmitDefaultValue = false)]
        public object Variables { get; set; }
        [DataMember(Name = "tasks", IsRequired = false, EmitDefaultValue = false)]
        public IList<object> Tasks { get; set; }
    }
}