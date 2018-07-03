using System;
using Microsoft.EntityFrameworkCore;

namespace MyLittleUrlAPI.Models
{
    public class LittleUrlContext : DbContext
    {
        public LittleUrlContext(DbContextOptions<LittleUrlContext> options)
            : base (options)
        {
        }

        public DbSet<LittleUrl> littleUrlList
        {
            get;
            set;
        }
    }
}
