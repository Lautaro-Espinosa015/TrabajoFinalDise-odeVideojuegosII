using System.Collections.Generic;
using UnityEngine;

namespace EcoTilt.Controls
{
    /// <summary>
    /// Combina varias fuentes y devuelve la de mayor magnitud en cada lectura.
    /// Agregar una fuente nueva (por ejemplo, un joystick táctil) no requiere modificar esta clase (OCP).
    /// </summary>
    public sealed class CompositeTiltInput : ITiltInput
    {
        private readonly IReadOnlyList<ITiltInput> _sources;

        public CompositeTiltInput(IReadOnlyList<ITiltInput> sources)
        {
            _sources = sources;
        }

        public Vector2 ReadTilt()
        {
            Vector2 strongest = Vector2.zero;
            for (int i = 0; i < _sources.Count; i++)
            {
                Vector2 value = _sources[i].ReadTilt();
                if (value.sqrMagnitude > strongest.sqrMagnitude) strongest = value;
            }
            return strongest;
        }
    }
}
