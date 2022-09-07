# xunit.assemblyfixture

Provides shared state/fixture data across tests in the same assembly, following the design of [class fixtures](https://xunit.github.io/docs/shared-context.html#class-fixture) (rather than the more convoluted [collection fixtures](https://xunit.github.io/docs/shared-context.html#collection-fixture)). To complement [xUnit documentation style](https://xunit.github.io/docs/shared-context.html), I shamelessly copy its layout here.

## Shared Context between Tests

Please read [xUnit documentation](https://xunit.github.io/docs/shared-context.html) on shared context and the various built-in options, which are:

- [Constructor and Dispose](https://xunit.github.io/docs/shared-context.html#constructor) (shared setup/cleanup code without sharing object instances)
- [Class Fixtures](https://xunit.github.io/docs/shared-context.html#class-fixture) (shared object instance across tests in a single class)
- [Collection Fixtures](https://xunit.github.io/docs/shared-context.html#collection-fixture) (shared object instances across multiple test classes

To which this project adds:

- Assembly Fixtures (shared object instances across multiple test classes within the same test assembly)

### Assembly Fixtures

***When to use***: when you want to create a single assembly-level context
  and share it among all tests in the assembly, and have it cleaned up after
  all the tests in the assembly have finished.

  Sometimes test context creation and cleanup can be very expensive. If you were
  to run the creation and cleanup code during every test, it might make the tests
  slower than you want. Sometimes, you just need to aggregate data across multiple 
  tests in multiple classes. You can use the *assembly fixture* feature of
  [xUnit.net [Assembly Fixtures]](https://www.nuget.org/packages/xunit.assemblyfixture) 
  to share a single object instance among all tests in a test assembly.
  
  When using an assembly fixture, xUnit.net will ensure that the fixture instance 
  will be created before any of the tests using it have run, and once all the tests 
  have finished, it will clean up the fixture object by calling `Dispose`, if present.

To use assembly fixtures, you need to take the following steps:

- Create the fixture class, and put the the startup code in the fixture
  class constructor.
- If the fixture class needs to perform cleanup, implement `IDisposable`
  on the fixture class, and put the cleanup code in the `Dispose()` method.
- Add `IAssemblyFixture<TFixture>` to the test class.
- If the test class needs access to the fixture instance, add it as a
  constructor argument, and it will be provided automatically.

Here is a simple example:

```csharp
public class DatabaseFixture : IDisposable
{
    public DatabaseFixture()
    {
        Db = new SqlConnection("MyConnectionString");

        // ... initialize data in the test database ...
    }

    public void Dispose()
    {
        // ... clean up test data from the database ...
    }

    public SqlConnection Db { get; private set; }
}

public class MyDatabaseTests : IAssemblyFixture<DatabaseFixture>
{
    DatabaseFixture fixture;

    public MyDatabaseTests(DatabaseFixture fixture)
    {
        this.fixture = fixture;
    }

    // ... write tests, using fixture.Db to get access to the SQL Server ...
}
```

  Just before the first test in the assembly to require the `DatabaseFixture`
  is run, xUnit.net will create an instance of `DatabaseFixture`. Each subsequent 
  test will receive the same shared instance, passed to the constructor of 
  `MyDatabaseTests`, just like a static singleton, but with predictable cleanup
  via `IDisposable`.  

  ***Important note:*** xUnit.net uses the presence of the interface
  `IAssemblyFixture<>` to know that you want an assembly fixture to
  be created and cleaned up. It will do this whether you take the instance of
  the class as a constructor argument or not. Simiarly, if you add the constructor
  argument but forget to add the interface, xUnit.net will let you know that it
  does not know how to satisfy the constructor argument.

  If you need multiple fixture objects, you can implement the interface as many
  times as you want, and add constructor arguments for whichever of the fixture
  object instances you need access to. The order of the constructor arguments
  is unimportant.

  Note that you cannot control the order that fixture objects are created, and
  fixtures cannot take dependencies on other fixtures. If you have need to
  control creation order and/or have dependencies between fixtures, you should
  create a class which encapsulates the other two fixtures, so that it can
  do the object creation itself.