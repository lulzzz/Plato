﻿using System;
using Microsoft.Extensions.Configuration;
using PlatoCore.Models.Shell;

namespace PlatoCore.Shell
{

    public static class ShellSettingsSerializer
    {

        public static ShellSettings ParseSettings(IConfigurationRoot configuration)
        {

            return new ShellSettings
            {
                Name = configuration["Name"],
                RequestedUrlHost = configuration["RequestedUrlHost"],
                RequestedUrlPrefix = configuration["RequestedUrlPrefix"],
                ConnectionString = configuration["ConnectionString"],
                TablePrefix = configuration["TablePrefix"],
                DatabaseProvider = configuration["DatabaseProvider"],
                Theme = configuration["Theme"],
                State = Enum.TryParse(configuration["State"], true, out TenantState state)
                    ? state
                    : TenantState.Uninitialized
            };

        }

    }

}
