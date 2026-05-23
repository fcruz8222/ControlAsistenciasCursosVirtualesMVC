using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Web.Mvc;
using ControlAsistenciasCursosVirtuales.Helpers;
using ControlAsistenciasCursosVirtuales.Models;

namespace ControlAsistenciasCursosVirtuales.Controllers
{
    public class EstadisticasController : Controller
    {
        public ActionResult Index(int? idCurso = null, string fechaInicio = "", string fechaFin = "")
        {
            if (Session["USER"] == null)
                return RedirectToAction("Login", "Auth");


            DateTime? inicio = ParseFecha(fechaInicio);
            DateTime? fin = ParseFecha(fechaFin);

            var vm = new EstadisticasIndexViewModel
            {
                IdCurso = idCurso,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                Cursos = ObtenerCursosSelect(),
                PorCurso = ObtenerEstadisticasPorCurso(idCurso, inicio, fin),
                Recientes = ObtenerRecientes(idCurso, inicio, fin)
            };

            CargarTotales(vm, idCurso, inicio, fin);
            return View(vm);
        }

        private void CargarTotales(EstadisticasIndexViewModel vm, int? idCurso, DateTime? inicio, DateTime? fin)
        {
            string sql = @"
                SELECT COUNT(1) AS TotalAsistencias,
                       COUNT(DISTINCT IdCurso) AS TotalCursos,
                       COUNT(DISTINCT ISNULL(NULLIF(LTRIM(RTRIM(Correo)), ''), NombreParticipante)) AS TotalParticipantes
                FROM dbo.RegistroAsistencia
                WHERE (@IdCurso IS NULL OR IdCurso = @IdCurso)
                  AND (@Inicio IS NULL OR FechaRegistro >= @Inicio)
                  AND (@Fin IS NULL OR FechaRegistro < DATEADD(day, 1, @Fin));
            ";

            var dt = Db.Query(sql,
                new SqlParameter("@IdCurso", idCurso.HasValue ? (object)idCurso.Value : DBNull.Value),
                new SqlParameter("@Inicio", inicio.HasValue ? (object)inicio.Value : DBNull.Value),
                new SqlParameter("@Fin", fin.HasValue ? (object)fin.Value : DBNull.Value)
            );

            if (dt.Rows.Count == 0)
                return;

            var r = dt.Rows[0];
            vm.TotalAsistencias = Convert.ToInt32(r["TotalAsistencias"]);
            vm.TotalCursosConAsistencia = Convert.ToInt32(r["TotalCursos"]);
            vm.TotalParticipantes = Convert.ToInt32(r["TotalParticipantes"]);
        }

        private List<EstadisticaCursoViewModel> ObtenerEstadisticasPorCurso(int? idCurso, DateTime? inicio, DateTime? fin)
        {
            string sql = @"
                WITH Asignados AS (
                    SELECT
                        c.IdCurso,
                        ce.CodigoUsuario
                    FROM dbo.Cursos c
                    LEFT JOIN dbo.CursoEstudiante ce
                        ON ce.IdCurso = c.IdCurso
                       AND ce.Status = 'ACTIVO'
                    WHERE (@IdCurso IS NULL OR c.IdCurso = @IdCurso)
                ),
                TiempoPorEstudiante AS (
                    SELECT
                        ra.IdCurso,
                        ra.CodigoUsuario,
                        SUM(ISNULL(ra.DuracionMinutos, 0)) AS MinutosAcumulados,
                        COUNT(ra.IdRegistro) AS TotalAsistencias,
                        MAX(ra.FechaRegistro) AS UltimaAsistencia
                    FROM dbo.RegistroAsistencia ra
                    WHERE (@Inicio IS NULL OR ra.FechaRegistro >= @Inicio)
                      AND (@Fin IS NULL OR ra.FechaRegistro < DATEADD(day, 1, @Fin))
                    GROUP BY ra.IdCurso, ra.CodigoUsuario
                )
                SELECT
                       c.IdCurso,
                       c.Nombre AS CursoNombre,
                       m.Nombre AS MaestroNombre,
                       c.DuracionHoras,
                       COUNT(DISTINCT a.CodigoUsuario) AS TotalAsignados,
                       COUNT(DISTINCT t.CodigoUsuario) AS EstudiantesConAsistencia,
                       ISNULL(SUM(t.TotalAsistencias), 0) AS TotalAsistencias,
                       SUM(CASE
                            WHEN ISNULL(t.MinutosAcumulados, 0) >= (c.DuracionHoras * 60)
                            THEN 1 ELSE 0
                           END) AS EstudiantesCompletaron,
                       CAST(
                           CASE
                               WHEN COUNT(DISTINCT a.CodigoUsuario) = 0 THEN 0
                               ELSE AVG(CASE
                                   WHEN ISNULL(t.MinutosAcumulados, 0) >= (c.DuracionHoras * 60) THEN 100.0
                                   ELSE (ISNULL(t.MinutosAcumulados, 0) * 100.0) / NULLIF((c.DuracionHoras * 60), 0)
                               END)
                           END AS DECIMAL(10,2)
                       ) AS AvancePromedio,
                       MAX(t.UltimaAsistencia) AS UltimaAsistencia
                FROM dbo.Cursos c
                INNER JOIN dbo.Maestros m ON m.IdMaestro = c.IdMaestro
                LEFT JOIN Asignados a ON a.IdCurso = c.IdCurso
                LEFT JOIN TiempoPorEstudiante t
                    ON t.IdCurso = c.IdCurso
                   AND t.CodigoUsuario = a.CodigoUsuario
                WHERE (@IdCurso IS NULL OR c.IdCurso = @IdCurso)
                GROUP BY c.IdCurso, c.Nombre, m.Nombre, c.DuracionHoras
                ORDER BY AvancePromedio DESC, c.Nombre;
            ";

            var dt = Db.Query(sql,
                new SqlParameter("@IdCurso", idCurso.HasValue ? (object)idCurso.Value : DBNull.Value),
                new SqlParameter("@Inicio", inicio.HasValue ? (object)inicio.Value : DBNull.Value),
                new SqlParameter("@Fin", fin.HasValue ? (object)fin.Value : DBNull.Value)
            );

            var lista = new List<EstadisticaCursoViewModel>();
            foreach (DataRow r in dt.Rows)
            {
                lista.Add(new EstadisticaCursoViewModel
                {
                    IdCurso = Convert.ToInt32(r["IdCurso"]),
                    CursoNombre = r["CursoNombre"].ToString(),
                    MaestroNombre = r["MaestroNombre"].ToString(),
                    HorasRequeridas = Convert.ToDecimal(r["DuracionHoras"]),
                    TotalAsignados = Convert.ToInt32(r["TotalAsignados"]),
                    EstudiantesConAsistencia = Convert.ToInt32(r["EstudiantesConAsistencia"]),
                    TotalAsistencias = Convert.ToInt32(r["TotalAsistencias"]),
                    EstudiantesCompletaron = Convert.ToInt32(r["EstudiantesCompletaron"]),
                    AvancePromedio = Convert.ToDecimal(r["AvancePromedio"]),
                    UltimaAsistencia = r["UltimaAsistencia"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["UltimaAsistencia"])
                });
            }

            return lista;
        }

