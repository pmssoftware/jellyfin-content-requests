using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.ContentRequests.Compatibility;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ContentRequests.Services;

/// <summary>
/// Registers a small compatibility transform with File Transformation without
/// taking a compile-time dependency on that plugin or Newtonsoft.Json.
/// </summary>
public sealed class StartupService : IScheduledTask
{
    private static readonly Guid TransformationId = Guid.Parse("bc848a77-09aa-4d71-90c0-3b87ca6299ad");
    private readonly ILogger<StartupService> _logger;

    public StartupService(ILogger<StartupService> logger)
    {
        _logger = logger;
    }

    public string Name => "Content Requests Startup";

    public string Key => "Jellyfin.Plugin.ContentRequests.Startup";

    public string Description => "Registers the resilient Content Requests homepage-tab bridge.";

    public string Category => "Startup Services";

    public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        try
        {
            var fileTransformationAssembly = AssemblyLoadContext.All
                .SelectMany(context => context.Assemblies)
                .FirstOrDefault(assembly => assembly.FullName?.Contains(".FileTransformation", StringComparison.Ordinal) == true);
            var pluginInterface = fileTransformationAssembly?.GetType("Jellyfin.Plugin.FileTransformation.PluginInterface");
            var registerMethod = pluginInterface?.GetMethod("RegisterTransformation", BindingFlags.Public | BindingFlags.Static);
            var payloadType = registerMethod?.GetParameters().SingleOrDefault()?.ParameterType;
            var parseMethod = payloadType?.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, new[] { typeof(string) });

            if (registerMethod is null || parseMethod is null)
            {
                _logger.LogWarning("File Transformation is unavailable; the CustomTabs compatibility bridge was not registered.");
                return Task.CompletedTask;
            }

            var payloadJson = JsonSerializer.Serialize(new
            {
                id = TransformationId,
                fileNamePattern = "index.html",
                callbackAssembly = GetType().Assembly.FullName,
                callbackClass = typeof(WebTransformations).FullName,
                callbackMethod = nameof(WebTransformations.IndexHtml)
            });
            var payload = parseMethod.Invoke(null, new object[] { payloadJson });
            registerMethod.Invoke(null, new[] { payload });
            _logger.LogInformation("Registered the Content Requests homepage-tab compatibility bridge.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not register the Content Requests homepage-tab compatibility bridge.");
        }

        return Task.CompletedTask;
    }

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        yield return new TaskTriggerInfo
        {
            Type = TaskTriggerInfoType.StartupTrigger
        };
    }
}
