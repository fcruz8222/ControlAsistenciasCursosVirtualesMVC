using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace ControlAsistenciasCursosVirtuales.Models
{
    public class AsistenciaViewModel
    {
        [Required(ErrorMessage = "Seleccione curso")]
        public int? IdCurso { get; set; }

        [Required(ErrorMessage = "Ingrese nombre del participante")]
        public string NombreParticipante { get; set; }

        [EmailAddress(ErrorMessage = "Ingrese un correo valido")]
        public string Correo { get; set; }

        public string Observacion { get; set; }
        public string Latitud { get; set; }
        public string Longitud { get; set; }
        public string IpPublica { get; set; }
        public string UbicacionEstado { get; set; }
        public List<SelectListItem> Cursos { get; set; }
    }

    public class AsistenciaListaViewModel
    {
        public int IdRegistro { get; set; }
        public string CursoNombre { get; set; }
        public DateTime? CursoFechaFin { get; set; }
        public string NombreParticipante { get; set; }
        public string Correo { get; set; }
        public DateTime FechaRegistro { get; set; }
        public string IpRegistro { get; set; }
        public string IpPublica { get; set; }
        public string Observacion { get; set; }
        public decimal? Latitud { get; set; }
        public decimal? Longitud { get; set; }
        public DateTime? FechaEntrada { get; set; }
        public DateTime? FechaSalida { get; set; }
        public int? DuracionMinutos { get; set; }
        public int? TiempoRestanteMinutos { get; set; }
        public string EstadoAsistencia { get; set; }
    }

    public class AsistenciasIndexViewModel
    {
        public string Buscar { get; set; }
        public int? IdCursoFiltro { get; set; }
        public AsistenciaViewModel Asistencia { get; set; }
        public List<AsistenciaListaViewModel> Asistencias { get; set; }
        public List<SelectListItem> CursosFiltro { get; set; }
    }

    public class AsistenciaConfirmacionViewModel
    {
        public int IdRegistro { get; set; }
        public string CursoNombre { get; set; }
        public DateTime? CursoFechaFin { get; set; }
        public string NombreParticipante { get; set; }
        public string Correo { get; set; }
        public DateTime FechaRegistro { get; set; }
        public string IpRegistro { get; set; }
        public string IpPublica { get; set; }
        public decimal? Latitud { get; set; }
        public decimal? Longitud { get; set; }
        public DateTime? FechaEntrada { get; set; }
        public DateTime? FechaSalida { get; set; }
        public int? DuracionMinutos { get; set; }
        public int? TiempoRestanteMinutos { get; set; }
        public string EstadoAsistencia { get; set; }
        public string Observacion { get; set; }
    }
}
