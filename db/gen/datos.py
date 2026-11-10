"""Procu360 - datos de prueba para dos Estudios.

Todo es ficticio: personas, documentos, CUIT, domicilios, comitentes y montos.
Determinístico (semilla fija) para que las pruebas sean reproducibles.
"""
import json
import random
import uuid
from collections import defaultdict
from datetime import date, datetime, timedelta
from decimal import Decimal, ROUND_HALF_UP

rng = random.Random(360)
HOY = date(2026, 11, 9)
D = defaultdict(list)          # tabla -> filas (dict)
_ids = defaultdict(int)


def nid(tabla):
    _ids[tabla] += 1
    return _ids[tabla]


def add(tabla, **fila):
    if "Id" not in fila and tabla not in SIN_ID:
        fila["Id"] = nid(tabla)
    D[tabla].append(fila)
    return fila.get("Id")


SIN_ID = {"cat.IndiceTasaPasiva", "seg.RolPermiso", "seg.SecuenciaEstudio", "conf.ConfiguracionEstudio",
          "conf.EtapaUsuarioNotificado", "dbo.TareaParticipante", "liq.PlanillaJuzgado",
          "dbo.ExpedienteFiscalizacion", "dbo.ExpedienteActa"}


def guid():
    return str(uuid.UUID(int=rng.getrandbits(128), version=4))


def m(x):
    return Decimal(x).quantize(Decimal("0.01"), ROUND_HALF_UP)


def ts(d, h=10, mi=0):
    return datetime(d.year, d.month, d.day, h, mi)


def cuit(prefijo, dni):
    base = f"{prefijo}{int(dni):08d}"
    pesos = [5, 4, 3, 2, 7, 6, 5, 4, 3, 2]
    s = sum(int(a) * b for a, b in zip(base, pesos))
    dv = 11 - s % 11
    if dv == 11:
        dv = 0
    if dv == 10:
        return cuit(23, dni)
    return f"{base[:2]}-{base[2:]}-{dv}"


# ====================================================================== catálogos compartidos
PROV = {}
for n, c in [("Córdoba", "X"), ("Buenos Aires", "B"), ("Santa Fe", "S"), ("San Luis", "D"), ("Ciudad Autónoma de Buenos Aires", "C")]:
    PROV[n] = add("cat.Provincia", Nombre=n, Codigo=c, LegadoId=guid())
LOC = {}
for n, p, cp in [("Córdoba", "Córdoba", "5000"), ("Río Cuarto", "Córdoba", "5800"), ("Villa María", "Córdoba", "5900"),
                 ("Villa Carlos Paz", "Córdoba", "5152"), ("Alta Gracia", "Córdoba", "5186"), ("Jesús María", "Córdoba", "5220"),
                 ("Las Higueras", "Córdoba", "5805"), ("Rosario", "Santa Fe", "2000"), ("Villa Mercedes", "San Luis", "5730"),
                 ("CABA", "Ciudad Autónoma de Buenos Aires", "1000")]:
    LOC[n] = add("cat.Localidad", ProvinciaId=PROV[p], Nombre=n, CodigoPostal=cp, LegadoId=guid())
TD = {}
for n, c in [("DNI", "DNI"), ("Libreta de Enrolamiento", "LE"), ("Libreta Cívica", "LC"), ("Pasaporte", "PAS"), ("CUIT", "CUIT")]:
    TD[c] = add("cat.TipoDocumento", Nombre=n, Codigo=c, LegadoId=guid())
IVA = {}
for n, p, l, flags in [("Responsable Inscripto", "21.00", "A", "RI"), ("Monotributo", "0", "C", "MT"),
                       ("Exento", "0", "C", "EX"), ("Consumidor Final", "0", "B", "CF"), ("No Responsable", "0", "C", "NR")]:
    IVA[flags] = add("cat.CondicionIva", Nombre=n, Porcentaje=Decimal(p), LetraComprobante=l,
                     EsResponsableInscripto=flags == "RI", EsMonotributo=flags == "MT", EsExento=flags == "EX",
                     EsConsumidorFinal=flags == "CF", EsNoResponsable=flags == "NR", LegadoId=guid())
FERIADOS = [(date(2026, 1, 1), "Año Nuevo", 1), (date(2026, 2, 16), "Carnaval", 1), (date(2026, 2, 17), "Carnaval", 1),
            (date(2026, 3, 24), "Día Nacional de la Memoria", 1), (date(2026, 4, 2), "Día del Veterano", 1),
            (date(2026, 4, 3), "Viernes Santo", 1), (date(2026, 5, 1), "Día del Trabajador", 1),
            (date(2026, 5, 25), "Revolución de Mayo", 1), (date(2026, 6, 15), "Paso a la Inmortalidad de Güemes", 1),
            (date(2026, 6, 20), "Día de la Bandera", 1), (date(2026, 7, 9), "Día de la Independencia", 1),
            (date(2026, 7, 13), "Receso judicial de invierno", 3), (date(2026, 7, 14), "Receso judicial de invierno", 3),
            (date(2026, 7, 6), "Fundación de Córdoba", 2), (date(2026, 8, 17), "Paso a la Inmortalidad de San Martín", 1),
            (date(2026, 10, 12), "Día del Respeto a la Diversidad Cultural", 1),
            (date(2026, 11, 23), "Día de la Soberanía Nacional", 1), (date(2026, 12, 8), "Inmaculada Concepción", 1),
            (date(2026, 12, 25), "Navidad", 1)]
for f, n, a in FERIADOS:
    add("cat.Feriado", Fecha=f, Descripcion=n, Ambito=a, ProvinciaId=PROV["Córdoba"] if a != 1 else None, LegadoId=guid())
FERIADO_SET = {f for f, _, _ in FERIADOS}

# índice acumulado de tasa pasiva: diario desde 2023-01-01 hasta hoy
coef = Decimal("100")
d = date(2023, 1, 1)
while d <= HOY:
    tasa_diaria = Decimal("0.00085") if d.year == 2023 else Decimal("0.00060") if d.year == 2024 else Decimal("0.00045")
    coef = (coef * (1 + tasa_diaria)).quantize(Decimal("0.000001"))
    add("cat.IndiceTasaPasiva", Fecha=d, Coeficiente=coef - 100)
    d += timedelta(days=1)
INDICE = {r["Fecha"]: r["Coeficiente"] for r in D["cat.IndiceTasaPasiva"]}

ACT = {}
for c, n in [("471110", "Venta al por menor en hipermercados"), ("561011", "Servicios de restaurantes y cantinas sin espectáculo"),
             ("620100", "Servicios de consultores en informática y suministros de programas"),
             ("452101", "Reparación de cámaras y cubiertas"), ("681098", "Servicios inmobiliarios realizados por cuenta propia")]:
    ACT[c] = add("cat.ActividadEconomica", Codigo=c, Nombre=n, LegadoId=guid())

# ====================================================================== estudios y usuarios
E1 = add("seg.Estudio", Nombre="Estudio Ferreyra & Asociados", Cuit="30-71234567-1", Estado=1,
         ZonaHoraria="America/Argentina/Cordoba", RetencionAuditoriaDias=365, CreadoUtc=ts(date(2026, 3, 2)))
E2 = add("seg.Estudio", Nombre="Lucero Procuraciones", Cuit="30-71765432-4", Estado=1,
         ZonaHoraria="America/Argentina/Cordoba", RetencionAuditoriaDias=90, EstudioOrigenCopiaId=E1,
         CopiaCatalogosJson=json.dumps({"origen": E1, "grupos": ["juzgados", "etapas", "instancias", "tiposTarea",
                                         "tiposEmbargo", "registros", "infracciones", "codigosDevolucion",
                                         "rubrosItemsPlanilla", "planillasJuzgado", "plantillas", "roles"]},
                                       ensure_ascii=False),
         CreadoUtc=ts(date(2026, 6, 10)), CreadoPor=1)

U = {}
for k, n, a, mail, sa in [
        ("super", "Soporte", "Plataforma", "soporte@procu360.test", True),
        ("laura", "Laura", "Ferreyra", "laura.ferreyra@ferreyra.test", False),
        ("martin", "Martín", "Ochoa", "martin.ochoa@ferreyra.test", False),
        ("sofia", "Sofía", "Paredes", "sofia.paredes@ferreyra.test", False),
        ("diego", "Diego", "Luna", "diego.luna@ferreyra.test", False),
        ("carla", "Carla", "Quiroga", "carla.quiroga@ferreyra.test", False),
        ("pablo", "Pablo", "Ruiz", "pablo.ruiz@ferreyra.test", False),
        ("julian", "Julián", "Bustos", "julian.bustos@correo.test", False),
        ("valeria", "Valeria", "Lucero", "valeria.lucero@lucero.test", False),
        ("tomas", "Tomás", "Vera", "tomas.vera@lucero.test", False),
        ("romina", "Romina", "Sosa", "romina.sosa@correo.test", False),
        ("ernesto", "Ernesto", "Gil", "ernesto.gil@ferreyra.test", False),
        ("nicolas", "Nicolás", "Méndez", "nicolas.mendez@lucero.test", False)]:
    U[k] = add("seg.Usuario", Email=mail, EmailNormalizado=mail.upper(), Nombre=n, Apellido=a, EsSuperAdmin=sa,
               Activo=k != "ernesto", MfaHabilitado=sa or k in ("laura", "valeria"),
               PasswordHash=None, SecurityStamp=guid().upper(), AccesosFallidos=0,
               UltimoAccesoUtc=ts(HOY - timedelta(days=rng.randint(0, 5)), 9) if k not in ("nicolas", "ernesto") else None,
               LegadoId=guid() if k in ("laura", "martin", "sofia", "diego", "carla", "pablo", "ernesto") else None,
               CreadoUtc=ts(date(2026, 3, 2)))

# ---------------------------------------------------------------- permisos (globales)
MODULOS = ["expedientes", "flujo", "tareas", "novedades", "escritos", "liquidaciones", "embargos", "planesPago",
           "documentos", "listados", "datosRevisar", "configuracion", "usuarios", "auditoria"]
ACCIONES = ["consultar", "crear", "editar", "anular", "exportar", "aprobar", "administrar"]
PERM = {}
for mo in MODULOS:
    for ac in ACCIONES:
        PERM[(mo, ac)] = add("seg.Permiso", Codigo=f"{mo}.{ac}", Modulo=mo, Accion=ac,
                             Descripcion=f"{ac.capitalize()} en {mo}")

OPERATIVOS = MODULOS[:11]


def permisos_rol(nombre):
    if nombre == "Administrador":
        return list(PERM)
    if nombre == "Abogado / Procurador":
        return [(mo, ac) for mo in OPERATIVOS for ac in ("consultar", "crear", "editar", "anular", "exportar")]
    if nombre == "Gestor":
        return [(mo, ac) for mo in ("expedientes", "flujo", "tareas", "novedades", "escritos", "planesPago", "documentos")
                for ac in ("consultar", "crear", "editar")] + [("listados", "consultar")]
    if nombre == "Liquidaciones":
        return [(mo, "consultar") for mo in OPERATIVOS] + [("liquidaciones", a) for a in ("crear", "editar", "anular", "exportar", "aprobar")]
    if nombre == "Solo lectura":
        return [(mo, "consultar") for mo in OPERATIVOS]
    if nombre == "Auditor":
        return [(mo, "consultar") for mo in OPERATIVOS] + [("auditoria", "consultar"), ("auditoria", "exportar")]


