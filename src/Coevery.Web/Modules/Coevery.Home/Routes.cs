using System.Collections.Generic;
using System.Web.Mvc;
using System.Web.Routing;
using Coevery.Mvc.Routes;

namespace Coevery.Home
{
    public class Routes : IRouteProvider
    {
        public IEnumerable<RouteDescriptor> GetRoutes()
        {
            return new[]
            {
                new RouteDescriptor
                {
                    Route = new Route("",
                        new RouteValueDictionary
                        {
                            {"area", "Coevery.Home"},
                            {"controller", "Home"},
                            {"action", "Index"}
                        }, new RouteValueDictionary(),
                        new RouteValueDictionary {{"area", "Coevery.Home"}},
                        new MvcRouteHandler())
                }
            };
        }

        public void GetRoutes(ICollection<RouteDescriptor> routes)
        {
            foreach (RouteDescriptor route in GetRoutes())
            {
                routes.Add(route);
            }
        }
    }
}