using System;
using System.Linq;
using System.Web.Mvc;

namespace ControlAsistenciasCursosVirtuales.Filters
{
    public class RoleAuthorizeAttribute : ActionFilterAttribute
    {
        private readonly string[] _rolesPermitidos;

        public RoleAuthorizeAttribute(params string[] rolesPermitidos)
        {
            _rolesPermitidos = rolesPermitidos;
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var session = filterContext.HttpContext.Session;

            if (session["USER"] == null)
            {
                filterContext.Result = new RedirectToRouteResult(
                    new System.Web.Routing.RouteValueDictionary
                    {
                        { "controller", "Auth" },
                        { "action", "Login" }
                    }
                );
                return;
            }

            string rolUsuario = session["ROLE"] == null ? "" : session["ROLE"].ToString();

            bool tieneAcceso = _rolesPermitidos.Any(r =>
                string.Equals(r, rolUsuario, StringComparison.OrdinalIgnoreCase)
            );

            if (!tieneAcceso)
            {
                filterContext.Result = new RedirectToRouteResult(
                    new System.Web.Routing.RouteValueDictionary
                    {
                        { "controller", "Home" },
                        { "action", "SinAcceso" }
                    }
                );
            }
        }
    }
}