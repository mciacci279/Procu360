"""Arma la planilla de equivalencias para que la complete el estudio.

Lee los CSV generados desde ProcuIntermedia y produce un .xlsx con:
  - Instrucciones
  - Estados (14 valores)  -> Instancia de Procu360
  - Ultimos movimientos (64 valores) -> Etapa de Procu360
  - Catalogos (las listas validas de destino, usadas por los desplegables)
"""
import csv
import os

from openpyxl import Workbook
from openpyxl.styles import Alignment, Border, Font, PatternFill, Side
from openpyxl.utils import get_column_letter
from openpyxl.worksheet.datavalidation import DataValidation

BASE = os.path.dirname(os.path.abspath(__file__))

INSTANCIAS = [
    "Pre judicial",
    "Judicial",
    "Con sentencia",
    "Plan de pagos",
    "Archivado",
]

ETAPAS = [
    "Intimación pre judicial",
    "Inicio de demanda",
    "Demanda presentada",
    "Cédula de notificación",
    "Oficio a registro",
    "Sentencia monitoria",
    "Ejecución de sentencia",
    "Planilla de liquidación presentada",
    "Archivo",
    "Embargo solicitado",
    "Embargo trabado",
    "Inhibición general de bienes",
    "Levantamiento de medida",
]

# Opciones que no son una etapa/instancia del catálogo pero hacen falta para cerrar el mapeo.
EXTRA = [
    "(no migrar: dato sin valor)",
    "(crear etapa nueva: indicar nombre en observaciones)",
    "(no sé: lo definimos juntos)",
]

AZUL = "1D4ED8"
CELESTE = "EFF6FF"
GRIS = "F1F5F9"
AMBAR = "FEF3C7"

borde = Border(*[Side(style="thin", color="CBD5E1")] * 4)


def leer(archivo):
    ruta = os.path.join(BASE, archivo)
    with open(ruta, encoding="utf-8-sig", newline="") as f:
        return list(csv.reader(f, delimiter=";"))


def encabezar(ws, titulos, anchos):
    ws.append(titulos)
    for i, (t, a) in enumerate(zip(titulos, anchos), start=1):
        c = ws.cell(row=1, column=i)
        c.font = Font(bold=True, color="FFFFFF", size=11)
        c.fill = PatternFill("solid", fgColor=AZUL)
        c.alignment = Alignment(vertical="center", wrap_text=True)
        c.border = borde
        ws.column_dimensions[get_column_letter(i)].width = a
    ws.row_dimensions[1].height = 32
    ws.freeze_panes = "A2"


def hoja_mapeo(wb, nombre, archivo, etiqueta_destino, opciones, rango_catalogo):
    filas = leer(archivo)[1:]  # sin encabezado
    filas = [f for f in filas if f and f[0]]

    ws = wb.create_sheet(nombre)
    encabezar(
        ws,
        [
            "Valor en el sistema actual",
            "Expedientes",
            "% del total",
            etiqueta_destino,
            "Observaciones (nombre de etapa nueva, dudas, etc.)",
        ],
        [52, 12, 11, 34, 50],
    )

    for valor, cantidad, pct, _, _ in filas:
        ws.append([valor, int(cantidad), float(pct) / 100.0, None, None])

    ultima = ws.max_row
    for r in range(2, ultima + 1):
        ws.cell(row=r, column=1).alignment = Alignment(wrap_text=True, vertical="center")
        ws.cell(row=r, column=2).number_format = "#,##0"
        ws.cell(row=r, column=3).number_format = "0.0%"
        destino = ws.cell(row=r, column=4)
        destino.fill = PatternFill("solid", fgColor=AMBAR)
        destino.alignment = Alignment(vertical="center")
        ws.cell(row=r, column=5).alignment = Alignment(wrap_text=True, vertical="center")
        for c in range(1, 6):
            ws.cell(row=r, column=c).border = borde

    # Desplegable apuntando a la hoja Catalogos, para que no haya que tipear.
    dv = DataValidation(type="list", formula1=rango_catalogo, allow_blank=True, showDropDown=False)
    dv.error = "Elegí un valor de la lista desplegable."
    dv.errorTitle = "Valor no válido"
    dv.prompt = "Elegí a qué corresponde este valor en Procu360."
    dv.promptTitle = etiqueta_destino
    ws.add_data_validation(dv)
    dv.add(f"D2:D{ultima}")

    # Total al pie, para que se vea que la suma cierra con los 9.167 expedientes.
    ws.append([])
    fila_total = ws.max_row + 1
    ws.cell(row=fila_total, column=1, value="TOTAL").font = Font(bold=True)
    t = ws.cell(row=fila_total, column=2, value=f"=SUM(B2:B{ultima})")
    t.font = Font(bold=True)
    t.number_format = "#,##0"
    return len(filas)


