﻿using Microsoft.AspNetCore.Routing;

namespace Plato.Site.Demo.Models
{

    public class SampleDataDescriptor
    {

        public string ModuleId { get; set; }

        public string EntityType { get; set; }
        
        public int EntitiesToCreate { get; set; } = 10;

        public int EntityRepliesToCreate { get; set; } = 25;

    }

}

