using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MediWait.Wpf.MVVM;

public abstract class ZakladViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void Nastavit<T>(ref T pole, T hodnota, [CallerMemberName] string? jmeno = null)
    {
        if (EqualityComparer<T>.Default.Equals(pole, hodnota))
        {
            return;
        }

        pole = hodnota;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(jmeno));
    }

    protected void OznacitZmenu([CallerMemberName] string? jmeno = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(jmeno));
    }
}
