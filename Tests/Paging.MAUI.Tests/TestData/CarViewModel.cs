namespace Paging.MAUI.Tests.TestData
{
    /// <summary>
    /// The UI-facing type bound by the collection. A page loader returns <see cref="CarDto"/>s from the
    /// backend and the collection maps them into <see cref="CarViewModel"/>s for binding.
    /// </summary>
    [DebuggerDisplay("CarViewModel: {this.Id}")]
    public class CarViewModel
    {
        public int Id { get; set; }

        public string? Name { get; set; }
    }
}
