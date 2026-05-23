using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Web.Mvc;
using ControlAsistenciasCursosVirtuales.Helpers;
using ControlAsistenciasCursosVirtuales.Models;
using ControlAsistenciasCursosVirtuales.Filters;
using System.Linq;

namespace ControlAsistenciasCursosVirtuales.Controllers
{
    [RoleAuthorize("Admin", "Maestro")]
    public class CursosController : Controller
    {
        public ActionResult Index(string buscar = "", string mensaje = "")
        {
            if (Session["USER"] == null)
                return RedirectToAction("Login", "Auth");

            var vm = new CursosIndexViewModel
            {
                Buscar = buscar,
                Mensaje = mensaje,
                Cursos = ObtenerCursos(buscar),
                Curso = NuevoCursoViewModel()
            };

            return View(vm);
        }

        [HttpGet]
        public ActionResult Editar(int id, string buscar = "")
        {
            if (Session["USER"] == null)
                return RedirectToAction("Login", "Auth");

            var vm = new CursosIndexViewModel
            {
                Buscar = buscar,
                Cursos = ObtenerCursos(buscar),
                Curso = ObtenerCursoPorId(id)
            };

            ViewBag.AbrirModal = true;
            return View("Index", vm);
        }

        [HttpPost]
        public ActionResult Guardar(CursosIndexViewModel vm)
        {
            if (Session["USER"] == null)
                return RedirectToAction("Login", "Auth");

            CargarCombos(vm.Curso);

            if (!ModelState.IsValid)
            {
                vm.Cursos = ObtenerCursos(vm.Buscar);
                ViewBag.AbrirModal = true;
                return View("Index", vm);
            }

            DateTime fechaHora;
            if (!TryBuildFechaHora(vm.Curso.Fecha, vm.Curso.Hora, out fechaHora))
            {
                ModelState.AddModelError("", "Fecha/Hora inválida.");
                vm.Cursos = ObtenerCursos(vm.Buscar);
                ViewBag.AbrirModal = true;
                return View("Index", vm);
            }

            DateTime fechaFin;
            if (!DateTime.TryParseExact(
                vm.Curso.FechaFin,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out fechaFin))
            {
                ModelState.AddModelError("", "Fecha de finalización inválida.");
                vm.Cursos = ObtenerCursos(vm.Buscar);
                ViewBag.AbrirModal = true;
                return View("Index", vm);
            }

            decimal duracion;
            if (!decimal.TryParse(vm.Curso.DuracionHoras, NumberStyles.Any, CultureInfo.InvariantCulture, out duracion) &&
                !decimal.TryParse(vm.Curso.DuracionHoras, NumberStyles.Any, CultureInfo.CurrentCulture, out duracion))
            {
                ModelState.AddModelError("", "Duración inválida. Ejemplo: 2 o 1.5");
                vm.Cursos = ObtenerCursos(vm.Buscar);
                ViewBag.AbrirModal = true;
                return View("Index", vm);
            }

                fechaFin = fechaFin.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

                if (fechaFin < fechaHora)
                {
                    ModelState.AddModelError("", "La fecha de finalización debe ser mayor o igual a la fecha de inicio.");
                    vm.Cursos = ObtenerCursos(vm.Buscar);
                    ViewBag.AbrirModal = true;
                    return View("Index", vm);
                }

                if (vm.Curso.IdCurso.HasValue)
            {
                string sql = @"
                    UPDATE dbo.Cursos
                    SET TipoCurso=@Tipo, Nombre=@Nombre, Descripcion=@Desc, IdMaestro=@IdMaestro,
                        Status=@Status, FechaHora=@FechaHora, FechaFin=@FechaFin, DuracionHoras=@Dur
                    WHERE IdCurso=@IdCurso;
                ";

                Db.Execute(sql,
                    new SqlParameter("@Tipo", vm.Curso.TipoCurso),
                    new SqlParameter("@Nombre", vm.Curso.Nombre.Trim()),
                    new SqlParameter("@Desc", vm.Curso.Descripcion.Trim()),
                    new SqlParameter("@IdMaestro", vm.Curso.IdMaestro.Value),
                    new SqlParameter("@Status", vm.Curso.Status),
                    new SqlParameter("@FechaHora", fechaHora),
                    new SqlParameter("@FechaFin", fechaFin),
                    new SqlParameter("@Dur", duracion),
                    new SqlParameter("@IdCurso", vm.Curso.IdCurso.Value)
                );

                TempData["Success"] = vm.Curso.IdCurso.HasValue
                ? "Curso actualizado correctamente."
                : "Curso creado correctamente.";

                return RedirectToAction("Index", new { buscar = vm.Buscar });

           
            }
            else
            {
                string sql = @"
                    INSERT INTO dbo.Cursos 
                    (TipoCurso, Nombre, Descripcion, IdMaestro, Status, FechaHora, FechaFin, DuracionHoras)
                    VALUES 
                    (@Tipo, @Nombre, @Desc, @IdMaestro, @Status, @FechaHora, @FechaFin, @Dur);
                ";

                Db.Execute(sql,
                    new SqlParameter("@Tipo", vm.Curso.TipoCurso),
                    new SqlParameter("@Nombre", vm.Curso.Nombre.Trim()),
                    new SqlParameter("@Desc", vm.Curso.Descripcion.Trim()),
                    new SqlParameter("@IdMaestro", vm.Curso.IdMaestro.Value),
                    new SqlParameter("@Status", vm.Curso.Status),
                    new SqlParameter("@FechaHora", fechaHora),
                    new SqlParameter("@FechaFin", fechaFin),
                    new SqlParameter("@Dur", duracion)
                );

                TempData["Success"] = vm.Curso.IdCurso.HasValue
                ? "Curso actualizado correctamente."
                : "Curso creado correctamente.";

                return RedirectToAction("Index", new { buscar = vm.Buscar });
            }
        }