ROLES = [("Administrador", 1), ("Abogado / Procurador", 1), ("Gestor", 3), ("Liquidaciones", 1),
         ("Solo lectura", 2), ("Auditor", 1)]

# ====================================================================== por Estudio
CTX = {}


def catalogos_estudio(E, var):
    """Catálogos del Estudio. var=1 Estudio Ferreyra (origen), var=2 Lucero (copiados del 1 y ajustados)."""
    c = CTX.setdefault(E, {"E": E})
    creador = U["laura"] if var == 1 else U["valeria"]
    base = dict(EstudioId=E, CreadoPor=creador, CreadoUtc=ts(date(2026, 3, 2) if var == 1 else date(2026, 6, 10)))
    leg = (lambda: guid()) if var == 1 else (lambda: None)

    # roles
    c["rol"] = {}
    for n, amb in ROLES:
        rid = add("seg.Rol", Nombre=n, Descripcion=f"Rol sugerido: {n}", Ambito=amb, EsSistema=True, **base)
        c["rol"][n] = rid
        for p in permisos_rol(n):
            add("seg.RolPermiso", EstudioId=E, RolId=rid, PermisoId=PERM[p])

    # comitentes y conceptos
    if var == 1:
        com = [("Municipalidad de San Aurelio", "30-99900001-5", "Av. Central 1200", "5000", "Córdoba"),
               ("Dirección de Rentas Provincial (prueba)", "30-99900002-3", "Calle Rivera 650", "5000", "Córdoba"),
               ("Ente Vial Regional", "30-99900003-1", "Ruta 9 km 712", "5220", "Jesús María")]
        conc = [("Inmobiliario urbano", 0, "INM", True, False),
                ("Contribución que incide sobre comercio", 0, "COM", True, False),
                ("Ingresos Brutos", 1, "IIBB", True, True),
                ("Impuesto automotor", 1, "AUT", True, True),
                ("Multas de tránsito", 2, "MUL", False, False)]
    else:
        com = [("Municipalidad de Las Acacias", "30-99900011-2", "Belgrano 45", "5805", "Las Higueras"),
               ("Comuna de Los Algarrobos", "30-99900012-0", "San Martín s/n", "5800", "Río Cuarto")]
        conc = [("Tasa de servicios a la propiedad", 0, "TSP", True, False),
                ("Tasa de comercio e industria", 0, "TCI", True, False),
                ("Contribución por mejoras", 1, "MEJ", True, False),
                ("Multas de faltas", 0, "MUL", False, False)]
    c["com"] = []
    for n, cu, dom, cp, loc in com:
        c["com"].append(add("conf.Comitente", Nombre=n, Cuit=cu, Domicilio=dom, CodigoPostal=cp, LocalidadId=LOC[loc],
                            Activo=True, LegadoId=leg(), **base))
    c["conc"] = []
    for n, ci, item, cons, dgr in conc:
        c["conc"].append(add("conf.ConceptoDeuda", ComitenteId=c["com"][ci], Nombre=n, Item=item,
                             ConsideraPeriodos=cons, PeriodosFormatoDGR=dgr, Activo=True,
                             TextoEscrito=("por la suma de {{Expediente.MontoLetras}} ({{Expediente.Monto}}) en concepto de "
                                           + n.lower() + " correspondiente a la cuenta {{Expediente.NroCuenta}}, períodos {{Expediente.Periodos}}"),
                             LegadoId=leg(), **base))
    c["conc_com"] = {cc: com_i for cc, (_, com_i, *_r) in zip(c["conc"], conc)}

    # profesionales
    if var == 1:
        prof = [("Ferreyra, Laura Inés", "27-24111222-3", "1-30455", "RI", dict(EsProcurador=True, EsPatrocinante=True, EsApoderado=True), "Abogada"),
                ("Ochoa, Martín Ezequiel", "20-30222333-5", "1-38110", "MT", dict(EsProcurador=True), "Abogado"),
                ("Paredes, Sofía", "27-35333444-7", "1-41202", "MT", dict(EsProcurador=True, EsApoderado=True), "Abogada"),
                ("Bustos, Julián", "20-28444555-0", "2-11870", "RI", dict(EsAbogadoInterior=True, EsPatrocinante=True), "Abogado"),
                ("Ramallo, Héctor", "20-22555666-1", "M-0412", "MT", dict(EsMartillero=True), "Martillero público")]
        loc = "Córdoba"
    else:
        prof = [("Lucero, Valeria", "27-29666777-6", "2-20345", "RI", dict(EsProcurador=True, EsPatrocinante=True, EsApoderado=True), "Abogada"),
                ("Bustos, Julián", "20-28444555-0", "2-11870", "RI", dict(EsProcurador=True), "Abogado"),
                ("Vera, Tomás", "20-37777888-4", "2-25110", "MT", dict(EsApoderado=True), "Abogado")]
        loc = "Río Cuarto"
    c["prof"] = []
    for n, cu, mat, iva, flags, tit in prof:
        c["prof"].append(add("conf.Profesional", Nombre=n, Cuit=cu, Matricula=mat, CondicionIvaId=IVA[iva],
                             Calle="Duarte Quirós" if var == 1 else "Constitución", Numero=str(rng.randint(100, 1500)),
                             Barrio="Centro", LocalidadId=LOC[loc], Carpeta=None, Descripcion=tit,
                             TituloApoderado=tit if flags.get("EsApoderado") else None,
                             TextoEscrito=f"{n}, {tit}, M.P. {mat}, constituyendo domicilio en {{{{Profesional.Domicilio}}}}",
                             Activo=True, LegadoId=leg(), **{**dict(EsProcurador=False, EsPatrocinante=False, EsApoderado=False,
                                                                     EsAbogadoInterior=False, EsMartillero=False), **flags}, **base))

    # juzgados (en el Estudio 2 se copian y ajustan)
    juz = [("Juzgado de Ejecución Fiscal N° 1", "Sec. Única", "Dra. A. Morales", "Tribunales II, Laprida 753, P.B.", True, "Córdoba"),
           ("Juzgado de Ejecución Fiscal N° 2", "Sec. Única", "Dr. R. Pereyra", "Tribunales II, Laprida 753, 1° piso", True, "Córdoba"),
           ("Juzgado Civil, Com. y Fam. Villa Carlos Paz", "Sec. Fiscal", "Dra. M. Rivas", "José Hernández 35", False, "Villa Carlos Paz"),
           ("Juzgado Civil, Com. y Fam. 1ª Nom. Río Cuarto", "Sec. N° 2", "Dr. F. Gatti", "Balcarce 451", False, "Río Cuarto")]
    if var == 2:
        juz.append(("Juzgado de Paz de Las Higueras", "Sec. Única", "Dra. S. Ledesma", "Mitre 12", False, "Las Higueras"))
    c["juz"] = []
    for i, (n, s, j, u, cap, lo) in enumerate(juz):
        c["juz"].append(add("conf.Juzgado", Nombre=n, Descripcion=n.replace("Juzgado", "Juzg."), Secretaria=s, Juez=j,
                            Ubicacion=u, Telefono=f"0351-44{rng.randint(10000, 99999)}", EsCapital=cap,
                            LocalidadId=LOC[lo], DomicilioParaEscritos="Laprida 753, Córdoba" if cap else None, Orden=i + 1,
                            Activo=not (var == 2 and i == 2), LegadoId=leg(), **base))

    # instancias
    c["inst"] = {}
    for i, (n, col, per, baja) in enumerate([("Pre judicial", "#6B7280", False, False), ("Judicial", "#2563EB", True, False),
                                              ("Con sentencia", "#7C3AED", True, False), ("Plan de pagos", "#059669", False, False),
                                              ("Archivado", "#9CA3AF", False, True)]):
        c["inst"][n] = add("conf.Instancia", Nombre=n, Color=col, Perime=per, EsBaja=baja, Orden=i + 1, Activa=True,
                           LegadoId=leg(), **base)

    # etapas principal y cautelar
    etapas = [  # codigo, nombre, dias, habiles, corta, interr, monit, ejec, dilig, genera, hito, instancia
        ("P01", "Intimación pre judicial", 15, False, False, False, False, False, False, True, None, "Pre judicial"),
        ("P02", "Inicio de demanda", 5, True, False, False, True, False, False, False, 1, "Judicial"),
        ("P03", "Demanda presentada", 30, True, True, True, True, False, False, True, 2, "Judicial"),
        ("P04", "Cédula de notificación", 10, True, True, False, True, False, True, True, None, "Judicial"),
        ("P05", "Oficio a registro", 30, True, False, False, True, False, True, False, None, "Judicial"),
        ("P06", "Sentencia monitoria", 6, True, True, True, True, False, False, True, 3, "Con sentencia"),
        ("P07", "Ejecución de sentencia", 30, True, True, False, False, True, False, False, 4, "Con sentencia"),
        ("P08", "Planilla de liquidación presentada", 10, True, True, False, False, True, False, True, None, "Con sentencia"),
        ("P09", "Archivo", None, True, False, False, False, False, False, False, None, "Archivado")]
    cautelares = [("C01", "Embargo solicitado", 20, True), ("C02", "Embargo trabado", None, True),
                  ("C03", "Inhibición general de bienes", 30, True), ("C04", "Levantamiento de medida", 10, True)]
    c["etapa"], c["etapa_info"] = {}, {}
    ids = [nid("conf.Etapa") for _ in etapas]
    for i, (cod, n, dias, hab, corta, inter, mon, eje, dil, gen, hito, inst) in enumerate(etapas):
        add("conf.Etapa", Id=ids[i], Flujo=1, Codigo=cod, Nombre=n, Orden=i + 1, Dias=dias, DiasHabiles=hab,
            Interrumpe=inter, CortaPerencion=corta, EsMonitorio=mon, EsEjecucion=eje, EsDiligencia=dil,
            EtapaAnteriorId=ids[i - 1] if i else None, EtapaSiguienteId=ids[i + 1] if i + 1 < len(ids) else None,
            PasaSiguienteAutomatica=cod in ("P01",), GeneraTarea=gen, TareaParaUsuarios=1 if gen else 0,
            HitoSistema=hito, Activa=True, LegadoId=leg(), **base)
        c["etapa"][cod] = ids[i]
        c["etapa_info"][cod] = dict(dias=dias, hab=hab, inst=inst, nombre=n)
    cids = [nid("conf.Etapa") for _ in cautelares]
    for i, (cod, n, dias, hab) in enumerate(cautelares):
        add("conf.Etapa", Id=cids[i], Flujo=2, Codigo=cod, Nombre=n, Orden=i + 1, Dias=dias, DiasHabiles=hab,
            EtapaAnteriorId=cids[i - 1] if i and i < 2 else None, EtapaSiguienteId=cids[1] if i == 0 else None,
            Interrumpe=False, CortaPerencion=True, EsMonitorio=False, EsEjecucion=True, EsDiligencia=False,
            PasaSiguienteAutomatica=False, GeneraTarea=i == 0, TareaParaUsuarios=1 if i == 0 else 0,
            HitoSistema=None, Activa=True, LegadoId=leg(), **base)
        c["etapa"][cod] = cids[i]
        c["etapa_info"][cod] = dict(dias=dias, hab=hab, inst=None, nombre=n)
    notif = [U["laura"], U["martin"]] if var == 1 else [U["valeria"]]
    for u in notif:
        add("conf.EtapaUsuarioNotificado", EstudioId=E, EtapaId=c["etapa"]["P03"], UsuarioId=u)

    c["tt"] = {}
    for n, dias, hab in [("Controlar proveído", 3, True), ("Llamar al contribuyente", 2, True),
                         ("Retirar oficio diligenciado", 10, True), ("Revisar plan de pagos", 30, False)]:
        c["tt"][n] = add("conf.TipoTarea", Nombre=n, Dias=dias, DiasHabiles=hab, Activo=True, LegadoId=leg(), **base)
    c["ubi"] = [add("conf.Ubicacion", Nombre=n, Activa=True, LegadoId=leg(), **base)
                for n in (["Estudio - archivo A", "Tribunales - casillero 14", "Procuración - escritorio"] if var == 1
                          else ["Oficina Río Cuarto", "Tribunales Río Cuarto"])]
    c["reg"] = [add("conf.Region", Nombre=n, **base) for n in (["Centro", "Norte", "Sur"] if var == 1 else ["Río Cuarto", "Sur"])]
    c["camp"] = [add("conf.Campania", Numero=n, Nombre=nn, **base)
                 for n, nn in ([("2025-02", "Inmobiliario 2do semestre 2025"), ("2026-01", "Comercio 1er semestre 2026")] if var == 1
                               else [("RC-2026", "Tasas 2026")])]
    c["temb"] = {}
    for n, cl in [("Inmueble", 1), ("Automotor", 2), ("Embargo de cuentas corrientes", 3),
                  ("Inhibición", 4), ("Intervención de Caja", 5), ("Reserva de cautelar", 6)]:
        c["temb"][cl] = add("conf.TipoEmbargo", Nombre=n, Clase=cl, LegadoId=leg(), **base)
    c["regp"] = {}
    for k, n, dom in [("RGP", "Registro General de la Provincia", "Luis de Tejeda 4618, Córdoba"),
                      ("RNPA", "Registro Nacional de la Propiedad del Automotor N° 3", "Av. Colón 1200, Córdoba"),
                      ("BCO", "Banco de la Provincia (oficios)", "San Jerónimo 166, Córdoba")]:
        c["regp"][k] = add("conf.RegistroPublico", Nombre=n, Domicilio=dom, Zona="Córdoba", LegadoId=leg(), **base)
    c["inf"] = [add("conf.Infraccion", Codigo=co, Nombre=n, Descripcion=de, LegadoId=leg(), **base) for co, n, de in
                [("77-A", "Exceso de velocidad", "Circular a velocidad superior a la permitida"),
                 ("77-M", "Falta de licencia", "Conducir sin licencia habilitante"),
                 ("48-B", "Estacionamiento prohibido", "Estacionar en lugar no habilitado")]]
    c["cdev"] = [add("conf.CodigoDevolucion", Codigo=co, Descripcion=de, LegadoId=leg(), **base) for co, de in
                 [("01", "Se mudó"), ("02", "Desconocido"), ("03", "Dirección inexistente"), ("04", "Rechazado")]]
    # plantillas
    c["plant"] = {}
    for n, tipo, et, ins, uni in [("Demanda ejecutiva fiscal", 1, "P02", None, True), ("Cédula art. 6 ley 9024", 2, "P04", None, False),
                                  ("Oficio Registro General", 4, "P05", None, False), ("Intimación pre judicial (carta)", 3, "P01", None, False),
                                  ("Solicita embargo", 1, "C01", None, False), ("Escrito de archivo", 1, None, "Archivado", True)]:
        c["plant"][n] = add("conf.PlantillaEscrito", Nombre=n, Tipo=tipo, EtapaId=c["etapa"].get(et),
                            InstanciaId=c["inst"].get(ins), EsUnico=uni, Activa=True, LegadoId=leg(), **base)
    # configuración y feriados del Estudio
    for k, v, t in [("SesionInactividadMinutos", "30", 2), ("NroOrdenRelleno", "5", 2),
                    ("AvisarNovedadesAntesDeEscritos", "true", 5), ("DiasSinMovimientoAlerta", "90", 2),
                    ("FechaCorteInteres", "2014-03-31", 4),
                    ("Feriados.ProvinciaId", "1", 2),   # Córdoba: feriados provinciales que cuentan en plazos
                    ("Perencion.DiasAlerta", "30", 2),
                    # PROVISORIO: plazo de perención a confirmar con el Estudio (ver docs/04 §4.3).
                    ("Perencion.PlazoDias", "1095", 2)]:
        add("conf.ConfiguracionEstudio", EstudioId=E, Clave=k, Valor=v, Tipo=t, CreadoPor=creador)
    if var == 1:
        add("conf.FeriadoEstudio", Fecha=date(2026, 9, 30), Descripcion="Asueto por mudanza de Tribunales II", **base)
    else:
        add("conf.FeriadoEstudio", Fecha=date(2026, 11, 11), Descripcion="Fiesta patronal de Río Cuarto", **base)
    return c


