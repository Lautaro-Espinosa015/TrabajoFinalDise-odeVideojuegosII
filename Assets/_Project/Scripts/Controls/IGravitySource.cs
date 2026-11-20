using UnityEngine;

namespace EcoTilt.Controls
{
    /// <summary>
    /// Fuente de la dirección de la gravedad en ejes del teléfono (x = derecha, y = borde superior,
    /// z = hacia la pantalla). Separa "de dónde sale la gravedad" de "cómo se convierte en inclinación".
    /// </summary>
    public interface IGravitySource
    {
        /// <summary>Devuelve false si todavía no hay datos.</summary>
        bool TryRead(out Vector3 gravity);

        /// <summary>Nombre del sensor que se está usando (diagnóstico).</summary>
        string Name { get; }
    }
}
