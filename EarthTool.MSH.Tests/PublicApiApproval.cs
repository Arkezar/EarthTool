using AwesomeAssertions;
using System.Reflection;
using System.Text;

namespace EarthTool.MSH.Tests;

internal static class PublicApiApproval
{
  internal static void Verify(string name, IEnumerable<Type> types)
  {
    var actual = CreateSurface(types);
    var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));
    var approvedPath = Path.Combine(root, "EarthTool.MSH.Tests", "Approvals", $"{name}.approved.txt");
    var receivedPath = Path.Combine(root, "EarthTool.MSH.Tests", "Approvals", $"{name}.received.txt");
    var approved = File.ReadAllText(approvedPath).ReplaceLineEndings("\n");
    if (actual != approved)
    {
      File.WriteAllText(receivedPath, actual);
    }
    else
    {
      File.Delete(receivedPath);
    }

    actual.Should().Be(approved);
  }

  private static string CreateSurface(IEnumerable<Type> types)
  {
    var lines = new List<string>();
    foreach (var type in types.OrderBy(type => type.FullName, StringComparer.Ordinal))
    {
      lines.Add(FormatTypeDeclaration(type));
      foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
        .OrderBy(field => field.Name, StringComparer.Ordinal))
      {
        lines.Add($"  {FormatObsolete(field)}field {FormatType(field.FieldType, null)} {field.Name} = {FormatValue(field.GetRawConstantValue(), field.FieldType)}");
      }

      foreach (var constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
        .OrderBy(constructor => constructor.ToString(), StringComparer.Ordinal))
      {
        lines.Add($"  {FormatObsolete(constructor)}constructor {type.Name}({FormatParameters(constructor.GetParameters())})");
      }

      foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
        .OrderBy(property => property.Name, StringComparer.Ordinal))
      {
        var nullability = new NullabilityInfoContext().Create(property);
        var accessor = $"{{ {(property.GetMethod is null ? string.Empty : "get; ")}{(property.SetMethod is null ? string.Empty : "set; ")}}}";
        lines.Add($"  {FormatObsolete(property)}property {FormatType(property.PropertyType, nullability)} {property.Name} {accessor}");
      }

      foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
        .Where(method => !method.IsSpecialName)
        .OrderBy(method => method.Name, StringComparer.Ordinal)
        .ThenBy(method => method.ToString(), StringComparer.Ordinal))
      {
        var nullability = new NullabilityInfoContext().Create(method.ReturnParameter);
        lines.Add($"  {FormatObsolete(method)}method {FormatType(method.ReturnType, nullability)} {method.Name}({FormatParameters(method.GetParameters())})");
      }
    }

    return string.Join("\n", lines) + "\n";
  }

  private static string FormatTypeDeclaration(Type type)
  {
    var modifiers = type.IsInterface || type.IsEnum || type.IsValueType
      ? string.Empty
      : type.IsAbstract && type.IsSealed
        ? "static"
        : type.IsAbstract
          ? "abstract"
          : type.IsSealed
            ? "sealed"
            : string.Empty;
    var kind = type.IsInterface ? "interface" : type.IsEnum ? "enum" : type.IsValueType ? "struct" : "class";
    var bases = new List<string>();
    if (type.BaseType is not null && type.BaseType != typeof(object) && type.BaseType != typeof(ValueType) && type.BaseType != typeof(Enum))
    {
      bases.Add(FormatType(type.BaseType, null));
    }

    bases.AddRange(type.GetInterfaces().Select(@interface => FormatType(@interface, null)).Order(StringComparer.Ordinal));
    var inheritance = bases.Count == 0 ? string.Empty : " : " + string.Join(", ", bases);
    return $"{FormatObsolete(type)}public {modifiers} {kind} {FormatType(type, null)}{inheritance}{FormatConstraints(type)}"
      .Replace("  ", " ");
  }

  private static string FormatConstraints(Type type)
  {
    if (!type.IsGenericTypeDefinition)
    {
      return string.Empty;
    }

    var constraints = new List<string>();
    foreach (var argument in type.GetGenericArguments())
    {
      var values = new List<string>();
      var attributes = argument.GenericParameterAttributes;
      if ((attributes & GenericParameterAttributes.ReferenceTypeConstraint) != 0)
      {
        values.Add("class");
      }

      if ((attributes & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0)
      {
        values.Add("struct");
      }

      values.AddRange(argument.GetGenericParameterConstraints()
        .Where(constraint => constraint != typeof(ValueType))
        .Select(constraint => FormatType(constraint, null)));
      if ((attributes & GenericParameterAttributes.DefaultConstructorConstraint) != 0
        && !values.Contains("struct", StringComparer.Ordinal))
      {
        values.Add("new()");
      }

      if (values.Count > 0)
      {
        constraints.Add($" where {argument.Name} : {string.Join(", ", values)}");
      }
    }

    return string.Concat(constraints);
  }

  private static string FormatObsolete(MemberInfo member)
  {
    return member.GetCustomAttribute<ObsoleteAttribute>() is null ? string.Empty : "[Obsolete] ";
  }

  private static string FormatParameters(IEnumerable<ParameterInfo> parameters)
  {
    return string.Join(", ", parameters.Select(parameter =>
    {
      var nullability = new NullabilityInfoContext().Create(parameter);
      var defaultValue = parameter.HasDefaultValue
        ? $" = {FormatValue(parameter.DefaultValue, parameter.ParameterType)}"
        : string.Empty;
      return $"{FormatType(parameter.ParameterType, nullability)} {parameter.Name}{defaultValue}";
    }));
  }

  private static string FormatType(Type type, NullabilityInfo? nullability)
  {
    if (type.IsArray)
    {
      return FormatType(type.GetElementType()!, nullability?.ElementType) + "[]" + NullableSuffix(type, nullability);
    }

    if (type.IsGenericType)
    {
      var definition = type.GetGenericTypeDefinition();
      if (definition == typeof(Nullable<>))
      {
        return FormatType(type.GetGenericArguments()[0], nullability?.GenericTypeArguments.ElementAtOrDefault(0)) + "?";
      }

      var name = (definition.FullName ?? definition.Name).Split('`')[0];
      var arguments = type.GetGenericArguments();
      var formattedArguments = arguments.Select((argument, index) =>
        FormatType(argument, nullability?.GenericTypeArguments.ElementAtOrDefault(index)));
      return $"{name}<{string.Join(", ", formattedArguments)}>" + NullableSuffix(type, nullability);
    }

    return (type.FullName ?? type.Name) + NullableSuffix(type, nullability);
  }

  private static string NullableSuffix(Type type, NullabilityInfo? nullability)
  {
    return !type.IsValueType && nullability?.ReadState == NullabilityState.Nullable ? "?" : string.Empty;
  }

  private static string FormatValue(object? value, Type type)
  {
    if (value is null && type.IsValueType && Nullable.GetUnderlyingType(type) is null)
    {
      return "default";
    }

    return value switch
    {
      null => "null",
      string text => $"\"{text}\"",
      bool boolean => boolean ? "true" : "false",
      Enum enumeration => $"{enumeration.GetType().FullName}.{enumeration}",
      _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "null"
    };
  }
}
