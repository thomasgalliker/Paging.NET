namespace Paging.MAUI.Tests.TestData
{
    /// <summary>
    /// The transport type returned by a backend/API. This is what a page loader yields; the collection
    /// projects it into a <see cref="CarViewModel"/> for binding.
    /// </summary>
    [DebuggerDisplay("CarDto: {this.Id}")]
    public class CarDto
    {
        public int Id { get; set; }

        public string? Name { get; set; }
    }
}
