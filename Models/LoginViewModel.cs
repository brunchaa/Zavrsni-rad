using System.ComponentModel.DataAnnotations;

namespace SkladisteRobe.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Obavezno korisničko ime")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Obavezna lozinka")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}