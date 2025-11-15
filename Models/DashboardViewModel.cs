namespace SkladisteRobe.Models
{
    public class DashboardViewModel
    {
        public List<MaterijalKategorija> MaterijalPoKategoriji { get; set; } = new List<MaterijalKategorija>();
        public List<TransakcijaStat> TransakcijeStats { get; set; } = new List<TransakcijaStat>();
        public List<UserStat> UserStats { get; set; } = new List<UserStat>();

        public class MaterijalKategorija
        {
            public string? Kategorija { get; set; } 
            public int Kolicina { get; set; }
        }

        public class TransakcijaStat
        {
            public string? Tip { get; set; } 
            public int Broj { get; set; }
        }

        public class UserStat
        {
            public string? UserName { get; set; } 
            public DateTime? LastLoginTime { get; set; }
            public TimeSpan TotalLoginDuration { get; set; }
        }
    }
}