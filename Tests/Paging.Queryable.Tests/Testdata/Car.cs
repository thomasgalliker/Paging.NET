using System.Diagnostics;

namespace Paging.Queryable.Tests.Testdata
{
    [DebuggerDisplay("Car: {this.Id} {this.Name} {this.Model} {this.Year}")]
    public class Car : IEquatable<Car?>
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        public string? Model { get; set; }

        public decimal? Price { get; set; }

        public int Year { get; set; }

        public DateTime? LastService { get; set; }

        public DateTimeOffset LastOilChange { get; set; }

        public bool IsElectric { get; set; }

        public bool Equals(Car? other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return this.Id == other.Id;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj is Car other && this.Equals(other);
        }

        public override int GetHashCode()
        {
            return this.Id;
        }
    }

    [DebuggerDisplay("CarDto: {this.Id} {this.Name} {this.Model} {this.Year}")]
    public class CarDto : IEquatable<CarDto>
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        public string? Model { get; set; }

        public decimal? Price { get; set; }

        public int Year { get; set; }

        public DateTime? LastService { get; set; }

        public DateTimeOffset LastOilChange { get; set; }

        public bool IsElectric { get; set; }

        public override string ToString()
        {
            return $"{this.Name} {this.Model}, Year {this.Year}";
        }

        public bool Equals(CarDto? other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return this.Id == other.Id;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj is CarDto other && this.Equals(other);
        }

        public override int GetHashCode()
        {
            return this.Id;
        }
    }
}