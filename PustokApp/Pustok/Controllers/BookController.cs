using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pustok.DAL;
using Pustok.Models;
using Pustok.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Pustok.Controllers
{
    public class BookController:Controller
    {
        private readonly PustokDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public BookController(PustokDbContext context,UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        public IActionResult GetBook(int id)
        {
            Book book = _context.Books.Include(x=> x.Genre).Include(x=> x.BookImages).Include(x=> x.Author).Include(X=> X.BookTags).ThenInclude(x=> x.Tag).FirstOrDefault(x=> x.Id == id);
            return PartialView("_BookModalPartial", book );
        }
        public  IActionResult Details(int id)
        {
            Book Book = _context.Books
             .Include(x => x.Author)
             .Include(x => x.BookImages)
             .Include(x => x.Genre)
             .Include(x => x.BookTags)
             .ThenInclude(x => x.Tag)
             .FirstOrDefault(x => x.Id == id);

            var RelatedBooks = _context.Books
                .Include(x => x.BookImages)
                .Include(x => x.Author)
                .Include(x => x.Genre)
                .Include(x => x.BookTags).ThenInclude(x => x.Tag)
                .Where(x => x.Genre == Book.Genre || x.AuthorId == Book.AuthorId)
                .ToList();

            BookDetailViewModel bookModel = new BookDetailViewModel
            {
                Book = Book,
                RelatedBooks = RelatedBooks,
                ReviewCreate = new ReviewCreateViewModel { BookId = id },
                Reviews = _context.Reviews.Include(x=> x.AppUser).Where(x=> x.BookId == id).ToList(),

            };
            return View(bookModel);
        }
        [Authorize(Roles ="Member")]
        [HttpPost]
        public async Task<IActionResult> Review(ReviewCreateViewModel reviewVm)
        {
            // # deactivate if you want to enable a person to add multiple reviews
            if (_context.Reviews.Include(x => x.AppUser).Any(x => x.BookId == reviewVm.BookId && x.AppUser.UserName == User.Identity.Name))
                ModelState.AddModelError("", "You have already reviewed this product");



            if (!ModelState.IsValid)
            {
                Book book = _context.Books
           .Include(x => x.Author)
           .Include(x => x.BookImages)
           .Include(x => x.Genre)
           .Include(x => x.BookTags)
           .ThenInclude(x => x.Tag)
           .FirstOrDefault(x => x.Id == reviewVm.BookId);

                BookDetailViewModel detailVm = new BookDetailViewModel
                {
                    Book = book,
                    RelatedBooks = _context.Books
                    .Include(x => x.BookImages)
                    .Include(x => x.Author)
                    .Include(x => x.Genre)
                    .Include(x => x.BookTags).ThenInclude(x => x.Tag)
                    .Where(x => x.Genre == book.Genre || x.AuthorId == book.AuthorId)
                    .ToList(),
                    ReviewCreate = reviewVm,
                    Reviews = _context.Reviews.Include(x => x.AppUser).Where(x => x.BookId == book.Id).ToList(),

                };

                return View("details", detailVm);
            }

            Review review = new Review
            {
                AppUser = await _userManager.FindByNameAsync(User.Identity.Name),
                BookId = reviewVm.BookId,
                Rate = reviewVm.Rate,
                Text = reviewVm.Text,
                CreatedAt = DateTime.UtcNow.AddHours(4),
                
            };

            


            _context.Reviews.Add(review);
            _context.SaveChanges();


            return RedirectToAction("details", new {id = reviewVm.BookId});
        }
    }
}
