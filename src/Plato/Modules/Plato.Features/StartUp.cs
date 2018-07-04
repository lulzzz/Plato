﻿using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Plato.Features.Handlers;
using Plato.Features.ViewModels;
using Plato.Features.ViewProviders;
using Plato.Internal.Abstractions.SetUp;
using Plato.Internal.Hosting;
using Plato.Internal.Layout.ViewProviders;
using Plato.Internal.Navigation;
using Plato.Internal.Hosting.Abstractions;

namespace Plato.Features
{
    public class Startup : StartupBase
    {
    
        public override void ConfigureServices(IServiceCollection services)
        {
            // Navigation provider
            services.AddScoped<INavigationProvider, AdminMenu>();

            // Setup event handler
            services.AddScoped<ISetUpEventHandler, SetUpEventHandler>();
            
            // View providers
            services.AddScoped<IViewProviderManager<FeaturesViewModel>, ViewProviderManager<FeaturesViewModel>>();
            services.AddScoped<IViewProvider<FeaturesViewModel>, FeaturesIndexViewProvider>();

        }

        public override void Configure(
            IApplicationBuilder app,
            IRouteBuilder routes,
            IServiceProvider serviceProvider)
        {

            //routes.MapAreaRoute(
            //    name: "AdminFeatures",
            //    areaName: "Plato.Features",
            //    template: "admin/features/{action}/{id?}",
            //    defaults: new { controller = "Admin", action = "Index" }
            //);

            //routes.MapAreaRoute(
            //    name: "AdminEnableFeatures",
            //    areaName: "Plato.Features",
            //    template: "admin/features/{action}/{id?}",
            //    defaults: new { controller = "Admin", action = "Enable" }
            //);

        }
    }
}