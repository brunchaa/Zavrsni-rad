using System;
using System.Collections.Generic;

namespace SkladisteRobe.Models
{
    public class DashboardViewModel
    {
        public List<MaterijalKategorija> MaterijalPoKategoriji { get; set; } = new List<MaterijalKategorija>();
        public List<TransakcijaStat> TransakcijeStats { get; set; } = new List<TransakcijaStat>();
        public List<UserStat> UserStats { get; set; } = new List<UserStat>();

        
        public List<Materijal> LowStockMaterials { get; set; } = new List<Materijal>();  
        public List<Transakcija> RecentTransakcije { get; set; } = new List<Transakcija>();  
        public List<TransakcijaPoDanu> TransakcijePoDanima { get; set; } = new List<TransakcijaPoDanu>();  
        public List<TopMaterijal> TopMaterials { get; set; } = new List<TopMaterijal>();  
        public List<TopUser> TopUsersByTransakcije { get; set; } = new List<TopUser>();  
        public double AverageDailyTransactions { get; set; }  

        public class MaterijalKategorija
        {
            public string Kategorija { get; set; }
            public int Kolicina { get; set; }
        }

        public class TransakcijaStat
        {
            public string Tip { get; set; }
            public int Broj { get; set; }
        }

        public class UserStat
        {
            public string UserName { get; set; }
            public DateTime? LastLoginTime { get; set; }
            public TimeSpan TotalLoginDuration { get; set; }
            public TimeSpan DailyLoginDuration { get; set; }  
        }

        public class TransakcijaPoDanu
        {
            public DateTime Datum { get; set; }
            public int Broj { get; set; }
        }

        public class TopMaterijal
        {
            public string Naziv { get; set; }
            public int BrojTransakcija { get; set; }
        }

        public class TopUser
        {
            public string UserName { get; set; }
            public int BrojTransakcija { get; set; }
        }
    }
}