C1 = catalogos_estudio(E1, 1)
C2 = catalogos_estudio(E2, 2)

# ---------------------------------------------------------------- membresías
MEMB = {}


def memb(E, u, estado, roles, prof=None, comit=None, invitado=None, aprobado=None, ult=None):
    mid = add("seg.EstudioUsuario", EstudioId=E, UsuarioId=U[u], Estado=estado, ProfesionalId=prof,
              InvitadoPor=invitado, InvitadoUtc=ts(date(2026, 3, 2)) if invitado else None,
              AprobadoPor=aprobado, AprobadoUtc=ts(date(2026, 6, 12)) if aprobado else None,
              UltimoAccesoUtc=ult, CreadoUtc=ts(date(2026, 3, 2)), CreadoPor=invitado or U["super"])
    c = CTX[E]
    for r in roles:
        add("seg.EstudioUsuarioRol", EstudioId=E, EstudioUsuarioId=mid, RolId=c["rol"][r],
            ComitenteId=comit if c and r in ("Gestor", "Solo lectura") else None)  # roles con ámbito por comitente
    MEMB[(E, u)] = mid


L, V = U["laura"], U["valeria"]
memb(E1, "laura", 3, ["Administrador"], prof=C1["prof"][0], ult=ts(HOY, 8, 45))
memb(E1, "martin", 3, ["Abogado / Procurador"], prof=C1["prof"][1], invitado=L, ult=ts(HOY, 9, 2))
memb(E1, "sofia", 3, ["Abogado / Procurador"], prof=C1["prof"][2], invitado=L)
memb(E1, "diego", 3, ["Gestor"], comit=C1["com"][0], invitado=L)
memb(E1, "carla", 3, ["Liquidaciones"], invitado=L)
memb(E1, "pablo", 3, ["Auditor", "Solo lectura"], invitado=L, comit=C1["com"][0])
memb(E1, "julian", 3, ["Abogado / Procurador"], prof=C1["prof"][3], invitado=L)
memb(E1, "romina", 3, ["Gestor"], comit=C1["com"][1], invitado=L)
memb(E1, "ernesto", 4, ["Gestor"], comit=C1["com"][0], invitado=L)
memb(E2, "valeria", 3, ["Administrador"], prof=C2["prof"][0], invitado=U["super"], ult=ts(HOY, 8, 30))
memb(E2, "tomas", 3, ["Gestor"], prof=C2["prof"][2], comit=C2["com"][0], invitado=V)
memb(E2, "julian", 3, ["Abogado / Procurador"], prof=C2["prof"][1], invitado=V, aprobado=U["super"])  # multi-Estudio aprobado
memb(E2, "romina", 2, ["Gestor"], comit=C2["com"][1], invitado=V)                                      # pendiente de aprobación
memb(E2, "nicolas", 1, ["Solo lectura"], invitado=V, comit=C2["com"][0])                                                 # invitado sin aceptar

add("seg.AccesoSoporte", EstudioId=E1, UsuarioId=U["super"], Motivo="Ticket #97: verificación de migración",
    DesdeUtc=ts(date(2026, 4, 2), 9), HastaUtc=ts(date(2026, 4, 2), 13), OtorgadoPor=L, CreadoPor=L)
add("seg.AccesoSoporte", EstudioId=E2, UsuarioId=U["super"], Motivo="Ticket #118: error al importar novedades",
    DesdeUtc=ts(date(2026, 10, 20), 14), HastaUtc=ts(date(2026, 10, 20), 18), OtorgadoPor=V, CreadoPor=V)


