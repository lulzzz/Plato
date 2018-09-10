﻿namespace Plato.Internal.Security.Abstractions
{
    public class StandardPermissions
    {

        public const string CategoryName = "Plato";

        public static readonly Permission Administrator = 
            new Permission("AdministratorAccess", "Can access administrator control panel", CategoryName);

    }
}