using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace api.Data
{
    public class ApplicationDBContex : IdentityDbContext<User>
    {
        public ApplicationDBContex(DbContextOptions<ApplicationDBContex> dbContextOptions)
        :base(dbContextOptions)
        {
            
        }
        public DbSet<ResponseItem> ResponseItem {get; set;}
        public DbSet<Rate> Rates{get; set;}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Rate>()
                .HasOne(r => r.ResponseItem)
                .WithMany(ri => ri.Rates)
                .HasForeignKey(r => r.ResponseItemId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}