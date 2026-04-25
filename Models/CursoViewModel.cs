using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace ControlAsistenciasCursosVirtuales.Models
{
    public class CursoViewModel
    {
        public int? IdCurso { get; set; }

        [Required(ErrorMessage = "Seleccione tipo de curso")]
        public string TipoCurso { get; set; }

        [Required(ErrorMessage = "Ingrese nombre")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "Ingrese descripción")]
        public string Descripcion { get; set; }

        [Required(ErrorMessage = "Seleccione maestro")]
        public int? IdMaestro { get; set; }

        [Required(ErrorMessage = "Seleccione status")]
        public string Status { get; set; }

        [Required(ErrorMessage = "Ingrese fecha")]
        public string Fecha { get; set; }

        [Required(ErrorMessage = "Ingrese hora")]
        public string Hora { get; set; }

        [Required(ErrorMessage = "Ingrese duración")]
        public string DuracionHoras { get; set; }

        public List<SelectListItem> TiposCurso { get; set; }
        public List<SelectListItem> StatusList { get; set; }
        public List<SelectListItem> Maestros { get; set; }
    }

    public class CursoListaViewModel
    {
        public int IdCurso { get; set; }
        public string TipoCurso { get; set; }
        public string Nombre { get; set; }
        public string MaestroNombre { get; set; }
        public string Status { get; set; }
        public DateTime FechaHora { get; set; }
        public decimal DuracionHoras { get; set; }
    }

    public class CursosIndexViewModel
    {
        public string Buscar { get; set; }
        public string Mensaje { get; set; }
        public CursoViewModel Curso { get; set; }
        public List<CursoListaViewModel> Cursos { get; set; }
    }
}