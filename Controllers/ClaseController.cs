using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Web.Mvc;
using ControlAsistenciasCursosVirtuales.Helpers;
using ControlAsistenciasCursosVirtuales.Models;

namespace ControlAsistenciasCursosVirtuales.Controllers
{
    public class ClaseController : Controller
    {
        public ActionResult Index()
        {
            if (Session["USER"] == null)
                return RedirectToAction("Login", "Auth");

            // Obtener ID del alumno logueado
            int idAlumno = Convert.ToInt32(Session["IdAlumno"]);

            var vm = new IniciarClaseViewModel
            {
                IdAlumno = idAlumno,
                Cursos = ObtenerCursosAlumno(idAlumno)
            };

            return View(vm);
        }

        private List<IniciarClaseListaViewModel> ObtenerCursosAlumno(int idAlumno)
        {
            string sql = @"
                SELECT 
                    c.IdCurso,
                    c.TipoCurso,
                    c.Nombre,
                    m.Nombre AS MaestroNombre,
                    c.Status,
                    c.FechaHora,
                    c.DuracionHoras
                FROM dbo.Cursos c
                INNER JOIN dbo.Maestros m 
                    ON m.IdMaestro = c.IdMaestro
                INNER JOIN dbo.Inscripciones ac 
                    ON ac.IdCurso = c.IdCurso
                WHERE ac.IdEstudiante = @IdAlumno
                AND c.Status = 'ACTIVO'
                ORDER BY c.FechaHora DESC;
            ";

            var dt = Db.Query(sql,
                new SqlParameter("@IdAlumno", idAlumno));

            var lista = new List<IniciarClaseListaViewModel>();

            foreach (DataRow r in dt.Rows)
            {
                lista.Add(new IniciarClaseListaViewModel
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
        }
    }
}