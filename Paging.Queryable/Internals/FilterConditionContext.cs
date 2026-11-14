using System.Diagnostics;
using System.Linq.Expressions;

namespace Paging.Queryable.Internals
{
    /// <summary>
    /// Carries the per-condition context a filter predicate is built under:
    /// the external (client-facing) property name, the resolved property path
    /// and the configured <see cref="InvalidValueHandling"/>.
    /// </summary>
    internal readonly struct FilterConditionContext
    {
        internal FilterConditionContext(string externalName, string propertyPath, InvalidValueHandling invalidValueHandling)
        {
            this.ExternalName = externalName;
            this.PropertyPath = propertyPath;
            this.InvalidValueHandling = invalidValueHandling;
        }

        internal string ExternalName { get; }

        internal string PropertyPath { get; }

        internal InvalidValueHandling InvalidValueHandling { get; }

        /// <summary>
        /// Handles an invalid filter value according to the configured <see cref="InvalidValueHandling"/>:
        /// throws a <see cref="PagingException"/> naming the external property (Throw), or traces the
        /// reason and returns <c>null</c> so the condition is skipped (Skip, the default).
        /// </summary>
        internal Expression? Invalid(string reason, Exception? innerException = null)
        {
            if (this.InvalidValueHandling == InvalidValueHandling.Throw)
            {
                throw new PagingException(
                    $"Filter for property '{this.ExternalName}' cannot be applied: {reason}",
                    this.ExternalName,
                    innerException);
            }

            Trace.WriteLine($"Paging.ApplyFilter: {reason}");
            return null;
        }
    }
}
