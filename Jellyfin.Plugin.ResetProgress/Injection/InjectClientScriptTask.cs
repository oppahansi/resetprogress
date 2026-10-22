using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ResetProgress.Injection;

/// <summary>
/// Adds the client script to jellyfin-web on every server start. A hidden startup task rather than a
/// hosted service, because File Transformation only accepts registrations once its own startup is done.
/// </summary>
public class InjectClientScriptTask : IScheduledTask, IConfigurableScheduledTask
{
    private readonly IApplicationPaths _applicationPaths;
    private readonly ILogger<InjectClientScriptTask> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InjectClientScriptTask"/> class.
    /// </summary>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{T}"/> interface.</param>
    public InjectClientScriptTask(IApplicationPaths applicationPaths, ILogger<InjectClientScriptTask> logger)
    {
        _applicationPaths = applicationPaths;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "Reset Progress: inject client script";

    /// <inheritdoc />
    public string Key => "ResetProgressInjectClientScript";

    /// <inheritdoc />
    public string Description => "Adds the reset button script to the web client.";

    /// <inheritdoc />
    public string Category => "Reset Progress";

    /// <inheritdoc />
    public bool IsHidden => true;

    /// <inheritdoc />
    public bool IsEnabled => true;

    /// <inheritdoc />
    public bool IsLogged => false;

    /// <inheritdoc />
    public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var webPath = _applicationPaths.WebPath;

        try
        {
            if (FileTransformation.TryRegister())
            {
                // Don't leave an older on-disk tag behind, or the script would load twice.
                IndexHtml.TryRemoveFromDisk(webPath, out _);
                _logger.LogInformation("Reset Progress: client script registered with File Transformation");
                return Task.CompletedTask;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Reset Progress: File Transformation registration failed, falling back to index.html");
        }

        if (IndexHtml.TryInjectOnDisk(webPath, out var error))
        {
            _logger.LogInformation("Reset Progress: client script added to {Path}", webPath);
        }
        else
        {
            _logger.LogWarning(
                "Reset Progress: couldn't add the client script to index.html ({Error}). Install the File Transformation plugin or make the web directory writable",
                error);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return
        [
            new TaskTriggerInfo
            {
#if NET9_0_OR_GREATER
                Type = TaskTriggerInfoType.StartupTrigger
#else
                Type = TaskTriggerInfo.TriggerStartup
#endif
            }
        ];
    }
}
