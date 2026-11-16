"""Genera EcoTilt_Documentacion.docx con el formato del examen anterior (Pokimun).
Uso: python documento.py <carpeta_con_imagenes> <salida.docx>
Las imágenes se generan con figuras.py; los logos están en la carpeta img/."""
import re
import sys
from pathlib import Path

from docx import Document
from docx.enum.section import WD_SECTION
from docx.enum.table import WD_TABLE_ALIGNMENT, WD_CELL_VERTICAL_ALIGNMENT
from docx.enum.text import WD_ALIGN_PARAGRAPH, WD_BREAK
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.shared import Pt, Cm, RGBColor

VERSION = "v0.1 — 16/11/2026"
JUEGO = "EcoTilt"
LEMA = "Inclina, separá y reciclá"
INTEGRANTES = "Cussi Walter Leonel - Espinosa Lautaro Eduardo - Estrada Luis Brian Gabriel"
DOCENTES = "Felix Antonio Flores — Samuel Bustamante"
REPO = "https://github.com/Lautaro-Espinosa015/TrabajoFinalDise-odeVideojuegosII"

NAVY = RGBColor(0x1F, 0x38, 0x64)
BLUE = RGBColor(0x2E, 0x74, 0xB5)
LINK_BLUE = "0070C0"
HEAD_FILL = "6E9EF8"
BODY_FILL = "CCE8FF"
PURPLE = "7030A0"
BODY_FONT = "Cambria"

IMG = Path(sys.argv[1] if len(sys.argv) > 1 else ".")
OUT = Path(sys.argv[2] if len(sys.argv) > 2 else "EcoTilt_Documentacion.docx")


# ---------------------------------------------------------------- utilidades
def shade(cell, hex_fill):
    tc_pr = cell._tc.get_or_add_tcPr()
    shd = OxmlElement("w:shd")
    shd.set(qn("w:val"), "clear")
    shd.set(qn("w:color"), "auto")
    shd.set(qn("w:fill"), hex_fill)
    tc_pr.append(shd)


def set_borders(table, color="BFBFBF", size=4):
    tbl_pr = table._tbl.tblPr
    borders = OxmlElement("w:tblBorders")
    for edge in ("top", "left", "bottom", "right", "insideH", "insideV"):
        el = OxmlElement(f"w:{edge}")
        el.set(qn("w:val"), "single" if size else "nil")
        el.set(qn("w:sz"), str(size))
        el.set(qn("w:color"), color)
        borders.append(el)
    tbl_pr.append(borders)


def cell_margins(table, top=80, bottom=80, left=100, right=100):
    tbl_pr = table._tbl.tblPr
    mar = OxmlElement("w:tblCellMar")
    for k, v in (("top", top), ("left", left), ("bottom", bottom), ("right", right)):
        el = OxmlElement(f"w:{k}")
        el.set(qn("w:w"), str(v))
        el.set(qn("w:type"), "dxa")
        mar.append(el)
    tbl_pr.append(mar)


def add_rich(paragraph, text, size=None, color=None, font=None, base_bold=False, italic=False):
    """Admite **negrita** e *itálica* simples dentro del texto."""
    for part in re.split(r"(\*\*[^*]+\*\*|\*[^*]+\*)", text):
        if not part:
            continue
        bold, ital = base_bold, italic
        if part.startswith("**"):
            part, bold = part[2:-2], True
        elif part.startswith("*"):
            part, ital = part[1:-1], True
        run = paragraph.add_run(part)
        run.bold = bold
        run.italic = ital
        if size:
            run.font.size = Pt(size)
        if color is not None:
            run.font.color.rgb = color
        if font:
            run.font.name = font
            run._element.rPr.rFonts.set(qn("w:eastAsia"), font)
    return paragraph


def field(paragraph, instr):
    fld = OxmlElement("w:fldSimple")
    fld.set(qn("w:instr"), instr)
    r = OxmlElement("w:r")
    t = OxmlElement("w:t")
    t.text = "1"
    r.append(t)
    fld.append(r)
    paragraph._p.append(fld)
    return fld


def style_field_runs(fld, size, color_hex, bold=True):
    for r in fld.iter(qn("w:r")):
        rpr = OxmlElement("w:rPr")
        if bold:
            rpr.append(OxmlElement("w:b"))
        c = OxmlElement("w:color"); c.set(qn("w:val"), color_hex); rpr.append(c)
        s = OxmlElement("w:sz"); s.set(qn("w:val"), str(size * 2)); rpr.append(s)
        r.insert(0, rpr)


