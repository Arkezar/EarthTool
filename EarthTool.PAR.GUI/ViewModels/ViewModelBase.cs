using ReactiveUI;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;

namespace EarthTool.PAR.GUI.ViewModels;

public class ViewModelBase : ReactiveObject, INotifyDataErrorInfo
{
  private readonly Dictionary<string, List<string>> _errors = new();

  public bool HasErrors => _errors.Any();

  public event System.EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

  public System.Collections.IEnumerable GetErrors(string? propertyName)
  {
    if (string.IsNullOrEmpty(propertyName))
    {
      return _errors.SelectMany(x => x.Value);
    }

    return _errors.TryGetValue(propertyName, out var errors) ? errors : System.Linq.Enumerable.Empty<string>();
  }

  protected void SetErrors(string propertyName, IEnumerable<string> errors)
  {
    var errorList = errors.ToList();
    
    if (errorList.Any())
    {
      _errors[propertyName] = errorList;
    }
    else
    {
      _errors.Remove(propertyName);
    }

    ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
  }

  protected void ClearErrors([CallerMemberName] string? propertyName = null)
  {
    if (propertyName != null && _errors.Remove(propertyName))
    {
      ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }
  }

  protected virtual void ValidateProperty(object? value, [CallerMemberName] string? propertyName = null)
  {
    if (propertyName == null) return;

    var validationContext = new ValidationContext(this) { MemberName = propertyName };
    var results = new List<ValidationResult>();
    
    var isValid = Validator.TryValidateProperty(value, validationContext, results);
    
    if (isValid)
    {
      ClearErrors(propertyName);
    }
    else
    {
      var errors = results.Select(r => r.ErrorMessage ?? "Unknown validation error");
      SetErrors(propertyName, errors);
    }
  }

  protected bool RaiseAndSetIfChangedWithValidation<T>(ref T backingField, T newValue, [CallerMemberName] string? propertyName = null)
  {
    if (EqualityComparer<T>.Default.Equals(backingField, newValue))
    {
      return false;
    }

    backingField = newValue;
    ValidateProperty(newValue, propertyName);
    this.RaisePropertyChanged(propertyName);
    return true;
  }
}