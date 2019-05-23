using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;

namespace VisualStudio.OpenFolder
{
    /// <summary>
    /// Defines methods to support the comparison of objects of type DefaultTask for equality.
    /// </summary>
    [GeneratedCode("Microsoft.Json.Schema.ToDotNet", "0.63.0.0")]
    internal sealed class DefaultTaskEqualityComparer : IEqualityComparer<DefaultTask>
    {
        internal static readonly DefaultTaskEqualityComparer Instance = new DefaultTaskEqualityComparer();

        public bool Equals(DefaultTask left, DefaultTask right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
            {
                return false;
            }

            return true;
        }

        public int GetHashCode(DefaultTask obj)
        {
            if (ReferenceEquals(obj, null))
            {
                return 0;
            }

            int result = 17;
            return result;
        }
    }
}