using System.Collections.ObjectModel;
using System.Windows.Input;
using MediWait.Wpf.Data;
using MediWait.Wpf.Modely;
using MediWait.Wpf.MVVM;
using MediWait.Wpf.Sluzby;

namespace MediWait.Wpf.ViewModely;

public sealed class HlavniViewModel : ZakladViewModel
{
    private static readonly Color TmavePozadi = Color.FromRgb(30, 30, 34);
    private static readonly Color SvetlePozadi = Colors.WhiteSmoke;
    private readonly DatabazeSluzba _databazeSluzba;
    private readonly SenzorovaSluzba _senzorovaSluzba;
    private int _upravovaneId;
    private DateTime _datumNavstevy = DateTime.Today;
    private string _casNavstevy = DateTime.Now.ToString("HH:mm");
    private string _jmenoLekare = string.Empty;
    private string _nazevLeku = string.Empty;
    private string _poznamka = string.Empty;
    private string _nouzovaZprava = "Nouzové kontakty připraveny";
    private bool _jeNouzoveKontaktyViditelne;
    private bool _jeTmavyMotiv;
    private bool _simulovaneNizkeOsvetleni;
    private Color _pozadiAplikace = SvetlePozadi;
    private string _stavSenzoru = string.Empty;

    public HlavniViewModel(DatabazeSluzba databazeSluzba, SenzorovaSluzba senzorovaSluzba)
    {
        _databazeSluzba = databazeSluzba;
        _senzorovaSluzba = senzorovaSluzba;

        VsechnyNavstevy = new ObservableCollection<Navsteva>();
        DnesniPolozky = new ObservableCollection<Navsteva>();
        BudouciPolozky = new ObservableCollection<Navsteva>();
        HistorickePolozky = new ObservableCollection<Navsteva>();

        UlozitPrikaz = new RelayPrikaz(async _ => await UlozitAsync(), _ => LzeUlozit());
        VymazatFormularPrikaz = new RelayPrikaz(_ => VymazatFormular());
        UpravitPrikaz = new RelayPrikaz(p => NacistDoFormulare(p as Navsteva));
        SmazatPrikaz = new RelayPrikaz(async p => await SmazatAsync(p as Navsteva));
        OznacitJakoHotovoPrikaz = new RelayPrikaz(async p => await OznacitJakoHotovoAsync(p as Navsteva));
        OtevritNouzoveKontaktyPrikaz = new RelayPrikaz(_ => OtevritNouzoveKontakty());
        PrepnoutMotivPrikaz = new RelayPrikaz(_ => NastavitMotiv(!_jeTmavyMotiv));
        SimulovatZatraseniPrikaz = new RelayPrikaz(_ => SimulovatZatraseni());
        PrepnoutSenzorPrikaz = new RelayPrikaz(_ => PrepnoutSvetelnySenzor());

        _senzorovaSluzba.ZatraseniDetekovano += (_, _) =>
        {
            MainThread.BeginInvokeOnMainThread(OtevritNouzoveKontakty);
        };
        _senzorovaSluzba.ZmenaDoporucenehoMotivu += (_, tmavy) =>
        {
            MainThread.BeginInvokeOnMainThread(() => NastavitMotiv(tmavy));
        };

        _senzorovaSluzba.Spustit();
        _simulovaneNizkeOsvetleni = _senzorovaSluzba.JeSimulovaneNizkeOsvetleni;
        AktualizovatStavSenzoru();

        _ = InicializovatBezpecneAsync();
    }

    public ObservableCollection<Navsteva> VsechnyNavstevy { get; }
    public ObservableCollection<Navsteva> DnesniPolozky { get; }
    public ObservableCollection<Navsteva> BudouciPolozky { get; }
    public ObservableCollection<Navsteva> HistorickePolozky { get; }

    public DateTime DatumNavstevyNonNullable
    {
        get => _datumNavstevy;
        set
        {
            Nastavit(ref _datumNavstevy, value);
            AktualizovatStavUlozeni();
        }
    }

    public string CasNavstevy
    {
        get => _casNavstevy;
        set
        {
            Nastavit(ref _casNavstevy, value);
            AktualizovatStavUlozeni();
        }
    }

    public string JmenoLekare
    {
        get => _jmenoLekare;
        set
        {
            Nastavit(ref _jmenoLekare, value);
            AktualizovatStavUlozeni();
        }
    }

    public string NazevLeku
    {
        get => _nazevLeku;
        set
        {
            Nastavit(ref _nazevLeku, value);
            AktualizovatStavUlozeni();
        }
    }

    public string Poznamka
    {
        get => _poznamka;
        set => Nastavit(ref _poznamka, value);
    }

    public string NouzovaZprava
    {
        get => _nouzovaZprava;
        set => Nastavit(ref _nouzovaZprava, value);
    }

    public bool JeNouzoveKontaktyViditelne
    {
        get => _jeNouzoveKontaktyViditelne;
        set => Nastavit(ref _jeNouzoveKontaktyViditelne, value);
    }

    public Color PozadiAplikace
    {
        get => _pozadiAplikace;
        set => Nastavit(ref _pozadiAplikace, value);
    }

    public string StavSenzoru
    {
        get => _stavSenzoru;
        set => Nastavit(ref _stavSenzoru, value);
    }

    public ICommand UlozitPrikaz { get; }
    public ICommand VymazatFormularPrikaz { get; }
    public ICommand UpravitPrikaz { get; }
    public ICommand SmazatPrikaz { get; }
    public ICommand OznacitJakoHotovoPrikaz { get; }
    public ICommand OtevritNouzoveKontaktyPrikaz { get; }
    public ICommand PrepnoutMotivPrikaz { get; }
    public ICommand SimulovatZatraseniPrikaz { get; }
    public ICommand PrepnoutSenzorPrikaz { get; }

