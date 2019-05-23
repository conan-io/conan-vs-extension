using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;

namespace VisualStudio.OpenFolder
{
    /// <summary>
    /// Defines methods to support the comparison of objects of type CMakeSettings for equality.
    /// </summary>
    [GeneratedCode("Microsoft.Json.Schema.ToDotNet", "0.63.0.0")]
    internal sealed class CMakeSettingsEqualityComparer : IEqualityComparer<CMakeSettings>
    {
        internal static readonly CMakeSettingsEqualityComparer Instance = new CMakeSettingsEqualityComparer();

        public bool Equals(CMakeSettings left, CMakeSettings right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
            {
                return false;
            }

            if (!object.ReferenceEquals(left.Environments, right.Environments))
            {
                if (left.Environments == null || right.Environments == null)
                {
                    return false;
                }

                if (left.Environments.Count != right.Environments.Count)
                {
                    return false;
                }

                for (int index_0 = 0; index_0 < left.Environments.Count; ++index_0)
                {
                    if (!object.Equals(left.Environments[index_0], right.Environments[index_0]))
                    {
                        return false;
                    }
                }
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

                for (int index_1 = 0; index_1 < left.Configurations.Count; ++index_1)
                {
                    if (!object.Equals(left.Configurations[index_1], right.Configurations[index_1]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public int GetHashCode(CMakeSettings obj)
        {
            if (ReferenceEquals(obj, null))
            {
                return 0;
            }

            int result = 17;
            unchecked
            {
                if (obj.Environments != null)
                {
                    foreach (var value_0 in obj.Environments)
                    {
                        result = result * 31;
                        if (value_0 != null)
                        {
                            result = (result * 31) + value_0.GetHashCode();
                        }
                    }
                }

                if (obj.Configurations != null)
                {
                    foreach (var value_1 in obj.Configurations)
                    {
                        result = result * 31;
                        if (value_1 != null)
                        {
                            result = (result * 31) + value_1.GetHashCode();
                        }
                    }
                }
            }

            return result;
        }
    }
}