def fix_widths(table, widths_cm):
    """Fija anchos de columna (Word y LibreOffice) y evita que las filas se partan entre páginas."""
    tbl = table._tbl
    tbl_pr = tbl.tblPr
    layout = OxmlElement("w:tblLayout"); layout.set(qn("w:type"), "fixed"); tbl_pr.append(layout)
    grid = tbl.tblGrid
    for i, gc in enumerate(grid.findall(qn("w:gridCol"))):
        gc.set(qn("w:w"), str(int(widths_cm[i] * 567)))
    for row in table.rows:
        tr_pr = row._tr.get_or_add_trPr()
        cs = OxmlElement("w:cantSplit"); cs.set(qn("w:val"), "true"); tr_pr.append(cs)
        for i, cell in enumerate(row.cells):
            if i < len(widths_cm):
                cell.width = Cm(widths_cm[i])


def keep_with_next(p):
    p.paragraph_format.keep_with_next = True


# ---------------------------------------------------------------- documento
doc = Document()
styles = doc.styles
normal = styles["Normal"]
normal.font.name = BODY_FONT
normal.element.rPr.rFonts.set(qn("w:eastAsia"), BODY_FONT)
normal.font.size = Pt(11)
normal.paragraph_format.space_after = Pt(4)
normal.paragraph_format.line_spacing = 1.1

for name, size, color, before in (("Heading 1", 17, NAVY, 14), ("Heading 2", 13, BLUE, 10)):
    st = styles[name]
    st.font.name = BODY_FONT
    st.element.rPr.rFonts.set(qn("w:eastAsia"), BODY_FONT)
    st.element.rPr.rFonts.set(qn("w:ascii"), BODY_FONT)
    st.element.rPr.rFonts.set(qn("w:hAnsi"), BODY_FONT)
    st.font.size = Pt(size)
    st.font.bold = True
    st.font.color.rgb = color
    st.paragraph_format.space_before = Pt(before)
    st.paragraph_format.space_after = Pt(4)

sec = doc.sections[0]
sec.page_height, sec.page_width = Cm(29.7), Cm(21.0)
sec.left_margin = sec.right_margin = Cm(2.0)
sec.top_margin = Cm(1.2)
sec.bottom_margin = Cm(1.6)
sec.header_distance = Cm(0.6)
sec.footer_distance = Cm(0.8)
sec.different_first_page_header_footer = True
CONTENT_W = 17.0


def build_header(section):
    hdr = section.header
    hdr.is_linked_to_previous = False
    p0 = hdr.paragraphs[0]
    t = hdr.add_table(rows=2, cols=3, width=Cm(CONTENT_W))
    t.alignment = WD_TABLE_ALIGNMENT.CENTER
    set_borders(t, "000000", 6)
    fix_widths(t, (2.4, 12.2, 2.4))
    left = t.cell(0, 0).merge(t.cell(1, 0))
    right = t.cell(0, 2).merge(t.cell(1, 2))
    for c in (left, right):
        c.vertical_alignment = WD_CELL_VERTICAL_ALIGNMENT.CENTER
    lp = left.paragraphs[0]; lp.alignment = WD_ALIGN_PARAGRAPH.CENTER
    lp.add_run().add_picture(str(IMG / "logo_unju.png"), height=Cm(1.5))
    rp = right.paragraphs[0]; rp.alignment = WD_ALIGN_PARAGRAPH.CENTER
    rp.add_run().add_picture(str(IMG / "logo_gamepad.png"), height=Cm(0.95))
    add_rich(right.add_paragraph(), "Período Lectivo\n2026", size=7.5, font="Times New Roman").alignment = WD_ALIGN_PARAGRAPH.CENTER

    def two_lines(cell, a, b, size_b):
        p = cell.paragraphs[0]; p.alignment = WD_ALIGN_PARAGRAPH.CENTER
        p.paragraph_format.space_after = Pt(0)
        add_rich(p, a, size=9, font="Times New Roman", base_bold=True)
        q = cell.add_paragraph(); q.alignment = WD_ALIGN_PARAGRAPH.CENTER
        q.paragraph_format.space_after = Pt(0)
        add_rich(q, b, size=size_b, font="Times New Roman")

    two_lines(t.cell(0, 1), "Diseño de Videojuegos II", "Tecnicatura Universitaria en Diseño Integral de Videojuegos", 9)
    two_lines(t.cell(1, 1), "Segundo Examen Práctico", INTEGRANTES, 6.5)
    # quitar el párrafo vacío inicial del encabezado para que la tabla quede arriba
    p0._p.getparent().remove(p0._p)
    hdr._element.append(OxmlElement("w:p"))


