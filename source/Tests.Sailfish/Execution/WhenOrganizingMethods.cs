using System.Collections.Generic;
using System.Linq;
using Sailfish.Attributes;
using Sailfish.Execution;
using Shouldly;
using Xunit;

namespace Test.Execution;

public class WhenOrganizingMethods
{
    [Fact]
    public void MethodGroupsAreFormedCorrectlyWithASingleExecutionMethod()
    {
        var instances = new List<object>()
        {
            new OrganizingTestClassSingleMethod(1),
            new OrganizingTestClassSingleMethod(2),
        };

        var organizer = new MethodOrganizer();

        var result = organizer.FormMethodGroups(instances);

        var methodName = result.Keys.OrderBy(x => x).Single();

        result[methodName].Count.ShouldBe(2);
        result[methodName].First().Item1.Name.ShouldBe(nameof(OrganizingTestClassSingleMethod.MethodA));
        ((OrganizingTestClassSingleMethod)result[methodName].First().Item2).Ints.OrderBy(x => x).Single().ShouldBe(1);
        ((OrganizingTestClassSingleMethod)result[methodName].Last().Item2).Ints.OrderBy(x => x).Single().ShouldBe(2);
    }

    public class OrganizingTestClassSingleMethod
    {
        public OrganizingTestClassSingleMethod(params int[] ints)
        {
            Ints = ints;
        }

        public int[] Ints { get; set; }

        [ExecutePerformanceCheck]
        public void MethodA()
        {
        }
    }


    [Fact]
    public void MethodGroupsAreFormedCorrectlyWithTwoExecutionMethods()
    {
        var instances = new List<object>()
        {
            new OrganizingTestClassTwoMethods(1),
            new OrganizingTestClassTwoMethods(2),
        };

        var organizer = new MethodOrganizer();

        var result = organizer.FormMethodGroups(instances);

        var methods = result.Keys.OrderBy(x => x).ToList();

        foreach (var methodName in methods)
        {
            result[methodName].Count.ShouldBe(2);
            result[methodName].First().Item1.Name.ShouldBe(methodName);
            ((OrganizingTestClassTwoMethods)result[methodName].First().Item2).Ints.OrderBy(x => x).Single().ShouldBe(1);
            ((OrganizingTestClassTwoMethods)result[methodName].Last().Item2).Ints.OrderBy(x => x).Single().ShouldBe(2);
        }
        
        methods.First().ShouldBe(nameof(OrganizingTestClassTwoMethods.MethodA));
        methods.Last().ShouldBe(nameof(OrganizingTestClassTwoMethods.MethodB));

    }

    public class OrganizingTestClassTwoMethods
    {
        public OrganizingTestClassTwoMethods(params int[] ints)
        {
            Ints = ints;
        }

        public int[] Ints { get; set; }

        [ExecutePerformanceCheck]
        public void MethodA()
        {
        }

        [ExecutePerformanceCheck]
        public void MethodB()
        {
        }
    }
}