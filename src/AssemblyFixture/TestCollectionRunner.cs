using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Xunit;

class TestCollectionRunner : XunitTestCollectionRunner
{
    readonly Dictionary<Type, object> assemblyFixtureMappings;
    readonly IMessageSink diagnosticMessageSink;

    public TestCollectionRunner(Dictionary<Type, object> assemblyFixtureMappings,
        ITestCollection testCollection,
        IEnumerable<IXunitTestCase> testCases,
        IMessageSink diagnosticMessageSink,
        IMessageBus messageBus,
        ITestCaseOrderer testCaseOrderer,
        ExceptionAggregator aggregator,
        CancellationTokenSource cancellationTokenSource)
        : base(testCollection, testCases, diagnosticMessageSink, messageBus, testCaseOrderer, aggregator, cancellationTokenSource)
    {
        this.assemblyFixtureMappings = assemblyFixtureMappings;
        this.diagnosticMessageSink = diagnosticMessageSink;
    }

    protected override Task<RunSummary> RunTestClassAsync(ITestClass testClass, IReflectionTypeInfo @class, IEnumerable<IXunitTestCase> testCases)
    {
        foreach (var fixtureType in @class.Type.GetTypeInfo().ImplementedInterfaces
                .Where(i => i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(IAssemblyFixture<>))
                .Select(i => i.GetTypeInfo().GenericTypeArguments.Single())
                // First pass at filtering out before locking
                .Where(i => !assemblyFixtureMappings.ContainsKey(i)))
        {
            // ConcurrentDictionary's GetOrAdd does not lock around the value factory call, so we need
            // to do it ourselves.
            lock (assemblyFixtureMappings)
                if (!assemblyFixtureMappings.ContainsKey(fixtureType))
                    Aggregator.Run(() => assemblyFixtureMappings.Add(fixtureType, Activator.CreateInstance(fixtureType)));
        }

        // Don't want to use .Concat + .ToDictionary because of the possibility of overriding types,
        // so instead we'll just let collection fixtures override assembly fixtures.
        var combinedFixtures = new Dictionary<Type, object>(assemblyFixtureMappings);
        foreach (var kvp in CollectionFixtureMappings)
            combinedFixtures[kvp.Key] = kvp.Value;

        // We've done everything we need, so let the built-in types do the rest of the heavy lifting
        return new XunitTestClassRunner(testClass, @class, testCases, diagnosticMessageSink, MessageBus, TestCaseOrderer, new ExceptionAggregator(Aggregator), CancellationTokenSource, combinedFixtures).RunAsync();
    }
}
