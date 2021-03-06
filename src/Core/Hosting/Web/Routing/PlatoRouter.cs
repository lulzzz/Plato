﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace PlatoCore.Hosting.Web.Routing
{

    public class PlatoRouter : IPlatoRouter
    {
        private readonly IActionInvokerFactory _actionInvokerFactory;
        private readonly DiagnosticListener _diagnosticListener;
        private readonly IActionSelector _actionSelector;      
        private readonly ILogger _logger;

        public PlatoRouter(
            IActionInvokerFactory actionInvokerFactory,
            DiagnosticListener diagnosticListener,
            IActionSelector actionSelector,       
            ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<PlatoRouter>();
            _actionInvokerFactory = actionInvokerFactory;        
            _diagnosticListener = diagnosticListener;
            _actionSelector = actionSelector;
        }

        public VirtualPathData GetVirtualPath(VirtualPathContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // We return null here because we're not responsible for generating the url, the route is.
            return null;
        }

        public Task RouteAsync(RouteContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var candidates = _actionSelector.SelectCandidates(context);
            if (candidates == null || candidates.Count == 0)
            {
                //_logger.NoActionsMatched(context.RouteData.Values);
                return Task.CompletedTask;
            }

            var actionDescriptor = _actionSelector.SelectBestCandidate(context, candidates);
            if (actionDescriptor == null)
            {
                //_logger.NoActionsMatched(context.RouteData.Values);
                return Task.CompletedTask;
            }

            context.Handler = (c) =>
            {
                var routeData = c.GetRouteData();

                var actionContext = new ActionContext(context.HttpContext, routeData, actionDescriptor);
                var invoker = _actionInvokerFactory.CreateInvoker(actionContext);
                if (invoker == null)
                {
                    throw new InvalidOperationException($"Could not invoke {actionDescriptor.DisplayName}");
                }

                return invoker.InvokeAsync();
            };

            return Task.CompletedTask;
        }

    }

}
