using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace ControlAsistenciasCursosVirtuales.Models
{
    public class EstadisticasIndexViewModel
    {
        public int? IdCurso { get; set; }
        public string FechaInicio { get; set; }
        public string FechaFin { get; set; }
        public int TotalAsistencias { get; set; }
        public int TotalCursosConAsistencia { get; set; }
        public int TotalParticipantes { get; set; }
        public List<SelectListItem> Cursos { get; set; }
        public List<EstadisticaCursoViewModel> PorCurso { get; set; }
        public List<AsistenciaListaViewModel> Recientes { get; set; }
    }

    public class EstadisticaCursoViewModel
    {
        public int IdCurso { get; set; }
        public string CursoNombre { get; set; }
        public string MaestroNombre { get; set; }
        public int TotalAsistencias { get; set; }
        public DateTime? UltimaAsistencia { get; set; }
    }
}
