using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Web.Mvc;
using ControlAsistenciasCursosVirtuales.Helpers;
using ControlAsistenciasCursosVirtuales.Models;
using ControlAsistenciasCursosVirtuales.Filters;

namespace ControlAsistenciasCursosVirtuales.Controllers
{
    [RoleAuthorize("Admin", "Maestro", "Estudiante")]
    public class AsistenciasController : Controller
    {
        public ActionResult Index(string buscar = "", int? idCursoFiltro = null)
        {
            if (Session["USER"] == null)
                return RedirectToAction("Login", "Auth");

           /* Schema.EnsureCursos();
            Schema.EnsureRegistroAsistencia();
            Schema.EnsureCursoEstudiante();*/

            var vm = new AsistenciasIndexViewModel
            {
                Buscar = buscar,
                IdCursoFiltro = idCursoFiltro,
                Asistencia = NuevaAsistenciaViewModel(),
                Asistencias = ObtenerAsistencias(buscar, idCursoFiltro),
                CursosFiltro = ObtenerCursosSelect(true)
            };

            return View(vm);
        }

        public ActionResult Confirmacion(int id)
        {
            if (Session["USER"] == null)
                return RedirectToAction("Login", "Auth");

           /* Schema.EnsureCursos();
            Schema.EnsureRegistroAsistencia();*/

            var vm = ObtenerConfirmacion(id);
            if (vm == null)
                return RedirectToAction("Index");

            return View(vm);
        }

        [HttpPost]
        public ActionResult Guardar(AsistenciasIndexViewModel vm)
        {
            if (Session["USER"] == null)
                return RedirectToAction("Login", "Auth");

            /*Schema.EnsureCursos();
            Schema.EnsureRegistroAsistencia();
            Schema.EnsureCursoEstudiante();*/
            CargarCombos(vm);

            if (!ModelState.IsValid)
            {
                vm.Asistencias = ObtenerAsistencias(vm.Buscar, vm.IdCursoFiltro);
                ViewBag.AbrirModal = true;
                return View("Index", vm);
            }

            if (EsEstudiante() && !CursoAsignadoAlUsuario(vm.Asistencia.IdCurso.Value))
            {
                ModelState.AddModelError("", "El curso seleccionado no está asignado a este estudiante.");
                vm.Asistencias = ObtenerAsistencias(vm.Buscar, vm.IdCursoFiltro);
                ViewBag.AbrirModal = true;
                return View("Index", vm);
            }

            int? idSesionAbierta = ObtenerSesionAbierta(Session["USER"].ToString(), vm.Asistencia.IdCurso.Value);
            if (idSesionAbierta.HasValue)
            {
                TempData["Warning"] = "Ya existe una sesión en curso para este usuario y curso.";
                return RedirectToAction("Confirmacion", new { id = idSesionAbierta.Value });
            }

            int minutosCursoAntes = ObtenerMinutosCurso(vm.Asistencia.IdCurso.Value);
            int minutosAcumuladosAntes = ObtenerMinutosAcumulados(Session["USER"].ToString(), vm.Asistencia.IdCurso.Value, null);
            if (minutosCursoAntes > 0 && minutosAcumuladosAntes >= minutosCursoAntes)
            {
                int? ultimaSesion = ObtenerUltimaSesion(Session["USER"].ToString(), vm.Asistencia.IdCurso.Value);
                TempData["Warning"] = "Este curso ya completó la duración requerida para este estudiante.";
                if (ultimaSesion.HasValue)
                    return RedirectToAction("Confirmacion", new { id = ultimaSesion.Value });
            }

            DateTime? fechaFinCurso = ObtenerFechaFinCurso(vm.Asistencia.IdCurso.Value);
            if (fechaFinCurso.HasValue && DateTime.Now > fechaFinCurso.Value)
            {
                ModelState.AddModelError("", "El periodo disponible para este curso ya finalizó.");
                vm.Asistencias = ObtenerAsistencias(vm.Buscar, vm.IdCursoFiltro);
                ViewBag.AbrirModal = true;
                return View("Index", vm);
            }

            decimal? latitud = ParseDecimal(vm.Asistencia.Latitud);
            decimal? longitud = ParseDecimal(vm.Asistencia.Longitud);

            string sql = @"
                INSERT INTO dbo.RegistroAsistencia
                (IdCurso, CodigoUsuario, NombreParticipante, Correo, FechaRegistro, FechaEntrada,
                 IpRegistro, IpPublica, Latitud, Longitud, EstadoAsistencia, Observacion)
                VALUES
                (@IdCurso, @CodigoUsuario, @NombreParticipante, @Correo, GETDATE(), GETDATE(),
                 @IpRegistro, @IpPublica, @Latitud, @Longitud, 'EN_CURSO', @Observacion);
                SELECT SCOPE_IDENTITY();
            ";

            object idObj;

            try
            {
                idObj = Db.Scalar(sql,
                    new SqlParameter("@IdCurso", vm.Asistencia.IdCurso.Value),
                    new SqlParameter("@CodigoUsuario", Session["USER"].ToString()),
                    new SqlParameter("@NombreParticipante", vm.Asistencia.NombreParticipante.Trim()),
                    new SqlParameter("@Correo", string.IsNullOrWhiteSpace(vm.Asistencia.Correo) ? (object)DBNull.Value : vm.Asistencia.Correo.Trim()),
                    new SqlParameter("@IpRegistro", ObtenerIp()),
                    new SqlParameter("@IpPublica", string.IsNullOrWhiteSpace(vm.Asistencia.IpPublica) ? (object)DBNull.Value : vm.Asistencia.IpPublica.Trim()),
                    new SqlParameter("@Latitud", latitud.HasValue ? (object)latitud.Value : DBNull.Value),
                    new SqlParameter("@Longitud", longitud.HasValue ? (object)longitud.Value : DBNull.Value),
                    new SqlParameter("@Observacion", string.IsNullOrWhiteSpace(vm.Asistencia.Observacion) ? (object)DBNull.Value : vm.Asistencia.Observacion.Trim())
                );
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2601 || ex.Number == 2627)
                {
                    idSesionAbierta = ObtenerSesionAbierta(Session["USER"].ToString(), vm.Asistencia.IdCurso.Value);
                    if (idSesionAbierta.HasValue)
                    {
                        TempData["Warning"] = "Ya existe una sesión en curso para este usuario y curso.";
                        return RedirectToAction("Confirmacion", new { id = idSesionAbierta.Value });
                    }
                }

                throw;
            }

