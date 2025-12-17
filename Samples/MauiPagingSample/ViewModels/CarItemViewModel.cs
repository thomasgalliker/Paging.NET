using CommunityToolkit.Mvvm.ComponentModel;
using MauiPagingSample.Model;

namespace MauiPagingSample.ViewModels
{
    public class CarItemViewModel : ObservableObject
    {
        public CarItemViewModel(Car car)
        {
            this.Id = car.Id;
            this.Name = car.Name;
        }

        public int Id { get; init; }

        public string? Name { get; init; }
    }
}