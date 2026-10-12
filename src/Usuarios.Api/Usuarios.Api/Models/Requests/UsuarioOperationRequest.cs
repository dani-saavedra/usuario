using System.ComponentModel.DataAnnotations;

namespace Usuarios.Api.Models.Requests
{
    /// <summary>
    /// Forma común a ambas operaciones (Sección 4.1 y 4.2 del README).
    /// El identificador de usuario se valida en formato y longitud; el
    /// esquema real de la tabla "usuario" determina el límite exacto de
    /// longitud y el conjunto de caracteres permitido, por lo que estos
    /// valores deben confirmarse contra la base de datos real (Sección 5.4)
    /// antes de llevar esta validación a producción.
    /// </summary>
    public abstract class UsuarioOperationRequest
    {
        [Required(ErrorMessage = "El campo 'usuario' es obligatorio")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "El campo 'usuario' debe tener entre 1 y 50 caracteres")]
        [RegularExpression(@"^[A-Za-z0-9_.\-@]+$", ErrorMessage = "El campo 'usuario' contiene caracteres no permitidos")]
        public string Usuario { get; set; }

        [StringLength(500, ErrorMessage = "El campo 'motivo' no puede exceder 500 caracteres")]
        public string Motivo { get; set; }
    }
}
