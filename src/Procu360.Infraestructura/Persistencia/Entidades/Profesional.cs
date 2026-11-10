using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Procuradores, patrocinantes, apoderados, abogados de interior y martilleros. Reemplaza al ProcuradorID que el legado usaba como tenant parcial. Origen Monitorio: Procurador (sin UsuarioGant/PasswordGant).
/// </summary>
public partial class Profesional
{
    public int Id { get; set; }

    public int EstudioId { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Cuit { get; set; }

    public string? Matricula { get; set; }

    public int? CondicionIvaId { get; set; }

    public string? Calle { get; set; }

    public string? Numero { get; set; }

    public string? Barrio { get; set; }

    public int? LocalidadId { get; set; }

    public string? Carpeta { get; set; }

    public string? Descripcion { get; set; }

    public bool EsProcurador { get; set; }

    public bool EsPatrocinante { get; set; }

    public bool EsApoderado { get; set; }

    public bool EsAbogadoInterior { get; set; }

    public bool EsMartillero { get; set; }

    public string? TituloApoderado { get; set; }

    public string? TextoEscrito { get; set; }

    public string? Observaciones { get; set; }

    public bool Activo { get; set; }

    public Guid? LegadoId { get; set; }

    public DateTime CreadoUtc { get; set; }

    public int? CreadoPor { get; set; }

    public DateTime? ModificadoUtc { get; set; }

    public int? ModificadoPor { get; set; }

    public byte[] Version { get; set; } = null!;

    public virtual CondicionIva? CondicionIva { get; set; }

    public virtual ICollection<Embargo> Embargo { get; set; } = new List<Embargo>();

    public virtual Estudio Estudio { get; set; } = null!;

    public virtual ICollection<EstudioUsuario> EstudioUsuario { get; set; } = new List<EstudioUsuario>();

    public virtual ICollection<Expediente> ExpedienteProfesional { get; set; } = new List<Expediente>();

    public virtual ICollection<Expediente> ExpedienteProfesional1 { get; set; } = new List<Expediente>();

    public virtual ICollection<Expediente> ExpedienteProfesionalNavigation { get; set; } = new List<Expediente>();

    public virtual Localidad? Localidad { get; set; }
}
