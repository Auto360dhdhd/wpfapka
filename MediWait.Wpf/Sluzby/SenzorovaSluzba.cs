using Windows.Devices.Sensors;

namespace MediWait.Wpf.Sluzby;

public sealed class SenzorovaSluzba
{
    private const double PrahZatraseni = 1.8;
    private const float PrahNizkehoOsvetleniLux = 80f;
    private readonly Accelerometer? _akcelerometr;
    private readonly LightSensor? _svetelnySenzor;
    private bool _spusteno;
    private bool _simulovaneNizkeOsvetleni;

    public SenzorovaSluzba()
    {
        try
        {
            _akcelerometr = Accelerometer.GetDefault();
            _svetelnySenzor = LightSensor.GetDefault();
        }
        catch (Exception vyjimka)
        {
            _akcelerometr = null;
            _svetelnySenzor = null;
            PosledniChybaInicializace = vyjimka.Message;
        }
    }

    public event EventHandler? ZatraseniDetekovano;
    public event EventHandler<bool>? ZmenaDoporucenehoMotivu;

    public bool JeAkcelerometrDostupny => _akcelerometr is not null;
    public bool JeSvetelnySenzorDostupny => _svetelnySenzor is not null;
    public bool JeSimulovaneNizkeOsvetleni => _simulovaneNizkeOsvetleni;
    public string PosledniChybaInicializace { get; } = string.Empty;

    public void Spustit()
    {
        if (_spusteno)
        {
            return;
        }

        if (_akcelerometr is not null)
        {
            _akcelerometr.ReportInterval = Math.Max(_akcelerometr.MinimumReportInterval, 100);
            _akcelerometr.ReadingChanged += PriZmeneAkcelerometru;
        }

        if (_svetelnySenzor is not null)
        {
            _svetelnySenzor.ReportInterval = Math.Max(_svetelnySenzor.MinimumReportInterval, 300);
            _svetelnySenzor.ReadingChanged += PriZmeneSvetelnehoSenzoru;
        }

        _spusteno = true;
    }

    public void SimulovatZatraseni()
    {
        ZatraseniDetekovano?.Invoke(this, EventArgs.Empty);
    }

    public bool PrepnoutSimulovaneOsvetleni()
    {
        _simulovaneNizkeOsvetleni = !_simulovaneNizkeOsvetleni;
        ZmenaDoporucenehoMotivu?.Invoke(this, _simulovaneNizkeOsvetleni);
        return _simulovaneNizkeOsvetleni;
    }

    private void PriZmeneAkcelerometru(Accelerometer sender, AccelerometerReadingChangedEventArgs args)
    {
        var cteni = args.Reading;
        var sila = Math.Sqrt(
            cteni.AccelerationX * cteni.AccelerationX +
            cteni.AccelerationY * cteni.AccelerationY +
            cteni.AccelerationZ * cteni.AccelerationZ);

        if (sila > PrahZatraseni)
        {
            ZatraseniDetekovano?.Invoke(this, EventArgs.Empty);
        }
    }

    private void PriZmeneSvetelnehoSenzoru(LightSensor sender, LightSensorReadingChangedEventArgs args)
    {
        ZmenaDoporucenehoMotivu?.Invoke(this, args.Reading.IlluminanceInLux < PrahNizkehoOsvetleniLux);
    }
}
