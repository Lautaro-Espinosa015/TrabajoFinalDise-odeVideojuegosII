using UnityEngine;

namespace EcoTilt.Controls
{
    /// <summary>
    /// Fuente de inclinación normalizada.
    /// x: izquierda (-1) / derecha (+1). y: hacia el jugador (-1) / hacia adelante (+1).
    /// El tablero depende de esta abstracción y no de un dispositivo concreto (DIP).
    /// </summary>
    public interface ITiltInput
    {
        Vector2 ReadTilt();
    }

    /// <summary>
    /// Fuentes que admiten recalibrar su posición neutra (por ejemplo, los sensores del teléfono).
    /// Interfaz separada para no obligar a teclado/mouse a implementarla (ISP).
    /// </summary>
    public interface ICalibratable
    {
        void Calibrate();
    }
}
