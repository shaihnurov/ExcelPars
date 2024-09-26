using Microsoft.EntityFrameworkCore;

namespace ExcelPars.Repository
{
    public class ApplicationContextDb : DbContext
    {
        public ApplicationContextDb() => Database.EnsureCreated();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=ExcelData;Trusted_Connection=True;");
        }
    }
}