def build_footer(section):
    ftr = section.footer
    ftr.is_linked_to_previous = False
    ftr.paragraphs[0]._p.getparent().remove(ftr.paragraphs[0]._p)
    t = ftr.add_table(rows=1, cols=2, width=Cm(CONTENT_W))
    set_borders(t, "FFFFFF", 0)
    fix_widths(t, (15.9, 1.1))
    c0, c1 = t.rows[0].cells
    c0.vertical_alignment = c1.vertical_alignment = WD_CELL_VERTICAL_ALIGNMENT.CENTER
    add_rich(c0.paragraphs[0], f"Diseño de Videojuegos II — {DOCENTES}", size=8.5,
             color=RGBColor(0x5B, 0x9B, 0xD5), font="Times New Roman", base_bold=True, italic=True)
    shade(c1, PURPLE)
    p = c1.paragraphs[0]; p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    style_field_runs(field(p, "PAGE"), 13, "FFFFFF")
    ftr._element.append(OxmlElement("w:p"))


build_header(sec)
build_footer(sec)


# ---------------------------------------------------------------- bloques de contenido
def h1(text):
    p = doc.add_heading(text, level=1); keep_with_next(p); return p


def h2(text):
    p = doc.add_heading(text, level=2); keep_with_next(p); return p


def para(text, align=WD_ALIGN_PARAGRAPH.JUSTIFY, size=None):
    p = doc.add_paragraph(); p.alignment = align
    return add_rich(p, text, size=size)


def bullets(items, numbered=False):
    for it in items:
        p = doc.add_paragraph(style="List Number" if numbered else "List Bullet")
        p.alignment = WD_ALIGN_PARAGRAPH.LEFT if "http" in it else WD_ALIGN_PARAGRAPH.JUSTIFY
        p.paragraph_format.space_after = Pt(2)
        add_rich(p, it)


def table(headers, rows, widths, first_col_header=True, size=10):
    t = doc.add_table(rows=1 + len(rows), cols=len(headers))
    t.alignment = WD_TABLE_ALIGNMENT.CENTER
    set_borders(t, "9DC3E6", 4)
    cell_margins(t)
    for r_i, values in enumerate([headers] + rows):
        for c_i, value in enumerate(values):
            cell = t.cell(r_i, c_i)
            cell.width = Cm(widths[c_i])
            cell.vertical_alignment = WD_CELL_VERTICAL_ALIGNMENT.CENTER
            is_head = r_i == 0 or (first_col_header and c_i == 0)
            shade(cell, HEAD_FILL if is_head else BODY_FILL)
            lines = value.split("\n")
            p = cell.paragraphs[0]
            p.paragraph_format.space_after = Pt(0)
            add_rich(p, lines[0], size=size, base_bold=is_head)
            for extra in lines[1:]:
                q = cell.add_paragraph(); q.paragraph_format.space_after = Pt(0)
                add_rich(q, extra, size=size, base_bold=is_head)
    fix_widths(t, widths)
    # repetir encabezado si la tabla salta de página
    tr_pr = t.rows[0]._tr.get_or_add_trPr()
    th = OxmlElement("w:tblHeader"); th.set(qn("w:val"), "true"); tr_pr.append(th)
    doc.add_paragraph().paragraph_format.space_after = Pt(2)
    return t


def figure(path, width_cm, caption):
    p = doc.add_paragraph(); p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    keep_with_next(p)
    p.add_run().add_picture(str(path), width=Cm(width_cm))
    c = doc.add_paragraph(); c.alignment = WD_ALIGN_PARAGRAPH.CENTER
    add_rich(c, caption, size=9, color=RGBColor(0x59, 0x59, 0x59), italic=True)


def page_break():
    doc.add_paragraph().add_run().add_break(WD_BREAK.PAGE)


# ---------------------------------------------------------------- portada
p = doc.add_paragraph(); p.alignment = WD_ALIGN_PARAGRAPH.CENTER
p.paragraph_format.space_before = Pt(18)
p.add_run().add_picture(str(IMG / "logo_facultad.png"), width=Cm(11))