            int idRegistro = Convert.ToInt32(idObj);
            TempData["Success"] = "Curso iniciado correctamente.";
            return RedirectToAction("Confirmacion", new { id = idRegistro });
        }

        [HttpPost]
        public ActionResult Salir(int id)
        {
            if (Session["USER"] == null)
                return RedirectToAction("Login", "Auth");

            /*Schema.EnsureCursos();
            Schema.EnsureRegistroAsistencia();*/

            var dt = Db.Query(@"
                SELECT ra.IdRegistro, ra.IdCurso, ra.CodigoUsuario, ra.FechaEntrada, ra.FechaSalida,
                       c.DuracionHoras
                FROM dbo.RegistroAsistencia ra
                INNER JOIN dbo.Cursos c ON c.IdCurso = ra.IdCurso
                WHERE ra.IdRegistro = @id
                  AND (@SoloUsuario = 0 OR ra.CodigoUsuario = @CodigoUsuario)",
                new SqlParameter("@id", id),
                new SqlParameter("@SoloUsuario", EsEstudiante() ? 1 : 0),
                new SqlParameter("@CodigoUsuario", Session["USER"].ToString()));

            if (dt.Rows.Count != 1)
                return RedirectToAction("Index");

            var row = dt.Rows[0];
            if (row["FechaSalida"] != DBNull.Value)
            {
                TempData["Warning"] = "Este curso ya tiene hora de salida registrada.";
                return RedirectToAction("Confirmacion", new { id = id });
            }

            DateTime entrada = row["FechaEntrada"] == DBNull.Value
                ? DateTime.Now
                : Convert.ToDateTime(row["FechaEntrada"]);

            DateTime salida = DateTime.Now;
            int duracionMinutos = Math.Max(0, Convert.ToInt32(Math.Round((salida - entrada).TotalMinutes)));
            int minutosCurso = Convert.ToInt32(Math.Round(Convert.ToDecimal(row["DuracionHoras"]) * 60));
            int idCurso = Convert.ToInt32(row["IdCurso"]);
            int acumuladoAnterior = ObtenerMinutosAcumulados(row["CodigoUsuario"].ToString(), idCurso, id);
            int restante = Math.Max(0, minutosCurso - acumuladoAnterior - duracionMinutos);

            Db.Execute(@"
                UPDATE dbo.RegistroAsistencia
                SET FechaSalida = @FechaSalida,
                    DuracionMinutos = @DuracionMinutos,
                    TiempoRestanteMinutos = @TiempoRestanteMinutos,
                    EstadoAsistencia = 'FINALIZADO'
                WHERE IdRegistro = @IdRegistro",
                new SqlParameter("@FechaSalida", salida),
                new SqlParameter("@DuracionMinutos", duracionMinutos),
                new SqlParameter("@TiempoRestanteMinutos", restante),
                new SqlParameter("@IdRegistro", id));

            TempData["Success"] = "Salida registrada correctamente.";
            return RedirectToAction("Confirmacion", new { id = id });
        }

