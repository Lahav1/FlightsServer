using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace FlightsServer
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
            config.Routes.MapHttpRoute(
                name: "Flight",
                routeTemplate: "api/{controller}/{src}/{dest}/{y}/{m}/{d}/{tickets}",
                defaults: new { id = RouteParameter.Optional, src = RouteParameter.Optional, dest = RouteParameter.Optional,
                                y = RouteParameter.Optional, m = RouteParameter.Optional, d = RouteParameter.Optional,
                                tickets = RouteParameter.Optional }
            );
        }
    }
}
