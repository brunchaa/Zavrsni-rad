using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Ovo je ključno za [NotMapped]
using System.Collections.Generic;

namespace SkladisteRobe.Models
{
    public class Korisnik : IdentityUser<int>
    {
        [Required(ErrorMessage = "Obavezno korisničko ime")]
        public string? Username { get; set; }
        [Required(ErrorMessage = "Obavezna lozinka")]
        [DataType(DataType.Password)]
        [MinLength(8, ErrorMessage = "Lozinka mora imati najmanje 8 znakova")]
        public string? Password { get; set; }
        [Required(ErrorMessage = "Obavezno ime")]
        [RegularExpression(@"^(?!.*\d).+$", ErrorMessage = "Ime ne smije sadržavati brojeve.")]
        public string? Ime { get; set; }
        [Required(ErrorMessage = "Obavezno prezime")]
        [RegularExpression(@"^(?!.*\d).+$", ErrorMessage = "Prezime ne smije sadržavati brojeve.")]
        public string? Prezime { get; set; }
        [Required(ErrorMessage = "Obavezna uloga")]
        public Uloga Role { get; set; }
        public DateTime? LastLoginTime { get; set; }
        public TimeSpan TotalLoginDuration { get; set; } = TimeSpan.Zero;
        public DateTime? LastActivityTime { get; set; }

        [NotMapped] // Mora biti ovdje
        public IList<string> Roles { get; set; } = new List<string>();
    }
}