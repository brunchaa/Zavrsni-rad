namespace SkladisteRobe.Models
{
    public class UserActivityLog
    {
        public int Id { get; set; }
        public string? UserId { get; set; } // Dodano ?
        public DateTime LoginTime { get; set; }
        public DateTime? LogoutTime { get; set; }
        public TimeSpan Duration { get; set; }
    }
}