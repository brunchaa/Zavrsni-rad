using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SkladisteRobe.Models
{
    public class BulkTransactionItemViewModel
    {
        [Required(ErrorMessage = "Naziv je obavezan")]
        // String mora sadrzavati barem jedno slovo ne moze drugacije zbog imena stvari
        [RegularExpression(@"^(?=.*\p{L}).+$", ErrorMessage = "Naziv mora sadržavati barem jedno slovo.")]
        public string Naziv { get; set; }

        [Required(ErrorMessage = "Količina je obavezna")]
        [Range(1, int.MaxValue, ErrorMessage = "Količina mora biti veća od 0")]
        public int Kolicina { get; set; }

        [Required(ErrorMessage = "Mjerna jedinica je obavezna")]
        public MjernaJedinica Jedinica { get; set; }
    }

    public class BulkTransactionViewModel
    {
        public List<BulkTransactionItemViewModel> Items { get; set; } = new List<BulkTransactionItemViewModel>();
    }
}