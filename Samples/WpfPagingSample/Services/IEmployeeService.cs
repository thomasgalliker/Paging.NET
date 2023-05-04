using System.Threading.Tasks;
using Paging;
using WpfPagingSample.Model;

namespace WpfPagingSample.Services
{
    public interface IEmployeeService
    {
        Task<PaginationSet<Employee>> GetEmployeesAsync(PagingInfo pagingInfo);
    }
}