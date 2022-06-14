using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using VeerPerforma.Utils;

namespace VeerPerforma.TestAdapter.Utils;

internal class CustomTestDiscovery
{
    private readonly FileIo fileIo;
    private readonly TestFilter testFilter;
    private readonly DirectoryRecursion dirRecursor;
    private readonly TestCaseItemCreator testCaseCreator;
    private readonly TypeLoader typeLoader;

    public CustomTestDiscovery()
    {
        fileIo = new FileIo();
        testFilter = new TestFilter();
        dirRecursor = new DirectoryRecursion();
        testCaseCreator = new TestCaseItemCreator();
        typeLoader = new TypeLoader();
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

            var perfTestTypes = typeLoader.LoadTypes(sourceDllPath);

            var correspondingCsFiles = fileIo.FindAllFilesRecursively(
                project,
                "*.cs",
                s => fileIo.FilePathDoesNotContainBinOrObjDirs(s));


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
                var cases = testCaseCreator.AssembleTestCases(bag, sourceDllPath);
                testCases.AddRange(cases);
            }
        }

        return testCases;
    }


}