using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace VisualStudio.OpenFolder
{
    [DataContract]
    [GeneratedCode("Microsoft.Json.Schema.ToDotNet", "0.63.0.0")]
    public partial class Default
    {
        public static IEqualityComparer<Default> ValueComparer => DefaultEqualityComparer.Instance;

        public bool ValueEquals(Default other) => ValueComparer.Equals(this, other);
        public int ValueGetHashCode() => ValueComparer.GetHashCode(this);

        [DataMember(Name = "taskName", IsRequired = false, EmitDefaultValue = false)]
        public string TaskName { get; set; }
        [DataMember(Name = "taskLabel", IsRequired = false, EmitDefaultValue = false)]
        public string TaskLabel { get; set; }
        [DataMember(Name = "appliesTo", IsRequired = false, EmitDefaultValue = false)]
        public string AppliesTo { get; set; }
        [DataMember(Name = "contextType", IsRequired = false, EmitDefaultValue = false)]
        public string ContextType { get; set; }
        [DataMember(Name = "output", IsRequired = false, EmitDefaultValue = false)]
        public string Output { get; set; }
        [DataMember(Name = "inheritEnvironments", IsRequired = false, EmitDefaultValue = false)]
        public IList<string> InheritEnvironments { get; set; }
    }
}