# ====================================================================== liquidación (parámetros)
def parametros_liq(E, c, var):
    base = dict(EstudioId=E, CreadoPor=L if var == 1 else V)
    for cod, val, desde, hasta, comi, obs in [
            ("VALOR_JUS", "21500.00", date(2025, 1, 1), date(2025, 12, 31), None, "Acuerdo TSJ"),
            ("VALOR_JUS", "28910.00", date(2026, 1, 1), date(2026, 6, 30), None, None),
            ("VALOR_JUS", "33450.00", date(2026, 7, 1), None, None, None),
            ("IVA_PCT", "21", date(2014, 1, 1), None, None, None),
            ("REPARTO_PATROCINANTE_PCT", "60", date(2014, 1, 1), None, None, None),
            ("REPARTO_PROCURADOR_PCT", "40", date(2014, 1, 1), None, None, None),
            ("TASA_JUSTICIA_PCT", "2", date(2014, 1, 1), None, None, None),
            ("APORTES_PCT", "2", date(2014, 1, 1), None, None, None),
            ("INTERES_TASA_JUSTICIA_ANUAL_PCT", "24", date(2014, 1, 1), None, None, "Equivale a 0,0657534 % diario"),
            ("HONORARIOS_MINIMO_JUS", "6.5", date(2014, 1, 1), None, None, "Mitad de 13 JUS"),
            ("HONORARIOS_MAXIMO_PCT_BASE", "50", date(2014, 1, 1), None, None, None),
            ("ART104_INC5_JUS", "3", date(2014, 1, 1), None, None, None)]:
        add("liq.ParametroLiquidacion", ComitenteId=comi, Codigo=cod, Valor=Decimal(val), VigenteDesde=desde,
            VigenteHasta=hasta, Observaciones=obs, **base)
    add("liq.ParametroLiquidacion", ComitenteId=None, Codigo="FECHA_CORTE_INTERES", ValorFecha=date(2014, 3, 31),
        VigenteDesde=date(2014, 1, 1), Observaciones="Cambio de 0,083 % a 0,1 % diario", **base)
    # particular de un comitente: tasa de justicia distinta
    add("liq.ParametroLiquidacion", ComitenteId=c["com"][1], Codigo="TASA_JUSTICIA_PCT", Valor=Decimal("1.5"),
        VigenteDesde=date(2025, 1, 1), Observaciones="Convenio con el comitente", **base)
    for cod, per, val, desde, hasta, comi in [
            ("INTERES_CAPITAL", 1, "0.000830", date(2010, 1, 1), date(2014, 3, 31), None),
            ("INTERES_CAPITAL", 1, "0.001000", date(2014, 4, 1), None, None),
            ("INTERES_SIMPLE", 2, "0.020000", date(2014, 1, 1), None, None),
            ("INTERES_CAPITAL", 1, "0.000900", date(2024, 1, 1), None, c["com"][0])]:
        add("liq.TasaInteres", ComitenteId=comi, Codigo=cod, Periodicidad=per, Valor=Decimal(val), VigenteDesde=desde,
            VigenteHasta=hasta, **base)
    for desde in (date(2014, 1, 1), date(2026, 1, 1)):
        f = Decimal("1") if desde.year == 2014 else Decimal("12.5")
        prev = Decimal("0")
        for tope, pct in [("25870", "9"), ("77611", "7.8"), ("155222", "6.6"), ("258703", "5.4"), ("517406", "4.2"), (None, "3")]:
            hasta = (Decimal(tope) * f).quantize(Decimal("0.01")) if tope else None
            add("liq.EscalaHonorarios", ComitenteId=None, VigenteDesde=desde, BaseDesde=prev, BaseHasta=hasta,
                Porcentaje=Decimal(pct), **base)
            prev = (hasta + Decimal("0.01")) if hasta else prev
    c["rubro"] = {}
    for i, (n, tot) in enumerate([("Capital", "Total capital"), ("Intereses", "Total intereses"),
                                  ("Gastos", "Total gastos"), ("Honorarios", "Total honorarios")]):
        c["rubro"][n] = add("liq.RubroPlanilla", Nombre=n, Descripcion=None, DescripcionTotal=tot, Orden=i + 1,
                            LegadoId=guid() if var == 1 else None, **base)
    c["item"] = {}
    for n, rub, tipo, val, pct, minimo, manual, defa, desc in [
            ("Capital reclamado", "Capital", 3, None, False, None, False, True, "Capital al <<FechaInicio>>"),
            ("Interés sobre capital", "Intereses", 6, None, False, None, False, True, "Intereses desde el vencimiento al <<FechaHoy>>"),
            ("Interés tasa pasiva", "Intereses", 5, None, False, None, False, False, "Tasa pasiva BCRA"),
            ("Interés simple", "Intereses", 4, "2", True, None, False, False, "Interés 2 % mensual"),
            ("Tasa de justicia", "Gastos", 8, None, False, None, False, True, "Tasa de justicia e intereses"),
            ("Aportes Caja de Abogados", "Gastos", 9, None, False, None, False, True, "Aportes de ley"),
            ("Gastos de diligencias", "Gastos", 10, None, False, None, False, True, "Cédulas y oficios"),
            ("Gastos de publicación", "Gastos", 1, None, False, None, True, False, "Edictos"),
            ("Honorarios", "Honorarios", 7, None, False, None, False, True, "Honorarios de <<Patrocinante>> y <<Procurador>>"),
            ("Art. 104 inc. 5", "Honorarios", 2, "3", False, None, False, False, "3 JUS (Ley 9459)")]:
        c["item"][n] = add("liq.ItemPlanilla", RubroPlanillaId=c["rubro"][rub], Nombre=n, Descripcion=desc, TipoCalculo=tipo,
                           Valor=Decimal(val) if val else None, EsPorcentaje=pct, MontoMinimo=minimo, CargaManual=manual,
                           EsSubtotal=False, EsDefault=defa, OrdenDefault=len(c["item"]) + 1, Activo=True,
                           LegadoId=guid() if var == 1 else None, **base)
    for item, desde, hasta, mn, mx in [("Tasa de justicia", date(2025, 1, 1), date(2025, 12, 31), "15000", "1500000"),
                                       ("Tasa de justicia", date(2026, 1, 1), None, "20000", "2000000"),
                                       ("Aportes Caja de Abogados", date(2026, 1, 1), None, "12000", None)]:
        add("liq.ItemPlanillaTope", ItemPlanillaId=c["item"][item], VigenteDesde=desde, VigenteHasta=hasta,
            Minimo=Decimal(mn), Maximo=Decimal(mx) if mx else None, LegadoId=guid() if var == 1 else None, **base)
    for j in c["juz"]:
        for orden, it in enumerate([i for i in c["item"] if i not in ("Interés simple", "Interés tasa pasiva") or j == c["juz"][-1]]):
            add("liq.PlanillaJuzgado", EstudioId=E, JuzgadoId=j, ItemPlanillaId=c["item"][it], Orden=orden + 1)


parametros_liq(E1, C1, 1)
parametros_liq(E2, C2, 2)

# ====================================================================== expedientes
NOMBRES = ["María José", "Carlos Alberto", "Ana Laura", "Jorge Luis", "Silvia", "Ricardo", "Patricia", "Oscar",
           "Gabriela", "Héctor", "Lucía", "Raúl", "Verónica", "Sergio", "Florencia", "Daniel", "Natalia", "Gustavo"]
APELLIDOS = ["Gómez", "Rodríguez", "Fernández", "López", "Martínez", "Sánchez", "Pérez", "Romero", "Díaz",
             "Álvarez", "Torres", "Ruiz", "Benítez", "Acosta", "Medina", "Herrera", "Aguirre", "Castro"]
EMPRESAS = ["Distribuidora del Centro S.R.L.", "Construcciones Serranas S.A.", "La Esquina Gastronómica S.A.S.",
            "Agroinsumos del Sur S.A."]
CALLES = ["Av. Colón", "Bv. San Juan", "Ituzaingó", "27 de Abril", "Obispo Trejo", "Sarmiento", "Av. Vélez Sarsfield",
          "Rivadavia", "Belgrano", "Mitre"]


def habiles(desde, dias, hab):
    if dias is None:
        return None
    if not hab:
        return desde + timedelta(days=dias)
    d, n = desde, 0
    while n < dias:
        d += timedelta(days=1)
        if d.weekday() < 5 and d not in FERIADO_SET:
            n += 1
    return d


def persona(E, c, fisica=True, migrada=True):
    if fisica:
        dni = rng.randint(14_000_000, 42_000_000)
        ap, no = rng.choice(APELLIDOS), rng.choice(NOMBRES)
        sexo = rng.choice("FM")
        pid = add("dbo.Persona", EstudioId=E, EsPersonaFisica=True, Apellido=ap, Nombre=no, TipoDocumentoId=TD["DNI"],
                  NroDocumento=str(dni), Cuit=cuit(27 if sexo == "F" else 20, dni), Sexo=sexo,
                  FechaNacimiento=date(rng.randint(1950, 2000), rng.randint(1, 12), rng.randint(1, 28)),
                  Telefono1=f"351-{rng.randint(4000000, 6999999)}" if rng.random() < .7 else None,
                  Email=f"{no.split()[0].lower()}.{ap.lower()}@correo.test".replace("á", "a").replace("é", "e").replace("í", "i").replace("ó", "o").replace("ú", "u")
                  if rng.random() < .4 else None,
                  Anulado=False, LegadoId=guid() if migrada else None, CreadoPor=c["admin"])
        nombre = f"{ap}, {no}"
    else:
        rs = EMPRESAS.pop(0)
        pid = add("dbo.Persona", EstudioId=E, EsPersonaFisica=False, RazonSocial=rs, TipoDocumentoId=TD["CUIT"],
                  NroDocumento=None, Cuit=cuit(30, rng.randint(70_000_000, 71_999_999)),
                  Telefono1=f"351-{rng.randint(4000000, 6999999)}", Anulado=False,
                  LegadoId=guid() if migrada else None, CreadoPor=c["admin"])
        nombre = rs
    loc = rng.choice(c["locs"])
    add("dbo.Domicilio", EstudioId=E, PersonaId=pid, Tipo=1, Calle=rng.choice(CALLES), Numero=str(rng.randint(10, 3500)),
        Barrio=rng.choice(["Centro", "Alberdi", "General Paz", "Nueva Córdoba", "Banda Norte", None]),
        CodigoPostal=next(r["CodigoPostal"] for r in D["cat.Localidad"] if r["Id"] == LOC[loc]),
        LocalidadId=LOC[loc], Principal=True, LegadoId=guid() if migrada else None, CreadoPor=c["admin"])
    if rng.random() < .35:
        add("dbo.Domicilio", EstudioId=E, PersonaId=pid, Tipo=2, Calle=rng.choice(CALLES), Numero=str(rng.randint(10, 3500)),
            Piso=str(rng.randint(1, 9)), Depto=rng.choice("ABCD"), LocalidadId=LOC[loc], Principal=False,
            LegadoId=guid() if migrada else None, CreadoPor=c["admin"])
    return pid, nombre


C1.update(admin=L, locs=["Córdoba", "Córdoba", "Villa Carlos Paz", "Alta Gracia", "Jesús María"])
C2.update(admin=V, locs=["Río Cuarto", "Río Cuarto", "Las Higueras"])
EXP = []   # info para pasos posteriores


