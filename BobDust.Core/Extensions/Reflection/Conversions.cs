using System.Dynamic;

namespace BobDust.Core.Extensions.Reflection;

public static class Conversions
{
    public static ExpandoObject ToExpando(this object anonymousObject)
    {
        var expando = new ExpandoObject();
        var expandoDict = (IDictionary<string, object?>)expando!;

        foreach (var prop in anonymousObject.GetType().GetProperties())
        {
            expandoDict[prop.Name] = prop.GetValue(anonymousObject);
        }

        return expando;
    }
}

