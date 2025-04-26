using System.Threading.Tasks;

namespace Paging.Forms
{
    public interface IInfiniteScrollLoader
    {
        bool CanLoadMore { get; }

        Task LoadMoreAsync();
    }
}