def hoja_catalogos(wb):
    ws = wb.create_sheet("Catalogos")
    encabezar(ws, ["Instancias de Procu360", "Etapas de Procu360"], [34, 40])

    opciones_inst = INSTANCIAS + EXTRA
    opciones_etapa = ETAPAS + EXTRA
    for i in range(max(len(opciones_inst), len(opciones_etapa))):
        ws.append([
            opciones_inst[i] if i < len(opciones_inst) else None,
            opciones_etapa[i] if i < len(opciones_etapa) else None,
        ])
    for r in range(2, ws.max_row + 1):
        for c in (1, 2):
            celda = ws.cell(row=r, column=c)
            celda.border = borde
            if celda.value and str(celda.value).startswith("("):
                celda.font = Font(italic=True, color="64748B")
    return len(opciones_inst), len(opciones_etapa)


def hoja_instrucciones(wb, n_estados, n_movs):
    ws = wb.create_sheet("Instrucciones", 0)
    ws.column_dimensions["A"].width = 112

    titulo = ws.cell(row=1, column=1, value="Procu360 · Equivalencias para la migración")
    titulo.font = Font(bold=True, size=16, color=AZUL)
    ws.row_dimensions[1].height = 26

    texto = [
        "",
        "Qué necesitamos",
        f"En el sistema actual hay {n_estados} valores distintos de «estado» y {n_movs} de «último movimiento procesal». "
        "Para migrar los expedientes necesitamos saber a qué corresponde cada uno en Procu360.",
        "",
        "Cómo completarla",
        "1. Hoja «Estados»: por cada valor, elegí en la columna amarilla la INSTANCIA que le corresponde.",
        "2. Hoja «Ultimos movimientos»: por cada valor, elegí la ETAPA que le corresponde.",
        "3. Las columnas amarillas tienen una lista desplegable: no hace falta escribir.",
        "4. Si ninguna opción sirve, elegí «(crear etapa nueva…)» y escribí el nombre en Observaciones.",
        "5. Si un valor no debe migrarse (basura, pruebas), elegí «(no migrar: dato sin valor)».",
        "6. Si hay dudas, elegí «(no sé: lo definimos juntos)» y seguí: no hace falta trabarse.",
        "",
        "Por dónde conviene empezar",
        "Las filas están ordenadas de mayor a menor cantidad de expedientes. Las primeras 10 de cada hoja "
        "cubren más de el 90% de los casos, así que son las que más importan. Las de 1 o 2 expedientes "
        "pueden resolverse al final o marcarse como «no sé».",
        "",
        "Aclaraciones que nos ayudan mucho",
        "· «Estado» y «último movimiento» tienen valores repetidos entre sí (por ejemplo «Cancelado» "
        "aparece en los dos). ¿Son lo mismo o significan cosas distintas según la columna?",
        "· Hay valores que parecen duplicados por mayúsculas o tildes (por ejemplo «1er Decreto», "
        "«1ER. DECRETO»). Si son el mismo concepto, alcanza con mapearlos a la misma etapa.",
        "· «SIN DATOS» y los vacíos son casi un tercio de los expedientes: ¿qué significan? "
        "¿Son expedientes sin movimiento todavía, o se perdió el dato?",
        "· «Planilla Dif», «Pedidos decaimiento», «jarrón»: no tenemos claro a qué se refieren. "
        "Una línea de explicación de cada uno nos alcanza.",
        "",
        "La columna «Expedientes» es la cantidad real que tiene hoy ese valor, sobre 9.167 expedientes en total.",
    ]

    for t in texto:
        ws.append([t])
        c = ws.cell(row=ws.max_row, column=1)
        c.alignment = Alignment(wrap_text=True, vertical="top")
        if t in ("Qué necesitamos", "Cómo completarla", "Por dónde conviene empezar",
                 "Aclaraciones que nos ayudan mucho"):
            c.font = Font(bold=True, size=12)
            c.fill = PatternFill("solid", fgColor=CELESTE)
        elif t.startswith(("1.", "2.", "3.", "4.", "5.", "6.", "·")):
            ws.row_dimensions[ws.max_row].height = None


def main():
    wb = Workbook()
    wb.remove(wb.active)

    n_inst, n_etapa = 0, 0
    n_estados = hoja_mapeo(
        wb, "Estados", "1-estados.csv", "INSTANCIA en Procu360",
        INSTANCIAS, "=Catalogos!$A$2:$A$9",
    )
    n_movs = hoja_mapeo(
        wb, "Ultimos movimientos", "2-ultimos-movimientos.csv", "ETAPA en Procu360",
        ETAPAS, "=Catalogos!$B$2:$B$17",
    )
    hoja_catalogos(wb)
    hoja_instrucciones(wb, n_estados, n_movs)

    wb._sheets = [
        wb["Instrucciones"], wb["Estados"], wb["Ultimos movimientos"], wb["Catalogos"],
    ]

    salida = os.path.join(BASE, "Procu360-equivalencias-migracion.xlsx")
    wb.save(salida)
    print(f"OK {salida}")
    print(f"   Estados: {n_estados} filas | Ultimos movimientos: {n_movs} filas")


if __name__ == "__main__":
    main()
