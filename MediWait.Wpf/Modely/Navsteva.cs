namespace MediWait.Wpf.Modely;

public sealed class Navsteva
{
    public int Id { get; set; }
    public DateTime Termin { get; set; }
    public string Lekar { get; set; } = string.Empty;
    public string Lek { get; set; } = string.Empty;
    public string Poznamka { get; set; } = string.Empty;
    public bool JeHotovo { get; set; }
}
