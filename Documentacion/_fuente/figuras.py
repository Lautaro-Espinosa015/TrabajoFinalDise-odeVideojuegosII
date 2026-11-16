"""Genera las figuras del documento a partir del mismo layout que usa Unity (BoardLayout.DefaultMap).
Uso: python figuras.py <carpeta_salida>"""
import math
import sys
from pathlib import Path

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.patches import FancyArrowPatch, Rectangle, Polygon

MAP = """+-+-+-+-+-+-+-+-+
|     |         |
+ + +-+-+ +-+ + +
|S|     |   | | |
+-+ +-+ +-+ +-+ +
|     |O    | | |
+ +-+-+ +-+-+ +-+
| |             |
+-+ + +-+-+ +-+ +
|   | |   | |   |
+ + +-+ +-+ + +-+
| |O    | |   | |
+-+ +-+ + +-+-+ +
|     | |  O     
+ +-+ +-+ + +-+ +
|   |     | |   |
+-+-+-+-+-+-+-+-+""".split("\n")

# Mismas medidas que BoardLayout / BoardDimensions
CELL, WALL, HOLE_FRACTION = 1.7, 0.25, 0.5
APRON_LEN, APRON_W = 2.2, 6.4
RAMP_LEN, RAMP_RISE, GAP = 1.6, 0.4, 1.1
BIN_W, BIN_D, BIN_H, BIN_SPACING, BIN_RIM = 1.8, 1.8, 1.0, 2.0, -0.6

C_FLOOR, C_WALL, C_HOLE = "#3A545E", "#CDB57A", "#0E1418"
C_RAMP, C_GLOW = "#4A616A", "#FF8524"
BINS = [("Plástico", "#F9C71F"), ("Metal", "#B8CCE0"), ("Cartón", "#3885FF")]

GW = max(len(l) for l in MAP); GH = len(MAP)
COLS, ROWS = (GW - 1) // 2, (GH - 1) // 2
W = COLS * CELL + (COLS + 1) * WALL
L = ROWS * CELL + (ROWS + 1) * WALL


def raw(gx, gy):
    if 0 <= gy < GH and 0 <= gx < len(MAP[gy]):
        return MAP[gy][gx]
    return " "


def is_wall(gx, gy):
    c = raw(gx, gy)
    if c in "-|":
        return True
    if c != "+":
        return False
    return any(raw(x, y) in "-|" for x, y in ((gx - 1, gy), (gx + 1, gy), (gx, gy - 1), (gx, gy + 1)))


