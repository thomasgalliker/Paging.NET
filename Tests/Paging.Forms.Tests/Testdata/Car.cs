﻿using System.Diagnostics;

namespace Paging.Queryable.Tests.Testdata
{
    [DebuggerDisplay("Car: {this.Id}")]
    public class Car
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }

    [DebuggerDisplay("CarDto: {this.Id}")]
    public class CarDto
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }
}