using System;
using System.Collections.Generic;

namespace ControlAsistenciasCursosVirtuales.Models
{
    public class IniciarClaseViewModel
    {
        public string CodigoUsuario { get; set; }

        public string NombreAlumno { get; set; }

        public List<IniciarClaseListaViewModel> Cursos { get; set; }
    }

    public class IniciarClaseListaViewModel
    {
        public int IdCurso { get; set; }

        public string TipoCurso { get; set; }

        public string Nombre { get; set; }

        public string MaestroNombre { get; set; }

        public string Status { get; set; }

        public DateTime FechaHora { get; set; }

        public decimal DuracionHoras { get; set; }
    }
}