def start_x(gx):
    return -W / 2 + ((gx + 1) // 2) * WALL + (gx // 2) * CELL


def top_z(gy):
    return L / 2 - ((gy + 1) // 2) * WALL - (gy // 2) * CELL


def size(g):
    return WALL if g % 2 == 0 else CELL


def cell_center(cx, cy):
    gx, gy = 2 * cx + 1, 2 * cy + 1
    return start_x(gx) + CELL / 2, top_z(gy) - CELL / 2


def find(ch):
    for cy in range(ROWS):
        for cx in range(COLS):
            if raw(2 * cx + 1, 2 * cy + 1) == ch:
                yield cx, cy


def exit_row():
    for r in range(ROWS):
        if not is_wall(GW - 1, 2 * r + 1):
            return r
    return None


def top_view(out, clean=False):
    fig, ax = plt.subplots(figsize=(9.6, 6.6), dpi=200)
    ax.add_patch(Rectangle((-W / 2, -L / 2), W, L, facecolor=C_FLOOR, edgecolor="none"))
    for gy in range(GH):
        for gx in range(GW):
            if is_wall(gx, gy):
                ax.add_patch(Rectangle((start_x(gx), top_z(gy) - size(gy)), size(gx), size(gy), facecolor=C_WALL,
                                       edgecolor="#5B4A2A", lw=0.3))
    pit = CELL * HOLE_FRACTION
    for cx, cy in find("O"):
        x, z = cell_center(cx, cy)
        ax.add_patch(Rectangle((x - pit / 2, z - pit / 2), pit, pit, facecolor=C_HOLE, edgecolor=C_GLOW, lw=1.5))
    for cx, cy in find("S"):
        x, z = cell_center(cx, cy)
        ax.add_patch(Rectangle((x - .6, z - .6), 1.2, 1.2, fill=False, edgecolor=C_GLOW, lw=2))
        ax.text(x, z, "S", ha="center", va="center", fontsize=12, weight="bold", color="white")

    r = exit_row()
    _, ez = cell_center(COLS - 1, r)
    limit = L / 2 - APRON_W / 2
    zc = max(-limit, min(limit, ez))
    x0 = W / 2
    ax.add_patch(Rectangle((x0, zc - APRON_W / 2), APRON_LEN, APRON_W, facecolor=C_FLOOR, edgecolor="none"))
    for side in (-1, 1):
        ax.add_patch(Rectangle((x0, zc + side * (APRON_W / 2) - (WALL if side > 0 else 0)), APRON_LEN, WALL,
                               facecolor=C_WALL, edgecolor="#5B4A2A", lw=0.3))
    rx = x0 + APRON_LEN
    ax.add_patch(Rectangle((rx, zc - APRON_W / 2 + WALL), RAMP_LEN, APRON_W - 2 * WALL, facecolor=C_RAMP,
                           edgecolor="black", lw=.6, hatch="//"))
    ax.text(rx + RAMP_LEN / 2, zc + APRON_W / 2 + .35, "RAMPA", ha="center", fontsize=8, weight="bold")
    ax.text(rx + RAMP_LEN + GAP / 2, zc + APRON_W / 2 + .35, "VACÍO", ha="center", fontsize=8, weight="bold",
            color="#B00020")
    bx = rx + RAMP_LEN + GAP
    for i, (name, col) in enumerate(BINS):
        z = zc - (i - 1) * BIN_SPACING
        ax.add_patch(Rectangle((bx, z - BIN_W / 2), BIN_D, BIN_W, facecolor=col, edgecolor="black", lw=1))
        ax.text(bx + BIN_D / 2, z, name, ha="center", va="center", fontsize=7.5, weight="bold",
                color="white" if name == "Cartón" else "black", rotation=90)

    # Recorrido de la solución (BFS)
    from collections import deque
    start = next(find("S"))
    goal = (COLS - 1, r)
    prev = {start: None}
    q = deque([start])
    while q:
        cx, cy = q.popleft()
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nx, ny = cx + dx, cy + dy
            if not (0 <= nx < COLS and 0 <= ny < ROWS) or (nx, ny) in prev:
                continue
            if is_wall(2 * cx + 1 + dx, 2 * cy + 1 + dy):
                continue
            prev[(nx, ny)] = (cx, cy)
            q.append((nx, ny))
    path, n = [], goal
    while n:
        path.append(cell_center(*n))
        n = prev[n]
    path = path[::-1] + [(rx + RAMP_LEN + .4, zc)]
    xs, zs = zip(*path)
    if not clean:
        ax.plot(xs, zs, color="white", lw=1.8, ls=(0, (4, 2)), zorder=5, alpha=.9)
        ax.add_patch(FancyArrowPatch(path[-2], path[-1], arrowstyle="-|>", mutation_scale=14, color="white", lw=1.8, zorder=6))

    ax.set_xlim(-W / 2 - .5, bx + BIN_D + .5)
    ax.set_ylim(-L / 2 - .5, L / 2 + .5)
    ax.set_aspect("equal")
    ax.set_xticks([]); ax.set_yticks([])
    for sp in ax.spines.values():
        sp.set_visible(False)
    if clean:
        for t in ax.texts:
            t.set_visible(False)
        fig.savefig(out, bbox_inches="tight", transparent=True)
        plt.close(fig)
        return
    legend = [Rectangle((0, 0), 1, 1, fc=C_FLOOR), Rectangle((0, 0), 1, 1, fc=C_WALL),
              Rectangle((0, 0), 1, 1, fc=C_HOLE, ec=C_GLOW), Rectangle((0, 0), 1, 1, fc=C_RAMP, hatch="//")]
    ax.legend(legend, ["Piso", "Baranda", "Pozo", "Rampa de salto"], loc="lower center",
              bbox_to_anchor=(.5, -.08), ncol=4, fontsize=8, frameon=False)
    ax.set_title(f"Vista superior del tablero ({COLS}×{ROWS} pasillos de {CELL} m)", fontsize=11)
    fig.savefig(out, bbox_inches="tight")
    plt.close(fig)


def side_view(out):
    HL = 0.0
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
    """Vista superior limpia (sin título, leyenda ni recorrido) para la portada."""
    top_view(out, clean=True)


if __name__ == "__main__":
    out = Path(sys.argv[1] if len(sys.argv) > 1 else ".")
    out.mkdir(parents=True, exist_ok=True)
    top_view(out / "layout_superior.png")
    side_view(out / "layout_lateral.png")
    cover_view(out / "portada_tablero.png")
    print("ok")
