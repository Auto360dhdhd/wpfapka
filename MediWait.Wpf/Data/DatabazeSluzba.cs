using Microsoft.Data.Sqlite;
using MediWait.Wpf.Modely;
using System.Globalization;

namespace MediWait.Wpf.Data;

public sealed class DatabazeSluzba
{
    private readonly string _cestaDatabaze;

    public DatabazeSluzba()
    {
        var cilovaSlozka = Path.Combine(FileSystem.AppDataDirectory, "MediWait");
        Directory.CreateDirectory(cilovaSlozka);
        _cestaDatabaze = Path.Combine(cilovaSlozka, "mediwait.db");
    }

    public async Task InicializovatAsync()
    {
        await using var spojeni = new SqliteConnection($"Data Source={_cestaDatabaze}");
        await spojeni.OpenAsync();

        var prikaz = spojeni.CreateCommand();
        prikaz.CommandText = @"
            CREATE TABLE IF NOT EXISTS Navstevy (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Termin TEXT NOT NULL,
                Lekar TEXT NOT NULL,
                Lek TEXT NOT NULL,
                Poznamka TEXT NOT NULL,
                JeHotovo INTEGER NOT NULL
            );";

        await prikaz.ExecuteNonQueryAsync();
    }

    public async Task<List<Navsteva>> NacistVsechnyAsync()
    {
        var vysledek = new List<Navsteva>();
        await using var spojeni = new SqliteConnection($"Data Source={_cestaDatabaze}");
        await spojeni.OpenAsync();

        var prikaz = spojeni.CreateCommand();
        prikaz.CommandText = "SELECT Id, Termin, Lekar, Lek, Poznamka, JeHotovo FROM Navstevy ORDER BY Termin DESC;";

        await using var ctecka = await prikaz.ExecuteReaderAsync();
        while (await ctecka.ReadAsync())
        {
            if (!DateTime.TryParseExact(
                    ctecka.GetString(1),
                    "o",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var termin))
            {
                continue;
            }

            vysledek.Add(new Navsteva
            {
                Id = ctecka.GetInt32(0),
                Termin = termin,
                Lekar = ctecka.GetString(2),
                Lek = ctecka.GetString(3),
                Poznamka = ctecka.GetString(4),
                JeHotovo = ctecka.GetInt32(5) == 1
            });
        }

        return vysledek;
    }

    public async Task UlozitAsync(Navsteva navsteva)
    {
        await using var spojeni = new SqliteConnection($"Data Source={_cestaDatabaze}");
        await spojeni.OpenAsync();

        var prikaz = spojeni.CreateCommand();

        if (navsteva.Id == 0)
        {
            prikaz.CommandText = @"
                INSERT INTO Navstevy (Termin, Lekar, Lek, Poznamka, JeHotovo)
                VALUES ($termin, $lekar, $lek, $poznamka, $jeHotovo);";
        }
        else
        {
            prikaz.CommandText = @"
                UPDATE Navstevy
                SET Termin = $termin,
                    Lekar = $lekar,
                    Lek = $lek,
                    Poznamka = $poznamka,
                    JeHotovo = $jeHotovo
                WHERE Id = $id;";
            prikaz.Parameters.AddWithValue("$id", navsteva.Id);
        }

        prikaz.Parameters.AddWithValue("$termin", navsteva.Termin.ToString("o"));
        prikaz.Parameters.AddWithValue("$lekar", navsteva.Lekar);
        prikaz.Parameters.AddWithValue("$lek", navsteva.Lek);
        prikaz.Parameters.AddWithValue("$poznamka", navsteva.Poznamka);
        prikaz.Parameters.AddWithValue("$jeHotovo", navsteva.JeHotovo ? 1 : 0);

        await prikaz.ExecuteNonQueryAsync();
    }

    public async Task SmazatAsync(int id)
    {
        await using var spojeni = new SqliteConnection($"Data Source={_cestaDatabaze}");
        await spojeni.OpenAsync();

        var prikaz = spojeni.CreateCommand();
        prikaz.CommandText = "DELETE FROM Navstevy WHERE Id = $id;";
        prikaz.Parameters.AddWithValue("$id", id);

        await prikaz.ExecuteNonQueryAsync();
    }
}