        [HttpPost]
        public ActionResult Borrar(int id, string buscar = "")
        {
            if (Session["USER"] == null)
                return RedirectToAction("Login", "Auth");

            string sqlCount = @"SELECT COUNT(1) FROM dbo.RegistroAsistencia WHERE IdCurso = @IdCurso;";
            object obj = Db.Scalar(sqlCount, new SqlParameter("@IdCurso", id));

            int totalAsistencias = 0;
            if (obj != null && obj != DBNull.Value)
                totalAsistencias = Convert.ToInt32(obj);


            if (totalAsistencias > 0)
            {
                string sqlUpdate = @"UPDATE dbo.Cursos SET Status = 'INACTIVO' WHERE IdCurso = @IdCurso;";
                Db.Execute(sqlUpdate, new SqlParameter("@IdCurso", id));
                TempData["Warning"] = "El curso tiene asistencias, se desactivó.";
            }
            else
            {
                string sqlDelete = @"DELETE FROM dbo.Cursos WHERE IdCurso = @IdCurso;";
                Db.Execute(sqlDelete, new SqlParameter("@IdCurso", id));
                TempData["Success"] = "Curso eliminado correctamente.";
            }
            return RedirectToAction("Index", new { buscar = buscar });
        }

        /*private List<CursoListaViewModel> ObtenerCursos(string filtro = "")
        {
            string sql = @"
                SELECT c.IdCurso, c.TipoCurso, c.Nombre,
                       m.Nombre AS MaestroNombre,
                       c.Status, c.FechaHora, c.DuracionHoras
                FROM dbo.Cursos c
                INNER JOIN dbo.Maestros m ON m.IdMaestro = c.IdMaestro
                WHERE (@filtro='' OR c.Nombre LIKE '%' + @filtro + '%')
                ORDER BY c.IdCurso DESC;
            ";

            var dt = Db.Query(sql, new SqlParameter("@filtro", filtro ?? ""));
            var lista = new List<CursoListaViewModel>();

            foreach (DataRow r in dt.Rows)
            {
                lista.Add(new CursoListaViewModel
                {
                    IdCurso = Convert.ToInt32(r["IdCurso"]),
                    TipoCurso = r["TipoCurso"].ToString(),
                    Nombre = r["Nombre"].ToString(),
                    MaestroNombre = r["MaestroNombre"].ToString(),
                    Status = r["Status"].ToString(),
                    FechaHora = Convert.ToDateTime(r["FechaHora"]),
                    DuracionHoras = Convert.ToDecimal(r["DuracionHoras"])
                });
            }

            return lista;
        }*/

