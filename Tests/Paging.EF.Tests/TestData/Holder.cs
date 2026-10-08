namespace Paging.EF.Tests.TestData
{
    public class Holder
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public List<License> Licenses { get; set; } = new List<License>();
    }
}