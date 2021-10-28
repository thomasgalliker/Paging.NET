using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Paging;
using WpfPagingSample.Model;

namespace WpfPagingSample.Services
{
    public class EmployeeServiceStub : IEmployeeService
    {
        private readonly List<Employee> employees;

        public EmployeeServiceStub()
        {
            this.employees = Enumerable.Range(1, 1000)
                .Select(i => new Employee { Id = i, FirstName = $"FirstName {i}", LastName = $"LastName {i}" })
                .ToList();
        }

        public Task<PaginationSet<Employee>> GetEmployeesAsync(PagingInfo pagingInfo)
        {
            // Use Paging.Queryable NuGet package to perform search/sort/group/paging
            var skip = (pagingInfo.CurrentPage - 1) * pagingInfo.ItemsPerPage;
            var take = pagingInfo.ItemsPerPage;
            var queryable = this.employees.Skip(skip).Take(take);

            var pagingSet = new PaginationSet<Employee>(pagingInfo, queryable, this.employees.Count, this.employees.Count);
            return Task.FromResult(pagingSet);
        }
    }
}