        private List<CursoListaViewModel> ObtenerCursos(string filtro = "")
        {
                    string sql = @"
                SELECT c.IdCurso, c.TipoCurso, c.Nombre,
                       m.Nombre AS MaestroNombre,
                       c.Status, c.FechaHora, c.FechaFin, c.DuracionHoras
                FROM dbo.Cursos c
                INNER JOIN dbo.Maestros m ON m.IdMaestro = c.IdMaestro
                WHERE (@filtro='' OR c.Nombre LIKE '%' + @filtro + '%')
                ORDER BY c.IdCurso DESC;
            ";

            var dt = Db.Query(sql, new SqlParameter("@filtro", filtro ?? ""));

            var lista = dt.AsEnumerable()
                .Select(r => new CursoListaViewModel
                {
                    IdCurso = Convert.ToInt32(r["IdCurso"]),
                    TipoCurso = r["TipoCurso"].ToString(),
                    Nombre = r["Nombre"].ToString(),
                    MaestroNombre = r["MaestroNombre"].ToString(),
                    Status = r["Status"].ToString(),
                    FechaHora = Convert.ToDateTime(r["FechaHora"]),
                    FechaFin = Convert.ToDateTime(r["FechaFin"]),
                    DuracionHoras = Convert.ToDecimal(r["DuracionHoras"])
                })
                .ToList();

            return lista;
        }

        private CursoViewModel ObtenerCursoPorId(int id)
        {
            var curso = NuevoCursoViewModel();

            var dt = Db.Query("SELECT * FROM dbo.Cursos WHERE IdCurso=@id",
                new SqlParameter("@id", id));

            if (dt.Rows.Count != 1)
                return curso;

            var r = dt.Rows[0];
            DateTime fh = Convert.ToDateTime(r["FechaHora"]);
            DateTime ff = Convert.ToDateTime(r["FechaFin"]);

            curso.IdCurso = id;
            curso.TipoCurso = r["TipoCurso"].ToString();
            curso.Status = r["Status"].ToString();
            curso.Nombre = r["Nombre"].ToString();
            curso.Descripcion = r["Descripcion"] == DBNull.Value ? "" : r["Descripcion"].ToString();
            curso.IdMaestro = Convert.ToInt32(r["IdMaestro"]);
            curso.Fecha = fh.ToString("yyyy-MM-dd");
            curso.Hora = fh.ToString("HH:mm");
            curso.FechaFin = ff.ToString("yyyy-MM-dd");
            curso.DuracionHoras = Convert.ToDecimal(r["DuracionHoras"]).ToString(CultureInfo.InvariantCulture);

            CargarCombos(curso);
            return curso;
        }

        private CursoViewModel NuevoCursoViewModel()
        {
            var curso = new CursoViewModel();
            CargarCombos(curso);
            return curso;
        }

        private void CargarCombos(CursoViewModel curso)
        {
            curso.TiposCurso = new List<SelectListItem>
            {
                new SelectListItem { Text = "-- Seleccione --", Value = "" },
                new SelectListItem { Text = "Virtual", Value = "Virtual" }
               /* new SelectListItem { Text = "Presencial", Value = "Presencial" },
                new SelectListItem { Text = "Mixto", Value = "Mixto" }*/
            };

            curso.StatusList = new List<SelectListItem>
            {
                new SelectListItem { Text = "-- Seleccione --", Value = "" },
                new SelectListItem { Text = "ACTIVO", Value = "ACTIVO" },
                new SelectListItem { Text = "INACTIVO", Value = "INACTIVO" }
            };

            curso.Maestros = new List<SelectListItem>
            {
                new SelectListItem { Text = "-- Seleccione --", Value = "" }
            };

            var dtM = Db.Query("SELECT IdMaestro, Nombre FROM dbo.Maestros WHERE Status='ACTIVO' ORDER BY Nombre");

            /*foreach (DataRow r in dtM.Rows)
            {
                curso.Maestros.Add(new SelectListItem
                {
                    Text = r["Nombre"].ToString(),
                    Value = r["IdMaestro"].ToString()
                });
            }*/
            var maestros = dtM.AsEnumerable()
                .Select(r => new SelectListItem
                {
                    Text = r["Nombre"].ToString(),
                    Value = r["IdMaestro"].ToString()
                })
                .ToList();

            curso.Maestros.AddRange(maestros);
        }

        private bool TryBuildFechaHora(string fecha, string hora, out DateTime result)
        {
            result = DateTime.MinValue;

            if (string.IsNullOrWhiteSpace(fecha) || string.IsNullOrWhiteSpace(hora))
                return false;

            var s = fecha.Trim() + " " + hora.Trim();

            return DateTime.TryParseExact(
                s,
                "yyyy-MM-dd HH:mm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out result
            );
        }
    }
}