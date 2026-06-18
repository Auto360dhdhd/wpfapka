using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using MediWait.Wpf.Data;
using MediWait.Wpf.Modely;
using MediWait.Wpf.MVVM;
using MediWait.Wpf.Sluzby;

namespace MediWait.Wpf.ViewModely;

public sealed class HlavniViewModel : ZakladViewModel
{
    private static readonly Brush TmavePozadi = new SolidColorBrush(Color.FromRgb(30, 30, 34));
    private readonly DatabazeSluzba _databazeSluzba;
    private readonly SenzorovaSluzba _senzorovaSluzba;
    private int _upravovaneId;
    private DateTime? _datumNavstevy = DateTime.Today;
    private string _casNavstevy = DateTime.Now.ToString("HH:mm");
    private string _jmenoLekare = string.Empty;
    private string _nazevLeku = string.Empty;
    private string _poznamka = string.Empty;
    private int _vybranaKarta;
    private string _nouzovaZprava = "Nouzové kontakty připraveny";
    private Visibility _nouzoveKontaktyViditelnost = Visibility.Collapsed;
    private bool _jeTmavyMotiv;
    private Brush _pozadiAplikace = Brushes.WhiteSmoke;
    private string _stavSenzoru = string.Empty;

    public HlavniViewModel(DatabazeSluzba databazeSluzba, SenzorovaSluzba senzorovaSluzba)
    {
        _databazeSluzba = databazeSluzba;
        _senzorovaSluzba = senzorovaSluzba;

        VsechnyNavstevy = new ObservableCollection<Navsteva>();

        DnesniPolozky = new ListCollectionView(VsechnyNavstevy);
        DnesniPolozky.Filter = o => o is Navsteva n && n.Termin.Date == DateTime.Today;

        BudouciPolozky = new ListCollectionView(VsechnyNavstevy);
        BudouciPolozky.Filter = o => o is Navsteva n && n.Termin.Date >= DateTime.Today;

        HistorickePolozky = new ListCollectionView(VsechnyNavstevy);
        HistorickePolozky.Filter = o => o is Navsteva n && (n.Termin.Date < DateTime.Today || n.JeHotovo);

        UlozitPrikaz = new RelayPrikaz(async _ => await UlozitAsync(), _ => LzeUlozit());
        VymazatFormularPrikaz = new RelayPrikaz(_ => VymazatFormular());
        UpravitPrikaz = new RelayPrikaz(p => NacistDoFormulare(p as Navsteva));
        SmazatPrikaz = new RelayPrikaz(async p => await SmazatAsync(p as Navsteva));
        OznacitJakoHotovoPrikaz = new RelayPrikaz(async p => await OznacitJakoHotovoAsync(p as Navsteva));
        OtevritNouzoveKontaktyPrikaz = new RelayPrikaz(_ => OtevritNouzoveKontakty());
        PrepnoutMotivPrikaz = new RelayPrikaz(_ => NastavitMotiv(!_jeTmavyMotiv));

        _senzorovaSluzba.ZatraseniDetekovano += (_, _) =>
        {
            Application.Current.Dispatcher.Invoke(OtevritNouzoveKontakty);
        };
        _senzorovaSluzba.ZmenaDoporucenehoMotivu += (_, tmavy) =>
        {
            Application.Current.Dispatcher.Invoke(() => NastavitMotiv(tmavy));
        };

        _senzorovaSluzba.Spustit();
        StavSenzoru = $"Akcelerometr: {(senzorovaSluzba.JeAkcelerometrDostupny ? "dostupný" : "není dostupný")} | Světelný senzor: {(senzorovaSluzba.JeSvetelnySenzorDostupny ? "dostupný" : "není dostupný")}";
        if (!string.IsNullOrWhiteSpace(senzorovaSluzba.PosledniChybaInicializace))
        {
            StavSenzoru += $" | Inicializace: {senzorovaSluzba.PosledniChybaInicializace}";
        }

        _ = InicializovatBezpecneAsync();
    }

    public ObservableCollection<Navsteva> VsechnyNavstevy { get; }
    public ICollectionView DnesniPolozky { get; }
    public ICollectionView BudouciPolozky { get; }
    public ICollectionView HistorickePolozky { get; }

    public DateTime? DatumNavstevy
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

    public int VybranaKarta
    {
        get => _vybranaKarta;
        set => Nastavit(ref _vybranaKarta, value);
    }

    public string NouzovaZprava
    {
        get => _nouzovaZprava;
        set => Nastavit(ref _nouzovaZprava, value);
    }

    public Visibility NouzoveKontaktyViditelnost
    {
        get => _nouzoveKontaktyViditelnost;
        set => Nastavit(ref _nouzoveKontaktyViditelnost, value);
    }

    public Brush PozadiAplikace
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
            Application.Current.Dispatcher.Invoke(() =>
            {
                NouzoveKontaktyViditelnost = Visibility.Visible;
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
        return DatumNavstevy.HasValue &&
               TimeSpan.TryParse(CasNavstevy, out _) &&
               !string.IsNullOrWhiteSpace(JmenoLekare) &&
               !string.IsNullOrWhiteSpace(NazevLeku);
    }

    private async Task UlozitAsync()
    {
        if (!LzeUlozit() || DatumNavstevy is null)
        {
            return;
        }

        TimeSpan.TryParse(CasNavstevy, out var cas);

        var navsteva = new Navsteva
        {
            Id = _upravovaneId,
            Termin = DatumNavstevy.Value.Date + cas,
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
        DatumNavstevy = navsteva.Termin.Date;
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
        DatumNavstevy = DateTime.Today;
        CasNavstevy = DateTime.Now.ToString("HH:mm");
        JmenoLekare = string.Empty;
        NazevLeku = string.Empty;
        Poznamka = string.Empty;
    }

    private void OtevritNouzoveKontakty()
    {
        NouzoveKontaktyViditelnost = Visibility.Visible;
        VybranaKarta = 0;
        NouzovaZprava = $"Aktivováno {DateTime.Now:HH:mm:ss} - připravené kontakty pro rychlou pomoc";
    }

    private void NastavitMotiv(bool tmavy)
    {
        _jeTmavyMotiv = tmavy;
        PozadiAplikace = tmavy
            ? TmavePozadi
            : Brushes.WhiteSmoke;
    }

    private void ObnovitPohledy()
    {
        DnesniPolozky.Refresh();
        BudouciPolozky.Refresh();
        HistorickePolozky.Refresh();
    }

    private void AktualizovatStavUlozeni()
    {
        if (UlozitPrikaz is RelayPrikaz prikaz)
        {
            prikaz.Obnovit();
        }
    }
}
