using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Data
{
    public class ApplicationDBContex : DbContext
    {
        public ApplicationDBContex(DbContextOptions dbContextOptions)
        :base(dbContextOptions)
        {
            
        }
        public DbSet<ResponseItem> ResponseItem {get; set;}
        public DbSet<Rate> Rates{get; set;}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Rate>()
                .HasOne(r => r.ResponseItem)
                .WithMany(ri => ri.Rates)
                .HasForeignKey(r => r.ResponseItemId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}