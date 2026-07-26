using System;
using System.IO;
using System.Text;
using System.Threading;

namespace EntityLengths.Generator.Emit;

/// <summary>
/// Writes the generated constants to a file outside the current project.
/// </summary>
/// <remarks>
/// A generator normally must not touch the file system: it runs on every keystroke in the IDE and once
/// more per target framework during the build. Three mitigations keep that bearable - the file is only
/// touched when its content actually changes, it is written through a temporary file so a concurrent
/// reader never sees a partial file, and a write that loses a race against another writer is retried
/// and then accepted if that writer produced the same content. Any remaining failure is reported as a
/// diagnostic instead of throwing, because an exception escaping a generator kills the compilation.
/// </remarks>
// RS1035: analyzers must not do file IO. Writing the constants outside the current project is the
// whole point of the EntityLengthsOutputPath opt-in, and it is only reached when the user sets that
// property, so the ban is suppressed for this file only.
#pragma warning disable RS1035
internal static class GeneratedFileWriter
{
    private const int MaxAttempts = 3;
    private const int RetryDelayMilliseconds = 20;

    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    public static bool TryWrite(string path, string content, out string? error)
    {
        error = null;

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                EnsureDirectory(path);

                if (IsUpToDate(path, content))
                {
                    return true;
                }

                Write(path, content);
                return true;
            }
            catch (Exception exception)
                when (exception is IOException
                    or UnauthorizedAccessException
                    or NotSupportedException
                    or ArgumentException
                )
            {
                // The inner builds of a multi-targeted project write the same path at the same time,
                // so losing the race is expected. Accept it once the file holds the wanted content.
                if (IsUpToDate(path, content))
                {
                    return true;
                }

                if (attempt >= MaxAttempts)
                {
                    error = exception.Message;
                    return false;
                }

                Thread.Sleep(RetryDelayMilliseconds);
            }
        }
    }

    private static void EnsureDirectory(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static void Write(string path, string content)
    {
        // Unique per write: a multi-targeted project runs one inner build per target framework in
        // parallel, and they all write the same output path.
        var temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";

        try
        {
            File.WriteAllText(temporaryPath, content, Utf8NoBom);
            File.Copy(temporaryPath, path, overwrite: true);
        }
        finally
        {
            TryDelete(temporaryPath);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // A leftover temporary file is not worth failing or warning about.
        }
    }

    private static bool IsUpToDate(string path, string content)
    {
        try
        {
            return File.Exists(path)
                && string.Equals(File.ReadAllText(path), content, StringComparison.Ordinal);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // The file is locked by another writer; treat it as stale and let the caller retry.
            return false;
        }
    }
}
