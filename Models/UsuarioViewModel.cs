using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace ControlAsistenciasCursosVirtuales.Models
{
    public class UsuarioViewModel
    {
        public string UsuarioOriginal { get; set; }

        [Required(ErrorMessage = "Ingrese código de usuario")]
        public string CodigoUsuario { get; set; }

        [Required(ErrorMessage = "Ingrese nombre")]
        public string NombreUsuario { get; set; }

        public string Password { get; set; }

        [Required(ErrorMessage = "Seleccione rol")]
        public string Rol { get; set; }

        public bool Activo { get; set; }

        public List<SelectListItem> Roles { get; set; }
    }

    public class UsuarioListaViewModel
    {
        public string CodigoUsuario { get; set; }
        public string NombreUsuario { get; set; }
        public string Rol { get; set; }
        public string Status { get; set; }
    }

    public class UsuariosIndexViewModel
    {
        public UsuarioViewModel Usuario { get; set; }
        public List<UsuarioListaViewModel> Usuarios { get; set; }
    }
}