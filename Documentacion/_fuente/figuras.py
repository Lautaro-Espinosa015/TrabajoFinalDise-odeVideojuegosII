"""Genera las figuras del documento a partir del mismo layout que usa Unity (BoardLayout.DefaultMap).
Uso: python figuras.py <carpeta_salida>"""
import math
import sys
from pathlib import Path

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.patches import FancyArrowPatch, Rectangle, Polygon

MAP = """##......##
#........#
#.OO.....#
#.OO..####
#........#
#######..#
#........#
#....O...#
#........#
#..#######
#........#
#....OO..#
#....OO..#
#........#
#......S.#
##########""".splitlines()

# Mismas medidas que BoardDimensions (Editor/BoardGeometryBuilder.cs)
RAMP_LEN, RAMP_RISE, GAP = 1.6, 0.4, 1.1
BIN_W, BIN_D, BIN_H, BIN_SPACING, BIN_RIM = 1.8, 1.8, 1.0, 2.0, -0.6

C_FLOOR, C_WALL, C_HOLE = "#6B9E54", "#8C6645", "#1B1B22"
C_RAMP, C_FRAME = "#D98C40", "#4D4742"
BINS = [("Plástico", "#F2C726"), ("Metal", "#99A3AD"), ("Cartón", "#3373D9")]

ROWS, COLS = len(MAP), len(MAP[0])
HW, HL = COLS / 2, ROWS / 2


def cell_xy(c, r):
    """Esquina inferior izquierda de la celda en coordenadas del tablero (x derecha, z adelante)."""
    return c - HW, HL - r - 1


def top_view(out):
    fig, ax = plt.subplots(figsize=(6.2, 9.6), dpi=200)
    for r, line in enumerate(MAP):
        for c, ch in enumerate(line):
            x, z = cell_xy(c, r)
            color = {"#": C_WALL, "O": C_HOLE}.get(ch, C_FLOOR)
            ax.add_patch(Rectangle((x, z), 1, 1, facecolor=color, edgecolor="#00000030", lw=0.5))
            if ch == "S":
                ax.add_patch(plt.Circle((x + .5, z + .5), .32, color="white", zorder=3))
                ax.text(x + .5, z + .5, "S", ha="center", va="center", fontsize=11, weight="bold", zorder=4)

    exits = [c for c, ch in enumerate(MAP[0]) if ch in ".S"]
    x0, x1 = exits[0] - HW, exits[-1] + 1 - HW
    ax.add_patch(Rectangle((x0, HL), x1 - x0, RAMP_LEN, facecolor=C_RAMP, edgecolor="black", lw=.8, hatch="//"))
    ax.text(x0 + 1.3, HL + RAMP_LEN / 2, "RAMPA", ha="center", va="center", fontsize=9, weight="bold")
    ax.text(x0 + 1.3, HL + RAMP_LEN + GAP / 2, "VACÍO", ha="center", va="center", fontsize=9,
            color="#B00020", weight="bold")

    zc = HL + RAMP_LEN + GAP
    for i, (name, col) in enumerate(BINS):
        x = (i - 1) * BIN_SPACING - BIN_W / 2
        ax.add_patch(Rectangle((x, zc), BIN_W, BIN_D, facecolor=col, edgecolor="black", lw=1))
        ax.text(x + BIN_W / 2, zc + BIN_D / 2, name, ha="center", va="center", fontsize=8.5, weight="bold",
                color="white" if name == "Cartón" else "black")

    # Recorrido sugerido
    path = [(2.5, -6.5), (2.5, -5.5), (-3, -5.5), (-3, -0.5), (3, -0.5), (3, 3.5), (0, 3.5),
            (0, 8.6), (0, 10.3)]
    xs, zs = zip(*path)
    ax.plot(xs, zs, color="white", lw=2.2, ls=(0, (4, 2)), zorder=5)
    ax.add_patch(FancyArrowPatch(path[-2], path[-1], arrowstyle="-|>", mutation_scale=16, color="white", lw=2.2, zorder=6))

    ax.set_xlim(-HW - .6, HW + .6)
    ax.set_ylim(-HL - .6, zc + BIN_D + .6)
    ax.set_aspect("equal")
    ax.set_xticks([]); ax.set_yticks([])
    for s in ax.spines.values():
        s.set_visible(False)
    legend = [Rectangle((0, 0), 1, 1, fc=C_FLOOR), Rectangle((0, 0), 1, 1, fc=C_WALL),
              Rectangle((0, 0), 1, 1, fc=C_HOLE), Rectangle((0, 0), 1, 1, fc=C_RAMP, hatch="//")]
    ax.legend(legend, ["Piso", "Pared", "Hueco (caída)", "Rampa de salto"], loc="lower center",
              bbox_to_anchor=(.5, -.07), ncol=4, fontsize=8, frameon=False)
    ax.set_title("Vista superior del tablero (1 celda = 1 m)", fontsize=11)
    fig.savefig(out, bbox_inches="tight")
    plt.close(fig)


