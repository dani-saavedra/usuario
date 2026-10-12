namespace Usuarios.Api.Data
{
    /// <summary>
    /// Códigos de resultado controlado que retornan los procedimientos
    /// almacenados (Sección 5.2 y 5.3). Deben coincidir exactamente con los
    /// valores usados en sql/02_create_usp_CerrarSesionYBloquearUsuario.sql y
    /// sql/03_create_usp_DesbloquearUsuario.sql.
    /// </summary>
    public enum ResultCode
    {
        Ok = 0,
        UsuarioNoExiste = 1,
        ErrorEjecucion = 2
    }
}
