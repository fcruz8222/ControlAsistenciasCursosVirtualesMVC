using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace ControlAsistenciasCursosVirtuales.Models
{
    // 🔹 Este modelo se usa para el FORMULARIO (crear/editar alumno)
    public class AlumnoViewModel
    {
        // 🔹 ID (nullable porque cuando creas no existe todavía)
        public int? IdEstudiante { get; set; }

        [Required(ErrorMessage = "Ingrese nombres")]
        public string Nombres { get; set; }

        [Required(ErrorMessage = "Ingrese apellidos")]
        public string Apellidos { get; set; }

        [Required(ErrorMessage = "Ingrese código")]
        public string Codigo { get; set; }

        [Required(ErrorMessage = "Ingrese email")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Ingrese teléfono")]
        public string Telefono { get; set; }

        // 🔹 Este campo conecta con la tabla Usuarios
        
        public string Usuario { get; set; }

        [Required(ErrorMessage = "Ingrese código de usuario")]
        public string CodigoUsuario { get; set; }

        
        public string Contrasena { get; set; }

        //  ACTIVO / INACTIVO (en BD es 1/0)
        [Required(ErrorMessage = "Seleccione estado")]
        public int Activo { get; set; }

        //  Dropdown del estado
        public List<SelectListItem> EstadoList { get; set; }
    }

    //  Este modelo se usa SOLO para mostrar la tabla
    public class AlumnoListaViewModel
    {
        public int IdEstudiante { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public string Codigo { get; set; }
        public string Email { get; set; }
        public string Telefono { get; set; }
        public string CodigoUsuario { get; set; }

        public string Activo { get; set; }
    }

    //  Este es el modelo principal que usa la vista Index
    public class AlumnosIndexViewModel
    {
        //  Campo de búsqueda
        public string Buscar { get; set; }

        //  Mensajes (errores o info)
        public string Mensaje { get; set; }

        // 🔹 Alumno actual (para el modal)
        public AlumnoViewModel Alumno { get; set; }

        //  Lista de alumnos (tabla)
        public List<AlumnoListaViewModel> Alumnos { get; set; }
    }
}