# EcoTilt — Inclina, separá y reciclá

Serious game para Android (Unity 6000.3.23f1) — Segundo Examen Práctico de Diseño de Videojuegos II (UNJu, 2026).
Integrantes: Cussi Walter Leonel, Espinosa Lautaro Eduardo, Estrada Luis Brian Gabriel.

El jugador inclina un tablero laberíntico suspendido (acelerómetro + giroscopio) para guiar residuos
(botella, lata, caja) hasta su contenedor de reciclaje, saltando un vacío al final del recorrido.

## Cómo probar
1. Abrir el proyecto en Unity 6000.3.23f1.
2. Menú **EcoTilt > Construir escena del nivel** (genera `Assets/_Project/Scenes/Nivel1_Laberinto.unity`).
3. Play. Controles en editor: **WASD / flechas** o **click izquierdo + mouse**.
   En Android (o con Unity Remote 5) se usa la inclinación del teléfono.

## Estructura
- `Assets/_Project/Scripts` — código del juego (ensamblado `EcoTilt.Runtime`).
  - `Controls/` input desacoplado: `ITiltInput`, teclado, mouse, sensores.
  - `Board/` rotación del tablero. `Gameplay/` reaparición y zona de caída. `Level/` layout del nivel.
- `Assets/_Project/Editor` — generador de la escena (ensamblado `EcoTilt.Editor`).
- `Documentacion/` — documento del examen (PDF y Word) y los scripts que lo generan (`_fuente/`).
