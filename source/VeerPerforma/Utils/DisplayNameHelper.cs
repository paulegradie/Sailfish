namespace VeerPerforma.Utils;

public static class DisplayNameHelper
{
    public static string CreateDisplayName(Type testType, string methodName, string paramsCombo)
    {
        return string.Join(".", testType.Name, methodName) + paramsCombo;
    }

    public static string CreateParamsDisplay(string[] variableNames, int[] paramSet)
    {
        if (variableNames.Length != paramSet.Length) throw new Exception("Number of variables and number of params does not match");
        var namedParams = variableNames.Zip(paramSet);
        
        
        return "(" + string.Join(", ", namedParams.Select(pair => FormNameString(pair)).ToArray()) + ")";
    }

    private static string FormNameString((string First, int Second) pair)
    {
        return $"{pair.First}: {pair.Second}";
    }
}