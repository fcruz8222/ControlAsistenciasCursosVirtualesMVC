using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Web.Mvc;
using ControlAsistenciasCursosVirtuales.Helpers;
using ControlAsistenciasCursosVirtuales.Models;
using ControlAsistenciasCursosVirtuales.Filters;

namespace ControlAsistenciasCursosVirtuales.Controllers
{
    [RoleAuthorize("Admin")]
    public class MaestrosController : Controller
    {
        public ActionResult Index(string buscar = "")
        {
            if (Session["USER"] == null)
                return RedirectToAction("Login", "Auth");

            var vm = new MaestrosIndexViewModel
            {
                Buscar = buscar,
                Maestros = ObtenerMaestros(buscar),
                Maestro = NuevoMaestroViewModel()
            };

            return View(vm);
        }

        [HttpGet]
        public ActionResult Editar(int id, string buscar = "")
        {
            if (Session["USER"] == null)
                return RedirectToAction("Login", "Auth");

            var vm = new MaestrosIndexViewModel
            {
                Buscar = buscar,
                Maestros = ObtenerMaestros(buscar),
                Maestro = ObtenerMaestroPorId(id)
            };

            ViewBag.AbrirModal = true;
            return View("Index", vm);
        }

        [HttpPost]
        public ActionResult Guardar(MaestrosIndexViewModel vm)
        {
            if (Session["USER"] == null)
                return RedirectToAction("Login", "Auth");

            CargarStatus(vm.Maestro);

            if (!ModelState.IsValid)
            {
                vm.Maestros = ObtenerMaestros(vm.Buscar);
                ViewBag.AbrirModal = true;
                return View("Index", vm);
            }

            if (vm.Maestro.IdMaestro.HasValue)
            {
                string sql = @"
                    UPDATE dbo.Maestros
                    SET Nombre = @Nombre,
                        Area = @Area,
                        Status = @Status
                    WHERE IdMaestro = @IdMaestro;
                ";

                Db.Execute(sql,
                    new SqlParameter("@Nombre", vm.Maestro.Nombre.Trim()),
                    new SqlParameter("@Area", vm.Maestro.Area.Trim()),
                    new SqlParameter("@Status", vm.Maestro.Status),
                    new SqlParameter("@IdMaestro", vm.Maestro.IdMaestro.Value)
                );

                TempData["Success"] = "Maestro actualizado correctamente.";
            }
            else
            {
                string sql = @"
                    INSERT INTO dbo.Maestros (Nombre, Area, Status)
                    VALUES (@Nombre, @Area, @Status);
                ";

                Db.Execute(sql,
                    new SqlParameter("@Nombre", vm.Maestro.Nombre.Trim()),
                    new SqlParameter("@Area", vm.Maestro.Area.Trim()),
                    new SqlParameter("@Status", vm.Maestro.Status)
                );

                TempData["Success"] = "Maestro creado correctamente.";
            }

            return RedirectToAction("Index", new { buscar = vm.Buscar });
        }

        [HttpPost]
        public ActionResult Borrar(int id, string buscar = "")
        {
            if (Session["USER"] == null)
                return RedirectToAction("Login", "Auth");

            /*
              Si el maestro ya está usado en Cursos, no se elimina.
              Solo se desactiva para evitar errores de llave foránea.
            */
            string sqlCount = @"SELECT COUNT(1) FROM dbo.Cursos WHERE IdMaestro = @IdMaestro;";
            object obj = Db.Scalar(sqlCount, new SqlParameter("@IdMaestro", id));

            int totalCursos = 0;
            if (obj != null && obj != DBNull.Value)
                totalCursos = Convert.ToInt32(obj);

            if (totalCursos > 0)
            {
                string sqlUpdate = @"UPDATE dbo.Maestros SET Status = 'INACTIVO' WHERE IdMaestro = @IdMaestro;";
                Db.Execute(sqlUpdate, new SqlParameter("@IdMaestro", id));

                TempData["Warning"] = "El maestro tiene cursos asociados, se desactivó.";
            }
            else
            {
                string sqlDelete = @"DELETE FROM dbo.Maestros WHERE IdMaestro = @IdMaestro;";
                Db.Execute(sqlDelete, new SqlParameter("@IdMaestro", id));

                TempData["Success"] = "Maestro eliminado correctamente.";
            }

            return RedirectToAction("Index", new { buscar = buscar });
        }

        private List<MaestroListaViewModel> ObtenerMaestros(string filtro = "")
        {
            string sql = @"
                SELECT IdMaestro, Nombre, Area, Status
                FROM dbo.Maestros
                WHERE (@filtro = '' 
                       OR Nombre LIKE '%' + @filtro + '%'
                       OR Area LIKE '%' + @filtro + '%')
                ORDER BY IdMaestro DESC;
            ";

            var dt = Db.Query(sql, new SqlParameter("@filtro", filtro ?? ""));
            var lista = new List<MaestroListaViewModel>();

            foreach (DataRow r in dt.Rows)
            {
                lista.Add(new MaestroListaViewModel
                {
                    IdMaestro = Convert.ToInt32(r["IdMaestro"]),
                    Nombre = r["Nombre"].ToString(),
                    Area = r["Area"].ToString(),
                    Status = r["Status"].ToString()
                });
            }

            return lista;
        }

        private MaestroViewModel ObtenerMaestroPorId(int id)
        {
            var maestro = NuevoMaestroViewModel();

            var dt = Db.Query(
                "SELECT IdMaestro, Nombre, Area, Status FROM dbo.Maestros WHERE IdMaestro = @IdMaestro",
                new SqlParameter("@IdMaestro", id)
            );

            if (dt.Rows.Count != 1)
                return maestro;

            var r = dt.Rows[0];

            maestro.IdMaestro = Convert.ToInt32(r["IdMaestro"]);
            maestro.Nombre = r["Nombre"].ToString();
            maestro.Area = r["Area"].ToString();
            maestro.Status = r["Status"].ToString();

            CargarStatus(maestro);

            return maestro;
        }

        private MaestroViewModel NuevoMaestroViewModel()
        {
            var maestro = new MaestroViewModel
            {
                Status = "ACTIVO"
            };

            CargarStatus(maestro);

            return maestro;
        }

        private void CargarStatus(MaestroViewModel maestro)
        {
            maestro.StatusList = new List<SelectListItem>
            {
                new SelectListItem { Text = "-- Seleccione --", Value = "" },
                new SelectListItem { Text = "ACTIVO", Value = "ACTIVO" },
                new SelectListItem { Text = "INACTIVO", Value = "INACTIVO" }
            };
        }
    }
}