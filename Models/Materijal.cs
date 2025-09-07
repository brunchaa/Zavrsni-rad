using System.ComponentModel.DataAnnotations;

namespace SkladisteRobe.Models
{
    public enum MjernaJedinica
    {
        KOMAD,
        METAR
    }

    public class Materijal
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Obavezan naziv materijala")]
        public string Naziv { get; set; }

        [Required(ErrorMessage = "Obavezna količina")]
        public int Kolicina { get; set; }

        [Required(ErrorMessage = "Obavezna mjerna jedinica")]
        public MjernaJedinica Jedinica { get; set; }
    }
}