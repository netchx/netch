using System.Diagnostics;
using System.Text;
using Netch.Enums;
using Netch.Models;
using Netch.Utils;

namespace Netch.Controllers;

public abstract class Guard
{
    private FileStream? _logFileStream;
    private StreamWriter? _logStreamWriter;
    private readonly SemaphoreSlim _logWriteLock = new(1, 1);
    private Task[] _outputTasks = Array.Empty<Task>();
    private bool _guardStopped;
    private int _startFailureReported;

    /// <param name="mainFile">application path relative of Netch\bin</param>
    /// <param name="redirectOutput"></param>
    /// <param name="encoding">application output encode</param>
    protected Guard(string mainFile, bool redirectOutput = true, Encoding? encoding = null)
    {
        RedirectOutput = redirectOutput;

        var fileName = Path.GetFullPath($"bin\\{mainFile}");

        if (!File.Exists(fileName))
            throw new MessageException(i18N.Translate($"bin\\{mainFile} file not found!"));

        Instance = new Process
        {
            StartInfo =
            {
                FileName = fileName,
                WorkingDirectory = $"{Global.NetchDir}\\bin",
                CreateNoWindow = true,
                UseShellExecute = !RedirectOutput,
                RedirectStandardOutput = RedirectOutput,
                StandardOutputEncoding = RedirectOutput ? encoding : null,
                RedirectStandardError = RedirectOutput,
                StandardErrorEncoding = RedirectOutput ? encoding : null,
                WindowStyle = ProcessWindowStyle.Hidden
            }
        };
    }

    protected string LogPath => Path.Combine(Global.NetchDir, $"logging\\{Name}.log");

    protected virtual IEnumerable<string> StartedKeywords { get; } = new List<string>();

    protected virtual IEnumerable<string> FailedKeywords { get; } = new List<string>();

    public abstract string Name { get; }

    private State State { get; set; } = State.Waiting;

    private bool RedirectOutput { get; }

    public Process Instance { get; }

    protected async Task StartGuardAsync(string argument, ProcessPriorityClass priority = ProcessPriorityClass.Normal)
    {
        State = State.Starting;
        Interlocked.Exchange(ref _startFailureReported, 0);

        _logFileStream = new FileStream(LogPath, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, true);
        _logStreamWriter = new StreamWriter(_logFileStream) { AutoFlush = true };

        Instance.StartInfo.Arguments = argument;
        Instance.Start();
        Global.Job.AddProcess(Instance);

        if (priority != ProcessPriorityClass.Normal)
            Instance.PriorityClass = priority;

        if (RedirectOutput)
        {
            // Both streams share one log writer; track their lifetime for shutdown.
            _outputTasks = new[]
            {
                ReadOutputAsync(Instance.StandardOutput),
                ReadOutputAsync(Instance.StandardError)
            };

            if (!StartedKeywords.Any())
            {
                // Skip, No started keyword
                if (Instance.HasExited)
                {
                    State = State.Stopped;
                    await StopGuardAsync();
                    ReportStartFailedOnce();
                    throw new MessageException($"{Name} 控制器启动失败");
                }

                State = State.Started;
                return;
            }

            // wait ReadOutput change State
            for (var i = 0; i < 1000; i++)
            {
                await Task.Delay(50);
                if (Instance.HasExited)
                {
                    State = State.Stopped;
                    await StopGuardAsync();
                    ReportStartFailedOnce();
                    throw new MessageException($"{Name} 控制器启动失败");
                }

                switch (State)
                {
                    case State.Started:
                        OnStarted();
                        return;
                    case State.Stopped:
                        await StopGuardAsync();
                        ReportStartFailedOnce();
                        throw new MessageException($"{Name} 控制器启动失败");
                }
            }

            await StopGuardAsync();
            throw new MessageException($"{Name} 控制器启动超时");
        }
    }

    private async Task ReadOutputAsync(TextReader reader)
    {
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            await _logWriteLock.WaitAsync();
            try
            {
                await _logStreamWriter!.WriteLineAsync(line);
            }
            finally
            {
                _logWriteLock.Release();
            }
            OnReadNewLine(line);

            if (State == State.Starting)
            {
                if (StartedKeywords.Any(s => line.Contains(s)))
                    State = State.Started;
                else if (FailedKeywords.Any(s => line.Contains(s)))
                {
                    State = State.Stopped;
                    ReportStartFailedOnce();
                }
            }
        }
    }

    public virtual Task StopAsync()
    {
        return StopGuardAsync();
    }

    protected async Task StopGuardAsync()
    {
        if (_guardStopped)
            return;

        // Process.HasExited throws when StartGuardAsync failed before
        // Process.Start(), e.g. while generating a proxy configuration.
        var processRunning = false;
        try
        {
            processRunning = !Instance.HasExited;
        }
        catch (InvalidOperationException)
        {
            // The process was never started; still dispose its log streams.
        }

        if (processRunning)
        {
            Instance.Kill(entireProcessTree: true);
            await Instance.WaitForExitAsync();
        }

        // Finish both redirected-output readers before disposing their writer.
        try
        {
            await Task.WhenAll(_outputTasks);
        }
        catch (Exception e)
        {
            Log.Warning(e, "Reading remaining output from {Name} failed", Name);
        }
        finally
        {
            if (_logStreamWriter != null)
                await _logStreamWriter.DisposeAsync();

            if (_logFileStream != null)
                await _logFileStream.DisposeAsync();

            Instance.Dispose();

            State = State.Stopped;
            _guardStopped = true;
        }
    }

    protected virtual void OnStarted()
    {
    }

    protected virtual void OnReadNewLine(string line)
    {
    }

    protected virtual void OnStartFailed()
    {
        Utils.Utils.Open(LogPath);
    }

    private void ReportStartFailedOnce()
    {
        if (Interlocked.Exchange(ref _startFailureReported, 1) == 0)
            OnStartFailed();
    }
}
