using System.Globalization;
using MediWait.Wpf.Modely;

namespace MediWait.Wpf.Prevodniky;

public sealed class StavTerminuNaBarvuPrevodnik : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not Navsteva navsteva)
        {
            return Colors.Black;
        }

        if (navsteva.JeHotovo)
        {
            return Colors.SeaGreen;
        }

        if (navsteva.Termin < DateTime.Now)
        {
            return Colors.IndianRed;
        }

        if (navsteva.Termin <= DateTime.Now.AddDays(2))
        {
            return Colors.DarkGoldenrod;
        }

        return Colors.DodgerBlue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
