using System.ComponentModel.DataAnnotations;

namespace SkladisteRobe.Models
{
    public class Korisnik
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Obavezno korisničko ime")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Obavezna lozinka")]
        [DataType(DataType.Password)]
        [MinLength(8, ErrorMessage = "Lozinka mora imati najmanje 8 znakova")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Obavezno ime")]
        // ime ne smije sadrzavati brojeve umalo promaklo
        [RegularExpression(@"^(?!.*\d).+$", ErrorMessage = "Ime ne smije sadržavati brojeve.")]
        public string Ime { get; set; }

        [Required(ErrorMessage = "Obavezno prezime")]
        [RegularExpression(@"^(?!.*\d).+$", ErrorMessage = "Prezime ne smije sadržavati brojeve.")]
        public string Prezime { get; set; }

        [Required(ErrorMessage = "Obavezna uloga")]
        public Uloga Role { get; set; }
    }
}