p = doc.add_paragraph(); p.alignment = WD_ALIGN_PARAGRAPH.CENTER
p.paragraph_format.space_before = Pt(26)
add_rich(p, "DISEÑO DE VIDEOJUEGOS II", size=28, color=NAVY, base_bold=True)
p = doc.add_paragraph(); p.alignment = WD_ALIGN_PARAGRAPH.CENTER
add_rich(p, "Segundo Examen Práctico — Acelerómetro y Giroscopio", size=14, color=BLUE, base_bold=True)

p = doc.add_paragraph(); p.alignment = WD_ALIGN_PARAGRAPH.CENTER
p.paragraph_format.space_before = Pt(10)
p.add_run().add_picture(str(IMG / "portada_tablero.png"), width=Cm(10.5))

p = doc.add_paragraph(); p.alignment = WD_ALIGN_PARAGRAPH.CENTER
add_rich(p, JUEGO, size=40, color=RGBColor(0x2E, 0x7D, 0x32), base_bold=True)
p = doc.add_paragraph(); p.alignment = WD_ALIGN_PARAGRAPH.CENTER
add_rich(p, LEMA, size=14, italic=True)

p = doc.add_paragraph(); p.alignment = WD_ALIGN_PARAGRAPH.CENTER
p.paragraph_format.space_before = Pt(26)
add_rich(p, "Integrantes: " + INTEGRANTES.replace(" - ", ", "), size=11)
p = doc.add_paragraph(); p.alignment = WD_ALIGN_PARAGRAPH.CENTER
add_rich(p, "Docentes: " + DOCENTES.replace(" —", ","), size=11)
p = doc.add_paragraph(); p.alignment = WD_ALIGN_PARAGRAPH.CENTER
add_rich(p, "Tecnicatura Universitaria en Diseño Integral de Videojuegos · Facultad de Ingeniería · UNJu · 2026",
         size=9.5, color=RGBColor(0x59, 0x59, 0x59))
p = doc.add_paragraph(); p.alignment = WD_ALIGN_PARAGRAPH.CENTER
add_rich(p, f"Documento en desarrollo · {VERSION}", size=9, color=RGBColor(0x80, 0x80, 0x80), italic=True)
page_break()

# ---------------------------------------------------------------- índice
INDICE = [
    ("1.", "Presentación y alcance del documento", []),
    ("2.", "Descripción rápida del nivel", ["2.1 Concepto de juego", "2.2 Premisa narrativa",
                                            "2.3 Serious Game y Triple Impacto", "2.4 Público objetivo y plataforma"]),
    ("3.", "Descripción de personajes", []),
    ("4.", "Controles", ["4.1 Esquema de interacción", "4.2 Procesamiento del sensor"]),
    ("5.", "Objetivos, retos y reglas", ["5.1 Condición de victoria y derrota", "5.2 Reglas",
                                         "5.3 Obstáculos y retos", "5.4 Ciclo de juego"]),
    ("6.", "Mecánicas principales", []),
    ("7.", "Boceto / Layout del nivel", ["7.1 Vista superior y flujo del jugador", "7.2 Zonas del nivel",
                                         "7.3 Salto final"]),
    ("8.", "Progresión de partida (3 minutos)", []),
    ("9.", "Cámara, estilo artístico y sonido", []),
    ("10.", "Tecnología específica y arquitectura", ["10.1 Herramientas", "10.2 Arquitectura y principios SOLID"]),
    ("11.", "Registro de desarrollo", []),
]
p = doc.add_paragraph(); add_rich(p, "Índice", size=17, color=NAVY, base_bold=True)
for num, title, subs in INDICE:
    q = doc.add_paragraph(); q.paragraph_format.space_after = Pt(1)
    q.paragraph_format.left_indent = Cm(0.6)
    r = q.add_run(f"{num}  {title}"); r.bold = True; r.font.color.rgb = RGBColor.from_string(LINK_BLUE)
    for s in subs:
        q = doc.add_paragraph(); q.paragraph_format.space_after = Pt(1)
        q.paragraph_format.left_indent = Cm(1.3)
        add_rich(q, s, size=10.5)
page_break()

