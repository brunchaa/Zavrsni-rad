using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Za NotMapped
using System.Collections.Generic;

namespace SkladisteRobe.Models
{
    public class Korisnik
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Obavezno korisničko ime")]
        public string? Username { get; set; }

        [Required(ErrorMessage = "Obavezna lozinka")]
        [DataType(DataType.Password)]
        [MinLength(8, ErrorMessage = "Lozinka mora imati najmanje 8 znakova")]
        public string? Password { get; set; }  // Plain text za testiranje
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

        [NotMapped] 
        public IList<string> Roles { get; set; } = new List<string>();
    }
}