using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace SkladisteRobe.Models
{
    public class Korisnik : IdentityUser<int> // Novo: Inherit od IdentityUser<int> za role i hash (Id je int)
    {
        public int Id { get; set; } // Zadržano, ali Identity koristi Id

        [Required(ErrorMessage = "Obavezno korisničko ime")]
        public string Username { get; set; } // Zadržano (Identity koristi UserName)

        [Required(ErrorMessage = "Obavezna lozinka")]
        [DataType(DataType.Password)]
        [MinLength(8, ErrorMessage = "Lozinka mora imati najmanje 8 znakova")]
        public string Password { get; set; } // Zadržano, ali Identity hashira automatski

        [Required(ErrorMessage = "Obavezno ime")]
        [RegularExpression(@"^(?!.*\d).+$", ErrorMessage = "Ime ne smije sadržavati brojeve.")]
        public string Ime { get; set; }

        [Required(ErrorMessage = "Obavezno prezime")]
        [RegularExpression(@"^(?!.*\d).+$", ErrorMessage = "Prezime ne smije sadržavati brojeve.")]
        public string Prezime { get; set; }

        [Required(ErrorMessage = "Obavezna uloga")]
        public Uloga Role { get; set; } // Zadržano za kompatibilnost, ali role su sada IdentityRole

        // Novo: Polja za praćenje login vremena i aktivnosti
        public DateTime? LastLoginTime { get; set; }
        public TimeSpan TotalLoginDuration { get; set; } = TimeSpan.Zero;
        public DateTime? LastActivityTime { get; set; }

        public IList<string> Roles { get; set; } = new List<string>();
    }
}