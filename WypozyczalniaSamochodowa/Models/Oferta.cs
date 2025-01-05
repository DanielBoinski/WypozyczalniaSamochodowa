namespace WypozyczalniaSamochodowa.Models
{
    public class Oferta
    {
        public int OfertaId { get; set; } 

        public int AutoId { get; set; }

        public virtual Auto? Auto { get; set; }
    }
}
