namespace Hexalith.FrontComposer.Testing;

/// <summary>A measured CSS-pixel rectangle, including the caller's complete painted focus bounds when appropriate.</summary>
/// <param name="X">Left coordinate.</param>
/// <param name="Y">Top coordinate.</param>
/// <param name="Width">Measured width.</param>
/// <param name="Height">Measured height.</param>
public sealed record UxRect(double X, double Y, double Width, double Height);
