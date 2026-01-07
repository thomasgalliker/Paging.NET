using System.Diagnostics;

namespace Paging.Internals
{
    [DebuggerDisplay("PagingInfo.Default", Type = "DefaultPagingInfo")]
    internal class DefaultPagingInfo : PagingInfo
    {
        public override int CurrentPage
        {
            get => base.CurrentPage;
            set => ThrowReadOnly();
        }

        public override int ItemsPerPage
        {
            get => base.ItemsPerPage;
            set => ThrowReadOnly();
        }

        public override string? SortBy
        {
            get => base.SortBy;
            set => ThrowReadOnly();
        }

        public override IReadOnlyDictionary<string, SortOrder> Sorting
        {
            get => base.Sorting;
            set => ThrowReadOnly();
        }

        public override bool Reverse
        {
            get => base.Reverse;
            set => ThrowReadOnly();
        }

        public override string? Search
        {
            get => base.Search;
            set => ThrowReadOnly();
        }

        public override IDictionary<string, object?> Filter
        {
            get => base.Filter;
            set => ThrowReadOnly();
        }

        private static void ThrowReadOnly()
        {
            throw new InvalidOperationException("Cannot modify read-only PagingInfo.Default instance.");
        }
    }
}