def expediente(E, c, nro, *, conc, avance, juz=None, fisica=True, codem=0, migrado=True, anulado=False,
               cautelar=None, fisca=False, acta=False, actividad=None, plan=None, sin_nro_judicial=False,
               region=None, campania=None, granc=False, alta=None, negociador=None, depurador=None, interior=False):
    exp_id = nid("dbo.Expediente")
    pid, dem = persona(E, c, fisica, migrado)
    codems = [persona(E, c, True, migrado) for _ in range(codem)]
    comi = c["com"][c["conc_com"][c["conc"][conc]]]
    alta = alta or date(rng.randint(2022, 2025), rng.randint(1, 12), rng.randint(1, 28))
    # períodos
    periodos, cap, rec = [], Decimal(0), Decimal(0)
    anio0 = min(alta.year - 1, 2024)
    for k in range(rng.randint(2, 6)):
        anio = anio0 - k // 4
        cuota = f"{(k % 4) * 3 + 1:02d}"
        trib = m(rng.uniform(4000, 65000))
        recg = m(trib * Decimal("0.10"))
        venc = date(anio, int(cuota), 10)
        periodos.append((anio, cuota, venc, trib, recg))
        cap += trib
        rec += recg
    nro_cta = f"{rng.randint(1100, 3399)}-{rng.randint(100000, 999999)}"
    es_auto = c["conc"][conc] == (c["conc"][3] if len(c["conc"]) > 3 else None)
    dominio = f"{rng.choice(['AB', 'AC', 'AD', 'AE'])}{rng.randint(100, 999)}{rng.choice(['KL', 'MN', 'PR'])}" if es_auto or acta else None
    juz = juz if juz is not None else (rng.choice(c["juz"][:2]) if c["E"] == E1 else c["juz"][3])
    caratula = None
    etapas_cods = ["P01", "P02", "P03", "P04", "P05", "P06", "P07", "P08", "P09"][:avance]
    judicial = avance >= 2
    nro_jud = None
    if judicial:
        conc_n = next(r["Nombre"] for r in D["conf.ConceptoDeuda"] if r["Id"] == c["conc"][conc])
        com_n = next(r["Nombre"] for r in D["conf.Comitente"] if r["Id"] == comi)
        caratula = f"{com_n.upper()} C/ {dem.upper()}{' Y OTROS' if codems else ''} - EJECUTIVO FISCAL - {conc_n.upper()}"
        nro_jud = None if sin_nro_judicial else str(rng.randint(9_000_000, 12_999_999))
    exp = dict(Id=exp_id, EstudioId=E, NroOrden=nro, ComitenteId=comi, ConceptoDeudaId=c["conc"][conc],
               NroLiquidacion=f"{rng.randint(100000, 999999)}/{alta.year % 100:02d}", FechaTitulo=alta - timedelta(days=rng.randint(20, 90)),
               FechaAlta=alta, MontoTitulo=cap + rec, MontoCapital=cap, NroCuenta=None if acta else nro_cta,
               Nomenclatura=None if (es_auto or acta) else f"11-01-{rng.randint(1, 30):02d}-{rng.randint(1, 40):02d}-{rng.randint(1, 99):03d}-{rng.randint(1, 50):03d}",
               Dominio=dominio, Designacion=None if (es_auto or acta) else f"Lote {rng.randint(1, 40)} Mz. {rng.randint(1, 90)}",
               MatriculaRpi=None if (es_auto or acta) else str(rng.randint(100000, 1999999)),
               DomicilioObjeto=None if acta else f"{rng.choice(CALLES)} {rng.randint(10, 3500)}",
               LocalidadObjetoId=LOC[c["locs"][0]], RegistroPublicoId=c["regp"]["RNPA" if es_auto else "RGP"] if not acta else None,
               NroResolucionMulta=f"RM-{rng.randint(1000, 9999)}/{alta.year}" if acta else None,
               FechaResolucion=alta - timedelta(days=40) if acta else None,
               JuzgadoId=juz if judicial else None, NroExpedienteJudicial=nro_jud, Caratula=caratula, Recaratulacion=None,
               Sorteo=str(rng.randint(1, 900)) if judicial else None, FechaSorteo=None, Cedula=None,
               ProcuradorId=c["prof"][0 if c["E"] == E2 else rng.choice([1, 2])],
               PatrocinanteId=c["prof"][0], AbogadoInteriorId=c["prof"][3] if interior else None,
               NegociadorUsuarioId=negociador, DepuradorUsuarioId=depurador,
               Comparecio=False, FechaComparecio=None, AbogadoContraparte=None, DomicilioAbogadoContraparte=None,
               RegionId=region, CampaniaId=campania, EsGranContribuyente=granc, EsFisca=fisca,
               ObservacionesTitulo=None, Observaciones=None, ObservacionesAdministrativas=None,
               Anulado=anulado, AnuladoUtc=ts(date(2026, 8, 3)) if anulado else None,
               AnuladoPor=c["admin"] if anulado else None, MotivoAnulacion="Título duplicado: se reemplazó por el expediente 00649" if anulado else None,
               LegadoId=guid() if migrado else None, CreadoPor=c["admin"], CreadoUtc=ts(alta))
    D["dbo.Expediente"].append(exp)
    add("dbo.ExpedientePersona", EstudioId=E, ExpedienteId=exp_id, PersonaId=pid, Rol=1, Posicion=1,
        LegadoId=guid() if migrado else None, CreadoPor=c["admin"])
    for i, (cp, _) in enumerate(codems):
        add("dbo.ExpedientePersona", EstudioId=E, ExpedienteId=exp_id, PersonaId=cp, Rol=2, Posicion=i + 2,
            LegadoId=guid() if migrado else None, CreadoPor=c["admin"])
    per_ids = []
    for anio, cuota, venc, trib, recg in periodos:
        per_ids.append(add("dbo.PeriodoAdeudado", EstudioId=E, ExpedienteId=exp_id, Anio=anio, Cuota=cuota,
                           Concepto="Cuota" if not acta else "Multa", Vencimiento=venc, Tributo=trib, Recargo=recg,
                           LegadoId=guid() if migrado else None, CreadoPor=c["admin"]))
    # etapas e instancias
    f = alta
    ult_inst = None
    fecha_hito2 = None
    ultima_etapa = None
    fechas_et = []
    f = alta + timedelta(days=3)
    for i in range(len(etapas_cods)):
        if i:
            f = f + timedelta(days=rng.randint(10, 70))
        fechas_et.append(f)
    tope = HOY - timedelta(days=1)
    if fechas_et and fechas_et[-1] > tope:               # comprime la historia para que termine antes de hoy
        span = (fechas_et[-1] - fechas_et[0]).days or 1
        disp = max((tope - fechas_et[0]).days, 0)
        fechas_et = [fechas_et[0] + timedelta(days=(x - fechas_et[0]).days * disp // span) for x in fechas_et]
        fechas_et = [min(x, tope) for x in fechas_et]
    for i, cod in enumerate(etapas_cods):
        info = c["etapa_info"][cod]
        f = fechas_et[i]
        ultima = i == len(etapas_cods) - 1
        venc = habiles(f, info["dias"], info["hab"])
        eid = add("dbo.EtapaExpediente", EstudioId=E, ExpedienteId=exp_id, EtapaId=c["etapa"][cod], FechaInicio=f,
                  FechaFin=None if ultima else fechas_et[i + 1], FechaVencimiento=venc,
                  Estado=1 if ultima else 2, Monto=None, LegadoId=guid() if migrado else None, CreadoPor=c["admin"])
        if cod == "P03":
            fecha_hito2 = f
        if info["inst"] and info["inst"] != ult_inst:
            add("dbo.InstanciaExpediente", EstudioId=E, ExpedienteId=exp_id, InstanciaId=c["inst"][info["inst"]], Fecha=f,
                LegadoId=guid() if migrado else None, CreadoPor=c["admin"])
            ult_inst = info["inst"]
            exp["InstanciaActualId"], exp["FechaInstanciaActual"] = c["inst"][ult_inst], f
        exp["EtapaActualId"], exp["FechaEtapaActual"] = c["etapa"][cod], f
        ultima_etapa = (eid, cod, f, venc)
    if plan:
        add("dbo.InstanciaExpediente", EstudioId=E, ExpedienteId=exp_id, InstanciaId=c["inst"]["Plan de pagos"], Fecha=plan[0],
            LegadoId=guid() if migrado else None, CreadoPor=c["admin"])
        exp["InstanciaActualId"], exp["FechaInstanciaActual"] = c["inst"]["Plan de pagos"], plan[0]
    if cautelar:
        fc = min((fecha_hito2 or alta) + timedelta(days=20), HOY - timedelta(days=30))
        for j, cod in enumerate(cautelar):
            last = j == len(cautelar) - 1
            add("dbo.EtapaExpediente", EstudioId=E, ExpedienteId=exp_id, EtapaId=c["etapa"][cod], FechaInicio=fc,
                FechaFin=None if last else fc + timedelta(days=15), FechaVencimiento=habiles(fc, c["etapa_info"][cod]["dias"], True),
                Estado=1 if last else 2, Monto=m(cap * Decimal("1.3")) if cod == "C01" else None,
                LegadoId=guid() if migrado else None, CreadoPor=c["admin"])
            exp["CautelarActualId"], exp["FechaCautelarActual"] = c["etapa"][cod], fc
            fc = min(fc + timedelta(days=25), HOY - timedelta(days=1))
    ubi = rng.choice(c["ubi"])
    add("dbo.UbicacionExpediente", EstudioId=E, ExpedienteId=exp_id, UbicacionId=ubi, Fecha=alta + timedelta(days=1),
        LegadoId=guid() if migrado else None, CreadoPor=c["admin"])
    exp["UbicacionActualId"] = ubi
    fechas = [r["FechaInicio"] for r in D["dbo.EtapaExpediente"] if r["ExpedienteId"] == exp_id] + [alta]
    exp["FechaUltimoMovimiento"] = max(fechas)
    if fisca:
        add("dbo.ExpedienteFiscalizacion", EstudioId=E, ExpedienteId=exp_id, TipoFisca=1, Tipo=2,
            NroExpediente=f"0032-{rng.randint(100000, 999999)}/2024", NroOrden=str(rng.randint(1, 999)),
            NroResolucion=f"DJ {rng.randint(100, 999)}/2024", ResolFechaResolucion=date(2024, 5, 14),
            ResolFechaNotificacion=date(2024, 5, 30), ReconFechaResolucion=date(2024, 8, 2),
            ReconFechaNotificacion=date(2024, 8, 20), FechaReconocimientoDeuda=None, ExtiendeResponsabilidad=True,
            ResponsableNombre=f"{rng.choice(APELLIDOS)}, {rng.choice(NOMBRES)}", ResponsableDomicilio="Sarmiento 455, Córdoba",
            Observaciones="Determinación de oficio sobre base presunta", CreadoPor=c["admin"])
    if acta:
        add("dbo.ExpedienteActa", EstudioId=E, ExpedienteId=exp_id, FechaLabrado=alta - timedelta(days=60), HoraLabrado="17:40",
            LugarLabrado="Ruta 9 km 718", TipoYNroLicencia=f"B1 - {rng.randint(20000000, 40000000)}",
            DescripcionDominio=f"Automotor marca Ejemplo, dominio {dominio}", NroCaso=f"C-{rng.randint(10000, 99999)}",
            JuzgadoFaltas="Juzgado Administrativo de Faltas Ente Vial", NroTramite=str(rng.randint(500000, 599999)),
            FechaTramite=alta - timedelta(days=30), CreadoPor=c["admin"])
        for inf in rng.sample(c["inf"], 2):
            add("dbo.ExpedienteInfraccion", EstudioId=E, ExpedienteId=exp_id, InfraccionId=inf,
                Fecha=alta - timedelta(days=60), LegadoId=guid() if migrado else None, CreadoPor=c["admin"])
    if actividad:
        for a in actividad:
            add("dbo.ExpedienteActividad", EstudioId=E, ExpedienteId=exp_id, ActividadEconomicaId=ACT[a],
                LegadoId=guid() if migrado else None, CreadoPor=c["admin"])
    if plan:
        fplan, estado, cant, pag = plan
        add("dbo.PlanPago", EstudioId=E, ExpedienteId=exp_id, NroPlan=f"PP-{rng.randint(10000, 99999)}", FechaPlan=fplan,
            PeriodosIncluidos="; ".join(f"{a}/{cu}" for a, cu, *_ in periodos), CantCuotas=cant, CuotasPagadas=pag,
            CuotasVencidasImpagas=3 if estado == 3 else 0, CuotasAVencer=cant - pag if estado == 1 else 0,
            FechaCaducidad=fplan + timedelta(days=200) if estado == 3 else None,
            Estado={1: "Vigente - al día", 3: "Caducado de oficio"}[estado], FechaConsulta=HOY - timedelta(days=4),
            Decreto="Caducado de oficio por incumplimiento" if estado == 3 else None, Activo=estado == 1,
            LegadoId=guid() if migrado else None, CreadoPor=c["admin"])
    info = dict(id=exp_id, E=E, c=c, nro=nro, periodos=list(zip(per_ids, periodos)), cap=cap, rec=rec,
                hito2=fecha_hito2, ultima=ultima_etapa, juz=juz if judicial else None, comi=comi, dem=pid,
                migrado=migrado, exp=exp)
    EXP.append(info)
    return info


# ---- Estudio 1: 12 expedientes con casos variados
g = expediente
X = {}
X[1] = g(E1, C1, 85, conc=0, avance=1, region=C1["reg"][0], campania=C1["camp"][0], alta=date(2025, 9, 15))          # pre judicial, nro bajo (00085)
X[2] = g(E1, C1, 641, conc=0, avance=3, codem=2, region=C1["reg"][0], negociador=U["diego"])                       # demanda presentada, 2 codemandados
X[3] = g(E1, C1, 642, conc=1, avance=4, fisica=False, actividad=["561011", "471110"], granc=True)                  # comercio, empresa, cédula
X[4] = g(E1, C1, 643, conc=0, avance=6, cautelar=["C01", "C02"], codem=1, depurador=U["romina"])                   # con sentencia y embargo
X[5] = g(E1, C1, 644, conc=3, avance=7, cautelar=["C01"])                                                           # automotor, ejecución
X[6] = g(E1, C1, 645, conc=2, avance=3, fisica=False, fisca=True, actividad=["620100"], granc=True)                # IIBB fiscalización
X[7] = g(E1, C1, 646, conc=4, avance=2, acta=True, juz=C1["juz"][2], interior=True)                               # multa, interior
X[8] = g(E1, C1, 647, conc=0, avance=3, plan=(date(2026, 4, 1), 1, 12, 7))                                         # plan vigente
X[9] = g(E1, C1, 648, conc=1, avance=3, plan=(date(2025, 6, 1), 3, 6, 2), sin_nro_judicial=True)                   # plan caduco, sin nro judicial
X[10] = g(E1, C1, 649, conc=0, avance=9)                                                                            # archivado
X[11] = g(E1, C1, 650, conc=0, avance=1, anulado=True)                                                              # anulado
X[12] = g(E1, C1, 651, conc=0, avance=2, migrado=False, alta=date(2026, 10, 27), campania=C1["camp"][1])          # alta en Procu360
X[13] = g(E1, C1, 652, conc=1, avance=1, migrado=False, alta=date(2026, 11, 5))                                    # alta en Procu360
# ---- Estudio 2: 8 expedientes
Y = {}
Y[1] = g(E2, C2, 1, conc=0, avance=1, region=C2["reg"][0], campania=C2["camp"][0])
Y[2] = g(E2, C2, 2, conc=0, avance=3, codem=1, negociador=U["tomas"])
Y[3] = g(E2, C2, 3, conc=1, avance=4, fisica=False, actividad=["452101"])
Y[4] = g(E2, C2, 4, conc=0, avance=6, cautelar=["C03"], juz=C2["juz"][4])
Y[5] = g(E2, C2, 5, conc=2, avance=3, plan=(date(2026, 2, 10), 1, 10, 8))
Y[6] = g(E2, C2, 6, conc=0, avance=7, cautelar=["C01", "C02"])
Y[7] = g(E2, C2, 7, conc=1, avance=2, migrado=False, alta=date(2026, 9, 1))
Y[8] = g(E2, C2, 8, conc=0, avance=9)
Y[9] = g(E2, C2, 9, conc=3, avance=2, acta=True)                                         # multa de faltas con acta
Y[10] = g(E2, C2, 10, conc=1, avance=3, fisica=False, fisca=True)                        # comercio con fiscalización

# recaratulación, comparecencia y sorteo
X[2]["exp"]["Recaratulacion"] = X[2]["exp"]["Caratula"].replace(" Y OTROS", " - SUCESORES")
X[4]["exp"].update(Comparecio=True, FechaComparecio=date(2026, 2, 3), AbogadoContraparte="Dr. Esteban Quinteros",
                   DomicilioAbogadoContraparte="Caseros 344, 2° B, Córdoba")
X[5]["exp"]["ObservacionesAdministrativas"] = "Se pidió informe de dominio actualizado"
Y[2]["exp"]["Observaciones"] = "Contribuyente propone pago en 6 cuotas"

for u, (E, c) in [("laura", (E1, C1)), ("valeria", (E2, C2))]:
    add("seg.SecuenciaEstudio", EstudioId=E, Nombre="NroOrden",
        UltimoValor=max(r["NroOrden"] for r in D["dbo.Expediente"] if r["EstudioId"] == E))

# informes de personas
for info in (X[4], X[6], Y[3]):
    add("dbo.InformePersona", EstudioId=info["E"], PersonaId=info["dem"], Fecha=info["exp"]["FechaAlta"] + timedelta(days=30),
        Texto="Informe Registro de la Propiedad: titular de 1 inmueble, sin gravámenes.", LegadoId=guid(), CreadoPor=info["c"]["admin"])

# ---- embargos
EMB_DET = {
    1: {"departamento": "Capital", "lote": "12", "manzana": "45", "nomenclatura": "11-01-01-04-012-045",
        "inscripcionDominial": "Matrícula 1.234.567", "phNro": None, "phMatricula": None, "diarioIngreso": "D-2026-04411"},
    2: {"dominio": None, "marca": "Ejemplo", "modelo": "Sedán 1.6", "tipoChasis": "Sedán 4 puertas", "sticker": "ST-88121"},
    3: {"banco": "Banco de la Provincia (prueba)", "sucursal": "922 - Centro", "domicilioBanco": "San Jerónimo 166",
        "cbuDepositar": "0200922711000012345671", "cuentaDepositar": "922/12345-6"},
    4: {"inscripcionRPC": "Inhibición N° 77812/2026"},
    5: {"martillero": "Ramallo, Héctor", "domicilioMartillero": "Caseros 120, Córdoba",
        "domicilioIntervenir": "Av. Colón 2450, Córdoba", "horario": "8 a 13 h"},
    6: {"medidaReservada": "Embargo sobre inmueble", "motivo": "Se reserva hasta contar con informe de dominio"},
}
for info, cl, est in [(X[4], 1, 2), (X[5], 2, 2), (X[4], 3, 1), (Y[4], 4, 2), (Y[6], 1, 3),
                      (X[3], 5, 1), (Y[3], 6, 1)]:
    det = dict(EMB_DET[cl])
    if cl == 2:
        det["dominio"] = info["exp"]["Dominio"]
    fs = (info["hito2"] or info["exp"]["FechaAlta"]) + timedelta(days=25)
    add("dbo.Embargo", EstudioId=info["E"], ExpedienteId=info["id"], TipoEmbargoId=info["c"]["temb"][cl], Estado=est,
        FechaSolicitud=fs, FechaTraba=fs + timedelta(days=30) if est >= 2 else None,
        FechaLevantamiento=fs + timedelta(days=200) if est == 3 else None,
        Monto=m(info["cap"] * Decimal("1.3")), Porcentaje=Decimal("10") if cl == 3 else None,
        RegistroPublicoId=info["c"]["regp"]["RNPA" if cl == 2 else "BCO" if cl == 3 else "RGP"],
        NroRegistro=str(rng.randint(10000, 99999)), Secretaria=None, Autorizados="Dra. L. Ferreyra y/o Sr. D. Luna" if info["E"] == E1 else "Dra. V. Lucero",
        Dominio=det.get("dominio"), Nomenclatura=det.get("nomenclatura"),
        MartilleroId=info["c"]["prof"][4] if (cl in (1, 5) and info["E"] == E1) else None,
        DetalleJson=json.dumps(det, ensure_ascii=False), LegadoId=guid() if info["migrado"] else None, CreadoPor=info["c"]["admin"])

# ---- requerimientos, estudios de título, devoluciones de correo
add("dbo.Requerimiento", EstudioId=E1, ExpedienteId=X[3]["id"], Fecha=date(2026, 9, 12),
    Descripcion="Requerimiento de información a AFIP sobre domicilio fiscal", Enviado=True, EnFolio=False, LegadoId=guid(), CreadoPor=L)
add("dbo.Requerimiento", EstudioId=E2, ExpedienteId=Y[3]["id"], Fecha=date(2026, 10, 1),
    Descripcion="Requerimiento de cuentas bancarias", Enviado=True, EnFolio=True, LegadoId=guid(), CreadoPor=V)
add("dbo.EstudioTitulo", EstudioId=E1, ExpedienteId=X[4]["id"], Fecha=date(2026, 7, 7),
    Descripcion="Estudio de títulos previo a subasta", Pedido=True, EnFolio=True, LegadoId=guid(), CreadoPor=L)
add("dbo.EstudioTitulo", EstudioId=E2, ExpedienteId=Y[6]["id"], Fecha=date(2026, 10, 5),
    Descripcion="Estudio de títulos del inmueble embargado", Pedido=True, EnFolio=False, LegadoId=None, CreadoPor=V)
for info, cod, cam in [(X[1], 0, C1["camp"][0]), (X[2], 1, C1["camp"][0]), (Y[1], 2, C2["camp"][0])]:
    add("dbo.DevolucionCorreo", EstudioId=info["E"], ExpedienteId=info["id"], CodigoDevolucionId=info["c"]["cdev"][cod],
        Fecha=date(2026, 10, 15), CampaniaId=cam, NroCaso=f"CA{rng.randint(100000000, 999999999)}AR",
        UsuarioId=info["c"]["admin"], LegadoId=guid(), CreadoPor=info["c"]["admin"])

# ---- tareas y novedades
TAREAS = []


def tarea(info, E, c, titulo, tipo, venc, resp, estado=1, origen=1, etapa_exp=None, part=(), todos=False, hora=None):
    tid = add("dbo.Tarea", EstudioId=E, ExpedienteId=info["id"] if info else None, TipoTareaId=c["tt"].get(tipo),
              Titulo=titulo, Descripcion=None, Lugar=None, FechaInicio=min(venc, HOY) - timedelta(days=3) if venc else HOY,
              FechaVencimiento=venc, HoraInicio=hora, HoraFin=None, TodoElDia=hora is None, Alerta=venc == HOY,
              ResponsableUsuarioId=resp, VisibleParaTodos=todos, Estado=estado,
              CumplidaUtc=ts(venc - timedelta(days=1)) if estado == 2 else None, Origen=origen,
              EtapaExpedienteId=etapa_exp, LegadoId=guid() if origen == 4 else None, CreadoPor=resp)
    for u in part:
        add("dbo.TareaParticipante", EstudioId=E, TareaId=tid, UsuarioId=u)
    TAREAS.append(tid)
    return tid


t_e1 = [
    tarea(X[2], E1, C1, "Controlar proveído de demanda", "Controlar proveído", HOY, U["martin"], origen=2, etapa_exp=X[2]["ultima"][0]),
    tarea(X[3], E1, C1, "Retirar cédula diligenciada", "Retirar oficio diligenciado", HOY, U["sofia"], origen=2, etapa_exp=X[3]["ultima"][0]),
    tarea(X[8], E1, C1, "Verificar pago cuota 8 del plan", "Revisar plan de pagos", HOY, U["diego"]),
    tarea(X[6], E1, C1, "Llamar al contador del contribuyente", "Llamar al contribuyente", HOY + timedelta(days=2), U["diego"], part=[U["martin"]]),
    tarea(X[5], E1, C1, "Presentar planilla de liquidación", None, HOY + timedelta(days=4), U["carla"]),
    tarea(X[4], E1, C1, "Controlar traba de embargo", "Controlar proveído", HOY - timedelta(days=3), U["martin"]),          # vencida
    tarea(X[9], E1, C1, "Intimar por caducidad del plan", None, date(2026, 10, 20), U["sofia"], estado=2),
    tarea(None, E1, C1, "Reunión semanal de cartera", None, HOY + timedelta(days=1), L, todos=True, origen=4,
          part=[U["martin"], U["sofia"], U["diego"]], hora="09:30:00"),
]
t_e2 = [
    tarea(Y[2], E2, C2, "Controlar proveído de demanda", "Controlar proveído", HOY, V, origen=2, etapa_exp=Y[2]["ultima"][0]),
    tarea(Y[5], E2, C2, "Revisar plan de pagos", "Revisar plan de pagos", HOY + timedelta(days=6), U["tomas"], part=[V, U["julian"]]),
    tarea(Y[3], E2, C2, "Llamar a la empresa", "Llamar al contribuyente", HOY - timedelta(days=1), U["tomas"]),
]
for info, txt, f, leida, por in [
        (X[2], "Téngase por presentada la demanda. Líbrese mandamiento de ejecución y embargo.", HOY - timedelta(days=1), False, None),
        (X[3], "Agréguese cédula diligenciada. Téngase presente.", HOY - timedelta(days=2), False, None),
        (X[4], "Ordénase la traba de embargo sobre el inmueble matrícula 1.234.567.", date(2026, 10, 30), True, U["martin"]),
        (X[6], "Previo a proveer, acompañe título en original.", HOY, False, None),
        (Y[2], "Por presentado, por parte y con el domicilio constituido.", HOY - timedelta(days=3), False, None),
        (Y[4], "Hágase saber la inhibición al Registro General.", date(2026, 10, 2), True, V)]:
    add("dbo.NovedadJudicial", EstudioId=info["E"], ExpedienteId=info["id"], NroExpedienteInformado=info["exp"]["NroExpedienteJudicial"],
        Fecha=f, Texto=txt, Fuente=3 if f < date(2026, 11, 1) else 2, Leida=leida, LeidaUtc=ts(f + timedelta(days=1)) if leida else None,
        LeidaPor=por, TareaId=t_e1[0] if info is X[2] else None, LegadoId=guid() if f < date(2026, 11, 1) else None,
        CreadoPor=info["c"]["admin"])
add("dbo.NovedadJudicial", EstudioId=E1, ExpedienteId=None, NroExpedienteInformado="10999888",
    Fecha=HOY - timedelta(days=1), Texto="Córrase vista a la parte actora de la excepción planteada.", Fuente=2, Leida=False,
    CreadoPor=L)  # sin vincular

# ---- trabajos en segundo plano, documentos, plantillas, escritos
TP1 = add("seg.TrabajoProceso", EstudioId=E1, Tipo=1, Estado=3, Total=5, Procesados=5, Errores=0,
          ParametrosJson=json.dumps({"plantilla": "Cédula art. 6 ley 9024", "expedientes": 5}, ensure_ascii=False),
          ResultadoJson=json.dumps({"zip": "e1/trabajos/1/escritos.zip", "excluidos": []}),
          IniciadoUtc=ts(date(2026, 11, 6), 15, 1), FinalizadoUtc=ts(date(2026, 11, 6), 15, 2), CreadoPor=U["sofia"])
TP2 = add("seg.TrabajoProceso", EstudioId=E1, Tipo=4, Estado=4, Total=7, Procesados=7, Errores=1,
          ParametrosJson=json.dumps({"archivo": "novedades_2026-11-08.xlsx"}),
          ResultadoJson=json.dumps({"sinVincular": 1, "errores": ["Fila 6: fecha inválida '31/11/2026'"]}, ensure_ascii=False),
          IniciadoUtc=ts(date(2026, 11, 8), 18), FinalizadoUtc=ts(date(2026, 11, 8), 18, 1), CreadoPor=L)
TP3 = add("seg.TrabajoProceso", EstudioId=E2, Tipo=2, Estado=2, Total=120, Procesados=48, Errores=0,
          ParametrosJson=json.dumps({"archivo": "padron_tasas_2026.xlsx", "comitente": "Municipalidad de Las Acacias"}),
          IniciadoUtc=ts(HOY, 11, 50), CreadoPor=V)


def documento(E, exp, tipo, nombre, ct, por, desc=None, eliminado=False):
    return add("dbo.Documento", EstudioId=E, ExpedienteId=exp, Tipo=tipo, NombreArchivo=nombre, ContentType=ct,
               TamanioBytes=rng.randint(18_000, 900_000), Sha256="%064x" % rng.getrandbits(256),
               RutaAlmacenamiento=f"e{E}/{'exp/' + str(exp) if exp else 'plantillas'}/{guid()}", Descripcion=desc,
               Eliminado=eliminado, EliminadoUtc=ts(date(2026, 10, 2)) if eliminado else None,
               EliminadoPor=por if eliminado else None, MotivoEliminacion="Archivo cargado por error en otro expediente" if eliminado else None,
               CreadoPor=por, CreadoUtc=ts(date(2026, 9, 15)))


DOCX = "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
PV = {}
for E, c, por in [(E1, C1, L), (E2, C2, V)]:
    for n, pid in c["plant"].items():
        versiones = 2 if (E == E1 and n == "Cédula art. 6 ley 9024") else 1
        for v in range(1, versiones + 1):
            doc = documento(E, None, 4, n.replace(" ", "_").replace(".", "") + f"_v{v}.docx", DOCX, por)
            PV[(E, n, v)] = add("conf.PlantillaEscritoVersion", EstudioId=E, PlantillaEscritoId=pid, NroVersion=v, DocumentoId=doc,
                                Notas="Convertida desde .dot del legado" if v == 1 else "Se agrega bloque de codemandados",
                                Vigente=v == versiones, CreadoPor=por)
for info in (X[2], X[3], X[4], X[8], X[9]):
    doc = documento(E1, info["id"], 2, f"Cedula_{info['nro']:05d}.docx", DOCX, U["sofia"])
    add("dbo.EscritoGenerado", EstudioId=E1, ExpedienteId=info["id"], PlantillaEscritoVersionId=PV[(E1, "Cédula art. 6 ley 9024", 2)],
        EtapaId=C1["etapa"]["P04"], InstanciaId=None, TrabajoProcesoId=TP1, UsuarioId=U["sofia"],
        FechaUtc=ts(date(2026, 11, 6), 15, 2), DocumentoId=doc, Estado=1, CreadoPor=U["sofia"])
for info, et, ins in [(X[4], "P02", None), (X[10], None, "Archivado"), (Y[2], "P02", None)]:
    add("dbo.EscritoGenerado", EstudioId=info["E"], ExpedienteId=info["id"], PlantillaEscritoVersionId=None,
        EtapaId=info["c"]["etapa"].get(et), InstanciaId=info["c"]["inst"].get(ins), TrabajoProcesoId=None,
        UsuarioId=info["c"]["admin"], FechaUtc=ts(info["exp"]["FechaAlta"] + timedelta(days=10)), DocumentoId=None,
        Estado=2, LegadoId=guid(), CreadoPor=info["c"]["admin"])
documento(E1, X[4]["id"], 1, "Informe_dominio.pdf", "application/pdf", U["martin"], "Informe de dominio RGP")
documento(E1, X[6]["id"], 1, "Determinacion_oficio.pdf", "application/pdf", L, "Resolución determinativa")
documento(E1, X[6]["id"], 1, "Foto_fachada.jpg", "image/jpeg", U["diego"], "Cargado por error", eliminado=True)
documento(E2, Y[3]["id"], 1, "Constancia_AFIP.pdf", "application/pdf", V, "Constancia de inscripción")


# ====================================================================== liquidaciones (instantáneas)
def vigente(tabla, E, cod, f, comi=None):
    filas = [r for r in D[tabla] if r["EstudioId"] == E and r["Codigo"] == cod and r["VigenteDesde"] <= f
             and (r.get("VigenteHasta") is None or r["VigenteHasta"] >= f) and r.get("ComitenteId") in (None, comi)]
    filas.sort(key=lambda r: (r.get("ComitenteId") is not None, r["VigenteDesde"]))
    return filas[-1]


def liquidar(info, fecha, version, estado, por):
    E, c = info["E"], info["c"]
    comi = info["comi"]
    hito = info["hito2"]
    corte = date(2014, 3, 31)
    tasa_cap = vigente("liq.TasaInteres", E, "INTERES_CAPITAL", fecha, comi)["Valor"]
    lineas = []

    def linea(rubro, item, concepto, importe, base=None, tasa=None, dias=None, per=None, det=None):
        lineas.append(dict(RubroPlanillaId=c["rubro"][rubro], ItemPlanillaId=c["item"].get(item), Concepto=concepto,
                           Base=base, Tasa=tasa, Dias=dias, PeriodoAdeudadoId=per, Importe=m(importe), EsManual=item is None,
                           Detalle=det))
    linea("Capital", "Capital reclamado", f"Capital al {hito:%d/%m/%Y}", info["cap"], base=info["cap"])
    interes = Decimal(0)
    for pid, (anio, cuota, venc, trib, recg) in info["periodos"]:
        dias = (fecha - venc).days
        imp = m(trib * tasa_cap * dias)
        interes += imp
        linea("Intereses", "Interés sobre capital", f"Interés período {anio}/{cuota}", imp, base=trib, tasa=tasa_cap,
              dias=dias, per=pid, det=f"{trib} × {tasa_cap} × {dias} días (tramo posterior a {corte:%d/%m/%Y})")
    tj_pct = vigente("liq.ParametroLiquidacion", E, "TASA_JUSTICIA_PCT", fecha, comi)["Valor"]
    tope = next(r for r in D["liq.ItemPlanillaTope"] if r["EstudioId"] == E and r["ItemPlanillaId"] == c["item"]["Tasa de justicia"]
                and r["VigenteDesde"] <= hito and (r["VigenteHasta"] is None or r["VigenteHasta"] >= hito)) \
        if hito >= date(2025, 1, 1) else dict(Minimo=Decimal("15000"), Maximo=Decimal("1500000"))
    tj = min(max(m(info["exp"]["MontoTitulo"] * tj_pct / 100), tope["Minimo"]), tope["Maximo"])
    dias_tj = (fecha - hito).days
    tj_int = m(tj * Decimal("0.0657534") * dias_tj / 100)
    aportes = max(m(info["exp"]["MontoTitulo"] * Decimal("0.02")), Decimal("12000"))
    gastos_dil = Decimal("2") * vigente("liq.ParametroLiquidacion", E, "VALOR_JUS", fecha)["Valor"] * Decimal("0.1")
    linea("Gastos", "Tasa de justicia", f"Tasa de justicia ({tj_pct} %)", tj, base=info["exp"]["MontoTitulo"], tasa=tj_pct / 100)
    linea("Gastos", "Tasa de justicia", "Interés tasa de justicia", tj_int, base=tj, dias=dias_tj)
    linea("Gastos", "Aportes Caja de Abogados", "Aportes de ley", aportes)
    linea("Gastos", "Gastos de diligencias", "Cédulas y oficios (0,2 JUS)", gastos_dil)
    if version == 2:
        linea("Gastos", None, "Gastos de publicación de edictos (manual)", Decimal("18500"), det="Ingresado por el usuario")
    base_h = info["cap"] + interes
    escala = [r for r in D["liq.EscalaHonorarios"] if r["EstudioId"] == E and r["VigenteDesde"] <= fecha]
    vig = max(r["VigenteDesde"] for r in escala)
    tramo = next(r for r in escala if r["VigenteDesde"] == vig and r["BaseDesde"] <= base_h and (r["BaseHasta"] is None or base_h <= r["BaseHasta"]))
    jus = vigente("liq.ParametroLiquidacion", E, "VALOR_JUS", fecha)["Valor"]
    hon = min(max(m(base_h * tramo["Porcentaje"] / 100), m(jus * Decimal("6.5"))), m(base_h / 2))
    patro, procu = m(hon * Decimal("0.6")), hon - m(hon * Decimal("0.6"))
    linea("Honorarios", "Honorarios", f"Honorarios patrocinante (60 %) – escala {tramo['Porcentaje']} %", patro, base=base_h, tasa=tramo["Porcentaje"] / 100)
    linea("Honorarios", "Honorarios", "IVA 21 % honorarios patrocinante (Responsable Inscripto)", m(patro * Decimal("0.21")))
    linea("Honorarios", "Honorarios", "Honorarios procurador (40 %)", procu)
    tot = defaultdict(Decimal)
    for ln in lineas:
        tot[[k for k, v in c["rubro"].items() if v == ln["RubroPlanillaId"]][0]] += ln["Importe"]
    lid = nid("liq.Liquidacion")
    D["liq.Liquidacion"].append(dict(
        Id=lid, EstudioId=E, ExpedienteId=info["id"], NroVersion=version, FechaCalculo=fecha, Estado=estado, JuzgadoId=info["juz"],
        TotalCapital=tot["Capital"], TotalInteres=tot["Intereses"], TotalGastos=tot["Gastos"],
        HonorariosJus=(tot["Honorarios"] / jus).quantize(Decimal("0.01")), HonorariosPesos=tot["Honorarios"],
        Total=tot["Capital"] + tot["Intereses"] + tot["Gastos"] + tot["Honorarios"],
        ParametrosJson=json.dumps({"fechaCalculo": fecha.isoformat(), "valorJus": str(jus), "tasaInteresCapitalDiaria": str(tasa_cap),
                                   "tasaJusticiaPct": str(tj_pct), "topeTasaJusticia": {"min": str(tope["Minimo"]), "max": str(tope["Maximo"])},
                                   "escalaHonorarios": {"vigenteDesde": vig.isoformat(), "porcentaje": str(tramo["Porcentaje"])},
                                   "repartoPatrocinanteProcurador": [60, 40], "ivaPct": 21,
                                   "fechaDemandaPresentada": hito.isoformat()}),
        DocumentoId=None, CreadoPor=por, CreadoUtc=ts(fecha, 12)))
    for i, ln in enumerate(lineas):
        add("liq.LiquidacionLinea", EstudioId=E, LiquidacionId=lid, Orden=i + 1, **ln)


liquidar(X[4], date(2026, 9, 1), 1, 2, U["carla"])
liquidar(X[4], date(2026, 11, 2), 2, 3, U["carla"])
liquidar(X[5], date(2026, 10, 15), 1, 2, U["carla"])
liquidar(X[2], HOY, 1, 1, U["carla"])
liquidar(Y[4], date(2026, 10, 20), 1, 2, V)
liquidar(Y[6], HOY, 1, 1, V)
D["liq.Liquidacion"][0]["Estado"] = 4    # la versión 1 queda anulada al presentar la 2

# ====================================================================== auditoría
AUD = []


def aud(E, u, f, mod, ent, eid, acc, res=1, cambios=None, tp=None, origen=1, ip="190.12.33.4"):
    add("aud.AuditoriaEvento", EstudioId=E, UsuarioId=u, FechaUtc=f, Modulo=mod, Entidad=ent, EntidadId=str(eid) if eid else None,
        Accion=acc, Resultado=res, CambiosJson=json.dumps(cambios, ensure_ascii=False) if cambios else None, TrabajoProcesoId=tp,
        CorrelacionId=guid(), Ip=ip, UserAgent="Mozilla/5.0 (Windows NT 10.0)")


# Estudio 2 retiene 90 días: estos eventos de junio y julio de 2026 los elimina la primera purga
for f, ent, eid, acc, camb in [
        (datetime(2026, 6, 20, 10, 5), "Expediente", Y[4]["id"], 2, {"JuzgadoId": {"antes": None, "despues": Y[4]["juz"]}}),
        (datetime(2026, 7, 3, 16, 45), "Expediente", Y[6]["id"], 2, {"ObservacionesAdministrativas": {"antes": None, "despues": "Pedir informe de dominio"}}),
        (datetime(2026, 7, 21, 9, 0), "EscritoGenerado", Y[2]["id"], 7, None)]:
    aud(E2, V, f, "expedientes" if ent == "Expediente" else "escritos", ent, eid, acc, cambios=camb)
aud(None, U["super"], ts(date(2026, 3, 2), 12), "plataforma", "Estudio", E1, 1, cambios={"Nombre": {"antes": None, "despues": "Estudio Ferreyra & Asociados"}})
aud(None, U["super"], ts(date(2026, 6, 10), 12), "plataforma", "Estudio", E2, 1,
    cambios={"Nombre": {"antes": None, "despues": "Lucero Procuraciones"}, "copiaDesde": {"antes": None, "despues": E1}})
aud(None, U["super"], ts(date(2026, 6, 12), 10), "plataforma", "EstudioUsuario", MEMB[(E2, "julian")], 13,
    cambios={"Estado": {"antes": 2, "despues": 3}})
aud(None, U["super"], ts(date(2026, 10, 20), 14), "plataforma", "AccesoSoporte", 1, 14)
aud(E1, L, ts(HOY, 8, 45), "seguridad", None, None, 9)
aud(E1, None, ts(HOY, 8, 41), "seguridad", None, None, 10, res=2, cambios={"email": "laura.f***@ferreyra.test"}, ip="45.230.9.17")
aud(E1, U["martin"], ts(HOY, 9, 2), "seguridad", None, None, 9)
aud(E1, U["martin"], ts(HOY, 9, 15), "expedientes", "Expediente", X[2]["id"], 2,
    cambios={"Recaratulacion": {"antes": None, "despues": X[2]["exp"]["Recaratulacion"]}})
aud(E1, L, ts(date(2026, 8, 3), 11), "expedientes", "Expediente", X[11]["id"], 3,
    cambios={"Anulado": {"antes": False, "despues": True}, "MotivoAnulacion": {"antes": None, "despues": X[11]["exp"]["MotivoAnulacion"]}})
aud(E1, U["sofia"], ts(date(2026, 11, 6), 15, 2), "escritos", "TrabajoProceso", TP1, 7, tp=TP1)
for info in (X[2], X[3], X[4], X[8], X[9]):
    aud(E1, U["sofia"], ts(date(2026, 11, 6), 15, 2), "escritos", "EscritoGenerado", info["id"], 7, tp=TP1)
aud(E1, U["carla"], ts(date(2026, 11, 2), 12), "liquidaciones", "Liquidacion", 2, 6)
aud(E1, U["carla"], ts(date(2026, 11, 2), 12, 5), "liquidaciones", "Liquidacion", 2, 8, cambios={"formato": {"antes": None, "despues": "PDF"}})
aud(E1, U["diego"], ts(date(2026, 10, 2), 10), "documentos", "Documento", 3, 4,
    cambios={"MotivoEliminacion": {"antes": None, "despues": "Archivo cargado por error en otro expediente"}})
aud(E1, L, ts(date(2026, 9, 1), 17), "usuarios", "EstudioUsuario", MEMB[(E1, "ernesto")], 5, cambios={"Estado": {"antes": 3, "despues": 4}})
aud(E1, L, ts(date(2026, 9, 1), 17, 5), "usuarios", "Rol", C1["rol"]["Gestor"], 12,
    cambios={"permisos": {"antes": ["planesPago.consultar"], "despues": ["planesPago.consultar", "planesPago.crear", "planesPago.editar"]}})
aud(E1, U["pablo"], ts(date(2026, 11, 3), 16), "auditoria", None, None, 8, cambios={"filtro": {"antes": None, "despues": "octubre 2026"}})
aud(E1, U["romina"], ts(date(2026, 11, 4), 11), "expedientes", "Expediente", X[9]["id"], 2, res=2,
    cambios={"motivo": {"antes": None, "despues": "Sin permiso: el expediente es de otro comitente"}})
aud(E2, V, ts(HOY, 8, 30), "seguridad", None, None, 9)
aud(E2, V, ts(date(2026, 6, 15), 9), "usuarios", "EstudioUsuario", MEMB[(E2, "romina")], 1, cambios={"Estado": {"antes": None, "despues": 2}})
aud(E2, U["tomas"], ts(HOY, 10, 10), "expedientes", "Expediente", Y[2]["id"], 2,
    cambios={"Observaciones": {"antes": None, "despues": Y[2]["exp"]["Observaciones"]}})
aud(E2, V, ts(HOY, 11, 50), "expedientes", "TrabajoProceso", TP3, 1, tp=TP3)

# ====================================================================== datos a revisar
for E, tipo, ent, eid, tabla, campo, orig, prop, est, por, com in [
        (E1, "Monto no numérico", "Expediente", X[3]["id"], "Expediente", "Monto", "$ 12.500,50 aprox", "12500.50", 1, None, None),
        (E1, "Fecha inválida", "PeriodoAdeudado", X[2]["periodos"][0][0], "ExpedientePeriodo", "Vencimiento", "31/02/2023", None, 1, None, None),
        (E1, "Persona duplicada", "Persona", X[2]["dem"], "Persona", "NroDocumento", "DNI repetido en 2 personas", "Unificar con la persona más reciente", 1, None, None),
        (E1, "Nro de expediente judicial faltante", "Expediente", X[9]["id"], "Expediente", "CodigoBarra", "", None, 1, None, None),
        (E1, "Estado de etapa inconsistente", "EtapaExpediente", None, "EtapaExpediente", "Estado", "0", "1 (alta)", 2, L, "Se normalizó 0 → alta"),
        (E1, "Plan de pagos sin detalle", "Expediente", X[10]["id"], "Expediente", "NroPlanPagos", "PP-4411", None, 3, L, "El plan no existe en el municipio"),
        (E2, "Monto no numérico", "Expediente", Y[3]["id"], "Expediente", "Monto", "s/d", None, 1, None, None),
        (E2, "Domicilio sin localidad", "Domicilio", None, "Domicilio", "LocalidadID", None, "Río Cuarto", 1, None, None)]:
    add("mig.DatoARevisar", EstudioId=E, Tipo=tipo, Entidad=ent, EntidadId=eid, TablaOrigen=tabla, ClaveOrigen=guid(), Campo=campo,
        ValorOriginal=orig, ValorPropuesto=prop, Estado=est, ResueltoPor=por, ResueltoUtc=ts(date(2026, 10, 30)) if por else None,
        Comentario=com, CreadoPor=None)
