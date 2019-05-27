﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Plato.Internal.Abstractions;
using Plato.Internal.Abstractions.Extensions;
using Plato.Internal.Abstractions.Settings;
using Plato.Internal.Data.Migrations.Abstractions;
using Plato.Internal.Features;
using Plato.Internal.Features.Abstractions;
using Plato.Internal.Hosting.Abstractions;
using Plato.Internal.Models.Features;
using Plato.Internal.Models.Shell;
using Plato.Internal.Security.Abstractions;
using Plato.Internal.Shell.Abstractions;
using Plato.Internal.Stores.Abstractions.Shell;

namespace Plato.Features.Updates.Services
{

    public class FeatureUpdater : IFeatureUpdater
    {

        private readonly IOptions<PlatoOptions> _platoOptions;
        private readonly IFeatureFacade _featureFacade;
        private readonly IShellDescriptorManager _shellDescriptorManager;
        private readonly IDataMigrationBuilder _migrationBuilder;
        private readonly IShellFeatureStore<ShellFeature> _shellFeatureStore;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IShellContextFactory _shellContextFactory;
        private readonly IRunningShellTable _runningShellTable;
        private readonly IPlatoHost _platoHost;
        private readonly ILogger<FeatureUpdater> _logger;

        public FeatureUpdater(
            IOptions<PlatoOptions> platoOptions,
            IFeatureFacade featureFacade,
            IDataMigrationBuilder migrationBuilder,
            IShellDescriptorManager shellDescriptorManager,
            IShellFeatureStore<ShellFeature> shellFeatureStore,
            ILogger<FeatureUpdater> logger,
            IHttpContextAccessor httpContextAccessor,
            IShellContextFactory shellContextFactory, 
            IRunningShellTable runningShellTable,
            IPlatoHost platoHost)
        {
            _platoOptions = platoOptions;
            _featureFacade = featureFacade;
            _migrationBuilder = migrationBuilder;
            _shellDescriptorManager = shellDescriptorManager;
            _shellFeatureStore = shellFeatureStore;
            _httpContextAccessor = httpContextAccessor;
            _shellContextFactory = shellContextFactory;
            _runningShellTable = runningShellTable;
            _platoHost = platoHost;
            _logger = logger;
        }

        public async Task<ICommandResultBase> UpdateAsync(string moduleId)
        {

            // Get installed shell feature
            var feature = await _featureFacade.GetFeatureByIdAsync(moduleId);

            // Ensure we found the installed feature 
            if (feature == null)
            {
                return new CommandResultBase().Failed($"A feature with Id '{moduleId}' could not be found within the ShellFeatures table.");
            }

            // 1. Invoke FeatureEventHandlers
            var results = await InvokeFeatureEventHandlersAsync(feature, async context =>
            {

                // The result to return
                var result = new CommandResultBase();

                // Get available module
                var module = await _shellDescriptorManager.GetFeatureAsync(moduleId);

                // Ensure we found the module 
                if (module == null)
                {
                    return result.Failed($"A module with Id '{moduleId}' could not be found on the file system.");
                }

                // ------------------------------------------------------------------
                // 1. Check to ensure the module we are updating is compatible 
                // with the current version of Plato we are running
                // ------------------------------------------------------------------

                var modulePlatoVersion = module.Descriptor.PlatoVersion.ToVersion();

                // Does the module have a Plato version defined?
                if (modulePlatoVersion != null)
                {
                    // Get current plato version
                    var currentPlatoVersion = _platoOptions.Value.Version.ToVersion();
                    if (currentPlatoVersion != null)
                    {
                        // Does the module require a newer version of Plato?
                        if (modulePlatoVersion > currentPlatoVersion)
                        {
                            return result.Failed($"{moduleId} {module.Descriptor.Version} requires Plato {modulePlatoVersion.ToString()} whilst you are using Plato {currentPlatoVersion.ToString()}. Please upgrade to Plato {modulePlatoVersion.ToString()} and try updating {moduleId} again.");
                        }
                    }

                }

                // ------------------------------------------------------------------
                // 2. Perform migrations from current installed feature version
                // to latest available migration available within modules IMigrationProvider
                // ------------------------------------------------------------------
                
                var context2 = context;

                var from = feature.Version.ToVersion();
                var to = module.Descriptor.Version.ToVersion();

                if (from == null)
                {
                    return result.Failed(
                        $"Could not convert version for feature {feature.ModuleId} of {feature.Version} to a valid version object. Please check the version within the ShellFeatures database table.");
                }

                if (to == null)
                {
                    return result.Failed(
                        $"Could not convert version for module {module.Descriptor.Id} of {module.Descriptor.Version} to a valid version object. Please check the version within the modules manifest file.");
                }

                // Build migrations for feature & versions
                var migrations = _migrationBuilder.BuildMigrations(moduleId, from, to);

                // Apply migrations
                var migrationResults = await migrations.ApplyMigrationsAsync();

                // Did any errors occur whilst applying the migration?
                if (migrationResults.Errors.Any())
                {
                    var errors = new List<CommandError>();
                    foreach (var error in migrationResults.Errors)
                    {
                        errors.Add(new CommandError(error.Message));
                    }
                    return result.Failed(errors.ToArray());
                }

                // ------------------------------------------------------------------
                // 3. If we reach this point everything went OK, finally update
                // the feature version within the ShellFeatures table to reflect
                // the version of the module we've just updated to
                // ------------------------------------------------------------------

                // Update version
                feature.Version = module.Descriptor.Version;
                await _shellFeatureStore.UpdateAsync((ShellFeature)feature);
                
                // Return success
                return result.Success();

            });
            
            // Did any event encounter errors?
            var handlerErrors = results
                .Where(c => c.Value.Errors.Any())
                .SelectMany(h => h.Value.Errors)
                .ToList();

            var output = new CommandResultBase();
            if (handlerErrors.Count > 0)
            {
                var errors = new List<CommandError>();
                foreach (var error in handlerErrors)
                    errors.Add(new CommandError(error.Value));
                return output.Failed(errors.ToArray());
            }
            
            // No errors, recycle shell context to apply updates
            RecycleShell();

            return output.Success();
            

        }


