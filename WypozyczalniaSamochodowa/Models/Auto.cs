namespace WypozyczalniaSamochodowa.Models
{
    public class Auto
    {
        public int AutoId { get; set; } 

        public string Marka { get; set; }

        public string Model { get; set; }

        public string Silnik { get; set; }

        public int RokProdukcji { get; set; }

        public string Opis { get; set; }

        public int Cena { get; set; }

        public bool Status { get; set; } = true;

        public string? Zdjecie { get; set; }

        public virtual Oferta? Oferta { get; set; }



    }
}
