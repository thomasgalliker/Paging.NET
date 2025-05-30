﻿namespace Paging.MAUI.Tests.TestData
{
    internal static class Cars
    {
        internal static IEnumerable<Car> CreateCars(int count)
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
