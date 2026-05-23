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
    [RoleAuthorize("Admin", "Maestro")]
    public class CursoEstudiantesController : Controller
    {
        public ActionResult Index(string buscar = "")
        {
            if (Session["USER"] == null)
                return RedirectToAction("Login", "Auth");

           // Schema.EnsureCursoEstudiante();

            var vm = new CursoEstudianteIndexViewModel
            {
                Buscar = buscar,
                Asignacion = NuevaAsignacionViewModel(),
                Asignaciones = ObtenerAsignaciones(buscar)
            };

            return View(vm);
        }

        [HttpGet]
        public ActionResult Editar(int id, string buscar = "")
        {
            if (Session["USER"] == null)
                return RedirectToAction("Login", "Auth");

          //  Schema.EnsureCursoEstudiante();

            var vm = new CursoEstudianteIndexViewModel
            {
                Buscar = buscar,
                Asignacion = ObtenerAsignacionPorId(id),
                Asignaciones = ObtenerAsignaciones(buscar)
            };

            ViewBag.AbrirModal = true;
            return View("Index", vm);
        }

        [HttpPost]
        public ActionResult Guardar(CursoEstudianteIndexViewModel vm)
        {
            if (Session["USER"] == null)
                return RedirectToAction("Login", "Auth");

            CargarCombos(vm.Asignacion);

            bool esEditar = vm.Asignacion.IdCursoEstudiante.HasValue;

            if (esEditar)
            {
                ModelState.Remove("Asignacion.CodigosUsuarios");
            }
            else
            {
                ModelState.Remove("Asignacion.CodigoUsuario");
            }

            if (!ModelState.IsValid)
            {
                vm.Asignaciones = ObtenerAsignaciones(vm.Buscar);
                ViewBag.AbrirModal = true;
                return View("Index", vm);
            }

            try
            {
                if (esEditar)
                {
                    string sql = @"
                UPDATE dbo.CursoEstudiante
                SET IdCurso = @IdCurso,
                    CodigoUsuario = @CodigoUsuario,
                    Status = @Status
                WHERE IdCursoEstudiante = @IdCursoEstudiante;
            ";

                    Db.Execute(sql,
                        new SqlParameter("@IdCurso", vm.Asignacion.IdCurso.Value),
                        new SqlParameter("@CodigoUsuario", vm.Asignacion.CodigoUsuario),
                        new SqlParameter("@Status", vm.Asignacion.Status),
                        new SqlParameter("@IdCursoEstudiante", vm.Asignacion.IdCursoEstudiante.Value)
                    );

                    TempData["Success"] = "Asignación actualizada correctamente.";
                }
                else
                {
                    if (vm.Asignacion.CodigosUsuarios == null || vm.Asignacion.CodigosUsuarios.Count == 0)
                    {
                        ModelState.AddModelError("", "Debe seleccionar al menos un estudiante.");
                        vm.Asignaciones = ObtenerAsignaciones(vm.Buscar);
                        ViewBag.AbrirModal = true;
                        return View("Index", vm);
                    }

                    string sql = @"
                IF NOT EXISTS (
                    SELECT 1 
                    FROM dbo.CursoEstudiante
                    WHERE IdCurso = @IdCurso 
                      AND CodigoUsuario = @CodigoUsuario
                )
                BEGIN
                    INSERT INTO dbo.CursoEstudiante 
                        (IdCurso, CodigoUsuario, FechaAsignacion, Status)
                    VALUES 
                        (@IdCurso, @CodigoUsuario, GETDATE(), @Status);
                END
                ELSE
                BEGIN
                    UPDATE dbo.CursoEstudiante
                    SET Status = @Status
                    WHERE IdCurso = @IdCurso 
                      AND CodigoUsuario = @CodigoUsuario;
                END
            ";

                    foreach (var codigoUsuario in vm.Asignacion.CodigosUsuarios)
                    {
                        Db.Execute(sql,
                            new SqlParameter("@IdCurso", vm.Asignacion.IdCurso.Value),
                            new SqlParameter("@CodigoUsuario", codigoUsuario),
                            new SqlParameter("@Status", vm.Asignacion.Status)
                        );
                    }

                    TempData["Success"] = "Curso asignado correctamente a los estudiantes seleccionados.";
                }

                return RedirectToAction("Index", new { buscar = vm.Buscar });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "No se pudo guardar la asignación: " + ex.Message;
                vm.Asignaciones = ObtenerAsignaciones(vm.Buscar);
                ViewBag.AbrirModal = true;
                return View("Index", vm);
            }
        }

        [HttpPost]
        public ActionResult Borrar(int id, string buscar = "")
        {
            if (Session["USER"] == null)
                return RedirectToAction("Login", "Auth");

           // Schema.EnsureCursoEstudiante();

            Db.Execute("UPDATE dbo.CursoEstudiante SET Status = 'INACTIVO' WHERE IdCursoEstudiante = @id",
                new SqlParameter("@id", id));

            TempData["Warning"] = "La asignación se desactivó correctamente.";
            return RedirectToAction("Index", new { buscar = buscar });
        }

        private CursoEstudianteViewModel NuevaAsignacionViewModel()
        {
            var asignacion = new CursoEstudianteViewModel
            {
                Status = "ACTIVO"
            };

            CargarCombos(asignacion);
            return asignacion;
        }

        private CursoEstudianteViewModel ObtenerAsignacionPorId(int id)
        {
            var asignacion = NuevaAsignacionViewModel();
            var dt = Db.Query(@"
                SELECT IdCursoEstudiante, IdCurso, CodigoUsuario, Status
                FROM dbo.CursoEstudiante
                WHERE IdCursoEstudiante = @id",
                new SqlParameter("@id", id));

            if (dt.Rows.Count != 1)
                return asignacion;

            var r = dt.Rows[0];
            asignacion.IdCursoEstudiante = Convert.ToInt32(r["IdCursoEstudiante"]);
            asignacion.IdCurso = Convert.ToInt32(r["IdCurso"]);
            asignacion.CodigoUsuario = r["CodigoUsuario"].ToString();
            asignacion.Status = r["Status"].ToString();

            return asignacion;
        }

        private void CargarCombos(CursoEstudianteViewModel asignacion)
        {
            asignacion.Cursos = ObtenerCursosSelect();
            asignacion.Estudiantes = ObtenerEstudiantesSelect();
            asignacion.StatusList = new List<SelectListItem>
            {
                new SelectListItem { Text = "-- Seleccione --", Value = "" },
                new SelectListItem { Text = "ACTIVO", Value = "ACTIVO" },
                new SelectListItem { Text = "INACTIVO", Value = "INACTIVO" }
            };
        }

        private List<SelectListItem> ObtenerCursosSelect()
        {
            var cursos = new List<SelectListItem>
            {
                new SelectListItem { Text = "-- Seleccione --", Value = "" }
            };

            var dt = Db.Query("SELECT IdCurso, Nombre FROM dbo.Cursos WHERE Status = 'ACTIVO' ORDER BY Nombre");
            foreach (DataRow r in dt.Rows)
            {
                cursos.Add(new SelectListItem
                {
                    Text = r["Nombre"].ToString(),
                    Value = r["IdCurso"].ToString()
                });
            }

            return cursos;
        }

        private List<SelectListItem> ObtenerEstudiantesSelect()
        {
            var estudiantes = new List<SelectListItem>
            {
                new SelectListItem { Text = "-- Seleccione --", Value = "" }
            };

            var dt = Db.Query(@"
                SELECT CodigoUsuario, NombreUsuario
                FROM dbo.Usuario
                WHERE Activo = 'true' AND Rol = 'Estudiante'
                ORDER BY NombreUsuario");

            foreach (DataRow r in dt.Rows)
            {
                estudiantes.Add(new SelectListItem
                {
                    Text = r["NombreUsuario"] + " (" + r["CodigoUsuario"] + ")",
                    Value = r["CodigoUsuario"].ToString()
                });
            }

            return estudiantes;
        }

        private List<CursoEstudianteListaViewModel> ObtenerAsignaciones(string filtro)
        {
            string sql = @"
                SELECT ce.IdCursoEstudiante,
                       ce.IdCurso,
                       c.Nombre AS CursoNombre,
                       ce.CodigoUsuario,
                       u.NombreUsuario,
                       ce.FechaAsignacion,
                       ce.Status
                FROM dbo.CursoEstudiante ce
                INNER JOIN dbo.Cursos c ON c.IdCurso = ce.IdCurso
                INNER JOIN dbo.Usuario u ON u.CodigoUsuario = ce.CodigoUsuario
                WHERE (@filtro = ''
                       OR c.Nombre LIKE '%' + @filtro + '%'
                       OR u.NombreUsuario LIKE '%' + @filtro + '%'
                       OR ce.CodigoUsuario LIKE '%' + @filtro + '%')
                ORDER BY ce.IdCursoEstudiante DESC;
            ";

            var dt = Db.Query(sql, new SqlParameter("@filtro", filtro ?? ""));
            var lista = new List<CursoEstudianteListaViewModel>();

            foreach (DataRow r in dt.Rows)
            {
                lista.Add(new CursoEstudianteListaViewModel
                {
                    IdCursoEstudiante = Convert.ToInt32(r["IdCursoEstudiante"]),
                    IdCurso = Convert.ToInt32(r["IdCurso"]),
                    CursoNombre = r["CursoNombre"].ToString(),
                    CodigoUsuario = r["CodigoUsuario"].ToString(),
                    NombreUsuario = r["NombreUsuario"].ToString(),
                    FechaAsignacion = Convert.ToDateTime(r["FechaAsignacion"]),
                    Status = r["Status"].ToString()
                });
            }

            return lista;
        }
    }
}
