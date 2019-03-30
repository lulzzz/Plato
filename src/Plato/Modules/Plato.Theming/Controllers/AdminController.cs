﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using Plato.Internal.Hosting.Abstractions;
using Plato.Internal.Layout;
using Plato.Theming.Models;
using Plato.Internal.Layout.Alerts;
using Plato.Internal.Layout.ModelBinding;
using Plato.Internal.Layout.ViewProviders;
using Plato.Internal.Navigation.Abstractions;
using Plato.Theming.Services;
using Plato.Theming.ViewModels;

namespace Plato.Theming.Controllers
{

    public class AdminController : Controller, IUpdateModel
    {
        
        private readonly IViewProviderManager<ThemeAdmin> _viewProvider;
        private readonly ISiteThemeCreator _siteThemeCreator;
        private readonly IBreadCrumbManager _breadCrumbManager;
        private readonly IContextFacade _contextFacade;
        private readonly IAlerter _alerter;

        public IHtmlLocalizer T { get; }

        public IStringLocalizer S { get; }
        
        public AdminController(
            IHtmlLocalizer htmlLocalizer,
            IStringLocalizer stringLocalizer,           
            IViewProviderManager<ThemeAdmin> viewProvider,
            ISiteThemeCreator siteThemeCreator,
            IBreadCrumbManager breadCrumbManager,
            IContextFacade contextFacade,
            IAlerter alerter)
        {

            _breadCrumbManager = breadCrumbManager;
            _contextFacade = contextFacade;
            _viewProvider = viewProvider;
            _alerter = alerter;
            _siteThemeCreator = siteThemeCreator;

            T = htmlLocalizer;
            S = stringLocalizer;

        }

        // ------------
        // Index
        // ------------

        public async Task<IActionResult> Index()
        {

            _breadCrumbManager.Configure(builder =>
            {
                builder.Add(S["Home"], home => home
                    .Action("Index", "Admin", "Plato.Admin")
                    .LocalNav()
                ).Add(S["Themes"]);
            });
                     
            return View((LayoutViewModel) await _viewProvider.ProvideIndexAsync(new ThemeAdmin(), this));
            
        }
        
        // ------------
        // Create
        // ------------

        public async Task<IActionResult> Create()
        {

            _breadCrumbManager.Configure(builder =>
            {
                builder
                    .Add(S["Home"], home => home
                        .Action("Index", "Admin", "Plato.Admin")
                        .LocalNav())
                    .Add(S["Theming"], tags => tags
                        .Action("Index", "Admin", "Plato.Theming")
                        .LocalNav())
                    .Add(S["Add Theme"]);
            });

            // We need to pass along the featureId
            return View((LayoutViewModel)await _viewProvider.ProvideEditAsync(new ThemeAdmin
            {
                IsNewTheme = true

            }, this));

        }

        [HttpPost, ValidateAntiForgeryToken, ActionName(nameof(Create))]
        public async Task<IActionResult> CreatePost(EditThemeViewModel viewModel)
        {

            var user = await _contextFacade.GetAuthenticatedUserAsync();

            if (user == null)
            {
                return Unauthorized();
            }

            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            // Create theme
            var model = new ThemeAdmin()
            {
                IsNewTheme = true
            };

            var result = _siteThemeCreator.CreateTheme(viewModel.ThemeId, viewModel.Name);
            if (result.Succeeded)
            {

                // Execute view providers
               await _viewProvider.ProvideUpdateAsync(model, this);

                // Add confirmation
                _alerter.Success(T["Theme Added Successfully!"]);

                // Return
                return RedirectToAction(nameof(Index));

            }
            else
            {
                // Report any errors
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(viewModel);

        }

       
    }
}