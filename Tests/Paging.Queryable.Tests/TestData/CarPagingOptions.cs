namespace Paging.Queryable.Tests.TestData
{
    /// <summary>
    /// Class-based paging options, e.g. registered in dependency injection.
    /// Runtime dependencies (like the current date/time) are injected via constructor
    /// and consumed in sort key expression factories so that each query
    /// uses a fresh value.
    /// </summary>
    internal class CarPagingOptions : PagingOptions<Car, CarDto>
    {
        public CarPagingOptions(IDateTime dateTime)
        {
            this.Property(c => c.Name).Sortable().Filterable();
            this.Property(c => c.Model).HasName("Brand").Sortable().Filterable();

            this.Property("Age").Sortable(() =>
            {
                var now = dateTime.UtcNow;
                return (Expression<Func<Car, int>>)(c => now.Year - c.Year);
            });

            this.DefaultSort(c => c.Id);

            this.Search(s => c => c.Name != null && c.Name.ToLower().Contains(s.ToLower()));

            this.Map(CarFactory.MapCarsToCarDtos);
        }
    }
}
