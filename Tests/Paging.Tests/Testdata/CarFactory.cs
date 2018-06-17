using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paging.Tests.Testdata
{
    internal static class CarFactory
    {
        internal static IEnumerable<Car> GenerateCarsList(int count)
        {
            for (var i = 0; i < count; i++)
            {
                yield return new Car { Id = i, Name = $"Car {i}" };
            }
        }

        internal static IEnumerable<CarDto> MapCarsToCarDtos(IEnumerable<Car> cars)
        {
            // Use some more sophisticated mapping logic here (e.g. AutoMapper)
            return cars.Select(car => new CarDto
            {
                Id = car.Id,
                Name = car.Name
            });
        }
    }
}
