using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace VisualStudio.OpenFolder
{
    [DataContract]
    [GeneratedCode("Microsoft.Json.Schema.ToDotNet", "0.63.0.0")]
    public partial class CppProperties
    {
        public static IEqualityComparer<CppProperties> ValueComparer => CppPropertiesEqualityComparer.Instance;

        public bool ValueEquals(CppProperties other) => ValueComparer.Equals(this, other);
        public int ValueGetHashCode() => ValueComparer.GetHashCode(this);

        /// <summary>
        /// The display name of the project
        /// </summary>
        [DataMember(Name = "projectName", IsRequired = false, EmitDefaultValue = false)]
        public string ProjectName { get; set; }

        /// <summary>
        /// A list of C++ configurations that apply to all C++ source files in the current directory tree
        /// </summary>
        [DataMember(Name = "configurations", IsRequired = true)]
        public IList<object> Configurations { get; set; }
    }
}