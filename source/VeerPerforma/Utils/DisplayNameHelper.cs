using System.Reflection;

namespace VeerPerforma.Utils;

public static class DisplayNameHelper
{
    public static string CreateDisplayName(Type testType, string methodName, string paramsCombo)
    {
        return string.Join(".", testType.Name, methodName, paramsCombo);
    }

    public static string CreateParamsDisplay(IEnumerable<int> paramSet)
    {
        return "(" + string.Join(", ", paramSet) + ")";
    }
}