# ---------------------------------------------------------------- 1
h1("1. Presentación y alcance del documento")
para(f"Este trabajo detalla el diseño técnico de **{JUEGO}**, un *serious game* de físicas en 3D para teléfonos "
     "Android en el que el jugador inclina un tablero laberíntico suspendido en el aire para guiar residuos "
     "(botellas, latas y cajas) hasta su contenedor de reciclaje. A lo largo del documento se abordan todos los "
     "apartados exigidos para el Segundo Examen Práctico de la materia Diseño de Videojuegos II.")
para("La tecnología elegida es el **acelerómetro y giroscopio** del teléfono: el tablero copia la inclinación "
     "del dispositivo. El prototipo jugable consiste en una partida única diseñada para resolverse en un "
     "**máximo de 3 minutos**.")
para("El proyecto se enmarca en el paradigma del **Triple Impacto con enfoque Social**: el juego es una "
     "herramienta educativa para enseñar a **separar los residuos en origen**, pensada para usarse en escuelas, "
     "talleres barriales y campañas de concientización.")
para(f"*Este es un documento vivo ({VERSION}): se actualiza con cada hito de desarrollo del prototipo.*", size=10)

# ---------------------------------------------------------------- 2
h1("2. Descripción rápida del nivel")
h2("2.1 Concepto de juego")
para(f"{JUEGO} es un juego de habilidad del estilo «laberinto de bolita»: el jugador no controla al objeto "
     "directamente, sino la superficie sobre la que se mueve. Cada residuo debe recorrer el laberinto, tomar "
     "envión en la rampa final y saltar un vacío para caer dentro del contenedor que le corresponde.")
table(["Característica", "Descripción"], [
    ["Género", "Puzle de físicas / habilidad. *Serious game* educativo."],
    ["Plataforma", "Android, orientación vertical. Jugable también en PC (teclado o mouse) para pruebas."],
    ["Tecnología", "Acelerómetro y giroscopio (sensor de gravedad del teléfono)."],
    ["Cámara", "Fija, en tercera persona, cenital-oblicua desde detrás del tablero."],
    ["Ambientación", "Tablero de clasificación flotando sobre la plaza de un barrio. Estilo *low poly* "
                     "inspirado en PlayStation 1."],
    ["Personajes", "**Botella PET**, **Lata** y **Caja de cartón** (los residuos a clasificar)."],
    ["Mecánica principal", "Inclinar el tablero en los ejes X y Z para guiar el residuo y dosificar el "
                           "impulso del salto final."],
    ["Objetivo principal", "Separar correctamente **6 residuos** antes de que pasen **3 minutos**."],
], [4.2, 12.3])

h2("2.2 Premisa narrativa")
para("En el barrio se organiza la jornada «Separá en Origen». La planta de reciclaje instaló un tablero de "
     "clasificación que flota sobre la plaza: los vecinos arrojan sus residuos desde arriba y el jugador, a cargo "
     "de la clasificación, tiene que inclinar el tablero para llevar cada residuo entre paredes y huecos hasta la "
     "rampa, y hacerlo saltar al contenedor correcto. Cada residuo bien separado suma a la meta del barrio; cada "
     "error muestra qué debería haberse hecho.")

h2("2.3 Serious Game y Triple Impacto")
para("El juego está diseñado para que el aprendizaje ocurra jugando: para ganar hay que reconocer el material "
     "de cada residuo y elegir su contenedor. Después de cada depósito aparece un consejo breve (por ejemplo, "
     "enjuagar y aplastar las botellas antes de tirarlas), de modo que la partida deja información práctica "
     "para aplicar en casa.")
table(["Eje", "Cómo lo aborda " + JUEGO], [
    ["Social (eje principal)", "Educación en separación en origen para chicos, chicas y adolescentes. Se juega "
                               "en el celular, sin necesidad de consola ni PC, y sirve para actividades en "
                               "escuelas, talleres barriales o campañas municipales."],
    ["Ambiental", "Refuerza hábitos que reducen lo que llega a basurales y mejoran la recuperación de "
                  "materiales reciclables."],
    ["Económico", "Los materiales bien separados valen más para las cooperativas de recicladores urbanos; el "
                  "juego visibiliza ese trabajo."],
], [4.2, 12.3])

h2("2.4 Público objetivo y plataforma")
para("Público principal: **chicos y chicas de 8 a 15 años** y, en segundo lugar, cualquier persona que quiera "
     "aprender a separar residuos. Las partidas son cortas (3 minutos) y los controles son naturales: inclinar "
     "el teléfono como si fuera el propio tablero. Se eligió **Android** por ser la plataforma más difundida "
     "entre el público objetivo y porque todos los equipos traen acelerómetro.")

