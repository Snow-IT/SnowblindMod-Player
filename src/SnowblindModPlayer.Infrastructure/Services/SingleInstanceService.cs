using System.IO.Pipes;
using System.Text;
using System.Threading;
using SnowblindModPlayer.Core.Services;

namespace SnowblindModPlayer.Infrastructure.Services;

public sealed class SingleInstanceService : ISingleInstanceService
{
    private const string MutexName = "SnowblindModPlayer.SingleInstance";
    private const string PipeName = "SnowblindModPlayer.Pipe";

    private Mutex? _mutex;
    private bool _hasHandle;
    private CancellationTokenSource? _cts;
    private Task? _listenTask;

    public bool TryAcquirePrimary()
    {
        _mutex = new Mutex(true, MutexName, out var createdNew);
        _hasHandle = createdNew;
        return createdNew;
    }

    public void StartListening(Action onShowRequested)
    {
        if (!_hasHandle)
            return;

        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        _listenTask = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    using var server = new NamedPipeServerStream(PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                    await server.WaitForConnectionAsync(token).ConfigureAwait(false);
                    using var reader = new StreamReader(server, Encoding.UTF8);
                    var command = await reader.ReadLineAsync().ConfigureAwait(false);
                    if (command != null && command.StartsWith("SHOW", StringComparison.OrdinalIgnoreCase))
                    {
                        onShowRequested?.Invoke();
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    // ignore and continue listening
                }
            }
        }, token);
    }

    public void NotifyPrimaryInstance(string command = "SHOW")
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(300);
            using var writer = new StreamWriter(client, Encoding.UTF8) { AutoFlush = true };
            writer.WriteLine(command);
        }
        catch
        {
            // ignore if primary not reachable
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        try { _listenTask?.Wait(200); } catch { }
        _cts?.Dispose();

        if (_hasHandle && _mutex != null)
        {
            try { _mutex.ReleaseMutex(); } catch { }
        }
        _mutex?.Dispose();
    }
}
