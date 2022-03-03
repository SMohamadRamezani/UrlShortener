using ShortUrl.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using System.Web;

namespace ShortUrl.DAL
{
    public class ShortUrlContext : DbContext
    {
        public ShortUrlContext() : base("ShortUrlContext")
        {
        }

        public DbSet<UrlAdresses> UrlAdresses { get; set; }


        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
        }
    }
}