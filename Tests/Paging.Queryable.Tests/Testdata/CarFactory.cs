using System;
using System.Collections.Generic;
using System.Linq;

namespace Paging.Queryable.Tests.Testdata
{
    internal static class CarFactory
    {
        internal static IEnumerable<Car> GenerateCarsList(int count)
        {
            return GenerateCarsList(name: "Car", model: "Model", count: count);
        }

        internal static IEnumerable<Car> GenerateCarsList(string name, string model, int count)
        {
            return GenerateCarsList(name: name, model: model, price: null, year: 2019, count: count);
        }

        internal static IEnumerable<Car> GenerateCarsList(string name, string model, decimal? price, int year, int count)
        {
            return GenerateCarsList(name: name, model: model, price: price, year: year, lastService: null, isElectric: false, count: count);
        }

        internal static IEnumerable<Car> GenerateCarsList(string name, string model, decimal? price, int year, DateTime? lastService, bool isElectric, int count)
        {
            for (var i = 0; i < count; i++)
            {
                yield return new Car
                {
                    Id = i,
                    Name = $"{name}",
                    Model = $"{model} {i}",
                    Price = price,
                    Year = year,
                    LastService = lastService,
                    LastOilChange = lastService ?? DateTimeOffset.MinValue,
                    IsElectric = isElectric,
                };
            }
        }

        internal static IEnumerable<Car> Union(this IEnumerable<Car> first, IEnumerable<Car> second)
        {
            return Enumerable.Concat(first, second).WithUnitqueIds();
        }

        internal static IEnumerable<Car> WithUnitqueIds(this IEnumerable<Car> cars)
        {
            var carsArray = cars.ToArray();
            for (var i = 0; i < carsArray.Length; i++)
            {
                carsArray[i].Id = i;
            }

            return carsArray;
        }

        internal static IEnumerable<CarDto> MapCarsToCarDtos(IEnumerable<Car> cars)
        {
            // Use some more sophisticated mapping logic here (e.g. AutoMapper)
            return cars.Select(car => new CarDto
            {
                Id = car.Id,
                Name = car.Name,
                Model = car.Model,
                Price = car.Price,
                Year = car.Year,
                LastService = car.LastService,
                LastOilChange = car.LastOilChange,
                IsElectric = car.IsElectric
            });
        }
    }
}
