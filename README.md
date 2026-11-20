# EcoTilt — Inclina, separá y reciclá

Serious game para Android (Unity 6000.3.23f1) — Segundo Examen Práctico de Diseño de Videojuegos II (UNJu, 2026).
Integrantes: Cussi Walter Leonel, Espinosa Lautaro Eduardo, Estrada Luis Brian Gabriel.

El jugador inclina un tablero laberíntico suspendido (acelerómetro + giroscopio) para guiar residuos
(botella, lata, caja) hasta su contenedor de reciclaje, saltando un vacío al final del recorrido.

## Cómo probar
Abrir `Assets/_Project/Scenes/MenuPrincipal.unity` y pulsar Play para iniciar desde el menú.
**Jugar** carga `Nivel1`; **Opciones** permite ajustar música, efectos, pantalla completa (PC)
y FPS. También incluye tutorial, créditos y salir. En pausa se puede volver al menú principal.
Los volúmenes y el límite de FPS se guardan entre sesiones. El menú utiliza audio nativo de Unity,
sin Wwise ni reconocimiento de voz. Para comprobar sus conexiones: **EcoTilt > Verificar menú principal**.

El menú muestra una demostración 3D en bucle con una copia visual del tablero de Nivel1:
recorre las rutas de los tres residuos, inclina el tablero y simula los saltos hasta los contenedores.
No ejecuta sensores ni sistemas de la partida. La interfaz usa Liberation Sans y una textura de piedra
verde con borde de bronce en `Assets/_Project/MenuArt`. Para reconstruirla: **EcoTilt > Actualizar estilo del menú**.
La textura se generó con ImageGen; el prompt está junto al PNG en `MenuArt/Textures/Prompt.txt`.

Para abrir directamente el nivel:
1. Abrir el proyecto en Unity 6000.3.23f1.
2. Abrir `Assets/_Project/Scenes/Nivel1.unity` (si falta o quedó vieja, se genera sola; también desde **EcoTilt > Regenerar escena del nivel**).
3. Play. Controles en editor: **WASD / flechas** o **click izquierdo + mouse**.
   En Android (o con Unity Remote 5) se usa la inclinación del teléfono.

## Estructura
- `Assets/_Project/Scripts` — código del juego (ensamblado `EcoTilt.Runtime`).
  - `Controls/` input desacoplado: `ITiltInput`, teclado, mouse, sensores.
  - `Board/` rotación del tablero. `Gameplay/` reaparición y zona de caída. `Level/` layout del nivel.
- `Assets/_Project/Editor` — generador de la escena (ensamblado `EcoTilt.Editor`).
- `Documentacion/` — documento del examen (PDF y Word) y los scripts que lo generan (`_fuente/`).