# ---------------------------------------------------------------- 3
h1("3. Descripción de personajes")
para("No hay un avatar humano: el jugador encarna a la persona encargada de clasificar. Los protagonistas son "
     "los **residuos**, y cada uno tiene un comportamiento físico propio que obliga a cambiar de estrategia.")
table(["Residuo", "Contenedor", "Forma física", "Comportamiento"], [
    ["Botella PET", "Plástico\n(amarillo)", "Cápsula acostada", "Liviana; rueda rápido y cuesta frenarla "
                                                              "antes de los huecos."],
    ["Lata", "Metal\n(gris)", "Cilindro acostado", "Rueda y rebota más contra las paredes; llega con "
                                                  "buena velocidad a la rampa."],
    ["Caja de cartón", "Cartón y papel\n(azul)", "Cubo", "No rueda: se desliza con poca fricción. Necesita "
                                                         "más inclinación para tomar impulso en el salto."],
], [3.3, 3.2, 3.4, 6.6])
para("*Los colores de los contenedores se eligieron para el prototipo y pueden adaptarse a la normativa de "
     "cada municipio.*", size=9.5)

# ---------------------------------------------------------------- 4
h1("4. Controles")
h2("4.1 Esquema de interacción")
table(["Acción", "Android (sensores)", "PC / Editor"], [
    ["Inclinar adelante / atrás", "Inclinar el teléfono hacia adelante o hacia el jugador", "W / S, ↑ / ↓ o "
                                                                                          "click + mouse"],
    ["Inclinar izquierda / derecha", "Inclinar el teléfono hacia los costados", "A / D, ← / → o click + mouse"],
    ["Recalibrar", "Botón «Calibrar»: toma la posición actual del teléfono como horizontal", "—"],
    ["Sacudón", "Agitar el teléfono: da un pequeño impulso al residuo si queda trabado (espera de 3 s)",
     "Barra espaciadora"],
    ["Pausa", "Botón en pantalla", "Esc"],
], [4.2, 7.6, 4.7])

h2("4.2 Procesamiento del sensor")
bullets([
    "Se lee el **vector de gravedad** del sensor de gravedad de Android, que el sistema calcula fusionando el "
    "**acelerómetro** con el **giroscopio**; así se filtran las vibraciones de la mano. Si el equipo no tiene "
    "ese sensor se usa el acelerómetro con un filtro paso bajo.",
    "Con ese vector se calculan dos ángulos: **inclinación lateral** (roll) e **inclinación frontal** (pitch).",
    "**Calibración:** la posición en la que se sostiene el teléfono al empezar se toma como neutra, así se "
    "puede jugar sentado, parado o con el teléfono apoyado.",
    "Una inclinación del teléfono de **20°** equivale a la inclinación máxima del tablero (**12°**). Hay una "
    "zona muerta de 5 % para que el tablero no tiemble.",
    "El tablero sigue ese valor con suavizado y una velocidad máxima de **60°/s**, para que un movimiento "
    "brusco no lance los residuos fuera.",
])

# ---------------------------------------------------------------- 5
h1("5. Objetivos, retos y reglas")
h2("5.1 Condición de victoria y derrota")
table(["Resultado", "Condición"], [
    ["Victoria", "Separar correctamente **6 residuos** antes de que el reloj llegue a 0:00."],
    ["Derrota", "Se terminan los **3 minutos** sin alcanzar la meta."],
], [4.2, 12.3])
h2("5.2 Reglas")
bullets([
    "Los residuos caen **de a uno** desde arriba sobre el punto de inicio; el tipo es aleatorio.",
    "**Contenedor correcto:** +100 puntos, suma 1 a la meta y aparece un consejo de reciclaje.",
    "**Contenedor incorrecto:** −50 puntos, no suma a la meta y se muestra a qué contenedor iba.",
    "Si el residuo cae fuera del tablero (huecos, bordes o el vacío del salto), **reaparece en el punto de "
    "inicio después de 1 segundo**. No resta puntos, pero se pierde tiempo.",
    "Cuando un residuo entra en cualquier contenedor, cae el siguiente.",
])
h2("5.3 Obstáculos y retos")
bullets([
    "**Laberinto en serpentina:** tres tramos separados por paredes con pasajes alternados a izquierda y "
    "derecha.",
    "**Huecos** de 1×1 m y 2×2 m en el piso, ubicados junto a los pasajes.",
    "**Rampa sin baranda** de 6 m de ancho: hay que entrar derecho para no caer por los costados.",
    "**Vacío de 1,1 m** entre la rampa y los contenedores: con poco impulso el residuo cae, con demasiado se pasa.",
    "**Puntería lateral:** hay tres contenedores lado a lado; hay que llegar a la rampa alineado con el correcto.",
    "**Física distinta por residuo:** la caja se desliza, la botella y la lata ruedan.",
])
h2("5.4 Ciclo de juego")
para("Cae un residuo → el jugador identifica su material → lo guía por el laberinto → toma envión en la recta "
     "final → salta al contenedor → recibe el resultado y un consejo → cae el siguiente residuo. El ciclo se "
     "repite hasta cumplir la meta o agotar el tiempo.")

