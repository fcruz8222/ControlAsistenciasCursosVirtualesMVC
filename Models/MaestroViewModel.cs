using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace ControlAsistenciasCursosVirtuales.Models
{
    public class MaestroViewModel
    {
        public int? IdMaestro { get; set; }

        [Required(ErrorMessage = "Ingrese el nombre")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "Ingrese el área")]
        public string Area { get; set; }

        [Required(ErrorMessage = "Seleccione estado")]
        public string Status { get; set; }

        public List<SelectListItem> StatusList { get; set; }
    }

    public class MaestroListaViewModel
    {
        public int IdMaestro { get; set; }
        public string Nombre { get; set; }
        public string Area { get; set; }
        public string Status { get; set; }
    }

    public class MaestrosIndexViewModel
    {
        public string Buscar { get; set; }
        public MaestroViewModel Maestro { get; set; }
        public List<MaestroListaViewModel> Maestros { get; set; }
    }
}