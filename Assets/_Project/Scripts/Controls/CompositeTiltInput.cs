using System.Collections.Generic;
using UnityEngine;

namespace EcoTilt.Controls
{
    /// <summary>
    /// Combina varias fuentes y devuelve la de mayor inclinación en cada lectura.
    /// Agregar una fuente nueva (por ejemplo, un joystick táctil) no requiere modificar esta clase (OCP).
    /// </summary>
    public sealed class CompositeTiltInput : ITiltInput
    {
        private readonly IReadOnlyList<ITiltInput> _sources;

        public CompositeTiltInput(IReadOnlyList<ITiltInput> sources)
        {
            _sources = sources;
        }

        public Quaternion ReadTilt()
        {
            Quaternion strongest = Quaternion.identity;
            float strongestAngle = 0f;
            for (int i = 0; i < _sources.Count; i++)
            {
                Quaternion value = _sources[i].ReadTilt();
                float angle = Tilt.Magnitude(value);
                if (angle > strongestAngle)
                {
                    strongest = value;
                    strongestAngle = angle;
                }
            }
            return strongest;
        }
    }
}
