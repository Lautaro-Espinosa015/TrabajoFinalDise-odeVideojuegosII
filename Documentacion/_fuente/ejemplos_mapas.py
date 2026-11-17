"""Ejemplos de laberintos aleatorios. Es una copia en Python del algoritmo de MazeGenerator.cs
(Prim aleatorio, inicio en el borde izquierdo, salida a la derecha, pozos en tramos rectos del camino).
Uso: python ejemplos_mapas.py <salida.png> [cantidad] [semilla]"""
import random
import sys
from collections import deque

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.patches import Rectangle

COLS = ROWS = 8
HOLES, EXTRA, MIN_PATH = 3, 0.03, 18
CELL, WALL, HOLE_FRACTION = 1.7, 0.25, 0.5
APRON_LEN, APRON_W, RAMP_LEN, GAP, BIN_D, BIN_W, BIN_SPACING = 2.2, 6.4, 1.6, 1.1, 1.8, 1.8, 2.0
C_FLOOR, C_WALL, C_HOLE, C_RAMP, C_GLOW = "#3A545E", "#CDB57A", "#0E1418", "#4A616A", "#FF8524"
BINS = ["#F9C71F", "#B8CCE0", "#3885FF"]
DIRS = [(1, 0), (-1, 0), (0, 1), (0, -1)]


def has_wall(c, d, h, v):
    x, y = c
    if d == (1, 0): return v[x + 1][y]
    if d == (-1, 0): return v[x][y]
    if d == (0, 1): return h[x][y + 1]
    return h[x][y]


def remove_wall(c, d, h, v):
    x, y = c
    if d == (1, 0): v[x + 1][y] = False
    elif d == (-1, 0): v[x][y] = False
    elif d == (0, 1): h[x][y + 1] = False
    else: h[x][y] = False


def try_generate(rng):  # noqa: C901
    h = [[True] * (ROWS + 1) for _ in range(COLS)]
    v = [[True] * ROWS for _ in range(COLS + 1)]
    inside = [[False] * ROWS for _ in range(COLS)]
    first = (rng.randrange(COLS), rng.randrange(ROWS))
    inside[first[0]][first[1]] = True
    frontier = []

    def add(c):
        for d in DIRS:
            n = (c[0] + d[0], c[1] + d[1])
            if 0 <= n[0] < COLS and 0 <= n[1] < ROWS:
                frontier.append((c, d))
    add(first)
    while frontier:
        i = rng.randrange(len(frontier))
        c, d = frontier[i]
        frontier[i] = frontier[-1]
        frontier.pop()
        n = (c[0] + d[0], c[1] + d[1])
        if inside[n[0]][n[1]]:
            continue
        remove_wall(c, d, h, v)
        inside[n[0]][n[1]] = True
        add(n)

    interior = (COLS - 1) * ROWS + COLS * (ROWS - 1)
    for _ in range(round(interior * EXTRA)):
        if rng.randrange(2) == 0: v[1 + rng.randrange(COLS - 1)][rng.randrange(ROWS)] = False
        else: h[rng.randrange(COLS)][1 + rng.randrange(ROWS - 1)] = False

    start = (0, rng.randrange(ROWS))
    goal = (COLS - 1, rng.randrange(ROWS))
    v[COLS][goal[1]] = False

    prev = {start: start}
    q = deque([start])
    while q:
        c = q.popleft()
        if c == goal: break
        for d in DIRS:
            n = (c[0] + d[0], c[1] + d[1])
            if not (0 <= n[0] < COLS and 0 <= n[1] < ROWS) or n in prev or has_wall(c, d, h, v):
                continue
            prev[n] = c
            q.append(n)
    path = []
    c = goal
    while c != start:
        path.append(c); c = prev[c]
    path.append(start)
    path.reverse()

    straight = [path[i] for i in range(2, len(path) - 2)
                if (path[i][0] - path[i - 1][0], path[i][1] - path[i - 1][1]) ==
                   (path[i + 1][0] - path[i][0], path[i + 1][1] - path[i][1])]
    holes = set()
    if straight:
        n = min(HOLES, len(straight))
        for k in range(n):
            holes.add(straight[min(len(straight) - 1, int((k + 0.5) * len(straight) / n))])
    # Contenedores: uno hundido en un callejón fuera del camino correcto, dos tras la rampa; orden aleatorio
    on_path = set(path)
    def open_sides(c):
        return sum(1 for d in DIRS if 0 <= c[0] + d[0] < COLS and 0 <= c[1] + d[1] < ROWS and not has_wall(c, d, h, v))
    cand = [(x, y) for x in range(COLS) for y in range(ROWS)
            if (x, y) not in on_path and (x, y) not in holes and abs(x - start[0]) + abs(y - start[1]) >= 3]
    dead = [c for c in cand if open_sides(c) == 1]
    pool = dead or cand
    inner = rng.choice(pool) if pool else None
    colors = BINS[:]
    rng.shuffle(colors)
    return dict(h=h, v=v, start=start, goal=goal, holes=holes, path=path, inner=inner, colors=colors)


