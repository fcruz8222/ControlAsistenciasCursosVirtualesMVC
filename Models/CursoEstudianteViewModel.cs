using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using System.Collections.Generic;

namespace ControlAsistenciasCursosVirtuales.Models
{
    public class CursoEstudianteViewModel
    {
        public int? IdCursoEstudiante { get; set; }

        [Required(ErrorMessage = "Seleccione curso")]
        public int? IdCurso { get; set; }

        [Required(ErrorMessage = "Seleccione estudiante")]
        public string CodigoUsuario { get; set; }

        [Required(ErrorMessage = "Seleccione estado")]
        public string Status { get; set; }

        public List<SelectListItem> Cursos { get; set; }
        public List<SelectListItem> Estudiantes { get; set; }
        public List<SelectListItem> StatusList { get; set; }
        public List<string> CodigosUsuarios { get; set; }
    }

    public class CursoEstudianteListaViewModel
    {
        public int IdCursoEstudiante { get; set; }
        public int IdCurso { get; set; }
        public string CursoNombre { get; set; }
        public string CodigoUsuario { get; set; }
        public string NombreUsuario { get; set; }
        public DateTime FechaAsignacion { get; set; }
        public string Status { get; set; }
    }

    public class CursoEstudianteIndexViewModel
    {
        public string Buscar { get; set; }
        public CursoEstudianteViewModel Asignacion { get; set; }
        public List<CursoEstudianteListaViewModel> Asignaciones { get; set; }
    }
}
