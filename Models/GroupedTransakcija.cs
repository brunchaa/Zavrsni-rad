using System;
using System.Collections.Generic;

namespace SkladisteRobe.Models
{
    public class GroupedTransakcija
    {
        public Guid? BatchId { get; set; }
        public DateTime Datum { get; set; }
        public string Tip { get; set; }
        public Korisnik Korisnik { get; set; }
        public List<Transakcija> Stavke { get; set; } = new List<Transakcija>();
    }
}