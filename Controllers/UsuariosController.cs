using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Web.Mvc;
using ControlAsistenciasCursosVirtuales.Helpers;
using ControlAsistenciasCursosVirtuales.Models;

namespace ControlAsistenciasCursosVirtuales.Controllers
{
    public class UsuariosController : Controller
    {
        public ActionResult Index()
        {
            if (Session["USER"] == null)
                return RedirectToAction("Login", "Auth");

            var vm = new UsuariosIndexViewModel
            {
                Usuarios = ObtenerUsuarios(),
                Usuario = NuevoUsuarioViewModel()
            };

            return View(vm);
        }

        [HttpGet]
        public ActionResult Editar(string id)
        {
            if (Session["USER"] == null)
                return RedirectToAction("Login", "Auth");

            var vm = new UsuariosIndexViewModel
            {
                Usuarios = ObtenerUsuarios(),
                Usuario = ObtenerUsuarioPorId(id)
            };

            ViewBag.AbrirModal = true;
            return View("Index", vm);
        }

        [HttpPost]
        public ActionResult Guardar(UsuariosIndexViewModel vm)
        {
            if (Session["USER"] == null)
                return RedirectToAction("Login", "Auth");

            CargarRoles(vm.Usuario);

            if (!ModelState.IsValid)
            {
                vm.Usuarios = ObtenerUsuarios();
                ViewBag.AbrirModal = true;
                return View("Index", vm);
            }

            try
            {
                bool esEditar = !string.IsNullOrWhiteSpace(vm.Usuario.UsuarioOriginal);
                string passwordNueva = vm.Usuario.Password == null ? "" : vm.Usuario.Password.Trim();

                if (esEditar)
                {
                    if (string.IsNullOrWhiteSpace(passwordNueva))
                    {
                        string sql = @"
                            UPDATE dbo.Usuario
                            SET CodigoUsuario = @CodigoUsuarioNuevo,
                                NombreUsuario = @NombreUsuario,
                                Rol = @Rol,
                                Status = @Activo
                            WHERE CodigoUsuario = @CodigoUsuarioOriginal;
                        ";

                        Db.Execute(sql,
                            new SqlParameter("@CodigoUsuarioNuevo", vm.Usuario.CodigoUsuario.Trim()),
                            new SqlParameter("@NombreUsuario", vm.Usuario.NombreUsuario.Trim()),
                            new SqlParameter("@Rol", vm.Usuario.Rol),
                            new SqlParameter("@Activo", vm.Usuario.Activo ? "ACTIVO" : "INACTIVO"),
                            new SqlParameter("@CodigoUsuarioOriginal", vm.Usuario.UsuarioOriginal)
                        );
                    }
                    else
                    {
                        string sql = @"
                            UPDATE dbo.Usuario
                            SET CodigoUsuario = @CodigoUsuarioNuevo,
                                NombreUsuario = @NombreUsuario,
                                Contraseña = @Contrasena,
                                Rol = @Rol,
                                Status = @Activo
                            WHERE CodigoUsuario = @CodigoUsuarioOriginal;
                        ";

                        Db.Execute(sql,
                            new SqlParameter("@CodigoUsuarioNuevo", vm.Usuario.CodigoUsuario.Trim()),
                            new SqlParameter("@NombreUsuario", vm.Usuario.NombreUsuario.Trim()),
                            new SqlParameter("@Contrasena", passwordNueva),
                            new SqlParameter("@Rol", vm.Usuario.Rol),
                            new SqlParameter("@Activo", vm.Usuario.Activo ? "ACTIVO" : "INACTIVO"),
                            new SqlParameter("@CodigoUsuarioOriginal", vm.Usuario.UsuarioOriginal)
                        );
                    }

                    TempData["Success"] = "Usuario actualizado correctamente.";
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(passwordNueva))
                    {
                        ModelState.AddModelError("Usuario.Password", "Ingrese contraseña");
                        vm.Usuarios = ObtenerUsuarios();
                        ViewBag.AbrirModal = true;
                        return View("Index", vm);
                    }

                    string sql = @"
                        INSERT INTO dbo.Usuario 
                        (CodigoUsuario, NombreUsuario, Contraseña, Rol, Status)
                        VALUES 
                        (@CodigoUsuario, @NombreUsuario, @Contrasena, @Rol, @Activo);
                    ";

                    Db.Execute(sql,
                        new SqlParameter("@CodigoUsuario", vm.Usuario.CodigoUsuario.Trim()),
                        new SqlParameter("@NombreUsuario", vm.Usuario.NombreUsuario.Trim()),
                        new SqlParameter("@Contrasena", passwordNueva),
                        new SqlParameter("@Rol", vm.Usuario.Rol),
                        new SqlParameter("@Activo", vm.Usuario.Activo ? "ACTIVO" : "INACTIVO")
                    );

                    TempData["Success"] = "Usuario creado correctamente.";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Ocurrió un error al guardar: " + ex.Message;
                vm.Usuarios = ObtenerUsuarios();
                ViewBag.AbrirModal = true;
                return View("Index", vm);
            }
        }

        private List<UsuarioListaViewModel> ObtenerUsuarios()
        {
            string sql = @"
                SELECT CodigoUsuario, NombreUsuario, Rol, Status
                FROM dbo.Usuario
                ORDER BY CodigoUsuario DESC;
            ";

            var dt = Db.Query(sql);
            var lista = new List<UsuarioListaViewModel>();

            foreach (DataRow r in dt.Rows)
            {
                lista.Add(new UsuarioListaViewModel
                {
                    CodigoUsuario = r["CodigoUsuario"].ToString(),
                    NombreUsuario = r["NombreUsuario"].ToString(),
                    Rol = r["Rol"].ToString(),
                    Status = r["Status"].ToString()
                });
            }

            return lista;
        }

        private UsuarioViewModel ObtenerUsuarioPorId(string id)
        {
            var usuario = NuevoUsuarioViewModel();

            var dt = Db.Query(
                "SELECT * FROM dbo.Usuario WHERE CodigoUsuario=@id",
                new SqlParameter("@id", id)
            );

            if (dt.Rows.Count != 1)
                return usuario;

            var r = dt.Rows[0];

            usuario.UsuarioOriginal = r["CodigoUsuario"].ToString();
            usuario.CodigoUsuario = r["CodigoUsuario"].ToString();
            usuario.NombreUsuario = r["NombreUsuario"].ToString();
            usuario.Password = "";
            usuario.Rol = r["Rol"].ToString();
            usuario.Activo = r["Status"].ToString() == "ACTIVO";

            CargarRoles(usuario);
            return usuario;
        }

        private UsuarioViewModel NuevoUsuarioViewModel()
        {
            var usuario = new UsuarioViewModel
            {
                Activo = true
            };

            CargarRoles(usuario);
            return usuario;
        }

        private void CargarRoles(UsuarioViewModel usuario)
        {
            usuario.Roles = new List<SelectListItem>
            {
                new SelectListItem { Text = "-- Seleccione --", Value = "" },
                new SelectListItem { Text = "Admin", Value = "Admin" },
                new SelectListItem { Text = "Maestro", Value = "Maestro" },
                new SelectListItem { Text = "Estudiante", Value = "Estudiante" }
            };
        }
    }
}