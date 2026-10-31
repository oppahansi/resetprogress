using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;

namespace Jellyfin.Plugin.ResetProgress.Injection;

/// <summary>
/// Optional integration with the File Transformation plugin, which patches jellyfin-web responses in memory
/// instead of changing files on disk. See https://github.com/IAmParadox27/jellyfin-plugin-file-transformation.
/// </summary>
public static class FileTransformation
{
    // Must be unique per plugin: File Transformation ignores a second registration with an id it already has.
    private static readonly Guid TransformationId = Guid.Parse("9cb8dc42-ec06-4d78-9202-a71a08645b91");

    /// <summary>
    /// Registers the index.html transformation if File Transformation is installed.
    /// </summary>
    /// <returns>Whether the transformation was registered.</returns>
    public static bool TryRegister()
    {
        // It lives in its own load context, so it can only be reached by reflection.
        var assembly = AssemblyLoadContext.All
            .SelectMany(context => context.Assemblies)
            .FirstOrDefault(a => a.FullName?.Contains(".FileTransformation", StringComparison.Ordinal) ?? false);
        var register = assembly?.GetType("Jellyfin.Plugin.FileTransformation.PluginInterface")?.GetMethod("RegisterTransformation");
        var payloadType = register?.GetParameters().FirstOrDefault()?.ParameterType;
        if (register is null || payloadType is null)
        {
            return false;
        }

        var json = JsonSerializer.Serialize(new
        {
            id = TransformationId,
            fileNamePattern = "index.html",
            callbackAssembly = typeof(FileTransformation).Assembly.FullName,
            callbackClass = typeof(FileTransformation).FullName,
            callbackMethod = nameof(TransformIndexHtml)
        });

        // The method takes a Newtonsoft JObject. Build it from that plugin's own copy of Newtonsoft.Json.
        var payload = payloadType.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, [typeof(string)])?.Invoke(null, [json]);
        if (payload is null)
        {
            return false;
        }

        register.Invoke(null, [payload]);
        return true;
    }

    /// <summary>
    /// Callback invoked by File Transformation whenever index.html is served.
    /// </summary>
    /// <param name="payload">The file being served.</param>
    /// <returns>The patched file.</returns>
    public static string TransformIndexHtml(TransformationPayload payload)
    {
        return IndexHtml.Inject(payload.Contents ?? string.Empty);
    }

    /// <summary>
    /// The object File Transformation passes to <see cref="TransformIndexHtml"/>.
    /// </summary>
    public class TransformationPayload
    {
        /// <summary>
        /// Gets or sets the file contents.
        /// </summary>
        public string? Contents { get; set; }
    }
}
