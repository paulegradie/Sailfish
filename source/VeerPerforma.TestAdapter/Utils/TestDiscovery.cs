using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace VeerPerforma.TestAdapter.Utils
{
    internal class TestDiscovery
    {
        private readonly DirectoryRecursion dirRecursor;
        private readonly FileIo fileIo;
        private readonly TestCaseItemCreator testCaseCreator;
        private readonly TestFilter testFilter;
        private readonly TypeLoader typeLoader;

        public TestDiscovery()
        {
            fileIo = new FileIo();
            testFilter = new TestFilter();
            dirRecursor = new DirectoryRecursion();
            testCaseCreator = new TestCaseItemCreator();
            typeLoader = new TypeLoader();
        }

        public IEnumerable<TestCase> DiscoverTests(IEnumerable<string> sourceDllPaths)
        {
            var testCases = new List<TestCase>();
            foreach (var sourceDllPath in sourceDllPaths.Distinct())
            {
                var project = dirRecursor.RecurseUpwardsUntilFileIsFound(
                    ".csproj",
                    sourceDllPath,
                    10);

                Type[] perfTestTypes;
                try
                {
                    perfTestTypes = typeLoader.LoadTypes(sourceDllPath);
                }
                catch
                {
                    continue;
                }

                if (perfTestTypes.Length == 0) continue;

                var correspondingCsFiles = dirRecursor.FindAllFilesRecursively(
                    project,
                    "*.cs",
                    s => DirectoryRecursion.FileSearchFilters.FilePathDoesNotContainBinOrObjDirs(s));
                if (correspondingCsFiles.Count == 0) continue;

                var bags = new List<DataBag>();
                foreach (var csFilePath in correspondingCsFiles)
                {
                    var fileContent = fileIo.ReadFileContents(csFilePath);
                    var perfTypesInThisCsFile = testFilter.FindTestTypesInTheCurrentFile(fileContent, perfTestTypes);

                    foreach (var perfTestType in perfTypesInThisCsFile)
                        bags.Add(new DataBag(csFilePath, fileContent, perfTestType));
                }

                if (bags.Count == 0) continue;
                foreach (var bag in bags)
                {
                    var cases = testCaseCreator.AssembleTestCases(bag, sourceDllPath);
                    testCases.AddRange(cases);
                }
            }

            return testCases;
        }
    }
}