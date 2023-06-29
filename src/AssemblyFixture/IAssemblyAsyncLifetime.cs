using System.Threading.Tasks;

namespace Xunit;

/// <summary>
/// Used to provide asynchronous lifetime functionality per assembly.
/// It is like xunit IAsyncLifetime, but per assembly
/// </summary>
public interface IAssemblyAsyncLifetime
{
    /// <summary>
    /// Called before tests started in the assembly.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Called when all tests finished in the assembly.
    /// </summary>
    Task DisposeAsync();
}
