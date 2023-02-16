# Using Sailfish as a Console App

There are two options to choose from when using Sailfish as a console app

## Use the built-in SailfishProgramBase

If you wish to use the program base that ships with Sailfish, there are small number of things to do. Lets have a look at each piece, while assuming that each block below is in its own file but in the same project together:

### The Main Program (program.cs)
The quickest way to set up a basic console app is to inherit directly from `SailfishProgramBase`. This base class implements a command line and various other things that will enable you to configure your Sailfish run from the commandline, and invoke your tests.

There are **two overrides** in this example, each of which has a specific function:

 - `SourceTypesProvider()`
   - The source types provider is use to direct Sailfish where to scan for Sailfish tests. You can hold your tests in multiple assemblies, and so lang as you provide at least one type from each assembly, Sailfish will attempt to discover types from those assemblies.
 - `RegistrationTypeProvider()`
  - The RegistrationTypeProvider takes an array of types that specify which assemblies Sailfish will scan for implementations of the `IProvideARegistrationCallback` interface.

```csharp
class Program : SailfishProgramBase
{
    public static async Task Main(string[] testNamesToFilterBy)
    {
        await SailfishMain<Program>(testNamesToFilterBy);
    }

    protected override IEnumerable<Type> SourceTypesProvider()
    {
       return new[] { GetType() };
    }

    protected override RegistrationTypeProvider()
    {
        return new[] {typeof(RegistrationProvider)}
    }
}
```
#### When would I use this?
You might consider using this approach when you need to run your sailfish tests from an external assembly, but you don't necessarily have access to them directly - in other words, you've defined a solution that has a console app project, but that solution doesn't **not** also contain your tests.

Say for example you've devised a system that is capable of receiving DLLs that are developed independantly by various different teams. When you receive the request and write the dll to a temporary location, the system then reads the types from the `.dll`, discovers any sailfish tests, and then executes them. In this scenario, the teams are writing tests, but the system is exectuting them and tracking the results. The teams would have only the `Sailfish.TestAdapter` package to write / run their tests in dev.

---
### The Registration Provider

This class is used to provide registrations to the DI container that Sailfish uses internally. You don't instantiate this type, you simple implement the interface. Internally, Sailfish uses Autofac 4.6 (for backwards compatibility reasons), so you are provided the container build with which you can register components. The enables Sailfish to inject any dependencies you wish to resolve in your tests.

> **Note**
> This may feel like 'magic' to some users since we're holding our registrations outside of the normal program code flow. However, this design facilatates a very clean way to partition our registrations when having [a console app next to a test project](./).

```csharp
// this will be picked up by Sailfish's dependency scanner
public class RegistrationProvider : IProvideARegistrationCallback
{
    public async Task RegisterAsync(ContainerBuilder builder, CancellationToken ct)
    {
       var typeInstance = await MyClientFactory.Create(ct);
       builder.RegisterType(typeInstance).As<IClient>();
    }
}
```

---
### The Test
This is just a simple test that assumes an `IClient` has been registered.
```csharp
[Sailfish]
public class AMostBasicTest
{
    private readonly IClient myClient;

    public AMostBasicTest(IClient myClient) // type is injected so long as its registered
    {
        this.myClient = myClient;
    }

    [SailfishMethod]
    public async Task TestMethod(CancellationToken cancellationToken) // token is injected when requested
    {
        await myClient.Get("/api", cancellationToken);
    }
}
```

> **Note**: If you place your tests in the same project as your console app, you will not be able to run them from your IDE. Instead, place them in a separate project, reference that project, and then provide the registration provider to the console app to either the override method show below (if you are using the provided base class) or the `RunSettings` object (if you are invoking the SailfishRunner directly).


## Write your own Program.cs

If you would prefer build out your own console app program, you are certain free to do so. Sailflsh exposes the `SailfishRunner` **static** type, which you can use to kick of a run.

```csharp
// .net6 program.cs

var SourceTypesProvider = new Type[] { RegistrationProvider };
var RegistrationProviderTypesProvider = new Type[] { RegistrationProvider };

var sailfishRunResult = await SailfishRunner.Run(
            AssembleRunRequest(
                args,
                SourceTypesProvider,
                RegistrationProviderTypesProvider),
            cancellationToken);
var not = sailfishRunResult.IsValid ? string.Empty : "not ";
Console.WriteLine($"Test run was {not}valid");

RunSettings AssembleRunRequest(args)
{
    return new RunSettings(...);
}
```
> **Note**:
> This approach provides the opportunity to configure sailfish runs dynamically depending on, e.g. custom command line arguments that are recieved the the console app is invoked. Flexible tracking systems can be built around this design.


---
### Next: [As A Test Project](./as-a-test-project.md)