        async Task<IDictionary<string, IFeatureEventContext>> InvokeFeatureEventHandlersAsync(
            IShellFeature feature,
            Func<IFeatureEventContext, Task<CommandResultBase>> configure)
        {
            
            // Raise updating & updated event handlers for features
            return await InvokeFeatureEvents(new[] {feature},
                async (context, handler) =>
                {

                    var contexts = new ConcurrentDictionary<string, IFeatureEventContext>();

                    try
                    {
                        await handler.UpdatingAsync(context);
                        contexts.AddOrUpdate(context.Feature.ModuleId, context, (k, v) =>
                        {
                            foreach (var error in context.Errors)
                            {
                                v.Errors.Add(error.Key, error.Value);
                            }

                            return v;
                        });

                    }
                    catch (Exception e)
                    {
                        contexts.AddOrUpdate(context.Feature.ModuleId, context, (k, v) =>
                        {
                            v.Errors.Add(context.Feature.ModuleId, e.Message);
                            return v;
                        });
                    }

                    // Did any event encounter errors?
                    var hasErrors = contexts
                        .Where(c => c.Value.Errors.Any());

                    // No errors raise UpdatedAsync
                    if (!hasErrors.Any())
                    {

                        // Execute upgrade configuration
                        var configureResult = await configure(context);
                        if (!configureResult.Errors.Any())
                        {

                            try
                            {
                                await handler.UpdatedAsync(context);
                                contexts.AddOrUpdate(context.Feature.ModuleId, context, (k, v) =>
                                {
                                    foreach (var error in context.Errors)
                                    {
                                        v.Errors.Add(error.Key, error.Value);
                                    }

                                    return v;
                                });
                            }
                            catch (Exception e)
                            {
                                contexts.AddOrUpdate(context.Feature.ModuleId, context, (k, v) =>
                                {
                                    v.Errors.Add(context.Feature.ModuleId, e.Message);
                                    return v;
                                });

                            }

                        }
                        else
                        {

                            foreach (var error in configureResult.Errors)
                            {
                                if (context.Errors == null)
                                {
                                    context.Errors = new Dictionary<string, string>();
                                }
                                context.Errors.Add(error.Code, error.Description);
                            }

                            contexts.AddOrUpdate(context.Feature.ModuleId, context, (k, v) =>
                            {
                                foreach (var error in configureResult.Errors)
                                {
                                    v.Errors.Add(error.Code, error.Description);
                                }

                                return v;
                            });
                        }

                    }

                    return contexts;

                }, async context =>
                {

                    var contexts = new ConcurrentDictionary<string, IFeatureEventContext>();

                    // Execute upgrade configuration
                    var configureResult = await configure(context);
                    if (configureResult.Errors.Any())
                    {

                        foreach (var error in configureResult.Errors)
                        {
                            if (context.Errors == null)
                            {
                                context.Errors = new Dictionary<string, string>();
                            }
                            context.Errors.Add(error.Code, error.Description);
                        }

                        contexts.AddOrUpdate(context.Feature.ModuleId, context, (k, v) =>
                        {
                            foreach (var error in configureResult.Errors)
                            {
                                v.Errors.Add(error.Code, error.Description);
                            }

                            return v;
                        });
                    }

                    return contexts;

                });

        }
        

