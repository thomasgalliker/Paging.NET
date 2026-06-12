namespace Paging.Queryable.EF.Tests.TestData
{
    public class License
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public DateTime ValidFrom { get; set; }

        public DateTime ValidTo { get; set; }

        public bool IsRevoked { get; set; }

        public int HolderId { get; set; }

        public Holder Holder { get; set; } = null!;
    }
}