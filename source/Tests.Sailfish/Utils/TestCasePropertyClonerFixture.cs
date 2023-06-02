using System;
using Sailfish.Utils;
using Shouldly;
using Xunit;

namespace Test.Utils;

public class TestCasePropertyClonerFixture
{
    public class Dehydrated
    {
        public Dehydrated()
        {
        }

        public Dehydrated(
            string? @protectedField,
            string? @privateField,
            string? @publicField,
            string? @internalField,
            string? @public,
            string? @protected)
        {
            ProtectedField = @protectedField;
            PrivateField = @privateField;
            PublicField = @publicField;
            InternalField = @internalField;
            Public = @public;
            Protected = @protected;
        }

        public string? Public { get; set; }
        protected string? Protected { get; set; }
        internal string? InternalField;
        protected string? ProtectedField;
        private string? PrivateField;
        public string? PublicField;
        
        
        public string? InternalSet { get; internal set; }
        internal string? Internal { get; set; }
        private string? Private { get; set; }
        public string? ProtectedSet { get; protected set; }
        public string? PrivateSet { get; private set; }
    }

    [Fact]
    public void AllPropertiesAreCloned()
    {
        // these are the allowed 
        var hydrated = new Dehydrated(
            @internalField: "InternalField",
            @privateField: "PrivateField",
            @protectedField: "ProtectedField",
            @publicField: "PublicField",
            @public: "Public",
            @protected: "Protected"
        );

        hydrated.Public.ShouldBe("Public");
        hydrated.InternalField.ShouldBe("InternalField");
        var propertiesAndFields = hydrated.RetrievePropertiesAndFields();
        var result = Activator.CreateInstance<Dehydrated>();
        propertiesAndFields.ApplyPropertiesAndFieldsTo(result);

        result.ShouldBeEquivalentTo(hydrated);
    }
}