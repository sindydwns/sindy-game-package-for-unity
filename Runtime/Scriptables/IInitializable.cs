using System;

public interface IInitializable : IDisposable
{
    /// <summary>
    /// Initializes the object.
    /// </summary>
    /// <returns>The initialized object.</returns>
    void Init();
}
