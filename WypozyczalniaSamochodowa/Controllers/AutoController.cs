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

namespace WypozyczalniaSamochodowa.Controllers
{
    public class AutoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AutoController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Auto
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Index()
        {
            return View(await _context.Auto.ToListAsync());
        }

        // GET: Auto/Details/5
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var auto = await _context.Auto
                .FirstOrDefaultAsync(m => m.AutoId == id);
            if (auto == null)
            {
                return NotFound();
            }

            return View(auto);
        }

        // GET: Auto/Create
        [Authorize(Roles = "Administrator")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Auto/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Administrator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AutoId,Marka,Model,Silnik,RokProdukcji,Opis,Cena")] Auto auto, IFormFile Zdjecie)
        {
            if (ModelState.IsValid)
            {
                
                if (Zdjecie != null && Zdjecie.Length > 0)
                {
                    
                    var fileName = Path.GetFileNameWithoutExtension(Zdjecie.FileName);
                    var extension = Path.GetExtension(Zdjecie.FileName);
                    var uniqueFileName = $"{fileName}_{Guid.NewGuid()}{extension}";

                    
                    var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", uniqueFileName);

                    
                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await Zdjecie.CopyToAsync(stream);
                    }

                    
                    auto.Zdjecie = $"/images/{uniqueFileName}";
                }

                
                _context.Add(auto);
                await _context.SaveChangesAsync();

                var oferta = new Oferta
                {
                    AutoId = auto.AutoId 
                };

                _context.Set<Oferta>().Add(oferta); 
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            return View(auto);
        }


        // GET: Auto/Edit/5
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var auto = await _context.Auto.FindAsync(id);
            if (auto == null)
            {
                return NotFound();
            }
            return View(auto);
        }

        // POST: Auto/Edit/5
        [Authorize(Roles = "Administrator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("AutoId,Marka,Model,Silnik,RokProdukcji,Opis,Cena,Zdjecie")] Auto auto, IFormFile? noweZdjecie)
        {
            if (id != auto.AutoId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                   
                    var istniejąceAuto = await _context.Auto.AsNoTracking().FirstOrDefaultAsync(a => a.AutoId == id);
                    if (istniejąceAuto == null)
                    {
                        return NotFound();
                    }

                    
                    if (noweZdjecie != null && noweZdjecie.Length > 0)
                    {
                       
                        if (!string.IsNullOrEmpty(istniejąceAuto.Zdjecie))
                        {
                            var staraSciezka = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", istniejąceAuto.Zdjecie.TrimStart('/'));
                            if (System.IO.File.Exists(staraSciezka))
                            {
                                System.IO.File.Delete(staraSciezka);
                            }
                        }

                        
                        var fileName = Path.GetFileNameWithoutExtension(noweZdjecie.FileName);
                        var extension = Path.GetExtension(noweZdjecie.FileName);
                        var uniqueFileName = $"{fileName}_{Guid.NewGuid()}{extension}";
                        var nowaSciezka = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", uniqueFileName);

                        using (var stream = new FileStream(nowaSciezka, FileMode.Create))
                        {
                            await noweZdjecie.CopyToAsync(stream);
                        }

                        auto.Zdjecie = $"/images/{uniqueFileName}"; 
                    }
                    else
                    {
                       
                        auto.Zdjecie = istniejąceAuto.Zdjecie;
                    }

                   
                    _context.Update(auto);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AutoExists(auto.AutoId))
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

            return View(auto);
        }

        


       



        // GET: Auto/Delete/5
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var auto = await _context.Auto
                .FirstOrDefaultAsync(m => m.AutoId == id);
            if (auto == null)
            {
                return NotFound();
            }

            return View(auto);
        }

        // POST: Auto/Delete/5
        [Authorize(Roles = "Administrator")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var auto = await _context.Auto.FindAsync(id);

            if (auto != null)
            {
                // Usunięcie zdjęcia, jeśli istnieje
                if (!string.IsNullOrEmpty(auto.Zdjecie))
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", auto.Zdjecie.TrimStart('/'));

                    if (System.IO.File.Exists(filePath))
                    {
                        try
                        {
                            System.IO.File.Delete(filePath); // Usuń plik zdjęcia
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Błąd podczas usuwania pliku: {ex.Message}");
                        }
                    }
                }

                // Usuń rekord auta z bazy danych
                _context.Auto.Remove(auto);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool AutoExists(int id)
        {
            return _context.Auto.Any(e => e.AutoId == id);
        }

        // GET: Formularz blokowania auta
        [Authorize(Roles = "Administrator")]
        public IActionResult Block(int id)
        {
            var auto = _context.Auto.Find(id);
            if (auto == null)
            {
                return NotFound();
            }

            ViewBag.AutoId = id;
            ViewBag.Marka = auto.Marka;
            ViewBag.Model = auto.Model;

            return View();
        }

        // POST: Blokowanie auta

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Block(int autoId, DateTime dataRozpoczecia, DateTime dataZakonczenia)
        {
            if (dataRozpoczecia >= dataZakonczenia)
            {
                ModelState.AddModelError("", "Data rozpoczęcia musi być wcześniejsza niż data zakończenia.");
                ViewBag.AutoId = autoId;
                return View();
            }

            var czyAutoDostepne = !_context.Wynajecie.Any(w =>
                w.AutoId == autoId &&
                dataRozpoczecia < w.DataZakonczenia &&
                dataZakonczenia > w.DataRozpoczecia);

            if (!czyAutoDostepne)
            {
                ModelState.AddModelError("", "Auto jest już zajęte w wybranym okresie.");
                ViewBag.AutoId = autoId;
                return View();
            }

            string adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(adminUserId))
            {
                return Forbid();
            }

            var blokada = new Wynajecie
            {
                AutoId = autoId,
                DataRozpoczecia = dataRozpoczecia,
                DataZakonczenia = dataZakonczenia,
                UserId = adminUserId,
                CenaCalkowita = 0,
                PaymentMethod = "AdminAction" 
            };

            _context.Wynajecie.Add(blokada);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }




    }
}