# ---------------------------------------------------------------- 6
h1("6. Mecánicas principales")
table(["Mecánica", "Qué hace el jugador", "Impacto en el estado del juego"], [
    ["Inclinación del tablero", "Inclina el teléfono en dos ejes", "Cambia la dirección de la gravedad "
                                                                   "relativa: todo lo que está sobre el "
                                                                   "tablero acelera hacia el lado bajo."],
    ["Física de residuos", "—", "Cada residuo es un cuerpo rígido con masa, fricción y rebote propios "
                                "(rueda o desliza)."],
    ["Salto con impulso", "Regula la velocidad al llegar a la rampa", "La velocidad define el alcance: "
                                                                       "vacío, contenedor o de largo."],
    ["Clasificación", "Elige a qué contenedor saltar", "El contenedor detecta el residuo y compara su "
                                                       "categoría: suma o resta puntos y muestra un consejo."],
    ["Reaparición", "—", "Si el residuo sale del tablero, vuelve al inicio a 1 s."],
    ["Sacudón", "Agita el teléfono", "Pequeño impulso vertical para destrabar un residuo; tiene espera."],
    ["Calibración", "Toca «Calibrar»", "Redefine la posición neutra del teléfono."],
], [3.6, 4.4, 8.5])

# ---------------------------------------------------------------- 7
h1("7. Boceto / Layout del nivel")
para("El nivel se define como un **mapa de caracteres** dentro de un asset de datos de Unity (BoardLayout). A "
     "partir de ese mapa una herramienta del editor genera la escena completa: piso, paredes, huecos, rampa, "
     "contenedores y punto de inicio. Por eso este boceto y el nivel jugable son siempre el mismo diseño.")
h2("7.1 Vista superior y flujo del jugador")
figure(IMG / "layout_superior.png", 8.4, "Figura 1. Vista superior del tablero de 10 × 16 m. La línea "
                                         "punteada es el recorrido sugerido desde el inicio (S).")
h2("7.2 Zonas del nivel")
table(["Zona", "Descripción"], [
    ["1. Inicio", "Fila inferior derecha. El residuo cae desde 1,2 m de altura sobre el punto S."],
    ["2. Tramo bajo", "Recorrido hacia la izquierda pasando junto a un hueco de 2×2 m. Pasaje por la "
                      "izquierda."],
    ["3. Tramo medio", "Recorrido hacia la derecha con un hueco de 1×1 m en el medio. Pasaje por la derecha."],
    ["4. Recta final", "Hueco de 2×2 m a la izquierda y pared a la derecha: hay que encarar por el centro."],
    ["5. Rampa", "6 m de ancho, 1,6 m de largo y 0,4 m de altura. Sin barandas."],
    ["6. Meta", "Vacío de 1,1 m y tres contenedores de 1,8 × 1,8 m: Plástico, Metal y Cartón."],
], [3.4, 13.1])
h2("7.3 Salto final")
figure(IMG / "layout_lateral.png", 15.5, "Figura 2. Vista lateral del salto: la velocidad con la que el "
                                         "residuo sale de la rampa define dónde cae.")

# ---------------------------------------------------------------- 8
h1("8. Progresión de partida (3 minutos)")
table(["Etapa", "Tiempo", "Qué pasa"], [
    ["Inicio", "0:00 – 0:10", "Calibración del teléfono y cartel con instrucciones. Cae el primer residuo."],
    ["Aprendizaje", "0:10 – 1:00", "Primeros residuos: el jugador aprende el recorrido y el salto."],
    ["Desarrollo", "1:00 – 2:30", "Ritmo constante con los tres tipos de residuo mezclados."],
    ["Cierre", "2:30 – 3:00", "La música acelera y el reloj se destaca en pantalla."],
    ["Final", "3:00 o meta", "Pantalla de resultados: aciertos, errores, puntaje y un consejo final."],
], [3.2, 3.0, 10.3])

