namespace LangFilesEditor.Structure;

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

/// <summary>
/// Объект, уведомляющий об изменении свойств
/// </summary>
[PublicAPI]
public class ObservableObject : INotifyPropertyChanged
{
    /// <inheritdoc />
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Вызвать уведомление изменения свойства
    /// </summary>
    /// <param name="propertyName">Имя свойства</param>
    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}