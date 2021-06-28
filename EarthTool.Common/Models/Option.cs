namespace EarthTool.Common.Models
{
  public class Option
  {
    public string Name
    {
      get;
    }

    public object Value
    {
      get;
    }

    public Option(string name, object value)
    {
      Name = name;
      Value = value;
    }

    public T GetValue<T>()
    {
      return (T)Value;
    }
  }
}
