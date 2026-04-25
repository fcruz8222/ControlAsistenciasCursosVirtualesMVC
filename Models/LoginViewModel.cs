using System.ComponentModel.DataAnnotations;

namespace ControlAsistenciasCursosVirtuales.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Ingrese usuario")]
        public string CodUser { get; set; }

        [Required(ErrorMessage = "Ingrese contraseña")]
        [DataType(DataType.Password)]
        public string Pass { get; set; }
    }
}