using System;

namespace Paging
{
    public class PagingInfo : IEquatable<PagingInfo>
    {
        public static readonly PagingInfo Default = new PagingInfo();

        public PagingInfo()
        {
            this.CurrentPage = 1;
        }

        public int CurrentPage { get; set; }

        public int ItemsPerPage { get; set; }

        public string SortBy { get; set; }

        public bool Reverse { get; set; }

        public string Search { get; set; }

        public static bool operator ==(PagingInfo pi1, PagingInfo pi2)
        {
            return Equals(pi1, pi2);
        }

        public static bool operator !=(PagingInfo pi1, PagingInfo pi2)
        {
            return !(pi1 == pi2);
        }

        public bool Equals(PagingInfo other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return
                this.CurrentPage == other.CurrentPage &&
                this.ItemsPerPage == other.ItemsPerPage &&
                string.Equals(this.SortBy, other.SortBy) &&
                this.Reverse == other.Reverse &&
                string.Equals(this.Search, other.Search);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return this.Equals((PagingInfo)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = this.CurrentPage;
                hashCode = (hashCode * 397) ^ this.ItemsPerPage;
                hashCode = (hashCode * 397) ^ (this.SortBy != null ? this.SortBy.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ this.Reverse.GetHashCode();
                hashCode = (hashCode * 397) ^ (this.Search != null ? this.Search.GetHashCode() : 0);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return this.ToQueryString();
        }
    }
}