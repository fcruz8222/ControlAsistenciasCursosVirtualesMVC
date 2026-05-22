using System.Data.SqlClient;
using System.Web.Mvc;
using ControlAsistenciasCursosVirtuales.Helpers;
using ControlAsistenciasCursosVirtuales.Models;

namespace ControlAsistenciasCursosVirtuales.Controllers
{
    public class AuthController : Controller
    {
        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            string sql = @"
                SELECT CodigoUsuario, NombreUsuario, Rol, Activo
                FROM dbo.Usuario
                WHERE CodigoUsuario = @CodUser AND Contraseña = @Pass
            ";

            var dt = Db.Query(sql,
                new SqlParameter("@CodUser", model.CodUser.Trim()),
                new SqlParameter("@Pass", model.Pass.Trim())
            );

            if (dt.Rows.Count == 1)
            {
                var row = dt.Rows[0];

                if (!System.Convert.ToBoolean(row["Activo"]))
                {
                    ViewBag.Error = "Usuario inactivo.";
                    return View(model);
                }

                Session["USER"] = row["CodigoUsuario"].ToString();
                Session["USER_NAME"] = row["NombreUsuario"].ToString();
                Session["ROLE"] = row["Rol"].ToString();

                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Credenciales inválidas.";
            return View(model);
        }

        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login", "Auth");
        }
    }
}