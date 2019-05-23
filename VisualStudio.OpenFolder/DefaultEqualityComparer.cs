using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;

namespace VisualStudio.OpenFolder
{
    /// <summary>
    /// Defines methods to support the comparison of objects of type Default for equality.
    /// </summary>
    [GeneratedCode("Microsoft.Json.Schema.ToDotNet", "0.63.0.0")]
    internal sealed class DefaultEqualityComparer : IEqualityComparer<Default>
    {
        internal static readonly DefaultEqualityComparer Instance = new DefaultEqualityComparer();

        public bool Equals(Default left, Default right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
            {
                return false;
            }

            if (left.TaskName != right.TaskName)
            {
                return false;
            }

            if (left.TaskLabel != right.TaskLabel)
            {
                return false;
            }

            if (left.AppliesTo != right.AppliesTo)
            {
                return false;
            }

            if (left.ContextType != right.ContextType)
            {
                return false;
            }

            if (left.Output != right.Output)
            {
                return false;
            }

            if (!object.ReferenceEquals(left.InheritEnvironments, right.InheritEnvironments))
            {
                if (left.InheritEnvironments == null || right.InheritEnvironments == null)
                {
                    return false;
                }

                if (left.InheritEnvironments.Count != right.InheritEnvironments.Count)
                {
                    return false;
                }

                for (int index_0 = 0; index_0 < left.InheritEnvironments.Count; ++index_0)
                {
                    if (left.InheritEnvironments[index_0] != right.InheritEnvironments[index_0])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public int GetHashCode(Default obj)
        {
            if (ReferenceEquals(obj, null))
            {
                return 0;
            }

            int result = 17;
            unchecked
            {
                if (obj.TaskName != null)
                {
                    result = (result * 31) + obj.TaskName.GetHashCode();
                }

                if (obj.TaskLabel != null)
                {
                    result = (result * 31) + obj.TaskLabel.GetHashCode();
                }

                if (obj.AppliesTo != null)
                {
                    result = (result * 31) + obj.AppliesTo.GetHashCode();
                }

                if (obj.ContextType != null)
                {
                    result = (result * 31) + obj.ContextType.GetHashCode();
                }

                if (obj.Output != null)
                {
                    result = (result * 31) + obj.Output.GetHashCode();
                }

                if (obj.InheritEnvironments != null)
                {
                    foreach (var value_0 in obj.InheritEnvironments)
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