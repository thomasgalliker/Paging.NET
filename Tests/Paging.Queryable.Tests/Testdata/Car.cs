using System;
using System.Diagnostics;

namespace Paging.Queryable.Tests.Testdata
{
    [DebuggerDisplay("Car: {this.Id} {this.Name} {this.Model} {this.Year}")]
    public class Car
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Model { get; set; }

        public decimal? Price { get; set; }

        public int Year { get; set; }

        public DateTime? LastService { get; set; }

        public bool IsElectric { get; set; }
    }

    [DebuggerDisplay("CarDto: {this.Id} {this.Name} {this.Model} {this.Year}")]
    public class CarDto
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Model { get; set; }

        public decimal? Price { get; set; }

        public int Year { get; set; }

        public DateTime? LastService { get; set; }

        public bool IsElectric { get; set; }

        public override string ToString()
        {
            return $"{this.Name} {this.Model}, Year {this.Year}";
        }
    }
}