        [HttpPost]
        public ActionResult Borrar(int id, string buscar = "", int? idCursoFiltro = null)
        {
            if (Session["USER"] == null)
                return RedirectToAction("Login", "Auth");

           /* Schema.EnsureCursos();
            Schema.EnsureRegistroAsistencia*/

            Db.Execute("DELETE FROM dbo.RegistroAsistencia WHERE IdRegistro = @IdRegistro",
                new SqlParameter("@IdRegistro", id));

            TempData["Success"] = "Registro de asistencia eliminado correctamente.";
            return RedirectToAction("Index", new { buscar = buscar, idCursoFiltro = idCursoFiltro });
        }

        private AsistenciaViewModel NuevaAsistenciaViewModel()
        {
            return new AsistenciaViewModel
            {
                NombreParticipante = Session["USER_NAME"] == null ? "" : Session["USER_NAME"].ToString(),
                Cursos = ObtenerCursosSelect(false)
            };
        }

        private void CargarCombos(AsistenciasIndexViewModel vm)
        {
            if (vm.Asistencia == null)
                vm.Asistencia = NuevaAsistenciaViewModel();

            vm.Asistencia.Cursos = ObtenerCursosSelect(false);
            vm.CursosFiltro = ObtenerCursosSelect(true);
        }

        private List<SelectListItem> ObtenerCursosSelect(bool incluirTodos)
        {
            var cursos = new List<SelectListItem>();

            if (incluirTodos)
                cursos.Add(new SelectListItem { Text = "-- Todos los cursos --", Value = "" });
            else
                cursos.Add(new SelectListItem { Text = "-- Seleccione --", Value = "" });

            string sql;
            SqlParameter[] parameters;

            if (EsEstudiante())
            {
                sql = @"
                    SELECT c.IdCurso, c.Nombre
                    FROM dbo.Cursos c
                    INNER JOIN dbo.CursoEstudiante ce ON ce.IdCurso = c.IdCurso
                    WHERE c.Status = 'ACTIVO'
                      AND ce.Status = 'ACTIVO'
                      AND ce.CodigoUsuario = @CodigoUsuario
                    ORDER BY c.FechaHora DESC, c.Nombre;
                ";

                parameters = new[] { new SqlParameter("@CodigoUsuario", Session["USER"].ToString()) };
            }
            else
            {
                sql = @"
                    SELECT IdCurso, Nombre
                    FROM dbo.Cursos
                    WHERE Status = 'ACTIVO'
                    ORDER BY FechaHora DESC, Nombre;
                ";

                parameters = new SqlParameter[0];
            }

            var dt = Db.Query(sql, parameters);
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

        private List<AsistenciaListaViewModel> ObtenerAsistencias(string filtro, int? idCursoFiltro)
        {
            string sql = @"
                SELECT TOP 100
                       ra.IdRegistro,
                       c.Nombre AS CursoNombre,
                       c.FechaFin AS CursoFechaFin,
                       ra.NombreParticipante,
                       ra.Correo,
                       ra.FechaRegistro,
                       ra.FechaEntrada,
                       ra.FechaSalida,
                       ra.DuracionMinutos,
                       ra.TiempoRestanteMinutos,
                       ra.EstadoAsistencia,
                       ra.IpRegistro,
                       ra.IpPublica,
                       ra.Latitud,
                       ra.Longitud,
                       ra.Observacion
                FROM dbo.RegistroAsistencia ra
                INNER JOIN dbo.Cursos c ON c.IdCurso = ra.IdCurso
                WHERE (@filtro = ''
                       OR ra.NombreParticipante LIKE '%' + @filtro + '%'
                       OR ISNULL(ra.Correo, '') LIKE '%' + @filtro + '%'
                       OR c.Nombre LIKE '%' + @filtro + '%')
                  AND (@IdCurso IS NULL OR ra.IdCurso = @IdCurso)
                  AND (@SoloUsuario = 0 OR ra.CodigoUsuario = @CodigoUsuario)
                ORDER BY ra.FechaRegistro DESC, ra.IdRegistro DESC;
            ";

            var dt = Db.Query(sql,
                new SqlParameter("@filtro", filtro ?? ""),
                new SqlParameter("@IdCurso", idCursoFiltro.HasValue ? (object)idCursoFiltro.Value : DBNull.Value),
                new SqlParameter("@SoloUsuario", EsEstudiante() ? 1 : 0),
                new SqlParameter("@CodigoUsuario", Session["USER"].ToString())
            );

            var lista = new List<AsistenciaListaViewModel>();
            foreach (DataRow r in dt.Rows)
            {
                lista.Add(MapAsistenciaLista(r));
            }

            return lista;
        }

        private AsistenciaConfirmacionViewModel ObtenerConfirmacion(int id)
        {
            var dt = Db.Query(@"
                SELECT ra.IdRegistro,
                       c.Nombre AS CursoNombre,
                       c.FechaFin AS CursoFechaFin,
                       ra.NombreParticipante,
                       ra.Correo,
                       ra.FechaRegistro,
                       ra.FechaEntrada,
                       ra.FechaSalida,
                       ra.DuracionMinutos,
                       ra.TiempoRestanteMinutos,
                       ra.EstadoAsistencia,
                       ra.IpRegistro,
                       ra.IpPublica,
                       ra.Latitud,
                       ra.Longitud,
                       ra.Observacion
                FROM dbo.RegistroAsistencia ra
                INNER JOIN dbo.Cursos c ON c.IdCurso = ra.IdCurso
                WHERE ra.IdRegistro = @id
                  AND (@SoloUsuario = 0 OR ra.CodigoUsuario = @CodigoUsuario)",
                new SqlParameter("@id", id),
                new SqlParameter("@SoloUsuario", EsEstudiante() ? 1 : 0),
                new SqlParameter("@CodigoUsuario", Session["USER"].ToString()));

            if (dt.Rows.Count != 1)
                return null;

            var r = dt.Rows[0];
            return new AsistenciaConfirmacionViewModel
            {
                IdRegistro = Convert.ToInt32(r["IdRegistro"]),
                CursoNombre = r["CursoNombre"].ToString(),
                CursoFechaFin = r["CursoFechaFin"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["CursoFechaFin"]),
                NombreParticipante = r["NombreParticipante"].ToString(),
                Correo = r["Correo"] == DBNull.Value ? "" : r["Correo"].ToString(),
                FechaRegistro = Convert.ToDateTime(r["FechaRegistro"]),
                IpRegistro = r["IpRegistro"] == DBNull.Value ? "" : r["IpRegistro"].ToString(),
                IpPublica = r["IpPublica"] == DBNull.Value ? "" : r["IpPublica"].ToString(),
                Latitud = r["Latitud"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(r["Latitud"]),
                Longitud = r["Longitud"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(r["Longitud"]),
                FechaEntrada = r["FechaEntrada"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["FechaEntrada"]),
                FechaSalida = r["FechaSalida"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["FechaSalida"]),
                DuracionMinutos = r["DuracionMinutos"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["DuracionMinutos"]),
                TiempoRestanteMinutos = r["TiempoRestanteMinutos"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["TiempoRestanteMinutos"]),
                EstadoAsistencia = r["EstadoAsistencia"] == DBNull.Value ? "" : r["EstadoAsistencia"].ToString(),
                Observacion = r["Observacion"] == DBNull.Value ? "" : r["Observacion"].ToString()
            };
        }

        private AsistenciaListaViewModel MapAsistenciaLista(DataRow r)
        {
            return new AsistenciaListaViewModel
            {
                IdRegistro = Convert.ToInt32(r["IdRegistro"]),
                CursoNombre = r["CursoNombre"].ToString(),
                CursoFechaFin = r["CursoFechaFin"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["CursoFechaFin"]),
                NombreParticipante = r["NombreParticipante"].ToString(),
                Correo = r["Correo"] == DBNull.Value ? "" : r["Correo"].ToString(),
                FechaRegistro = Convert.ToDateTime(r["FechaRegistro"]),
                IpRegistro = r["IpRegistro"] == DBNull.Value ? "" : r["IpRegistro"].ToString(),
                IpPublica = r["IpPublica"] == DBNull.Value ? "" : r["IpPublica"].ToString(),
                Latitud = r["Latitud"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(r["Latitud"]),
                Longitud = r["Longitud"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(r["Longitud"]),
                FechaEntrada = r["FechaEntrada"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["FechaEntrada"]),
                FechaSalida = r["FechaSalida"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["FechaSalida"]),
                DuracionMinutos = r["DuracionMinutos"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["DuracionMinutos"]),
                TiempoRestanteMinutos = r["TiempoRestanteMinutos"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["TiempoRestanteMinutos"]),
                EstadoAsistencia = r["EstadoAsistencia"] == DBNull.Value ? "" : r["EstadoAsistencia"].ToString(),
                Observacion = r["Observacion"] == DBNull.Value ? "" : r["Observacion"].ToString()
            };
        }

        private bool CursoAsignadoAlUsuario(int idCurso)
        {
            object obj = Db.Scalar(@"
                SELECT COUNT(1)
                FROM dbo.CursoEstudiante
                WHERE IdCurso = @IdCurso
                  AND CodigoUsuario = @CodigoUsuario
                  AND Status = 'ACTIVO'",
                new SqlParameter("@IdCurso", idCurso),
                new SqlParameter("@CodigoUsuario", Session["USER"].ToString()));

            return obj != null && obj != DBNull.Value && Convert.ToInt32(obj) > 0;
        }

        private int? ObtenerSesionAbierta(string codigoUsuario, int idCurso)
        {
            object obj = Db.Scalar(@"
                SELECT TOP 1 IdRegistro
                FROM dbo.RegistroAsistencia
                WHERE CodigoUsuario = @CodigoUsuario
                  AND IdCurso = @IdCurso
                  AND FechaSalida IS NULL
                ORDER BY FechaRegistro DESC, IdRegistro DESC",
                new SqlParameter("@CodigoUsuario", codigoUsuario),
                new SqlParameter("@IdCurso", idCurso));

            if (obj == null || obj == DBNull.Value)
                return null;

            return Convert.ToInt32(obj);
        }

        private int? ObtenerUltimaSesion(string codigoUsuario, int idCurso)
        {
            object obj = Db.Scalar(@"
                SELECT TOP 1 IdRegistro
                FROM dbo.RegistroAsistencia
                WHERE CodigoUsuario = @CodigoUsuario
                  AND IdCurso = @IdCurso
                ORDER BY FechaRegistro DESC, IdRegistro DESC",
                new SqlParameter("@CodigoUsuario", codigoUsuario),
                new SqlParameter("@IdCurso", idCurso));

            if (obj == null || obj == DBNull.Value)
                return null;

            return Convert.ToInt32(obj);
        }

        private int ObtenerMinutosCurso(int idCurso)
        {
            object obj = Db.Scalar(@"
                SELECT DuracionHoras
                FROM dbo.Cursos
                WHERE IdCurso = @IdCurso",
                new SqlParameter("@IdCurso", idCurso));

            if (obj == null || obj == DBNull.Value)
                return 0;

            return Convert.ToInt32(Math.Round(Convert.ToDecimal(obj) * 60));
        }

        private DateTime? ObtenerFechaFinCurso(int idCurso)
        {
            object obj = Db.Scalar(@"
                SELECT FechaFin
                FROM dbo.Cursos
                WHERE IdCurso = @IdCurso",
                new SqlParameter("@IdCurso", idCurso));

            if (obj == null || obj == DBNull.Value)
                return null;

            return Convert.ToDateTime(obj);
        }

        private int ObtenerMinutosAcumulados(string codigoUsuario, int idCurso, int? excluirIdRegistro)
        {
            object obj = Db.Scalar(@"
                SELECT ISNULL(SUM(ISNULL(DuracionMinutos, 0)), 0)
                FROM dbo.RegistroAsistencia
                WHERE CodigoUsuario = @CodigoUsuario
                  AND IdCurso = @IdCurso
                  AND (@ExcluirId IS NULL OR IdRegistro <> @ExcluirId)",
                new SqlParameter("@CodigoUsuario", codigoUsuario),
                new SqlParameter("@IdCurso", idCurso),
                new SqlParameter("@ExcluirId", excluirIdRegistro.HasValue ? (object)excluirIdRegistro.Value : DBNull.Value));

            if (obj == null || obj == DBNull.Value)
                return 0;

            return Convert.ToInt32(obj);
        }

        private bool EsEstudiante()
        {
            return Session["ROLE"] != null && Session["ROLE"].ToString() == "Estudiante";
        }

        private decimal? ParseDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            decimal result;
            if (decimal.TryParse(value.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out result))
                return result;

            return null;
        }

        private string ObtenerIp()
        {
            string forwarded = Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (!string.IsNullOrWhiteSpace(forwarded))
                return forwarded.Split(',')[0].Trim();

            return Request.UserHostAddress;
        }
    }
}
