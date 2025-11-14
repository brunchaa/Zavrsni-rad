namespace SkladisteRobe.Models
{
    public enum Uloga
    {
        Zaposlenik,  // Obični korisnik (bivši Radnik)
        Voditelj,    // Voditelj sa pristupom dashboardu i skladištu
        Admin        // Puni pristup
    }
}