using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Proiect_Planificare_Buget.ViewModels;

public abstract class ViewModelBase : INotifyPropertyChanged
{
    private bool _isBusy;
    private string _statusMessage = string.Empty;
    private Func<Task>? _pendingRefreshAction;

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsBusy
    {
        get => _isBusy;
        protected set => SetProperty(ref _isBusy, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        protected set => SetProperty(ref _statusMessage, value);
    }

    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected Task HandleDataChangedAsync(Func<Task> refreshAction)
    {
        if (IsBusy)
        {
            _pendingRefreshAction = refreshAction;
            return Task.CompletedTask;
        }

        return refreshAction();
    }

    protected async Task RunBusyOperationAsync(Func<Task> action, string? successMessage = null, string? errorPrefix = null)
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            await action();
            if (!string.IsNullOrWhiteSpace(successMessage))
            {
                StatusMessage = successMessage;
            }
        }
        catch (Exception exception)
        {
            StatusMessage = string.IsNullOrWhiteSpace(errorPrefix)
                ? exception.Message
                : $"{errorPrefix}: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }

        while (_pendingRefreshAction is not null)
        {
            var pendingRefresh = _pendingRefreshAction;
            _pendingRefreshAction = null;
            await pendingRefresh();
        }
    }
}
