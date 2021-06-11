using System;
using System.IO;
using System.Reactive.Linq;

namespace Netch.Models
{
    public class ObservableFileSystemWatcher : IDisposable
    {
        public readonly FileSystemWatcher Watcher;

        public IObservable<FileSystemEventArgs> Created { get; }

        public IObservable<FileSystemEventArgs> Deleted { get; }

        public IObservable<RenamedEventArgs> Renamed { get; }

        public IObservable<FileSystemEventArgs> Changed { get; }

        public IObservable<ErrorEventArgs> Errors { get; }

        public ObservableFileSystemWatcher(FileSystemWatcher watcher)
        {
            Watcher = watcher;

            Changed = Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(h => Watcher.Changed += h, h => Watcher.Changed -= h)
                .Select(x => x.EventArgs);

            Renamed = Observable.FromEventPattern<RenamedEventHandler, RenamedEventArgs>(h => Watcher.Renamed += h, h => Watcher.Renamed -= h)
                .Select(x => x.EventArgs);

            Deleted = Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(h => Watcher.Deleted += h, h => Watcher.Deleted -= h)
                .Select(x => x.EventArgs);

            Errors = Observable.FromEventPattern<ErrorEventHandler, ErrorEventArgs>(h => Watcher.Error += h, h => Watcher.Error -= h)
                .Select(x => x.EventArgs);

            Created = Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(h => Watcher.Created += h, h => Watcher.Created -= h)
                .Select(x => x.EventArgs);
        }

        public void Dispose()
        {
            Watcher.Dispose();
        }
    }
}