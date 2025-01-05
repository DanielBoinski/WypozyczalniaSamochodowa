using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WypozyczalniaSamochodowa.Data;
using WypozyczalniaSamochodowa.Models;
using WypozyczalniaSamochodowa.Services;

namespace WypozyczalniaSamochodowa.Controllers
{
    public class WynajecieController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration; 

        public WynajecieController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration; 
        }



        // GET: Wynajecie
        [Authorize]
        public async Task<IActionResult> Index(string? UserEmail)
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool isAdmin = User.IsInRole("Administrator");

            IQueryable<Wynajecie> wynajecia = _context.Wynajecie
                .Include(w => w.Auto)
                .Include(w => w.User);

            if (!isAdmin)
            {
                wynajecia = wynajecia.Where(w => w.UserId == userId);
            }

           
            if (!string.IsNullOrEmpty(UserEmail))
            {
                wynajecia = wynajecia.Where(w => w.User.Email.Contains(UserEmail));
                ViewData["UserEmail"] = UserEmail; 
            }

            return View(await wynajecia.ToListAsync());
        }



        // GET: Wynajecie/Details/5
        [Authorize]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var wynajecie = await _context.Wynajecie
                .Include(w => w.Auto)
                .Include(w => w.User)
                .FirstOrDefaultAsync(m => m.WynajecieId == id);
            if (wynajecie == null)
            {
                return NotFound();
            }

            return View(wynajecie);
        }

        // GET: Wynajecie/Create
        [Authorize]
        public IActionResult Create(int? autoId, int? cena)
        {
            if (autoId == null || cena == null)
            {
                return BadRequest("Brak wymaganych parametrów.");
            }

          
            Console.WriteLine($"autoId: {autoId}, cena: {cena}");

           
            var auto = _context.Auto.FirstOrDefault(a => a.AutoId == autoId);
            if (auto == null)
            {
                Console.WriteLine("Auto nie zostało znalezione w bazie danych.");
                return NotFound("Nie znaleziono auta.");
            }

        
            var zajeteOkresy = _context.Wynajecie
                .Where(w => w.AutoId == autoId)
                .Select(w => new
                {
                    Start = w.DataRozpoczecia,
                    End = w.DataZakonczenia
                }).ToList();

           
            ViewData["AutoId"] = new SelectList(_context.Auto, "AutoId", "AutoId", autoId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id");
            ViewBag.Cena = cena; 
            ViewBag.AutoNazwa = $"{auto.Marka} {auto.Model}"; 
            ViewBag.ZajeteOkresy = zajeteOkresy;

            return View();
        }





        // POST: Wynajecie/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("WynajecieId,AutoId,DataRozpoczecia,DataZakonczenia,CenaCalkowita,Latitude,Longitude,Address,PaymentMethod")] Wynajecie wynajecie)
        {
          
            wynajecie.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

           
            Console.WriteLine($"Latitude: {wynajecie.Latitude}, Longitude: {wynajecie.Longitude}, Address: {wynajecie.Address}");
            Console.WriteLine($"DataRozpoczecia: {wynajecie.DataRozpoczecia}, DataZakonczenia: {wynajecie.DataZakonczenia}");

           
            if (wynajecie.DataRozpoczecia >= wynajecie.DataZakonczenia)
            {
                ModelState.AddModelError("", "Data rozpoczęcia musi być wcześniejsza niż data zakończenia.");
                ViewBag.Cena = wynajecie.CenaCalkowita; 
                ViewData["AutoId"] = new SelectList(_context.Auto, "AutoId", "AutoId", wynajecie.AutoId);
                return View(wynajecie);
            }

         
            var czyAutoDostepne = !_context.Wynajecie
                .Any(w => w.AutoId == wynajecie.AutoId &&
                          ((wynajecie.DataRozpoczecia >= w.DataRozpoczecia && wynajecie.DataRozpoczecia < w.DataZakonczenia) ||
                           (wynajecie.DataZakonczenia > w.DataRozpoczecia && wynajecie.DataZakonczenia <= w.DataZakonczenia)));

            if (!czyAutoDostepne)
            {
                ModelState.AddModelError("", "Auto jest już wynajęte w wybranym okresie.");
                ViewBag.Cena = wynajecie.CenaCalkowita; 
                ViewData["AutoId"] = new SelectList(_context.Auto, "AutoId", "AutoId", wynajecie.AutoId);
                return View(wynajecie);
            }

            
            var auto = await _context.Auto.FindAsync(wynajecie.AutoId);
            if (auto == null)
            {
                return NotFound("Nie znaleziono auta.");
            }

           
            wynajecie.Address ??= "Nie podano adresu";
            wynajecie.Latitude ??= 0;
            wynajecie.Longitude ??= 0;

        
            var liczbaDni = (wynajecie.DataZakonczenia - wynajecie.DataRozpoczecia).Days;
            wynajecie.CenaCalkowita = liczbaDni * auto.Cena;

           
            if (!ModelState.IsValid)
            {
                Console.WriteLine("ModelState is not valid");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"ModelState Error: {error.ErrorMessage}");
                }
                ViewData["AutoId"] = new SelectList(_context.Auto, "AutoId", "AutoId", wynajecie.AutoId);
                return View(wynajecie);
            }

         
            if (wynajecie.PaymentMethod == "Cash")
            {
                _context.Add(wynajecie);
                auto.Status = false; 
                _context.Update(auto);

                await _context.SaveChangesAsync();

                
                try
                {
                    var user = await _context.Users.FindAsync(wynajecie.UserId);
                    if (user != null)
                    {
                        var emailService = new EmailService(_configuration);
                        string subject = "Potwierdzenie wynajęcia auta";
                        string plainTextContent = $"Dziękujemy za wynajęcie auta {auto.Marka} {auto.Model}. Twoje wynajęcie trwa od {wynajecie.DataRozpoczecia:dd.MM.yyyy} do {wynajecie.DataZakonczenia:dd.MM.yyyy}.";
                        string htmlContent = $"<p>Dziękujemy za wynajęcie auta <strong>{auto.Marka} {auto.Model}</strong>.</p>" +
                                             $"<p>Twoje wynajęcie trwa od <strong>{wynajecie.DataRozpoczecia:dd.MM.yyyy}</strong> do <strong>{wynajecie.DataZakonczenia:dd.MM.yyyy}</strong>.</p>" +
                                             $"<p>Adres dostarczenia: {wynajecie.Address}</p>";

                        await emailService.SendEmailAsync(user.Email, subject, plainTextContent, htmlContent);
                        Console.WriteLine("Email został wysłany.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Wystąpił błąd podczas wysyłania e-maila: {ex.Message}");
                }

                return RedirectToAction(nameof(Index));
            }

            
            if (wynajecie.PaymentMethod == "Online")
            {
                
                return RedirectToAction("Payment", "Stripe", new
                {
                    amount = wynajecie.CenaCalkowita,
                    autoId = wynajecie.AutoId,
                    startDate = wynajecie.DataRozpoczecia.ToString("yyyy-MM-dd"),
                    endDate = wynajecie.DataZakonczenia.ToString("yyyy-MM-dd")
                });
            }

            return RedirectToAction(nameof(Index));
        }





        // GET: Wynajecie/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var wynajecie = await _context.Wynajecie.FindAsync(id);
            if (wynajecie == null)
            {
                return NotFound();
            }
            ViewData["AutoId"] = new SelectList(_context.Auto, "AutoId", "AutoId", wynajecie.AutoId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", wynajecie.UserId);
            return View(wynajecie);
        }

        // POST: Wynajecie/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("WynajecieId,KlientId,AutoId,DataRozpoczecia,DataZakonczenia,CenaCalkowita")] Wynajecie wynajecie)
        {
            if (id != wynajecie.WynajecieId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(wynajecie);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!WynajecieExists(wynajecie.WynajecieId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["AutoId"] = new SelectList(_context.Auto, "AutoId", "AutoId", wynajecie.AutoId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", wynajecie.UserId);
            return View(wynajecie);
        }

        // GET: Wynajecie/Delete/5
        [Authorize]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var wynajecie = await _context.Wynajecie
                .Include(w => w.Auto)
                .Include(w => w.User)
                .FirstOrDefaultAsync(m => m.WynajecieId == id);
            if (wynajecie == null)
            {
                return NotFound();
            }

            return View(wynajecie);
        }

        // POST: Wynajecie/Delete/5
        [Authorize]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var wynajecie = await _context.Wynajecie.FindAsync(id);
            if (wynajecie != null)
            {
                _context.Wynajecie.Remove(wynajecie);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool WynajecieExists(int id)
        {
            return _context.Wynajecie.Any(e => e.WynajecieId == id);
        }
    }
}
