using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using MediWait.Wpf.Modely;

namespace MediWait.Wpf.Prevodniky;

public sealed class StavTerminuNaStetecPrevodnik : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not Navsteva navsteva)
        {
            return Brushes.Black;
        }

        if (navsteva.JeHotovo)
        {
            return Brushes.SeaGreen;
        }

        if (navsteva.Termin < DateTime.Now)
        {
            return Brushes.IndianRed;
        }

        if (navsteva.Termin <= DateTime.Now.AddDays(2))
        {
            return Brushes.DarkGoldenrod;
        }

        return Brushes.DodgerBlue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
