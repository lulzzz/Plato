﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Plato.Layout.Views;

namespace Plato.Layout.Drivers
{
    public interface IViewDriver
    {

        Task<IViewDriverResult> Configure();

    }

    public class BaseDriver 
    {

        public async Task<IViewDriverResult> Initialize(
            string name,
            Action<IViewDriverBuilder> builder
            ) 
        {
            var driverContext = new ViewDriverBuilder();;

            builder(driverContext);

            return new ViewDriverResult()
            {

            };

        }
    }
}
