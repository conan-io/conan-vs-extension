using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace VisualStudio.OpenFolder
{
    [DataContract]
    [GeneratedCode("Microsoft.Json.Schema.ToDotNet", "0.63.0.0")]
    public partial class DefaultTask
    {
        public static IEqualityComparer<DefaultTask> ValueComparer => DefaultTaskEqualityComparer.Instance;

        public bool ValueEquals(DefaultTask other) => ValueComparer.Equals(this, other);
        public int ValueGetHashCode() => ValueComparer.GetHashCode(this);
    }
}