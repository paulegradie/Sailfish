using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using VeerPerforma.Attributes;
using VeerPerforma.Utils;

namespace VeerPerforma.TestAdapter.Utils;

internal class CustomTestDiscovery
{
    private readonly DllIterator dllIterator;
    private readonly FileIo fileIo;
    private readonly TestFilter testFilter;
    private readonly DirectoryRecursion dirRecursor;
    private readonly TestCaseItemCreator testCaseCreator;

    public CustomTestDiscovery()
    {
        dllIterator = new DllIterator();
        fileIo = new FileIo();
        testFilter = new TestFilter();
        dirRecursor = new DirectoryRecursion();
        testCaseCreator = new TestCaseItemCreator();
    }

    public IEnumerable<TestCase> DiscoverTests(IEnumerable<string> sourceDlls)
    {
        var sourceDllPaths = sourceDlls.ToList();
        logger.Verbose("Entering into the DiscoverTests method!");
        logger.Verbose("Processing source .dlls: {dlls}", string.Join(", ", sourceDllPaths));
        var referenceFile = sourceDllPaths.First();

        var testCases = new List<TestCase>();
        foreach (var sourceDllPath in sourceDllPaths)
        {
            var project = dirRecursor.RecurseUpwardsUntilFileIsFound(
                ".csproj",
                referenceFile,
                maxParentDirLevel: 5);

            var assembly = LoadAssemblyFromDll(sourceDllPath);

            var correspondingCsFiles = fileIo.FindAllFilesRecursively(
                project,
                "*.cs",
                s => fileIo.FilePathDoesNotContainBinOrObjDirs(s));

            var perfTestTypes = CollectTestTypesFromAssembly(assembly);

            logger.Verbose("Beginning assembly of test cases!");

            var bags = new List<DataBag>();
            foreach (var csFilePath in correspondingCsFiles)
            {
                var fileContent = fileIo.ReadFileContents(csFilePath);
                var perfTypesInThisCsFile = testFilter.FindTestTypesInTheCurrentFile(fileContent, perfTestTypes);

                foreach (var perfTestType in perfTypesInThisCsFile)
                {
                    bags.Add(new DataBag(csFilePath, fileContent, perfTestType));
                }
            }

            if (bags.Count < 1) throw new Exception("Failed to find any test type file paths");

            foreach (var bag in bags)
            {
                var cases = testCaseCreator.AssembleClassTestCases(bag, sourceDllPath);
                testCases.AddRange(cases);
            }
        }

        return testCases;
    }

    public Type[] CollectTestTypesFromAssembly(Assembly assembly)
    {
        var perfTestTypes = assembly // mvp only supports test discovery in current assembly 
            .GetTypes()
            .Where(x => x.HasAttribute<VeerPerformaAttribute>())
            .ToArray();

        if (perfTestTypes.Length < 1) throw new Exception("No perf test types found");

        logger.Verbose("\rTests Types Discovered:\r");
        foreach (var testType in perfTestTypes)
        {
            logger.Verbose("--- Perf tests: {0}", testType.Name);
        }

        return perfTestTypes;
    }

    private Assembly LoadAssemblyFromDll(string dllPath)
    {
        var assembly = Assembly.LoadFile(dllPath);
        AppDomain.CurrentDomain.Load(assembly.GetName()); // is this necessary?
        return assembly;
    }
}