        public async Task<ConcurrentDictionary<string, IFeatureEventContext>> InvokeFeatureEvents(
           IList<IShellFeature> features,
           Func<IFeatureEventContext, IFeatureEventHandler, Task<ConcurrentDictionary<string, IFeatureEventContext>>> handler,
           Func<IFeatureEventContext, Task<ConcurrentDictionary<string, IFeatureEventContext>>> noHandler)
        {

            // Holds the results of all our event execution contexts
            var contexts = new ConcurrentDictionary<string, IFeatureEventContext>();

            // Get setting before recycle
            var httpContext = _httpContextAccessor.HttpContext;
            var shellSettings = _runningShellTable.Match(httpContext);

            // Build a list of all unique features we are enabling / disabling
            var uniqueFeatures = new ConcurrentDictionary<string, IShellFeature>();
            foreach (var feature in features)
            {
                // The feature may also reference dependencies so ensure we also
                // add any dependencies for the features to our temporary shell descriptors
                if (feature.FeatureDependencies.Any())
                {
                    foreach (var dependency in feature.FeatureDependencies)
                    {
                        if (!uniqueFeatures.ContainsKey(dependency.ModuleId))
                        {
                            uniqueFeatures.TryAdd(dependency.ModuleId, dependency);
                        }
                    }
                }
                if (!uniqueFeatures.ContainsKey(feature.ModuleId))
                {
                    uniqueFeatures.TryAdd(feature.ModuleId, feature);
                }
            }

            // Ensure minimum features are always available within the temporary shell descriptor
            // We may depend upon services from the required features within the features we are enabling / disabling
            var minimumShellDescriptor = _shellContextFactory.MinimumShellDescriptor();

            // Add features and dependencies we are enabling / disabling to our minimum shell descriptor
            foreach (var feature in uniqueFeatures.Values)
            {
                minimumShellDescriptor.Modules.Add(new ShellModule(feature.ModuleId, feature.Version));
            }

            // Create a new shell context with features and all dependencies we need to enable / disable feature
            using (var shellContext = _shellContextFactory.CreateDescribedContext(shellSettings, minimumShellDescriptor))
            {
                using (var scope = shellContext.ServiceProvider.CreateScope())
                {

                    var handlers = scope.ServiceProvider.GetServices<IFeatureEventHandler>();
                    var handlersList = handlers.ToList();

                    // Iterate through each feature we wish to invoke
                    // Use the event handlers if available else just add to contexts collection
                    foreach (var feature in features)
                    {

                        // Context that will be passed around
                        var context = new FeatureEventContext()
                        {
                            Feature = feature,
                            ServiceProvider = scope.ServiceProvider,
                            Logger = _logger
                        };

                        // Get event handler for feature we are invoking
                        var featureHandler = handlersList.FirstOrDefault(h => h.ModuleId == feature.ModuleId);

                        // Get response from responsible func
                        var handlerContexts = featureHandler != null
                            ? await handler(context, featureHandler)
                            : await noHandler(context);

                        // Compile results from delegates
                        if (handlerContexts != null)
                        {
                            foreach (var handlerContext in handlerContexts)
                            {
                                contexts.AddOrUpdate(feature.ModuleId, handlerContext.Value, (k, v) =>
                                {
                                    foreach (var error in handlerContext.Value.Errors)
                                    {
                                        v.Errors.Add(error.Key, error.Value);
                                    }

                                    return v;
                                });


                            }

                        }

                        // Log any errors
                        if (context.Errors.Count > 0)
                        {
                            foreach (var error in context.Errors)
                            {
                                _logger.LogCritical(error.Value,
                                    $"An error occurred whilst invoking within {this.GetType().FullName}");
                            }
                        }

                    }

                }

            }

            return contexts;

        }

        public void RecycleShell()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var shellSettings = _runningShellTable.Match(httpContext);
            _platoHost.RecycleShellContext(shellSettings);
        }

    }

}