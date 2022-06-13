using System;

namespace VeerPerforma.TestAdapter.Utils;

public class DataBag
{
    public DataBag(string csFilePath, string fileContent, Type type)
    {
        CsFilePath = csFilePath;
        CsFileContentString = fileContent;
        Type = type;
    }

    public string CsFilePath { get; set; }
    public string CsFileContentString { get; set; }
    public Type Type { get; set; }

    public string[] GetContentLines()
    {
        if (CsFileContentString is null) throw new Exception("CsFileContent is not set.");
        return CsFileContentString.Split("\r");
    }
    // public static TestProperty TestProp = GetTestProperty();
    //
    // static TestProperty GetTestProperty()
    // {
    //     return TestProperty.Register("VeerPerformaTestCase", "VeerPerforma Test Case", typeof(Assembly), typeof(DataMap));
    // } 
}