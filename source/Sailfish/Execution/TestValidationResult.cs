using System;
using System.Collections.Generic;

namespace Sailfish.Execution
{
    internal class TestValidationResult
    {
        private TestValidationResult(bool isValid, Type[] tests, Dictionary<string, List<string>> errors)
        {
            IsValid = isValid;
            Errors = errors;
            Tests = tests;
        }

        public Type[] Tests { get; set; }
        public bool IsValid { get; set; }
        public Dictionary<string, List<string>> Errors { get; }

        public static TestValidationResult CreateSuccess(Type[] tests)
        {
            return new TestValidationResult(true, tests, new Dictionary<string, List<string>>());
        }

        public static TestValidationResult CreateFailure(Type[] tests, Dictionary<string, List<string>> errors)
        {
            return new TestValidationResult(false, tests, errors);
        }
    }
}