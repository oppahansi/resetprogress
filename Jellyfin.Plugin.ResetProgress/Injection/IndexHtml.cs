using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Jellyfin.Plugin.ResetProgress.Injection;

/// <summary>
/// Adds and removes the client script tag in jellyfin-web's index.html.
/// </summary>
public static class IndexHtml
{
    private const string FileName = "index.html";
    private const int MaxAttempts = 20;

    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(250);
    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);

    private static readonly Regex ScriptTag = new(
        @"<script plugin=""ResetProgress""[^>]*></script>\s*",
        RegexOptions.Compiled);

    /// <summary>
    /// Tells whether the contents are an HTML document, rather than a script whose file name looks like index.html.
    /// </summary>
    /// <param name="contents">The file contents.</param>
    /// <returns>Whether tags can be added to the contents.</returns>
    public static bool IsHtmlDocument(string contents)
    {
        var start = contents.AsSpan().TrimStart().TrimStart('\uFEFF');
        return start.StartsWith("<!doctype html", StringComparison.OrdinalIgnoreCase)
            || start.StartsWith("<html", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns the page with exactly one, current script tag.
    /// </summary>
    /// <param name="html">The index.html contents.</param>
    /// <returns>The patched contents.</returns>
    public static string Inject(string html)
    {
        html = Remove(html);

        // Relative to /web/, so it also works when the server runs under a base URL.
        var version = Plugin.Instance?.Version.ToString() ?? "0";
        var tag = $"<script plugin=\"ResetProgress\" defer src=\"../ResetProgress/ClientScript?v={version}\"></script>";

        var bodyEnd = html.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
        return bodyEnd < 0 ? html + tag : html.Insert(bodyEnd, tag);
    }

    /// <summary>
    /// Returns the page without the script tag.
    /// </summary>
    /// <param name="html">The index.html contents.</param>
    /// <returns>The cleaned contents.</returns>
    public static string Remove(string html) => ScriptTag.Replace(html, string.Empty);

    /// <summary>
    /// Writes the script tag into index.html on disk.
    /// </summary>
    /// <param name="webPath">The jellyfin-web directory.</param>
    /// <param name="error">Why it failed, if it did.</param>
    /// <returns>Whether the file now contains the tag.</returns>
    public static bool TryInjectOnDisk(string webPath, out string? error) => TryRewrite(webPath, Inject, out error);

    /// <summary>
    /// Removes the script tag from index.html on disk.
    /// </summary>
    /// <param name="webPath">The jellyfin-web directory.</param>
    /// <param name="error">Why it failed, if it did.</param>
    /// <returns>Whether the file no longer contains the tag.</returns>
    public static bool TryRemoveFromDisk(string webPath, out string? error) => TryRewrite(webPath, Remove, out error);

    private static bool TryRewrite(string webPath, Func<string, string> rewrite, out string? error)
    {
        error = null;
        var path = Path.Combine(webPath, FileName);

        if (!File.Exists(path))
        {
            error = $"{path} doesn't exist";
            return false;
        }

        // Other plugins patch index.html on startup too, at the same moment. Read and write through one exclusively
        // locked handle, so nobody reads a half-written file; wait a little while someone else holds it.
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                string html;
                using (var reader = new StreamReader(stream, Utf8, detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: true))
                {
                    html = reader.ReadToEnd();
                }

                // Never build on a file another writer already broke; that would only make it permanent.
                if (html.IndexOf("<html", StringComparison.OrdinalIgnoreCase) < 0 || html.IndexOf("</body>", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    error = $"{path} looks damaged ({html.Length} characters, no <html>/<body>), left it alone";
                    return false;
                }

                var patched = rewrite(html);
                if (!string.Equals(html, patched, StringComparison.Ordinal))
                {
                    stream.SetLength(0);
                    using var writer = new StreamWriter(stream, Utf8, bufferSize: 4096, leaveOpen: true);
                    writer.Write(patched);
                }

                return true;
            }
            catch (IOException) when (attempt < MaxAttempts)
            {
                Thread.Sleep(RetryDelay);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}
