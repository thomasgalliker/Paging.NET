using System;
using System.Linq;
using Paging;

namespace ConsoleApp
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Paging.NET Sample App");
            Console.WriteLine("");
            Console.WriteLine("Total number of items:");
            var itemsCount = Convert.ToInt32(Console.ReadLine());

            Console.WriteLine("Items per page: ");
            var pageInfo = new PagingInfo
            {
                ItemsPerPage = Convert.ToInt32(Console.ReadLine())
            };

            PaginationSet<int> pagingSet;

            do
            {
                var from = (pageInfo.CurrentPage - 1) * pageInfo.ItemsPerPage + 1;
                var remainingItems = itemsCount - (pageInfo.CurrentPage - 1) * pageInfo.ItemsPerPage;
                var count = remainingItems < pageInfo.ItemsPerPage
                    ? remainingItems
                    : pageInfo.ItemsPerPage;

                var items = Enumerable.Range(from, count).ToList();
                pagingSet = new PaginationSet<int>(pageInfo, items, itemsCount, itemsCount);
                foreach (var item in pagingSet.Items)
                {
                    Console.WriteLine("Item: {0}", item);
                }

                if (pagingSet.TotalPages > pagingSet.CurrentPage)
                {
                    pageInfo.CurrentPage++;

                    Console.BackgroundColor = ConsoleColor.Gray;
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.Write("[ENTER]");
                    Console.ResetColor();
                    Console.WriteLine($" to continue [{pagingSet.CurrentPage}/{pagingSet.TotalPages}]");
                    var input = Console.ReadLine();
                }
            } while (pagingSet.HasMorePages());

            // Alternative way to check if more pages available:
            //} while (!pagingSet.StopScroll(pageInfo));
        }
    }
}
