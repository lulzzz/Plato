﻿using Plato.Internal.Models.Features;

namespace Plato.Internal.Models.Shell
{
    public class ShellModule
    {
 
        public int Id { get; set; }

        public string ModuleId { get; set; }

        public string Version { get; set; } = "1.0.0";
        
        public ShellModule()
        {
        }
        public ShellModule(IShellFeature feature)
        {
            this.Id = feature.Id;
            this.ModuleId = feature.ModuleId;
            this.Version = feature.Version;
        }
        
        public ShellModule(string moduleId, string version)
        {
            this.ModuleId = moduleId;
            this.Version = version;
        }

        
    }

}