using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace WypozyczalniaSamochodowa.Models
{
    public class Wynajecie
    {
        public int WynajecieId { get; set; } 

        public int AutoId { get; set; } 

        private DateTime _dataRozpoczecia;
        public DateTime DataRozpoczecia
        {
            get => _dataRozpoczecia;
            set => _dataRozpoczecia = value.Date; 
        }

        private DateTime _dataZakonczenia;
        public DateTime DataZakonczenia
        {
            get => _dataZakonczenia;
            set => _dataZakonczenia = value.Date; 
        }

        public int CenaCalkowita { get; set; }

        public string? UserId { get; set; }

        public IdentityUser? User { get; set; }

        public virtual Auto? Auto { get; set; }

        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
        public double? Latitude { get; set; }

        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
        public double? Longitude { get; set; }

        public string? Address { get; set; }

        public string PaymentMethod { get; set; }

    }
}
