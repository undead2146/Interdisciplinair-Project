using InterdisciplinairProject.Core.Models.Enums;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace InterdisciplinairProject.ViewModels;

/// <summary>
/// Represents a selectable effect option in the UI.
/// </summary>
public class EffectSelectionViewModel : INotifyPropertyChanged
{
    private bool _isSelected;

    public EffectSelectionViewModel(EffectType type, bool isSelected = false)
    {
        Type = type;
        Name = type.ToString();
        _isSelected = isSelected;
    }

    /// <summary>
    /// Gets the effect type.
    /// </summary>
    public EffectType Type { get; }

    /// <summary>
    /// Gets the display name of the effect.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets or sets a value indicating whether this effect is selected.
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
