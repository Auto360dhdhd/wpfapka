using System.Windows.Input;

namespace MediWait.Wpf.MVVM;

public sealed class RelayPrikaz : ICommand
{
    private readonly Action<object?> _provedeni;
    private readonly Func<object?, bool>? _muzeProvest;

    public RelayPrikaz(Action<object?> provedeni, Func<object?, bool>? muzeProvest = null)
    {
        _provedeni = provedeni;
        _muzeProvest = muzeProvest;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        return _muzeProvest?.Invoke(parameter) ?? true;
    }

    public void Execute(object? parameter)
    {
        _provedeni(parameter);
    }

    public void Obnovit()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
