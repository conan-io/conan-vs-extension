using System.CodeDom.Compiler;
using System.Collections.Generic;

namespace VisualStudio.OpenFolder
{
    /// <summary>
    /// Defines methods to support the comparison of objects of type CppProperties for equality.
    /// </summary>
    [GeneratedCode("Microsoft.Json.Schema.ToDotNet", "0.63.0.0")]
    internal sealed class CppPropertiesEqualityComparer : IEqualityComparer<CppProperties>
    {
        internal static readonly CppPropertiesEqualityComparer Instance = new CppPropertiesEqualityComparer();

        public bool Equals(CppProperties left, CppProperties right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
            {
                return false;
            }

            if (left.ProjectName != right.ProjectName)
            {
                return false;
            }

            if (!object.ReferenceEquals(left.Configurations, right.Configurations))
            {
                if (left.Configurations == null || right.Configurations == null)
                {
                    return false;
                }

                if (left.Configurations.Count != right.Configurations.Count)
                {
                    return false;
                }

                for (int index_0 = 0; index_0 < left.Configurations.Count; ++index_0)
                {
                    if (!object.Equals(left.Configurations[index_0], right.Configurations[index_0]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public int GetHashCode(CppProperties obj)
        {
            if (ReferenceEquals(obj, null))
            {
                return 0;
            }

            int result = 17;
            unchecked
            {
                if (obj.ProjectName != null)
                {
                    result = (result * 31) + obj.ProjectName.GetHashCode();
                }

                if (obj.Configurations != null)
                {
                    foreach (var value_0 in obj.Configurations)
                    {
                        result = result * 31;
                        if (value_0 != null)
                        {
                            result = (result * 31) + value_0.GetHashCode();
                        }
                    }
                }
            }

            return result;
        }
    }
}