// Using directives stay at file scope, above the namespace declaration: inside a namespace beginning
// with "Enigma.", the simple name "Avalonia" resolves against the referenced Enigma.Avalonia.*
// assemblies instead of Avalonia itself.
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Threading;

namespace Enigma.Msi.Desktop.UnitTests;

/// <summary>
/// Stands up the app's real <see cref="App"/> on a headless platform once per test run, and runs test
/// bodies on its UI thread.
/// </summary>
/// <remarks>
/// <para>
/// Avalonia can be initialised once per process, and every control must be built and measured on the
/// thread that initialised it. The fixture therefore owns a dedicated thread: it sets the platform up
/// there, then pumps a queue of test work and the dispatcher's own jobs. It replaces
/// <c>Avalonia.Headless.XUnit</c>, whose 12.1.x build is compiled against xunit.v3 3.x and fails test
/// discovery under 4.x — the same fixture Enigma.GitClient runs its headless tests on.
/// </para>
/// <para>
/// The real application object rather than a stand-in, because what these tests assert is what the
/// markup resolves — every colour is a <c>DynamicResource</c> <c>Enigma*</c> key, and those keys come
/// from the <c>FluentTheme</c> and the <c>Enigma.Avalonia.Desktop</c> dictionary that <c>App.axaml</c>
/// merges. Nothing of the app's composition runs: <c>SetupWithoutStarting</c> gives the application no
/// lifetime, so <c>OnFrameworkInitializationCompleted</c> finds no desktop lifetime and resolves no
/// windows. Headless drawing stays on: the tests read the control tree, and none of them needs pixels.
/// </para>
/// </remarks>
public sealed class HeadlessAvaloniaFixture : IDisposable
{
    private readonly BlockingCollection<Action> _work = [];
    private readonly Thread _uiThread;
    private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Starts the UI thread and waits for the headless platform to come up.</summary>
    public HeadlessAvaloniaFixture()
    {
        _uiThread = new Thread(RunUiThread)
        {
            IsBackground = true,
            Name = "avalonia-headless-tests",
        };

        _uiThread.Start();
        _ready.Task.GetAwaiter().GetResult();
    }

    /// <summary>Runs an action on the Avalonia UI thread and waits for it.</summary>
    /// <param name="action">The work to run.</param>
    public void Run(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        _work.Add(() =>
        {
            try
            {
                action();
                completion.SetResult();
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }
        });

        completion.Task.GetAwaiter().GetResult();
    }

    /// <summary>Runs a function on the Avalonia UI thread and returns its result.</summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="function">The work to run.</param>
    /// <returns>The function's result.</returns>
    public TResult Run<TResult>(Func<TResult> function)
    {
        ArgumentNullException.ThrowIfNull(function);

        TResult result = default!;

        // A block body on purpose: an expression-bodied assignment is also a Func<TResult>, and overload
        // resolution prefers that — this very method — so it would recurse until the stack overflows.
        Run(() => { result = function(); });
        return result;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _work.CompleteAdding();

        // A stuck UI thread must not hang the whole run: it is a background thread, and the process
        // reclaims it.
        _ = _uiThread.Join(TimeSpan.FromSeconds(5));

        _work.Dispose();
    }

    private void RunUiThread()
    {
        try
        {
            _ = AppBuilder.Configure<App>()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions())
                .WithInterFont()
                .SetupWithoutStarting();

            _ready.SetResult();
        }
        catch (Exception exception)
        {
            _ready.SetException(exception);
            return;
        }

        foreach (Action action in _work.GetConsumingEnumerable())
        {
            action();

            // Let what the work posted (bindings, layout passes) drain before the next test looks at
            // the tree.
            Dispatcher.UIThread.RunJobs();
        }
    }
}
