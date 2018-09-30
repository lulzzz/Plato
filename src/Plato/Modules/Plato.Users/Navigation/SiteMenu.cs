﻿using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using Plato.Internal.Navigation;

namespace Plato.Users.Navigation
{
    public class SiteMenu : INavigationProvider
    {
        public SiteMenu(IStringLocalizer<AdminMenu> localizer)
        {
            T = localizer;
        }

        public IStringLocalizer T { get; set; }

        public void BuildNavigation(string name, NavigationBuilder builder)
        {
            if (!String.Equals(name, "site", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            builder
                .Add(T["Users"], "1", installed => installed
                        .Action("Index", "Home", "Plato.Users")
                        .IconCss("fal fa-users")
                        //.Permission(Permissions.ManageRoles)
                        .Attributes(new Dictionary<string, object>()
                        {
                            {"data-provide", "tooltip"},
                            {"title", T["Users"]}
                        })
                        .LocalNav()
                    , new List<string>() {"users"});
        }
    }

}
