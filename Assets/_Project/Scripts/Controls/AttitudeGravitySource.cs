using UnityEngine;
using UnityEngine.InputSystem;

namespace EcoTilt.Controls
{
    /// <summary>
    /// Gravedad calculada con la orientación del teléfono que entrega el giroscopio
    /// (AttitudeSensor: giroscopio + acelerómetro fusionados). Responde casi sin demora y no se desvía
    /// en la inclinación, a diferencia del sensor de gravedad, que filtra y llega tarde en los giros rápidos.
    ///
    /// Cada fabricante (y Unity Remote) entrega la orientación con su propia convención de ejes. Para no
    /// depender de eso, esta clase prueba todas las convenciones posibles y se queda con la que coincide
    /// con el sensor de referencia mientras el jugador mueve el teléfono. Hasta encontrarla (o si el
    /// teléfono no tiene giroscopio) devuelve la lectura de referencia, así que nunca queda peor que antes.
    /// </summary>
    public sealed class AttitudeGravitySource : IGravitySource
    {
        // Convenciones candidatas: eje de la gravedad en el "mundo" del sensor × permutación con signo de los ejes.
        private static readonly int[][] Permutations =
        {
            new[] { 0, 1, 2 }, new[] { 0, 2, 1 }, new[] { 1, 0, 2 },
            new[] { 1, 2, 0 }, new[] { 2, 0, 1 }, new[] { 2, 1, 0 }
        };
        private static readonly Vector3[] WorldAxes = { Vector3.right, Vector3.up, Vector3.forward };
        private const int Candidates = 3 * 6 * 8;

        private const float LearningTime = 1f;      // promedio móvil del error, en segundos
        private const float MinimumEvidence = 1.5f; // segundos comparando antes de confiar
        private const float AcceptError = 10f;      // error medio máximo para aceptar una convención (grados)
        private const float AcceptMargin = 8f;      // la segunda mejor tiene que estar al menos así de lejos
        private const float RejectError = 30f;      // si la elegida se aleja tanto, se vuelve a la referencia
        private const float MaxLearningSpeed = 90f; // °/s: más rápido, el sensor de referencia llega tarde y no sirve para comparar
        private const float BlendTime = 0.5f;       // segundos para pasar de un sensor al otro sin saltos

        private readonly IGravitySource _reference;
        private readonly float[] _error = new float[Candidates];
        private float _evidence;
        private int _chosen = -1;
        private int _active = -1;   // convención usada para la salida (sigue mientras se desvanece)
        private float _blend;       // 0 = referencia, 1 = giroscopio
        private Vector3 _previousProbe;
        private AttitudeSensor _sensor;

        public AttitudeGravitySource(IGravitySource reference)
        {
            _reference = reference;
        }

        /// <summary>True cuando ya se usa el giroscopio (convención encontrada).</summary>
        public bool UsingGyro => _chosen >= 0;

        public string Name => UsingGyro
            ? $"Giroscopio ({_sensor.displayName})"
            : _reference.Name + (FindSensor() != null ? " · ajustando giroscopio" : string.Empty);

        public bool TryRead(out Vector3 gravity)
        {
            bool hasReference = _reference.TryRead(out Vector3 reference);
            AttitudeSensor sensor = FindSensor();
            if (sensor == null || !SensorUtility.Ready(sensor) || !TryAttitude(sensor, out Quaternion attitude))
            {
                gravity = reference;
                return hasReference;
            }

            Quaternion inverse = Quaternion.Inverse(attitude);
            if (hasReference && MovingSlowly(inverse)) Learn(inverse, reference.normalized);

            // Transición suave entre referencia y giroscopio: un cambio de sensor nunca es un salto del tablero.
            if (_chosen >= 0) _active = _chosen;
            _blend = Mathf.MoveTowards(_blend, _chosen >= 0 ? 1f : 0f, Time.deltaTime / BlendTime);

            if (_active < 0 || _blend <= 0f)
            {
                gravity = reference;
                return hasReference;
            }

            Vector3 gyro = Candidate(inverse, _active);
            gravity = hasReference && _blend < 1f ? Vector3.Slerp(reference.normalized, gyro, _blend) : gyro;
            return true;
        }

        /// <summary>Solo se compara contra la referencia cuando el teléfono gira despacio (la referencia tiene demora).</summary>
        private bool MovingSlowly(Quaternion inverse)
        {
            Vector3 probe = inverse * Vector3.up; // todas las convenciones giran a la misma velocidad
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            bool slow = _previousProbe == Vector3.zero || Vector3.Angle(_previousProbe, probe) / dt <= MaxLearningSpeed;
            _previousProbe = probe;
            return slow;
        }

        private void Learn(Quaternion inverse, Vector3 reference)
        {
            float k = 1f - Mathf.Exp(-Mathf.Max(Time.deltaTime, 0.001f) / LearningTime);
            bool first = _evidence <= 0f;
            _evidence += Time.deltaTime;

            if (_chosen >= 0)
            {
                // Ya elegida: solo se vigila que siga coincidiendo.
                _error[_chosen] = Mathf.Lerp(_error[_chosen], Vector3.Angle(Candidate(inverse, _chosen), reference), k);
                if (_error[_chosen] > RejectError) Restart();
                return;
            }

            int best = -1, second = -1;
            for (int i = 0; i < Candidates; i++)
            {
                float angle = Vector3.Angle(Candidate(inverse, i), reference);
                _error[i] = first ? angle : Mathf.Lerp(_error[i], angle, k);
                if (best < 0 || _error[i] < _error[best]) { second = best; best = i; }
                else if (second < 0 || _error[i] < _error[second]) second = i;
            }

            if (_evidence >= MinimumEvidence && _error[best] <= AcceptError &&
                _error[second] - _error[best] >= AcceptMargin)
                _chosen = best;
        }

        private void Restart()
        {
            _chosen = -1;
            _evidence = 0f;
        }

        private static Vector3 Candidate(Quaternion inverse, int index)
        {
            int axis = index / 48;
            int permutation = (index / 8) % 6;
            int signs = index % 8;
            Vector3 v = inverse * WorldAxes[axis];
            int[] p = Permutations[permutation];
            return new Vector3(
                ((signs & 1) == 0 ? 1f : -1f) * v[p[0]],
                ((signs & 2) == 0 ? 1f : -1f) * v[p[1]],
                ((signs & 4) == 0 ? 1f : -1f) * v[p[2]]);
        }

        private static bool TryAttitude(AttitudeSensor sensor, out Quaternion attitude)
        {
            attitude = sensor.attitude.ReadValue();
            float norm = attitude.x * attitude.x + attitude.y * attitude.y + attitude.z * attitude.z + attitude.w * attitude.w;
            if (norm < 0.5f) return false; // sin datos todavía
            attitude = Quaternion.Normalize(attitude);
            return true;
        }

        /// <summary>Prefiere el sensor "Game Rotation Vector" de Android (no usa la brújula).</summary>
        private AttitudeSensor FindSensor()
        {
            if (_sensor != null && _sensor.added) return _sensor;
            _sensor = null;
            Restart();
            foreach (InputDevice device in InputSystem.devices)
            {
                if (!(device is AttitudeSensor attitude)) continue;
                if (_sensor == null || device.layout.Contains("Game")) _sensor = attitude;
            }
            return _sensor;
        }
    }
}
