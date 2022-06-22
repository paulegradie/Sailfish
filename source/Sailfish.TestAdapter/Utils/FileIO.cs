using System;
using System.IO;

namespace Sailfish.TestAdapter.Utils
{
    internal class FileIo // This is all DirectoryRecursion logic - should move this.
    {
        public string ReadFileContents(string sourceFile)
        {
            var content = ReadFile(sourceFile);
            return content;
        }

        private string ReadFile(string filePath)
        {
            try
            {
                using var fileStream = new StreamReader(filePath);
                var content = fileStream.ReadToEnd();
                return content;
            }
            catch
            {
                throw new Exception($"Could not read the file provided: {filePath}");
            }
        }
    }
}