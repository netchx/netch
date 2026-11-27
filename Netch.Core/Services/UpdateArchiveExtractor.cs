using System.IO.Compression;
using SharpCompress.Archives;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Common;

namespace Netch.Services;

public static class UpdateArchiveExtractor
{
    private static readonly byte[] SevenZipMagic = [0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C];

    public static bool IsSupportedArchiveName(string? fileName) =>
        !string.IsNullOrWhiteSpace(fileName) &&
        (fileName.EndsWith(".7z", StringComparison.OrdinalIgnoreCase) ||
         fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));

    public static bool IsSevenZipSignature(ReadOnlySpan<byte> header) =>
        header.Length >= SevenZipMagic.Length && header[..SevenZipMagic.Length].SequenceEqual(SevenZipMagic);

    public static bool IsZipSignature(ReadOnlySpan<byte> header) =>
        header.Length >= 2 && header[0] == 0x50 && header[1] == 0x4B;

    public static void ExtractToDirectory(string archivePath, string destinationDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(archivePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationDirectory);

        Span<byte> header = stackalloc byte[6];
        int read;
        using (var stream = File.OpenRead(archivePath))
            read = stream.ReadAtLeast(header, header.Length, throwOnEndOfStream: false);

        var signature = header[..read];
        Directory.CreateDirectory(destinationDirectory);

        if (IsSevenZipSignature(signature))
        {
            using var archive = SevenZipArchive.Open(archivePath);
            archive.WriteToDirectory(destinationDirectory, new ExtractionOptions
            {
                ExtractFullPath = true,
                Overwrite = true
            });
            return;
        }

        if (IsZipSignature(signature))
        {
            ZipFile.ExtractToDirectory(archivePath, destinationDirectory, overwriteFiles: true);
            return;
        }

        throw new InvalidDataException($"Unsupported update archive format: {Path.GetFileName(archivePath)}");
    }
}
