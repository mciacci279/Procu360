using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Flujo 1 principal, 2 cautelar. HitoSistema 1 inicio de demanda, 2 demanda presentada (reemplaza el código fijo M100 y Configuracion.DemPresID), 3 sentencia, 4 ejecución. TareaParaUsuarios 0 ninguno, 1 responsable, 2 usuarios seleccionados, 3 todos. Origen Monitorio: Etapa + Cautelar.
/// </summary>
public partial class Etapa
{
    public int Id { get; set; }

    public int EstudioId { get; set; }

    public byte Flujo { get; set; }

    public string? Codigo { get; set; }

    public string Nombre { get; set; } = null!;

    public int? Orden { get; set; }

    public int? Dias { get; set; }

    public bool DiasHabiles { get; set; }

    public bool Interrumpe { get; set; }

    public bool CortaPerencion { get; set; }

    public bool EsMonitorio { get; set; }

    public bool EsEjecucion { get; set; }

    public bool EsDiligencia { get; set; }

    public int? EtapaAnteriorId { get; set; }

    public int? EtapaSiguienteId { get; set; }

    public bool PasaSiguienteAutomatica { get; set; }

    public bool GeneraTarea { get; set; }

    public byte TareaParaUsuarios { get; set; }

    public byte? HitoSistema { get; set; }

    /// <summary>
    /// Instancia a la que pasa el expediente al registrar esta etapa. Solo para el flujo principal:
    /// las cautelares no mueven el macroestado (lo garantiza CK_conf_Etapa_4).
    /// </summary>
    public int? InstanciaId { get; set; }

    public bool Activa { get; set; }

    public Guid? LegadoId { get; set; }

    public DateTime CreadoUtc { get; set; }

    public int? CreadoPor { get; set; }

    public DateTime? ModificadoUtc { get; set; }

    public int? ModificadoPor { get; set; }

    public byte[] Version { get; set; } = null!;

    public virtual ICollection<EscritoGenerado> EscritoGenerado { get; set; } = new List<EscritoGenerado>();

    public virtual Estudio Estudio { get; set; } = null!;

    public virtual Etapa? Etapa1 { get; set; }

    public virtual ICollection<EtapaExpediente> EtapaExpediente { get; set; } = new List<EtapaExpediente>();

    public virtual Etapa? EtapaNavigation { get; set; }

    public virtual ICollection<EtapaUsuarioNotificado> EtapaUsuarioNotificado { get; set; } = new List<EtapaUsuarioNotificado>();

    public virtual ICollection<Expediente> ExpedienteEtapa { get; set; } = new List<Expediente>();

    public virtual ICollection<Expediente> ExpedienteEtapaNavigation { get; set; } = new List<Expediente>();

    public virtual ICollection<Etapa> InverseEtapa1 { get; set; } = new List<Etapa>();

    public virtual ICollection<Etapa> InverseEtapaNavigation { get; set; } = new List<Etapa>();

    public virtual Instancia? Instancia { get; set; }

    public virtual ICollection<PlantillaEscrito> PlantillaEscrito { get; set; } = new List<PlantillaEscrito>();
}
