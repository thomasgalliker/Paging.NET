namespace Paging.MAUI.Tests.TestData
{
    internal static class Cars
    {
        internal static IEnumerable<CarViewModel> CreateCarViewModels(int count)
        {
            for (var i = 0; i < count; i++)
            {
                yield return new CarViewModel { Id = i, Name = $"Car {i}" };
            }
        }
    }
}
