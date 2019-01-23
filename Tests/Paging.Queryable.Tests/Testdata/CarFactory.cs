using System.Collections.Generic;
using System.Linq;

namespace Paging.Queryable.Tests.Testdata
{
    internal static class CarFactory
    {
        internal static IEnumerable<Car> GenerateCarsList(int count)
        {
            return GenerateCarsList("Car", "Model", count);
        }

        internal static IEnumerable<Car> GenerateCarsList(string name, string model, int count)
        {
            for (var i = 0; i < count; i++)
            {
                yield return new Car { Id = i, Name = $"{name} {i}", Model = model };
            }
        }

        internal static IEnumerable<CarDto> MapCarsToCarDtos(IEnumerable<Car> cars)
        {
            // Use some more sophisticated mapping logic here (e.g. AutoMapper)
            return cars.Select(car => new CarDto
            {
                Id = car.Id,
                Name = car.Name,
                Model = car.Model
            });
        }
    }
}
