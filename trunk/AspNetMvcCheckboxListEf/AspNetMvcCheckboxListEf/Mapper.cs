using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using AspNetMvcCheckboxListEf.Models;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace AspNetMvcCheckboxListEf
{

    public class TheMovieContext : DbContext
    {
        public DbSet<Movie> Movies { get; set; }
        public DbSet<Genre> Genres { get; set; }


        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {           
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            modelBuilder.Entity<Movie>().HasMany(x => x.Genres).WithMany(x => x.Movies).Map(x =>
                {
                    x.ToTable("MovieAssocGenre");
                    x.MapLeftKey("MovieId");
                    x.MapRightKey("GenreId");
                });

            modelBuilder.Entity<Genre>().HasMany(x => x.Movies).WithMany(x => x.Genres).Map(x =>
            {
                x.ToTable("MovieAssocGenre");
                x.MapLeftKey("GenreId");
                x.MapRightKey("MovieId");
            });


            
            
            base.OnModelCreating(modelBuilder);
        }

        


    }
}