using System.Diagnostics;
using System.Text;
using Microsoft.VisualStudio.Threading;
using Netch.Enums;
using Netch.Models;
using Netch.Utils;

namespace Netch.Controllers;

public abstract class Guard
{
    private static readonly TimeSpan StartTimeout = TimeSpan.FromSeconds(50);

    private readonly Encoding? _encoding;
    private FileStream? _logFileStream;
    private StreamWriter? _logStreamWriter;
    private TaskCompletionSource<bool>? _startResult;
    private bool _started;

    /// <param name="mainFile">application path relative of Netch\bin</param>
    /// <param name="redirectOutput"></param>
    /// <param name="encoding">application output encode</param>
    protected Guard(string mainFile, bool redirectOutput = true, Encoding? encoding = null)
    {
        RedirectOutput = redirectOutput;
        _encoding = encoding;

        var fileName = Path.IsPathRooted(mainFile)
            ? mainFile
            : Path.Combine(Global.NetchDir, "bin", mainFile);

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

    public Process Instance { get; private set; }

    protected void ChangeMainFile(string mainFile)
    {
        if (_started)
            throw new InvalidOperationException("Cannot change the guarded process after it has started.");

        var fileName = Path.IsPathRooted(mainFile)
            ? mainFile
            : Path.Combine(Global.NetchDir, "bin", mainFile);

        if (!File.Exists(fileName))
            throw new MessageException(i18N.Translate($"bin\\{mainFile} file not found!"));

        Instance.Dispose();
        Instance = new Process
        {
            StartInfo =
            {
                FileName = fileName,
                WorkingDirectory = $"{Global.NetchDir}\\bin",
                CreateNoWindow = true,
                UseShellExecute = !RedirectOutput,
                RedirectStandardOutput = RedirectOutput,
                StandardOutputEncoding = RedirectOutput ? _encoding : null,
                RedirectStandardError = RedirectOutput,
                StandardErrorEncoding = RedirectOutput ? _encoding : null,
                WindowStyle = ProcessWindowStyle.Hidden
            }
        };
    }

    protected async Task StartGuardAsync(string argument, ProcessPriorityClass priority = ProcessPriorityClass.Normal)
    {
        State = State.Starting;
        _startResult = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        _logFileStream = new FileStream(LogPath, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, true);
        _logStreamWriter = new StreamWriter(_logFileStream) { AutoFlush = true };

        Instance.StartInfo.Arguments = argument;
        Instance.Start();
        _started = true;
        Global.Job.AddProcess(Instance);

        if (priority != ProcessPriorityClass.Normal)
            Instance.PriorityClass = priority;
        else
            ProcessTuning.Boost(Instance, Name);

        if (RedirectOutput)
        {
            ReadOutputAsync(Instance.StandardOutput).Forget();
            ReadOutputAsync(Instance.StandardError).Forget();

            if (!StartedKeywords.Any())
            {
                // Skip, No started keyword
                State = State.Started;
                return;
            }

            // React as soon as ReadOutput sees a keyword (instead of polling every
            // 50 ms), and fail fast when the process exits before it is ready
            // instead of waiting for the full timeout.
            var startResult = _startResult.Task;
            var exited = Instance.WaitForExitAsync();
            var timeout = Task.Delay(StartTimeout);
            var finished = await Task.WhenAny(startResult, exited, timeout);

            if (finished == startResult && await startResult)
            {
                OnStarted();
                return;
            }

            await StopGuardAsync();
            if (finished == timeout)
                throw new MessageException($"{Name} 控制器启动超时");

            OnStartFailed();
            throw new MessageException($"{Name} 控制器启动失败");
        }
    }

    private async Task ReadOutputAsync(TextReader reader)
    {
        try
        {
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                var writer = _logStreamWriter;
                if (writer != null)
                    await writer.WriteLineAsync(line);

                OnReadNewLine(line);

                if (State == State.Starting)
                {
                    if (StartedKeywords.Any(s => line.Contains(s)))
                    {
                        State = State.Started;
                        _startResult?.TrySetResult(true);
                    }
                    else if (FailedKeywords.Any(s => line.Contains(s)))
                    {
                        State = State.Stopped;
                        _startResult?.TrySetResult(false);
                    }
                }
            }
        }
        catch (Exception e) when (e is ObjectDisposedException or IOException or InvalidOperationException)
        {
            // The guarded process was stopped while its output was being read.
        }
    }

    public virtual Task StopAsync()
    {
        return StopGuardAsync();
    }

    protected async Task StopGuardAsync()
    {       
        try
        {
            if (_started && !Instance.HasExited)
            {
                Instance.Kill();
                await Instance.WaitForExitAsync();
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Stop {Name} failed", Name);
        }
        finally
        {
            if (_logStreamWriter != null)
                await _logStreamWriter.DisposeAsync();

            if (_logFileStream != null)
                await _logFileStream.DisposeAsync();

            Instance.Dispose();
            _started = false;

            State = State.Stopped;
            _startResult?.TrySetResult(false);
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
}
