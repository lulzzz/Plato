﻿using Microsoft.Extensions.Localization;
using System;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Plato.Internal.Navigation;
using Plato.Internal.Navigation.Abstractions;

namespace Plato.Docs.Navigation
{
    public class ProfileMenu : INavigationProvider
    {

        private readonly IActionContextAccessor _actionContextAccessor;

        public IStringLocalizer T { get; set; }
        
        public ProfileMenu(
            IStringLocalizer localizer, 
            IActionContextAccessor actionContextAccessor)
        {
            T = localizer;
            _actionContextAccessor = actionContextAccessor;
        }
        
        public void BuildNavigation(string name, INavigationBuilder builder)
        {

            if (!String.Equals(name, "profile", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var context = _actionContextAccessor.ActionContext;
            object id = context.RouteData.Values["opts.id"], 
                alias = context.RouteData.Values["opts.alias"];

            builder
                .Add(T["Docs"], 2, discuss => discuss
                    .Add(T["Docs"], 1, topics => topics
                        .Action("Index", "Profile", "Plato.Docs", new RouteValueDictionary()
                        {
                            ["opts.id"] = id?.ToString(),
                            ["opts.alias"] = alias?.ToString()
                        })
                        //.Permission(Permissions.ManageRoles)
                        .LocalNav()
                    ).Add(T["Favorites"], 999, favorites => favorites
                        .Action("Channels", "Admin", "Plato.Docs")
                        //.Permission(Permissions.ManageRoles)
                        .LocalNav()
                    ));
        }
    }

}