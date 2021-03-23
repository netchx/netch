using System;
using System.Diagnostics;

namespace Netch.Models
{
    public class LogStopwatch
    {
        public LogStopwatch(string message)
        {
            _stopwatch = Stopwatch.StartNew();
            Console.WriteLine($"Start {message} LogStopwatch");
        }

        private readonly Stopwatch _stopwatch;

        public void Log(string name,bool stop = false)
        {
            if (!_stopwatch.IsRunning)
                throw new Exception();

            _stopwatch.Stop();
            Console.WriteLine($"{name} LogStopwatch: {_stopwatch.ElapsedMilliseconds}");
            if(!stop)
                _stopwatch.Start();
        }
    }
}