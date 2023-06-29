using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Xunit;

class TestAssemblyRunner : XunitTestAssemblyRunner
{
    readonly Dictionary<Type, object> assemblyFixtureMappings = new();

    public TestAssemblyRunner(ITestAssembly testAssembly,
        IEnumerable<IXunitTestCase> testCases,
        IMessageSink diagnosticMessageSink,
        IMessageSink executionMessageSink,
        ITestFrameworkExecutionOptions executionOptions)
        : base(testAssembly, testCases, diagnosticMessageSink, executionMessageSink, executionOptions)
    {
    }
    
    protected override async Task AfterTestAssemblyStartingAsync()
    {
        await base.AfterTestAssemblyStartingAsync();

        // Go find all the IAssemblyAsyncLifetime adorned on the test assembly
        Aggregator.Run(() =>
        {
            var type = typeof(IAssemblyAsyncLifetime);
            
            var assemblyAsyncLifetimeClasses = ((IReflectionAssemblyInfo)TestAssembly.Assembly).Assembly
                .GetTypes()
                .Where(p => type.IsAssignableFrom(p));

            // Instantiate all the classes implements IAssemblyAsyncLifetime
            foreach (var assemblyAsyncLifetimeClass in assemblyAsyncLifetimeClasses)
                assemblyFixtureMappings[assemblyAsyncLifetimeClass] =
                    Activator.CreateInstance(assemblyAsyncLifetimeClass)!;
        });
        
        // Call InitializeAsync on all instances of IAssemblyAsyncLifetime, and use Aggregator.RunAsync to isolate
        // InitializeAsync failures
        foreach (var disposable in assemblyFixtureMappings.Values.OfType<IAssemblyAsyncLifetime>())
            await Aggregator.RunAsync(disposable.InitializeAsync);
    }

    protected override async Task BeforeTestAssemblyFinishedAsync()
    {
        // Make sure we clean up everybody who is disposable, and use Aggregator.Run to isolate Dispose failures
        foreach (var disposable in assemblyFixtureMappings.Values.OfType<IDisposable>())
            Aggregator.Run(disposable.Dispose);
        
        // Call DisposeAsync on all instances of IAssemblyAsyncLifetime, and use Aggregator.RunAsync to isolate
        // DisposeAsync failures
        foreach (var disposable in assemblyFixtureMappings.Values.OfType<IAssemblyAsyncLifetime>())
            await Aggregator.RunAsync(disposable.DisposeAsync);

        await base.BeforeTestAssemblyFinishedAsync();
    }


    protected override Task<RunSummary> RunTestCollectionAsync(IMessageBus messageBus,
                                                               ITestCollection testCollection,
                                                               IEnumerable<IXunitTestCase> testCases,
                                                               CancellationTokenSource cancellationTokenSource)
        => new TestCollectionRunner(assemblyFixtureMappings, testCollection, testCases, DiagnosticMessageSink, messageBus, TestCaseOrderer, new ExceptionAggregator(Aggregator), cancellationTokenSource).RunAsync();
}
