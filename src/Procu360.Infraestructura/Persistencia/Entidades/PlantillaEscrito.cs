using System;
using System.Collections.Generic;

namespace Procu360.Infraestructura.Persistencia.Entidades;

/// <summary>
/// Tipo 1 escrito, 2 cédula, 3 carta, 4 oficio. EsUnico: se genera una sola vez por expediente. Origen Monitorio: EtapaEscrito, Instancia.Path/EscritoNombre, Cautelar.Path/EscritoNombre.
/// </summary>
public partial class PlantillaEscrito
{
    public int Id { get; set; }

    public int EstudioId { get; set; }

    public string Nombre { get; set; } = null!;

    public byte Tipo { get; set; }

    public int? EtapaId { get; set; }

    public int? InstanciaId { get; set; }

    public bool EsUnico { get; set; }

    public bool Activa { get; set; }

    public Guid? LegadoId { get; set; }

    public DateTime CreadoUtc { get; set; }

    public int? CreadoPor { get; set; }

    public DateTime? ModificadoUtc { get; set; }

    public int? ModificadoPor { get; set; }

    public byte[] Version { get; set; } = null!;

    public virtual Estudio Estudio { get; set; } = null!;

    public virtual Etapa? Etapa { get; set; }

    public virtual Instancia? Instancia { get; set; }

    public virtual ICollection<PlantillaEscritoVersion> PlantillaEscritoVersion { get; set; } = new List<PlantillaEscritoVersion>();
}
