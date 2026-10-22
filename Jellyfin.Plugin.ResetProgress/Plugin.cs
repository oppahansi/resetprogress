using System;
using Jellyfin.Plugin.ResetProgress.Configuration;
using Jellyfin.Plugin.ResetProgress.Injection;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.ResetProgress;

/// <summary>
/// The Reset Progress plugin.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>
{
    private readonly IApplicationPaths _applicationPaths;

    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        _applicationPaths = applicationPaths;
        Instance = this;
    }

    /// <inheritdoc />
    public override string Name => "Reset Progress";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("498003e1-4a0c-47a8-b67e-958e8d4019fa");

    /// <inheritdoc />
    public override string Description => "Adds a button to Continue Watching that resets an item's watch progress to 0:00.";

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    /// <inheritdoc />
    public override void OnUninstalling()
    {
        // Undo the index.html fallback so jellyfin-web doesn't keep requesting a script that no longer exists.
        IndexHtml.TryRemoveFromDisk(_applicationPaths.WebPath, out _);
        base.OnUninstalling();
    }
}
