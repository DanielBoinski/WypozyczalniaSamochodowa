using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;
using WypozyczalniaSamochodowa.Data;
using WypozyczalniaSamochodowa.Models;

namespace WypozyczalniaSamochodowa.Controllers
{
    public class StripeController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        public StripeController(IConfiguration configuration, ApplicationDbContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        public IActionResult Payment(int amount, int autoId, string startDate, string endDate)
        {
            var model = new PaymentViewModel
            {
                Amount = amount,
                AutoId = autoId,
                StartDate = startDate,
                EndDate = endDate
            };
            return View(model);
        }



        [HttpPost]
        public IActionResult CreateSession([FromBody] PaymentViewModel model)
        {
            try
            {
                if (model.Amount <= 0 || model.AutoId <= 0 || string.IsNullOrEmpty(model.StartDate) || string.IsNullOrEmpty(model.EndDate))
                {
                    throw new ArgumentException("Brakuje wymaganych danych.");
                }

                StripeConfiguration.ApiKey = _configuration["Stripe:ApiKey"];

                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "pln",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "Wynajem samochodu"
                        },
                        UnitAmount = model.Amount * 100
                    },
                    Quantity = 1
                }
            },
                    Mode = "payment",
                    SuccessUrl = Url.Action("Success", "Stripe", new { model.AutoId, model.StartDate, model.EndDate, model.Amount }, Request.Scheme),
                    CancelUrl = Url.Action("Cancel", "Stripe", null, Request.Scheme),
                };

                var service = new SessionService();
                var session = service.Create(options);

                Console.WriteLine($"Stripe Session: AutoId={model.AutoId}, StartDate={model.StartDate}, EndDate={model.EndDate}, Amount={model.Amount}");
                return Json(new { id = session.Id, url = session.Url });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas tworzenia sesji Stripe: {ex.Message}");
                return BadRequest(new { error = ex.Message });
            }
        }


        public async Task<IActionResult> Success(int autoId, string startDate, string endDate, int amount)
        {
            try
            {
                if (autoId <= 0 || string.IsNullOrEmpty(startDate) || string.IsNullOrEmpty(endDate) || amount <= 0)
                {
                    throw new ArgumentException("Brakuje wymaganych danych.");
                }

                var auto = await _context.Auto.FindAsync(autoId);
                if (auto == null)
                {
                    throw new Exception("Nie znaleziono samochodu.");
                }

               
                var overlappingRentals = _context.Wynajecie
                    .Any(w => w.AutoId == autoId &&
                              ((DateTime.Parse(startDate) >= w.DataRozpoczecia && DateTime.Parse(startDate) < w.DataZakonczenia) ||
                               (DateTime.Parse(endDate) > w.DataRozpoczecia && DateTime.Parse(endDate) <= w.DataZakonczenia)));

                if (overlappingRentals)
                {
                    throw new Exception("Auto jest już wynajęte w tym okresie.");
                }

                
                var wynajecie = new Wynajecie
                {
                    AutoId = autoId,
                    DataRozpoczecia = DateTime.Parse(startDate),
                    DataZakonczenia = DateTime.Parse(endDate),
                    CenaCalkowita = amount,
                    UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                    PaymentMethod = "Online"
                };

                auto.Status = false; 
                _context.Add(wynajecie);
                _context.Update(auto);

                await _context.SaveChangesAsync();

                ViewBag.Message = "Płatność zakończona sukcesem! Samochód został wynajęty.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas zapisu danych: {ex.Message}");
                Console.WriteLine($"AutoId: {autoId}, StartDate: {startDate}, EndDate: {endDate}, Amount: {amount}");
                return RedirectToAction("Cancel");
            }

            return View();
        }



        public IActionResult Cancel()
        {
            ViewBag.Message = "Płatność została anulowana.";
            return View();
        }
    }
}
