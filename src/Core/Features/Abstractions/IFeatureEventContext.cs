﻿using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using PlatoCore.Models.Features;

namespace PlatoCore.Features.Abstractions
{
    public interface IFeatureEventContext
    {

        IServiceProvider ServiceProvider { get; set; }

        IShellFeature Feature { get; set; }

        ILogger Logger { get; set; }

        IDictionary<string, string> Errors { get; set; }

    }

}
