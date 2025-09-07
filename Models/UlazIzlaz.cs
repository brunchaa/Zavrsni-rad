using System;
using System.ComponentModel.DataAnnotations;

namespace SkladisteRobe.Models
{
    public class UlazIzlaz
    {
        public int Id { get; set; }

        [Required]
        public int MaterijalId { get; set; }

        public Materijal Materijal { get; set; }

        [Required(ErrorMessage = "Obavezna količina")]
        public int Kolicina { get; set; }

        [Required]
        public DateTime Datum { get; set; }

        [Required]
        public string Tip { get; set; } // ulaz ili izlaz

        [Required]
        public int KorisnikId { get; set; }

        public Korisnik Korisnik { get; set; }
    }
}