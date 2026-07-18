namespace TrainService.Core.Abstractions;

/// <summary>
/// WPF-bağımsız ICommand benzeri arayüz.
/// Cad katmanı WPF'e referans veremez (A1 arteri),
/// bu nedenle System.Windows.Input.ICommand yerine
/// bu arayüz kullanılır.
/// </summary>
public interface ICommand
{
    event EventHandler? CanExecuteChanged;
    bool CanExecute(object? parameter);
    void Execute(object? parameter);
}
