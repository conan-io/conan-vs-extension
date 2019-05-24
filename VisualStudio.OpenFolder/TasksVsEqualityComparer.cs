using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;

namespace VisualStudio.OpenFolder
{
    /// <summary>
    /// Defines methods to support the comparison of objects of type TasksVs for equality.
    /// </summary>
    [GeneratedCode("Microsoft.Json.Schema.ToDotNet", "0.63.0.0")]
    internal sealed class TasksVsEqualityComparer : IEqualityComparer<TasksVs>
    {
        internal static readonly TasksVsEqualityComparer Instance = new TasksVsEqualityComparer();

        public bool Equals(TasksVs left, TasksVs right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
            {
                return false;
            }

            if (left.Version != right.Version)
            {
                return false;
            }

            if (!object.Equals(left.Variables, right.Variables))
            {
                return false;
            }

            if (!object.ReferenceEquals(left.Tasks, right.Tasks))
            {
                if (left.Tasks == null || right.Tasks == null)
                {
                    return false;
                }

                if (left.Tasks.Count != right.Tasks.Count)
                {
                    return false;
                }

                for (int index_0 = 0; index_0 < left.Tasks.Count; ++index_0)
                {
                    if (!object.Equals(left.Tasks[index_0], right.Tasks[index_0]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public int GetHashCode(TasksVs obj)
        {
            if (ReferenceEquals(obj, null))
            {
                return 0;
            }

            int result = 17;
            unchecked
            {
                if (obj.Version != null)
                {
                    result = (result * 31) + obj.Version.GetHashCode();
                }

                if (obj.Variables != null)
                {
                    result = (result * 31) + obj.Variables.GetHashCode();
                }

                if (obj.Tasks != null)
                {
                    foreach (var value_0 in obj.Tasks)
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