# ---------------------------------------------------------------- 9
h1("9. Cámara, estilo artístico y sonido")
bullets([
    "**Cámara:** fija, detrás y por encima del tablero, para ver el recorrido completo y los contenedores en un "
    "teléfono en vertical.",
    "**Estilo PS1 / low poly:** pocos polígonos, colores planos y saturados, texturas de baja resolución con "
    "filtro *point*, vértices que «tiemblan» (vertex snapping), resolución interna baja escalada sin suavizado "
    "y niebla de distancia.",
    "**Paleta:** verdes y marrones para el tablero; amarillo, gris y azul para los contenedores, para que se "
    "distingan de un vistazo.",
    "**Sonido:** música de estilo retro, efectos de rodar, golpe contra pared, caída, acierto y error. La "
    "música acelera en los últimos 30 segundos.",
])

# ---------------------------------------------------------------- 10
h1("10. Tecnología específica y arquitectura")
h2("10.1 Herramientas")
bullets([
    "**Motor:** Unity 6 (6000.3.23f1) con Universal Render Pipeline (perfil Mobile).",
    "**Entrada:** Input System 1.20 — GravitySensor, Accelerometer, teclado y mouse.",
    "**Física:** PhysX de Unity; el tablero es un Rigidbody cinemático rotado en FixedUpdate.",
    "**Programación:** C#, organizado en dos ensamblados (EcoTilt.Runtime y EcoTilt.Editor).",
    "**Plataforma:** Android (API mínima 25).",
    f"**Versionado:** Git y GitHub — {REPO}",
])
h2("10.2 Arquitectura y principios SOLID")
para("El código sigue los principios SOLID. El tablero no sabe de dónde viene la inclinación: depende de la "
     "interfaz ITiltInput, y el teclado, el mouse y los sensores son implementaciones intercambiables.")
table(["Clase / interfaz", "Responsabilidad", "Principio aplicado"], [
    ["ITiltInput", "Contrato: devolver una inclinación normalizada (x, y).", "Inversión de dependencias (D)"],
    ["Keyboard / Mouse / SensorTiltInput", "Cada una lee un único dispositivo.", "Responsabilidad única (S), "
                                                                               "sustitución (L)"],
    ["CompositeTiltInput", "Combina varias fuentes; se agregan nuevas sin modificarla.", "Abierto/cerrado (O)"],
    ["ICalibratable", "Interfaz aparte para lo que se puede calibrar.", "Segregación de interfaces (I)"],
    ["TiltInputProvider", "Único lugar que arma las fuentes concretas según la configuración.", "Composición / "
                                                                                                "D"],
    ["BoardTiltController", "Solo rota el tablero a partir de un ITiltInput.", "S, D"],
    ["IRespawnable / Respawnable", "Ocultar, esperar 1 s y reubicar un objeto.", "S, I"],
    ["KillZone", "Detectar la caída y pedir la reaparición.", "S, D"],
    ["BoardLayout", "Datos del nivel (mapa) editables sin tocar código.", "O"],
    ["LevelSceneBuilder (editor)", "Genera la escena a partir del layout.", "S"],
], [4.6, 7.4, 4.5], size=9.5)

# ---------------------------------------------------------------- 11
h1("11. Registro de desarrollo")
table(["Hito", "Contenido", "Estado"], [
    ["1", "Input (teclado, mouse y sensores), tablero inclinable, reaparición a 1 s y generador de escena.",
     "Implementado — en prueba"],
    ["2", "Residuos como datos, contenedores que validan el material, aparición de residuos y puntaje.",
     "Pendiente"],
    ["3", "Partida completa: reloj de 3 min, interfaz, calibración, sacudón y consejos.", "Pendiente"],
    ["4", "Estética PS1 (shader, baja resolución) y audio.", "Pendiente"],
    ["5", "Compilación para Android, ajuste de sensibilidad y balanceo.", "Pendiente"],
    ["6", "Documento final, capturas y entrega (24/11/2026).", "Pendiente"],
], [1.4, 11.1, 4.0])

doc.save(OUT)
print("ok", OUT)
