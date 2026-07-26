using System;
using System.IO;
using System.Text;

namespace EntityLengths.Generator.Emit;

/// <summary>
/// Writes the generated constants to a file outside the current project.
/// </summary>
/// <remarks>
/// A generator normally must not touch the file system: it runs on every keystroke in the IDE and
/// once more during the build. Two mitigations keep that bearable - the file is only touched when its
/// content actually changes, and it is written through a temporary file so a concurrent reader never
/// sees a partial file. Any failure is reported as a diagnostic instead of throwing, because an
/// exception escaping a generator kills the whole compilation.
/// </remarks>
// RS1035: analyzers must not do file IO. Writing the constants outside the current project is the
// whole point of the EntityLengthsOutputPath opt-in, and it is only reached when the user sets that
// property, so the ban is suppressed for this file only.
#pragma warning disable RS1035
internal static class GeneratedFileWriter
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    public static bool TryWrite(string path, string content, out string? error)
    {
        error = null;

        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (IsUpToDate(path, content))
            {
                return true;
            }

            // Unique per write: a multi-targeted project runs one inner build per target framework in
            // parallel, and they all write the same output path.
            var temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";
            File.WriteAllText(temporaryPath, content, Utf8NoBom);
            File.Copy(temporaryPath, path, overwrite: true);
            File.Delete(temporaryPath);

            return true;
        }
        catch (Exception exception)
            when (exception is IOException
                or UnauthorizedAccessException
                or NotSupportedException
                or ArgumentException
            )
        {
            error = exception.Message;
            return false;
        }
    }

    private static bool IsUpToDate(string path, string content)
    {
        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            return string.Equals(File.ReadAllText(path), content, StringComparison.Ordinal);
        }
        catch (IOException)
        {
            // The file is locked by another writer; treat it as stale and let the write attempt report.
            return false;
        }
    }
}
