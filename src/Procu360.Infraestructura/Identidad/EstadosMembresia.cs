namespace Procu360.Infraestructura.Identidad;

/// <summary>Estados de seg.EstudioUsuario.Estado (docs/03 §6.1).</summary>
public static class EstadosMembresia
{
    public const byte Invitado = 1;
    public const byte Pendiente = 2;   // el email ya tiene membresía activa en otro Estudio: requiere aprobación de plataforma
    public const byte Activo = 3;
    public const byte Desactivado = 4;
    public const byte Rechazado = 5;
}