        private List<AsistenciaListaViewModel> ObtenerRecientes(int? idCurso, DateTime? inicio, DateTime? fin)
        {
            string sql = @"
                SELECT TOP 10
                       ra.IdRegistro,
                       c.Nombre AS CursoNombre,
                       ra.NombreParticipante,
                       ra.Correo,
                       ra.FechaRegistro,
                       ra.IpPublica,
                       ra.Observacion
                FROM dbo.RegistroAsistencia ra
                INNER JOIN dbo.Cursos c ON c.IdCurso = ra.IdCurso
                WHERE (@IdCurso IS NULL OR ra.IdCurso = @IdCurso)
                  AND (@Inicio IS NULL OR ra.FechaRegistro >= @Inicio)
                  AND (@Fin IS NULL OR ra.FechaRegistro < DATEADD(day, 1, @Fin))
                ORDER BY ra.FechaRegistro DESC, ra.IdRegistro DESC;
            ";

            var dt = Db.Query(sql,
                new SqlParameter("@IdCurso", idCurso.HasValue ? (object)idCurso.Value : DBNull.Value),
                new SqlParameter("@Inicio", inicio.HasValue ? (object)inicio.Value : DBNull.Value),
                new SqlParameter("@Fin", fin.HasValue ? (object)fin.Value : DBNull.Value)
            );

            var lista = new List<AsistenciaListaViewModel>();
            foreach (DataRow r in dt.Rows)
            {
                lista.Add(new AsistenciaListaViewModel
                {
                    IdRegistro = Convert.ToInt32(r["IdRegistro"]),
                    CursoNombre = r["CursoNombre"].ToString(),
                    NombreParticipante = r["NombreParticipante"].ToString(),
                    Correo = r["Correo"] == DBNull.Value ? "" : r["Correo"].ToString(),
                    FechaRegistro = Convert.ToDateTime(r["FechaRegistro"]),
                    IpPublica = r["IpPublica"] == DBNull.Value ? "" : r["IpPublica"].ToString(),
                    Observacion = r["Observacion"] == DBNull.Value ? "" : r["Observacion"].ToString()
                });
            }

            return lista;
        }

        private List<SelectListItem> ObtenerCursosSelect()
        {
            var cursos = new List<SelectListItem>
            {
                new SelectListItem { Text = "-- Todos los cursos --", Value = "" }
            };

            var dt = Db.Query("SELECT IdCurso, Nombre FROM dbo.Cursos ORDER BY Nombre");
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

        private DateTime? ParseFecha(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            DateTime fecha;
            if (DateTime.TryParseExact(value.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha))
                return fecha;

            return null;
        }
    }
}
