using Godot;

/// <summary>
/// Interface for components that require TickManager dependency injection.
/// Provides compile-time type safety instead of reflection-based Call().
/// </summary>
public interface ITickSystemUser
{
    /// <summary>
    /// Inject TickManager dependency.
    /// Must be called before the component starts processing ticks.
    /// </summary>
    void Initialize(TickManager tickManager);
}
