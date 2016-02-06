﻿namespace MyTested.Mvc.Internal.Routes
{
    using Microsoft.AspNet.Routing;
    using Internal.Application;
    using Microsoft.AspNet.Mvc;
    using Microsoft.AspNet.Mvc.Controllers;
    using Microsoft.AspNet.Mvc.Infrastructure;
    using Contracts;
    using System;
    using Microsoft.AspNet.Mvc.Abstractions;
    /// <summary>
    /// Used for resolving HTTP request to a route.
    /// </summary>
    public static class InternalRouteResolver
    {
        private const string UnresolvedRouteFormat = "it could not be resolved: '{0}'";

        /// <summary>
        /// Resolves HTTP request to a route using the provided route context and the action selector and invoker services.
        /// </summary>
        /// <param name="router">IRouter to resolve route values.</param>
        /// <param name="routeContext">RouteContext to use for resolving the route values.</param>
        /// <returns>Resolved route information.</returns>
        public static ResolvedRouteContext Resolve(IRouter router, RouteContext routeContext)
        {
            router.RouteAsync(routeContext).Wait();

            var actionSelector = TestServiceProvider.GetRequiredService<IActionSelector>();
            var actionInvokerFactory = TestServiceProvider.GetRequiredService<IActionInvokerFactory>();

            ActionDescriptor actionDescriptor;
            try
            {
                actionDescriptor = actionSelector.SelectAsync(routeContext).Result;
            }
            catch (Exception ex)
            {
                return new ResolvedRouteContext($"exception was thrown when trying to select an action: '{ex.Message}'");
            }

            if (actionDescriptor == null)
            {
                return new ResolvedRouteContext("action could not be matched");
            }

            var actionContext = new ActionContext(routeContext.HttpContext, routeContext.RouteData, actionDescriptor);

            var controllerActionDescriptor = actionDescriptor as ControllerActionDescriptor;
            if (controllerActionDescriptor == null)
            {
                throw new InvalidOperationException("Only controller actions are supported by the route testing.");
            }

            var invoker = actionInvokerFactory.CreateInvoker(actionContext);
            var modelBindingActionInvoker = invoker as IModelBindingActionInvoker;
            if (modelBindingActionInvoker == null)
            {
                throw new InvalidOperationException("Route testing requires the selected IActionInvoker by the IActionInvokerFactory to implement IModelBindingActionInvoker.");
            }

            try
            {
                modelBindingActionInvoker.InvokeAsync().Wait();
            }
            catch (Exception ex)
            {
                return new ResolvedRouteContext($"exception was thrown when trying to bind the action arguments: '{ex.Message}'");
            }

            return new ResolvedRouteContext(
                controllerActionDescriptor.ControllerTypeInfo,
                controllerActionDescriptor.ControllerName,
                controllerActionDescriptor.Name,
                modelBindingActionInvoker.BoundActionArguments,
                actionContext.RouteData,
                actionContext.ModelState);
        }
    }
}
