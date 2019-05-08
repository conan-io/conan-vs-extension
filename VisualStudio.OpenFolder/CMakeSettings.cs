using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace VisualStudio.OpenFolder
{
    [DataContract]
    [GeneratedCode("Microsoft.Json.Schema.ToDotNet", "0.63.0.0")]
    public partial class CMakeSettings
    {
        public static IEqualityComparer<CMakeSettings> ValueComparer => CMakeSettingsEqualityComparer.Instance;

        public bool ValueEquals(CMakeSettings other) => ValueComparer.Equals(this, other);
        public int ValueGetHashCode() => ValueComparer.GetHashCode(this);

        /// <summary>
        /// A list of “environment” groups that contain key value pairs defining variables that are applied to each configuration in a the CMakeSettings.json.
        /// </summary>
        [DataMember(Name = "environments", IsRequired = false, EmitDefaultValue = false)]
        public IList<object> Environments { get; set; }

        /// <summary>
        /// A list of CMake configurations that apply to the CMakeLists.txt file in the same folder.
        /// </summary>
        [DataMember(Name = "configurations", IsRequired = false, EmitDefaultValue = false)]
        public IList<object> Configurations { get; set; }
    }
}