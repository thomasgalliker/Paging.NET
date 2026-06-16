using Microsoft.EntityFrameworkCore;

namespace Paging.EF.Tests.TestData
{
    public class LicenseContext : DbContext
    {
        public LicenseContext(DbContextOptions<LicenseContext> options)
            : base(options)
        {
        }

        public DbSet<License> Licenses => this.Set<License>();
    }
}