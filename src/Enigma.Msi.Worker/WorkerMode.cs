namespace Enigma.Msi.Worker;

/// <summary>
/// Which of the worker's two entry points a command line selected.
/// </summary>
public enum WorkerMode
{
    /// <summary>
    /// The library's build client drives the worker: <c>--request &lt;request.json&gt; --result
    /// &lt;result.json&gt;</c>. The request is a serialized <c>MsiPackage</c>, the result a serialized
    /// <c>MsiBuildResult</c>.
    /// </summary>
    Internal,

    /// <summary>
    /// Headless/CI use with no library in the picture: <c>build &lt;file.msipkg.json&gt;</c>. The
    /// outcome is printed rather than written to a result file.
    /// </summary>
    Cli
}
