using System;
using System.ComponentModel.DataAnnotations;

namespace SkladisteRobe.Models
{
    public class Transakcija
    {
        public int Id { get; set; }

        [Required]
        public int MaterijalId { get; set; }

        // Navigacija u materijal (zadržano)
        public Materijal Materijal { get; set; }

        [Required(ErrorMessage = "Obavezna količina")]
        public int Kolicina { get; set; }

        [Required]
        public DateTime Datum { get; set; }

        [Required]
        public string Tip { get; set; } // Zadržano (npr. "Primka", "Izdaj robu")

        [Required]
        public int KorisnikId { get; set; }

        // Navigacija u korisnika (zadržano)
        public Korisnik Korisnik { get; set; }

        // Novo: Ako treba, možeš dodati QR referencu, ali za sada zadržano
    }
}