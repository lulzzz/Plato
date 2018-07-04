﻿using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Plato.Features.ViewModels;
using Plato.Internal.Features.Abstractions;
using Plato.Internal.Layout.Alerts;
using Plato.Internal.Layout.ModelBinding;
using Plato.Internal.Layout.ViewProviders;

namespace Plato.Features.Controllers
{

    public class AdminController : Controller, IUpdateModel
    {

        private readonly IViewProviderManager<FeaturesViewModel> _featuresIndexViewProvider;
        private readonly IShellFeatureManager _shellFeatureManager;
        private readonly IShellDescriptorManager _shellDescriptorManager;
        private readonly IAlerter _alerter;

        public IHtmlLocalizer T { get; }
        
        public AdminController(
            IHtmlLocalizer<AdminController> localizer,
            IShellFeatureManager shellFeatureManager,
            IShellDescriptorManager shellDescriptorManager,
            IAlerter alerter, IViewProviderManager<FeaturesViewModel> featuresIndexViewProvider)
        {
            _shellFeatureManager = shellFeatureManager;
            _shellDescriptorManager = shellDescriptorManager;
            _alerter = alerter;
            _featuresIndexViewProvider = featuresIndexViewProvider;

            T = localizer;
        }
        
        public async Task<IActionResult> Index()
        {
            
            var features = await _shellDescriptorManager.GetFeaturesAsync();
            

            //var enabledFeatures = _shellFEatureManager.

            var model = new FeaturesViewModel()
            {
                Features = features
            };
            

            var result = await _featuresIndexViewProvider.ProvideIndexAsync(model, this);
            return View(result);

            
        }

        [HttpPost]
        public async Task<IActionResult> Enable(string id)
        {
            
            var contexts = await _shellFeatureManager.EnableFeatureAsync(id);

            foreach (var context in contexts)
            {
                if (context.Errors.Any())
                {
                    foreach (var error in context.Errors)
                    {
                        _alerter.Danger(T[$"{context.Feature.ModuleId} could not be enabled. {error.Key} - {error.Value}"]);
                    }
                }
                else
                {
                    _alerter.Success(T[$"{context.Feature.ModuleId} enabled successfully!"]);
                }
                
            }
            
            return RedirectToAction(nameof(Index));

        }

        [HttpPost]
        public async Task<IActionResult> Disable(string id)
        {

            var contexts = await _shellFeatureManager.DisableFeatureAsync(id);

            foreach (var context in contexts)
            {
                if (context.Errors.Any())
                {
                    foreach (var error in context.Errors)
                    {
                        _alerter.Danger(T[$"{error.Key} could not be disabled. The following error occurred: {error.Value}"]);
                    }
                }
                else
                {
                    _alerter.Success(T[$"{context.Feature.ModuleId} disabled successfully!"]);
                }
                
            }
            
            return RedirectToAction(nameof(Index));

        }




    }
}