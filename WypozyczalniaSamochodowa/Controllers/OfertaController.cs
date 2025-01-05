using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WypozyczalniaSamochodowa.Data;
using WypozyczalniaSamochodowa.Models;

namespace WypozyczalniaSamochodowa.Controllers
{
    public class OfertaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OfertaController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Oferta
        public async Task<IActionResult> Index(string marka, int? cenaOd, int? cenaDo, DateTime? dataOd, DateTime? dataDo, string sortowanie)
        {
            var oferty = _context.Oferta.Include(o => o.Auto).AsQueryable();

            if (!string.IsNullOrEmpty(marka))
            {
                oferty = oferty.Where(o => o.Auto.Marka.Contains(marka));
            }

            if (cenaOd.HasValue)
            {
                oferty = oferty.Where(o => o.Auto.Cena >= cenaOd.Value);
            }

            if (cenaDo.HasValue)
            {
                oferty = oferty.Where(o => o.Auto.Cena <= cenaDo.Value);
            }

            if (dataOd.HasValue && dataDo.HasValue)
            {
                var zajeteAuta = _context.Wynajecie
                    .Where(w => !(w.DataZakonczenia < dataOd || w.DataRozpoczecia > dataDo))
                    .Select(w => w.AutoId)
                    .ToList();

                oferty = oferty.Where(o => !zajeteAuta.Contains(o.Auto.AutoId));
            }

            oferty = sortowanie switch
            {
                "CenaRosnaco" => oferty.OrderBy(o => o.Auto.Cena),
                "CenaMalejaco" => oferty.OrderByDescending(o => o.Auto.Cena),
                _ => oferty
            };

            return View(await oferty.ToListAsync());
        }



        // GET: Oferta/Details/5
        [Authorize]
        public async Task<IActionResult> Details(int? id, int? autoId, int? cena)
        {
            if (id == null)
            {
                return NotFound();
            }

            var oferta = await _context.Oferta
                .Include(o => o.Auto)
                .FirstOrDefaultAsync(m => m.OfertaId == id);

            if (oferta == null)
            {
                return NotFound();
            }

           
            ViewBag.AutoId = autoId;
            ViewBag.Cena = cena;

            return View(oferta);
        }


        // GET: Oferta/Create
        public IActionResult Create()
        {
            ViewData["AutoId"] = new SelectList(_context.Auto, "AutoId", "AutoId");
            return View();
        }

        // POST: Oferta/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("OfertaId,AutoId,Cena")] Oferta oferta)
        {
            if (ModelState.IsValid)
            {
                _context.Add(oferta);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["AutoId"] = new SelectList(_context.Auto, "AutoId", "AutoId", oferta.AutoId);
            return View(oferta);
        }

        // GET: Oferta/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var oferta = await _context.Oferta.FindAsync(id);
            if (oferta == null)
            {
                return NotFound();
            }
            ViewData["AutoId"] = new SelectList(_context.Auto, "AutoId", "AutoId", oferta.AutoId);
            return View(oferta);
        }

        // POST: Oferta/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("OfertaId,AutoId,Cena")] Oferta oferta)
        {
            if (id != oferta.OfertaId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(oferta);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OfertaExists(oferta.OfertaId))
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
            ViewData["AutoId"] = new SelectList(_context.Auto, "AutoId", "AutoId", oferta.AutoId);
            return View(oferta);
        }

        // GET: Oferta/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var oferta = await _context.Oferta
                .Include(o => o.Auto)
                .FirstOrDefaultAsync(m => m.OfertaId == id);
            if (oferta == null)
            {
                return NotFound();
            }

            return View(oferta);
        }

        // POST: Oferta/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var oferta = await _context.Oferta.FindAsync(id);
            if (oferta != null)
            {
                _context.Oferta.Remove(oferta);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool OfertaExists(int id)
        {
            return _context.Oferta.Any(e => e.OfertaId == id);
        }
    }
}