def side_view(out):
    fig, ax = plt.subplots(figsize=(8, 3.2), dpi=200)
    ax.add_patch(Rectangle((HL - 4, -0.3), 4, 0.3, fc=C_FLOOR, ec="black", lw=.6))
    ax.add_patch(Polygon([(HL, 0), (HL + RAMP_LEN, RAMP_RISE), (HL + RAMP_LEN, RAMP_RISE - .25), (HL, -.25)],
                         fc=C_RAMP, ec="black", lw=.6))
    zb = HL + RAMP_LEN + GAP
    ax.add_patch(Rectangle((zb, BIN_RIM - BIN_H), BIN_D, BIN_H, fc=BINS[1][1], ec="black", lw=1))
    ax.text(zb + BIN_D / 2, BIN_RIM - BIN_H / 2, "Contenedor", ha="center", va="center", fontsize=8)

    angle = math.atan2(RAMP_RISE, RAMP_LEN)
    for v, col, label in [(2.0, "#B00020", "Poco impulso: cae al vacío"),
                          (3.2, "#1B7F3B", "Impulso justo: entra"),
                          (4.6, "#7A4FB0", "Demasiado: se pasa")]:
        xs, ys = [], []
        t = 0.0
        while t < 1.2:
            x = HL + RAMP_LEN + v * math.cos(angle) * t
            y = RAMP_RISE + v * math.sin(angle) * t - 4.905 * t * t
            if y < -2.2:
                break
            xs.append(x); ys.append(y); t += .01
        ax.plot(xs, ys, color=col, lw=1.8, ls="--", label=label)

    ax.annotate("", xy=(zb, .9), xytext=(HL + RAMP_LEN, .9), arrowprops=dict(arrowstyle="<->"))
    ax.text(HL + RAMP_LEN + GAP / 2, 1.0, f"vacío {GAP:.1f} m", ha="center", fontsize=8)
    ax.axhline(-2.2, color="#B00020", lw=.6, ls=":")
    ax.text(HL - 3.9, -2.1, "zona de caída → reaparece en el inicio tras 1 s", fontsize=7.5, color="#B00020")
    ax.set_aspect("equal")
    ax.set_xlim(HL - 4, zb + BIN_D + 2.2)
    ax.set_ylim(-2.4, 1.6)
    ax.set_xticks([]); ax.set_yticks([])
    for s in ax.spines.values():
        s.set_visible(False)
    ax.legend(fontsize=7.5, loc="upper right", frameon=False)
    ax.set_title("Vista lateral del salto final (rampa → vacío → contenedor)", fontsize=10)
    fig.savefig(out, bbox_inches="tight")
    plt.close(fig)


def cover_view(out):
    """Vista 3D en voxels del tablero, estética low poly, para la portada."""
    import numpy as np
    fig = plt.figure(figsize=(6, 5), dpi=220)
    ax = fig.add_subplot(projection="3d")
    filled = np.zeros((COLS, ROWS, 2), dtype=bool)
    colors = np.empty(filled.shape, dtype=object)
    for r, line in enumerate(MAP):
        for c, ch in enumerate(line):
            zi = ROWS - 1 - r
            if ch != "O":
                filled[c, zi, 0] = True
                colors[c, zi, 0] = C_FLOOR
            if ch == "#":
                filled[c, zi, 1] = True
                colors[c, zi, 1] = C_WALL
    ax.voxels(filled, facecolors=colors, edgecolor="#00000055", linewidth=.3)
    for i, (_, col) in enumerate(BINS):
        x = COLS / 2 + (i - 1) * 2.2 - .9
        ax.bar3d(x, ROWS + 2.5, -1, 1.8, 1.8, 1.2, color=col, edgecolor="black", linewidth=.4, shade=True)
    ax.bar3d(2, ROWS, 0, 6, 1.6, .5, color=C_RAMP, edgecolor="black", linewidth=.4)
    ax.view_init(elev=38, azim=-62)
    ax.set_box_aspect((COLS, ROWS + 5, 3))
    ax.set_axis_off()
    fig.savefig(out, bbox_inches="tight", transparent=True)
    plt.close(fig)
    from PIL import Image
    im = Image.open(out)
    im.crop(im.getbbox()).save(out)


if __name__ == "__main__":
    out = Path(sys.argv[1] if len(sys.argv) > 1 else ".")
    out.mkdir(parents=True, exist_ok=True)
    top_view(out / "layout_superior.png")
    side_view(out / "layout_lateral.png")
    cover_view(out / "portada_tablero.png")
    print("ok")