    private async Task InicializovatAsync()
    {
        await _databazeSluzba.InicializovatAsync();
        await NacistDataAsync();
    }

    private async Task InicializovatBezpecneAsync()
    {
        try
        {
            await InicializovatAsync();
        }
        catch (Exception vyjimka)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                JeNouzoveKontaktyViditelne = true;
                NouzovaZprava = $"Inicializace selhala: {vyjimka.Message}";
            });
        }
    }

    private async Task NacistDataAsync()
    {
        var navstevy = await _databazeSluzba.NacistVsechnyAsync();

        VsechnyNavstevy.Clear();
        foreach (var navsteva in navstevy)
        {
            VsechnyNavstevy.Add(navsteva);
        }

        ObnovitPohledy();
    }

    private bool LzeUlozit()
    {
        return TimeSpan.TryParse(CasNavstevy, out _) &&
               !string.IsNullOrWhiteSpace(JmenoLekare) &&
               !string.IsNullOrWhiteSpace(NazevLeku);
    }

    private async Task UlozitAsync()
    {
        if (!LzeUlozit())
        {
            return;
        }

        TimeSpan.TryParse(CasNavstevy, out var cas);

        var navsteva = new Navsteva
        {
            Id = _upravovaneId,
            Termin = DatumNavstevyNonNullable.Date + cas,
            Lekar = JmenoLekare.Trim(),
            Lek = NazevLeku.Trim(),
            Poznamka = Poznamka.Trim(),
            JeHotovo = false
        };

        await _databazeSluzba.UlozitAsync(navsteva);
        await NacistDataAsync();
        VymazatFormular();
    }

    private void NacistDoFormulare(Navsteva? navsteva)
    {
        if (navsteva is null)
        {
            return;
        }

        _upravovaneId = navsteva.Id;
        DatumNavstevyNonNullable = navsteva.Termin.Date;
        CasNavstevy = navsteva.Termin.ToString("HH:mm");
        JmenoLekare = navsteva.Lekar;
        NazevLeku = navsteva.Lek;
        Poznamka = navsteva.Poznamka;
    }

    private async Task SmazatAsync(Navsteva? navsteva)
    {
        if (navsteva is null)
        {
            return;
        }

        await _databazeSluzba.SmazatAsync(navsteva.Id);
        await NacistDataAsync();
    }

    private async Task OznacitJakoHotovoAsync(Navsteva? navsteva)
    {
        if (navsteva is null)
        {
            return;
        }

        navsteva.JeHotovo = true;
        await _databazeSluzba.UlozitAsync(navsteva);
        await NacistDataAsync();
    }

    private void VymazatFormular()
    {
        _upravovaneId = 0;
        DatumNavstevyNonNullable = DateTime.Today;
        CasNavstevy = DateTime.Now.ToString("HH:mm");
        JmenoLekare = string.Empty;
        NazevLeku = string.Empty;
        Poznamka = string.Empty;
    }

    private void OtevritNouzoveKontakty()
    {
        JeNouzoveKontaktyViditelne = true;
        NouzovaZprava = $"Aktivováno {DateTime.Now:HH:mm:ss} - připravené kontakty pro rychlou pomoc";
    }

    private void NastavitMotiv(bool tmavy)
    {
        _jeTmavyMotiv = tmavy;
        PozadiAplikace = tmavy ? TmavePozadi : SvetlePozadi;
    }

    private void PrepnoutSvetelnySenzor()
    {
        _simulovaneNizkeOsvetleni = _senzorovaSluzba.PrepnoutSimulovaneOsvetleni();
        AktualizovatStavSenzoru();
    }

    private void SimulovatZatraseni()
    {
        _senzorovaSluzba.SimulovatZatraseni();
    }

    private void AktualizovatStavSenzoru()
    {
        var stavAkcelerometru = _senzorovaSluzba.JeAkcelerometrDostupny ? "dostupný" : "není dostupný";
        var stavSvetelnehoSenzoru = _senzorovaSluzba.JeSvetelnySenzorDostupny ? "dostupný" : "není dostupný";
        var stavOsvetleni = _simulovaneNizkeOsvetleni ? "nízké světlo" : "normální světlo";

        StavSenzoru = $"Akcelerometr: {stavAkcelerometru} | Světelný senzor: {stavSvetelnehoSenzoru} | Emulace senzoru: {stavOsvetleni}";
        if (!string.IsNullOrWhiteSpace(_senzorovaSluzba.PosledniChybaInicializace))
        {
            StavSenzoru += $" | Inicializace: {_senzorovaSluzba.PosledniChybaInicializace}";
        }
    }

    private void ObnovitPohledy()
    {
        NahraditKolekci(DnesniPolozky, VsechnyNavstevy.Where(n => n.Termin.Date == DateTime.Today));
        NahraditKolekci(BudouciPolozky, VsechnyNavstevy.Where(n => n.Termin.Date >= DateTime.Today));
        NahraditKolekci(HistorickePolozky, VsechnyNavstevy.Where(n => n.Termin.Date < DateTime.Today || n.JeHotovo));
    }

    private static void NahraditKolekci(ObservableCollection<Navsteva> cil, IEnumerable<Navsteva> zdroj)
    {
        cil.Clear();
        foreach (var navsteva in zdroj)
        {
            cil.Add(navsteva);
        }
    }

    private void AktualizovatStavUlozeni()
    {
        if (UlozitPrikaz is RelayPrikaz prikaz)
        {
            prikaz.Obnovit();
        }
    }
}
