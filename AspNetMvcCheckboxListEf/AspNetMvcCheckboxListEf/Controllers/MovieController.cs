using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using AspNetMvcCheckboxListEf.Models;
using AspNetMvcCheckboxListEf.ViewsModels;
using System.Data.Objects;
using System.Data.Entity.Infrastructure;
using System.Data;

namespace AspNetMvcCheckboxListEf.Controllers
{
    public class MovieController : Controller
    {
        //
        // GET: /Movie/

        public ViewResult Index()
        {
            using (var db = new TheMovieContext())
            {
                
                return View(db.Movies.OrderBy(x => x.MovieName).ToList());
            }
        }


        public ViewResult Input(int id = 0)
        {
            using (var db = new TheMovieContext())
            {
                var movie = id != 0 ? db.Movies.Find(id) : new Movie { Genres = new List<Genre>() };
                return View(new MovieInputViewModel
                {
                    TheMovie = movie,
                    GenreSelections = db.Genres.OrderBy(x => x.GenreName).ToList(),
                    SelectedGenres = movie.Genres.Select(x => x.GenreId).ToList()
                });
            }
        }//Input

        [HttpPost]
        public ActionResult Save(MovieInputViewModel input)
        {
            using (var db = new TheMovieContext())
            {
                try
                {
                    bool isNew = input.TheMovie.MovieId == 0;

                    input.SelectedGenres = input.SelectedGenres ?? new List<int>();                    
                    input.GenreSelections = db.Genres.AsNoTracking().OrderBy(x => x.GenreName).ToList();


                    var movie = !isNew ? db.Movies.Find(input.TheMovie.MovieId) : input.TheMovie;                    

                    if (isNew)
                        db.Movies.Add(movie);
                    else
                    {
                        db.Entry(movie).Property(x => x.Version).OriginalValue = input.TheMovie.Version;

                        // If you are using ConcurrencyCheck attribute on a Version property, add this code. 
                        // It's better to use Timestamp attribute, see the Movie class' Version property, we changed it now to Timestamp(was ConcurrencyCheck before)
                        // db.Entry(movie).State = System.Data.EntityState.Unchanged; 


                        // dirty this all the time even this is not changed by the user, 
                        // so we have a simplified mechanism for concurrent update 
                        // when the associated Genre(s) is modified
                        db.Entry(movie).Property(x => x.MovieName).IsModified = true;
                    }


                    movie.Genres = movie.Genres ?? new List<Genre>();
                    movie.Genres.Clear();
                    foreach (int g in input.SelectedGenres)
                    {
                        // What is Entity Framework analogous to NHibernate's session.Load<Genre>(g) ?
                        // db.Genres.Find(g) is not efficient

                        /*var gx = new Genre { GenreId = g };
                        db.Entry(gx).State = EntityState.Unchanged;*/


                        /*if (db.ChangeTracker.Entries().Where(x => ObjectContext.GetObjectType( x.Entity.GetType() ) == typeof(Genre)).Any())
                            throw new Exception("Working");

                        string sx = string.Join(", ",
                        db.ChangeTracker.Entries().Where(x => ObjectContext.GetObjectType(x.Entity.GetType()) == typeof(Genre)).Select(x => ((Genre)x.Entity).GenreId.ToString()).ToArray()
                        );*/

                        /*string sx = string.Join(", ",
                        db.ChangeTracker.Entries<Genre>().Select(x => x.Entity.GenreId.ToString() + " " + x.State).ToArray());*/

                        /*
                        if (!db.ChangeTracker.Entries<Genre>().Any(x => x.Entity.GenreId == g))
                        {
                            var gx = new Genre { GenreId = g };
                            movie.Genres.Add(gx);
                        }
                        else
                        {
                            // db.ChangeTracker.Entries<Genre>().Any(x => x.Entity.GenreId == g)
                            movie.Genres.Add(db.Genres.Find(g));
                        }*/


                        
                        var cachedGenre = db.ChangeTracker.Entries<Genre>().SingleOrDefault(x => x.Entity.GenreId == g);

                        Genre gx = null;
                        if (cachedGenre != null)
                        {
                            gx = cachedGenre.Entity;
                            input.MessageToUser = input.MessageToUser + "Already in cache: " + g.ToString() + ";";
                        }
                        else
                        {
                            gx = new Genre { GenreId = g };
                            db.Entry(gx).State = EntityState.Unchanged;
                            // db.Genres.Attach(gx);
                            input.MessageToUser = input.MessageToUser + "Not yet in cache " + g.ToString() + ";";
                        }

                        movie.Genres.Add(gx);

                        


                            // throw new Exception("Working" + sx);
                        /*var gx = db.Genres.Create();
                        gx.GenreId = g;                        
                        db.Entry(gx).Property(x => x.GenreName).IsModified = false;*/

                        // movie.Genres.Add(db.Genres.Find(g));
                        // movie.Genres.Add(gx);

                        
                        
                    }

                    // http://www.joe-stevens.com/2010/02/17/asp-net-mvc-using-controller-updatemodel-when-using-a-viewmodel/
                    // null will get all properties under TheMovie object, the Version is excluded                 
                    UpdateModel(movie, "TheMovie", includeProperties: null, excludeProperties: new string[] { "Version" });
                    



                    db.SaveChanges();

                    db.Entry(movie).Reload();
                    input.TheMovie.Version = movie.Version;


                    ModelState.Remove("TheMovie.MovieId");

                    // No need to remove TheMovie.Version, ASP.NET MVC is not preserving the ModelState of variables with byte array type.
                    // Hence, with byte array, the HiddenFor will always gets its value from the model, not from the ModelState
                    // ModelState.Remove("TheMovie.Version"); 



                    input.MessageToUser = input.MessageToUser + " " + string.Format("Saved. {0}", isNew ? "ID is " + input.TheMovie.MovieId : "");

                }
                catch (DbUpdateConcurrencyException)
                {
                    ModelState.AddModelError(string.Empty, 
                        "The record you attempted to edit was already modified by another user since you last loaded it. Open the latest changes on this record");
                }
            }

            return View("Input", input);
        }
    }
}
