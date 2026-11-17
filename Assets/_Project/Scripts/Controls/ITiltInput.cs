using UnityEngine;

namespace EcoTilt.Controls
{
    /// <summary>
    /// Fuente de inclinación del tablero. Devuelve la rotación que debe tener el tablero respecto de su
    /// posición nivelada (identidad = horizontal). Con el teléfono es un espejo de su orientación real.
    /// El tablero depende de esta abstracción y no de un dispositivo concreto (DIP).
    /// </summary>
    public interface ITiltInput
    {
        Quaternion ReadTilt();
    }

    /// <summary>
    /// Fuentes que admiten recalibrar su posición neutra (por ejemplo, los sensores del teléfono).
    /// Interfaz separada para no obligar a teclado/mouse a implementarla (ISP).
    /// </summary>
    public interface ICalibratable
    {
        void Calibrate();
    }

    /// <summary>Estado del sensor que le interesa a la interfaz: si hay datos, si está calibrando y la inclinación actual.</summary>
    public interface ITiltStatus
    {
        bool HasData { get; }
        bool IsCalibrating { get; }
        /// <summary>0 a 1.</summary>
        float CalibrationProgress { get; }
        Quaternion LastTilt { get; }
    }

    /// <summary>Fuentes que pueden describir su estado para depurar (qué sensor usan, qué valores leen).</summary>
    public interface ITiltDiagnostics
    {
        string Diagnostics { get; }
    }

    /// <summary>Utilidades comunes para construir y limitar inclinaciones.</summary>
    public static class Tilt
    {
        /// <summary>Inclinación desde ángulos: <paramref name="forward"/> baja el borde lejano (+Z), <paramref name="right"/> baja el lado derecho (+X).</summary>
        public static Quaternion FromAngles(float right, float forward)
        {
            return Quaternion.Euler(forward, 0f, -right);
        }

        /// <summary>Ángulo total de la inclinación, en grados.</summary>
        public static float Magnitude(Quaternion tilt)
        {
            return Quaternion.Angle(Quaternion.identity, tilt);
        }

        /// <summary>Recorta la inclinación a un ángulo máximo conservando su dirección.</summary>
        public static Quaternion Clamp(Quaternion tilt, float maxAngle)
        {
            float angle = Magnitude(tilt);
            return angle <= maxAngle || angle < 0.0001f
                ? tilt
                : Quaternion.Slerp(Quaternion.identity, tilt, maxAngle / angle);
        }
    }
}