def generate(rng):
    best = None
    for _ in range(60):
        m = try_generate(rng)
        if best is None or len(m["path"]) > len(best["path"]): best = m
        if len(m["path"]) >= MIN_PATH: break
    return best


W = COLS * CELL + (COLS + 1) * WALL
L = ROWS * CELL + (ROWS + 1) * WALL


def cx(x): return -W / 2 + WALL + x * (CELL + WALL) + CELL / 2
def cz(y): return L / 2 - WALL - y * (CELL + WALL) - CELL / 2


def draw(ax, m, title):
    h, v = m["h"], m["v"]
    ax.add_patch(Rectangle((-W / 2, -L / 2), W, L, fc=C_FLOOR))
    for x in range(COLS):
        for y in range(ROWS + 1):
            if h[x][y]:
                z = L / 2 - y * (CELL + WALL)
                ax.add_patch(Rectangle((cx(x) - CELL / 2 - WALL, z - WALL), CELL + 2 * WALL, WALL, fc=C_WALL, lw=0))
    for x in range(COLS + 1):
        for y in range(ROWS):
            if v[x][y]:
                xx = -W / 2 + x * (CELL + WALL)
                ax.add_patch(Rectangle((xx, cz(y) - CELL / 2 - WALL), WALL, CELL + 2 * WALL, fc=C_WALL, lw=0))
    pit = CELL * HOLE_FRACTION
    for (x, y) in m["holes"]:
        ax.add_patch(Rectangle((cx(x) - pit / 2, cz(y) - pit / 2), pit, pit, fc=C_HOLE, ec=C_GLOW, lw=1.2))
    sx, sy = m["start"]
    ax.add_patch(Rectangle((cx(sx) - .6, cz(sy) - .6), 1.2, 1.2, fill=False, ec=C_GLOW, lw=1.6))
    ax.text(cx(sx), cz(sy), "S", ha="center", va="center", color="white", fontsize=9, weight="bold")

    limit = L / 2 - APRON_W / 2
    zc = max(-limit, min(limit, cz(m["goal"][1])))
    ax.add_patch(Rectangle((W / 2, zc - APRON_W / 2), APRON_LEN, APRON_W, fc=C_FLOOR))
    ax.add_patch(Rectangle((W / 2 + APRON_LEN, zc - APRON_W / 2 + WALL), RAMP_LEN, APRON_W - 2 * WALL, fc=C_RAMP,
                           ec="black", lw=.4, hatch="//"))
    bx = W / 2 + APRON_LEN + RAMP_LEN + GAP
    colors = m["colors"]
    if m["inner"]:
        ix, iy = m["inner"]
        size = CELL - 0.1
        ax.add_patch(Rectangle((cx(ix) - size / 2, cz(iy) - size / 2), size, size, fc=colors[0], ec=C_GLOW, lw=1.4))
    for i, col in enumerate(colors[1:]):
        ax.add_patch(Rectangle((bx, zc + (i - 0.5) * BIN_SPACING - BIN_W / 2), BIN_D, BIN_W, fc=col, ec="black", lw=.6))

    pts = [(cx(x), cz(y)) for x, y in m["path"]] + [(W / 2 + APRON_LEN + RAMP_LEN, zc)]
    xs, zs = zip(*pts)
    ax.plot(xs, zs, color="white", lw=1.2, ls=(0, (3, 2)), alpha=.85)
    ax.set_xlim(-W / 2 - .3, bx + BIN_D + .3)
    ax.set_ylim(-L / 2 - .3, L / 2 + .3)
    ax.set_aspect("equal")
    ax.axis("off")
    ax.set_title(title, fontsize=10)


if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else "ejemplos_mapas.png"
    count = int(sys.argv[2]) if len(sys.argv) > 2 else 6
    rng = random.Random(int(sys.argv[3]) if len(sys.argv) > 3 else 2026)
    cols = 3
    rows = (count + cols - 1) // cols
    fig, axes = plt.subplots(rows, cols, figsize=(cols * 5.2, rows * 3.9), dpi=170)
    for i, ax in enumerate(fig.axes):
        if i >= count:
            ax.axis("off"); continue
        m = generate(rng)
        draw(ax, m, f"Mapa {i + 2}  ·  camino de {len(m['path'])} celdas")
    fig.suptitle("Ejemplos de laberintos generados (nuevo nivel o al perder las 3 vidas)", fontsize=13, weight="bold")
    fig.tight_layout()
    fig.savefig(out, bbox_inches="tight")
    print("ok", out)
