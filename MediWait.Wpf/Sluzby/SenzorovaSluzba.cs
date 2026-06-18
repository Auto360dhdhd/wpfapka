using Microsoft.Maui.Devices.Sensors;

namespace MediWait.Wpf.Sluzby;

public sealed class SenzorovaSluzba
{
    private const double PrahZatraseni = 1.8;
    private const float PrahNizkehoOsvetleniLux = 80f;
    private readonly IAccelerometer _akcelerometr;
    private readonly ILightSensor _svetelnySenzor;
    private bool _spusteno;
    private bool _simulovaneNizkeOsvetleni;

    public SenzorovaSluzba()
    {
        _akcelerometr = Accelerometer.Default;
        _svetelnySenzor = LightSensor.Default;
    }

    public event EventHandler? ZatraseniDetekovano;
    public event EventHandler<bool>? ZmenaDoporucenehoMotivu;

    public bool JeAkcelerometrDostupny => _akcelerometr.IsSupported;
    public bool JeSvetelnySenzorDostupny => _svetelnySenzor.IsSupported;
    public bool JeSimulovaneNizkeOsvetleni => _simulovaneNizkeOsvetleni;
    public string PosledniChybaInicializace { get; private set; } = string.Empty;

    public void Spustit()
    {
        if (_spusteno)
        {
            return;
        }

        try
        {
            if (_akcelerometr.IsSupported)
            {
                _akcelerometr.ReadingChanged += PriZmeneAkcelerometru;
                if (!_akcelerometr.IsMonitoring)
                {
                    _akcelerometr.Start(SensorSpeed.UI);
                }
            }

            if (_svetelnySenzor.IsSupported)
            {
                _svetelnySenzor.ReadingChanged += PriZmeneSvetelnehoSenzoru;
                if (!_svetelnySenzor.IsMonitoring)
                {
                    _svetelnySenzor.Start(SensorSpeed.UI);
                }
            }

            _spusteno = true;
        }
        catch (Exception vyjimka)
        {
            PosledniChybaInicializace = vyjimka.Message;
        }
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

    private void PriZmeneAkcelerometru(object? sender, AccelerometerChangedEventArgs args)
    {
        var cteni = args.Reading;
        var sila = Math.Sqrt(
            cteni.Acceleration.X * cteni.Acceleration.X +
            cteni.Acceleration.Y * cteni.Acceleration.Y +
            cteni.Acceleration.Z * cteni.Acceleration.Z);

        if (sila > PrahZatraseni)
        {
            ZatraseniDetekovano?.Invoke(this, EventArgs.Empty);
        }
    }

    private void PriZmeneSvetelnehoSenzoru(object? sender, LightSensorChangedEventArgs args)
    {
        ZmenaDoporucenehoMotivu?.Invoke(this, args.Reading.Illuminance < PrahNizkehoOsvetleniLux);
    }
}
