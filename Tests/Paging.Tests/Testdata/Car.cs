using System.Diagnostics;

namespace Paging.Tests.Testdata
{
    [DebuggerDisplay("Car: {this.Id}")]
    public class Car
    {
        public int Id { get; set; }
    }
}