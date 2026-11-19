using System.Diagnostics;
using System.IO.Compression;
using System.Text;

namespace Netch.App.Services;

internal static class AppUpdate
{
    public static async Task RestartAsync(string archive)
    {
        var staging = Directory.CreateTempSubdirectory("netch-update-").FullName;
        try
        {
            ZipFile.ExtractToDirectory(archive, staging);
            var source = File.Exists(Path.Combine(staging, "Netch.App.exe")) ? staging : Path.Combine(staging, "Netch");
            foreach (var relative in new[] { "Netch.App.exe", "Microsoft.UI.Xaml.dll", "coreclr.dll", @"bin\nfdriver.sys", @"bin\Redirector.bin" })
                if (!File.Exists(Path.Combine(source, relative)))
                    throw new InvalidDataException($"The update archive is missing {relative}.");

            var script = Path.Combine(staging, "install.ps1");
            await File.WriteAllTextAsync(script, Installer, new UTF8Encoding(true));
            var info = new ProcessStartInfo("powershell.exe") { UseShellExecute = false, CreateNoWindow = true };
            foreach (var arg in new[] { "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", script, "-Previous", Environment.ProcessId.ToString(),
                         "-Source", source, "-Staging", staging, "-Destination", Global.NetchDir })
                info.ArgumentList.Add(arg);
            using var worker = Process.Start(info) ?? throw new InvalidOperationException("The updater could not start.");
        }
        catch
        {
            Directory.Delete(staging, recursive: true);
            throw;
        }
        finally
        {
            try { File.Delete(archive); }
            catch (IOException e) { Log.Warning(e, "Could not remove downloaded update archive"); }
            catch (UnauthorizedAccessException e) { Log.Warning(e, "Could not remove downloaded update archive"); }
        }
        await App.ExitAsync();
    }

    private const string Installer = """
        param([int]$Previous, [string]$Source, [string]$Staging, [string]$Destination)
        $ErrorActionPreference = 'Stop'
        $exe = Join-Path $Destination 'Netch.App.exe'
        $previousProcess = Get-Process -Id $Previous -ErrorAction SilentlyContinue
        if ($previousProcess -and -not $previousProcess.WaitForExit(30000)) {
            throw 'Netch did not shut down within 30 seconds.'
        }
        $backup = Join-Path $Destination ('.netch-update-' + [Guid]::NewGuid().ToString('N'))
        $changed = [System.Collections.Generic.List[string]]::new()
        $staged = $null
        $restored = $true
        try {
            foreach ($file in Get-ChildItem -LiteralPath $Source -File -Recurse) {
                if ($file.Name -eq 'install.ps1') { continue }
                $relative = $file.FullName.Substring($Source.Length + 1)
                if ($relative -match '^(data|logging|mode)\\') { continue }
                $target = Join-Path $Destination $relative
                $original = Join-Path $backup $relative
                New-Item -ItemType Directory -Force -Path (Split-Path $target) | Out-Null
                $staged = $target + '.' + [Guid]::NewGuid().ToString('N') + '.tmp'
                [IO.File]::Copy($file.FullName, $staged)
                if ([IO.File]::Exists($target)) {
                    New-Item -ItemType Directory -Force -Path (Split-Path $original) | Out-Null
                    [IO.File]::Replace($staged, $target, $original)
                } else {
                    [IO.File]::Move($staged, $target)
                }
                $changed.Add($relative)
                $staged = $null
            }
        }
        catch {
            $errorLog = Join-Path $Destination 'data\update-error.log'
            Add-Content -LiteralPath $errorLog -Value $_ -ErrorAction Continue
            foreach ($relative in $changed) {
                $target = Join-Path $Destination $relative
                $original = Join-Path $backup $relative
                try {
                    if ([IO.File]::Exists($original)) { [IO.File]::Replace($original, $target, [NullString]::Value) }
                    else { [IO.File]::Delete($target) }
                } catch {
                    $restored = $false
                    Add-Content -LiteralPath $errorLog -Value $_ -ErrorAction Continue
                }
            }
        }
        if ($staged) { Remove-Item -LiteralPath $staged -Force -ErrorAction SilentlyContinue }
        if (-not $restored) { exit 1 }
        foreach ($candidate in @($backup, $Staging)) {
            $path = [IO.Path]::GetFullPath($candidate)
            if ($path.StartsWith([IO.Path]::GetFullPath($Destination).TrimEnd('\') + '\.netch-update-', [StringComparison]::OrdinalIgnoreCase) -or
                $path.StartsWith([IO.Path]::GetTempPath().TrimEnd('\') + '\netch-update-', [StringComparison]::OrdinalIgnoreCase)) {
                Remove-Item -LiteralPath $path -Recurse -Force -ErrorAction SilentlyContinue
            }
        }
        Start-Process -FilePath $exe -WorkingDirectory $Destination
        """;
}
