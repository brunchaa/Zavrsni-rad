using System;
using System.ComponentModel.DataAnnotations;

namespace SkladisteRobe.Models
{
    public class Transakcija
    {
        public int Id { get; set; }

        [Required]
        public int MaterijalId { get; set; }

        // navigiraj property u materijal
        public Materijal Materijal { get; set; }

        [Required(ErrorMessage = "Obavezna količina")]
        public int Kolicina { get; set; }

        [Required]
        public DateTime Datum { get; set; }

        
        [Required]
        public string Tip { get; set; }

        [Required]
        public int KorisnikId { get; set; }

        // navigira property u korisnika
        public Korisnik Korisnik { get; set; }
    }
}