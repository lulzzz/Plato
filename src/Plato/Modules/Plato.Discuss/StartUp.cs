﻿using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Plato.Internal.Models.Shell;
using Plato.Internal.Navigation;
using Plato.Discuss.Handlers;
using Plato.Discuss.Resources;
using Plato.Discuss.ViewModels;
using Plato.Discuss.ViewProviders;
using Plato.Entities.Models;
using Plato.Internal.Features.Abstractions;
using Plato.Internal.Hosting.Abstractions;
using Plato.Internal.Layout.ViewProviders;
using Plato.Internal.Resources.Abstractions;

namespace Plato.Discuss
{
    public class Startup : StartupBase
    {
        private readonly IShellSettings _shellSettings;

        public Startup(IShellSettings shellSettings)
        {
            _shellSettings = shellSettings;
        }

        public override void ConfigureServices(IServiceCollection services)
        {

            // Feature installation event handler
            services.AddScoped<IFeatureEventHandler, FeatureEventHandler>();

            // Register navigation provider
            services.AddScoped<INavigationProvider, AdminMenu>();
            services.AddScoped<INavigationProvider, SiteMenu>();

            // Register client resources
            services.AddScoped<IResourceProvider, ResourceProvider>();

            // Register view providers
            services.AddScoped<IViewProviderManager<HomeIndexViewModel>, ViewProviderManager<HomeIndexViewModel>>();
            services.AddScoped<IViewProvider<HomeIndexViewModel>, HomeIndexViewProvider>();

            services.AddScoped<IViewProviderManager<HomeTopicViewModel>, ViewProviderManager<HomeTopicViewModel>>();
            services.AddScoped<IViewProvider<HomeTopicViewModel>, HomeTopicViewProvider>();


        }

        public override void Configure(
            IApplicationBuilder app,
            IRouteBuilder routes,
            IServiceProvider serviceProvider)
        {

            routes.MapAreaRoute(
                name: "Discuss",
                areaName: "Plato.Discuss",
                template: "discuss",
                defaults: new { controller = "Home", action = "Index" }
            );

        }
    }
}