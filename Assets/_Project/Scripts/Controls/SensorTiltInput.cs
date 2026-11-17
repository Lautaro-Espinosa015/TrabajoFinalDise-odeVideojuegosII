using UnityEngine;
using UnityEngine.InputSystem;

namespace EcoTilt.Controls
{
    /// <summary>
    /// El tablero como espejo del teléfono: toma la dirección de la gravedad medida por el teléfono
    /// (GravitySensor = acelerómetro + giroscopio fusionados en Android; si no existe, acelerómetro)
    /// y calcula la rotación que tiene que tener el tablero para "sentir" la misma gravedad.
    ///
    /// La inclinación es RELATIVA a la posición en la que el jugador sostiene el teléfono al calibrar:
    /// acostado, sentado, en la cama o boca abajo, esa posición pasa a ser "tablero horizontal".
    /// La calibración espera a que el teléfono esté quieto, para no tomar una posición de paso.
    /// </summary>
    public sealed class SensorTiltInput : ITiltInput, ICalibratable, ITiltDiagnostics, ITiltStatus
    {
        public struct Settings
        {
            /// <summary>Ángulo máximo del tablero, en grados.</summary>
            public float MaxAngle;
            /// <summary>1 = espejo exacto. Más de 1 exagera los movimientos, menos de 1 los suaviza.</summary>
            public float Sensitivity;
            /// <summary>Filtro de las lecturas. 0 = sin filtro; 5-10 absorbe el temblor de la mano.</summary>
            public float Smoothing;
            /// <summary>Inclinaciones menores a este ángulo (grados) se ignoran.</summary>
            public float DeadZone;
            /// <summary>Segundos que el teléfono tiene que estar quieto para calibrar.</summary>
            public float CalibrationTime;
            /// <summary>Cuánto puede moverse (grados) y seguir considerándose "quieto" al calibrar.</summary>
            public float CalibrationTolerance;
            /// <summary>Si en este tiempo (segundos) nunca queda quieto, se calibra igual con la última posición.</summary>
            public float CalibrationTimeout;
            public bool InvertX;
            public bool InvertY;
            public bool CalibrateOnFirstSample;
        }

        private readonly Settings _settings;
        private Vector3 _filtered;
        private Vector3 _lastRaw;
        private Quaternion _lastTilt = Quaternion.identity;
        private Quaternion _neutral = Quaternion.identity;
        private string _source = "ninguno";
        private bool _hasSample;

        private bool _calibrating;
        private Vector3 _calibrationAnchor;
        private Vector3 _calibrationSum;
        private float _stableTime;
        private float _calibrationElapsed;

        public SensorTiltInput(Settings settings)
        {
            _settings = settings;
            if (settings.CalibrateOnFirstSample) Calibrate();
        }

        /// <summary>True si el teléfono ya mandó datos (en la PC sin teléfono queda en false).</summary>
        public bool HasData => _hasSample;

        /// <summary>True mientras se espera que el teléfono quede quieto para calibrar (el tablero queda nivelado).</summary>
        public bool IsCalibrating => _calibrating;

        /// <summary>Progreso de la calibración (0 a 1).</summary>
        public float CalibrationProgress =>
            _calibrating ? Mathf.Clamp01(_stableTime / Mathf.Max(0.01f, _settings.CalibrationTime)) : 1f;

        public Quaternion LastTilt => _lastTilt;

        public string Diagnostics
        {
            get
            {
                Vector3 e = _lastTilt.eulerAngles;
                return $"Sensor: {_source}{(_calibrating ? "  [calibrando...]" : string.Empty)}\n" +
                       $"Gravedad: ({_lastRaw.x:0.00}, {_lastRaw.y:0.00}, {_lastRaw.z:0.00})\n" +
                       $"Tablero: {Tilt.Magnitude(_lastTilt):0}°  (X {Mathf.DeltaAngle(0f, e.x):0}°, Z {Mathf.DeltaAngle(0f, e.z):0}°)";
            }
        }

        /// <summary>
        /// Toma como "tablero horizontal" la posición en la que quede quieto el teléfono.
        /// Mientras tanto el tablero queda nivelado.
        /// </summary>
        public void Calibrate()
        {
            _calibrating = true;
            _stableTime = 0f;
            _calibrationElapsed = 0f;
            _calibrationSum = Vector3.zero;
            _calibrationAnchor = Vector3.zero;
        }

        public Quaternion ReadTilt()
        {
            if (!TryReadGravity(out Vector3 raw)) return _lastTilt = Quaternion.identity;

            _lastRaw = raw;
            _hasSample = true;

            if (_calibrating)
            {
                UpdateCalibration(raw.normalized);
                if (_calibrating) return _lastTilt = Quaternion.identity;
            }

            if (_settings.Smoothing > 0f)
                _filtered = Vector3.Lerp(_filtered, raw, 1f - Mathf.Exp(-_settings.Smoothing * Time.deltaTime));
            else
                _filtered = raw;

            // Gravedad vista desde el tablero = gravedad vista desde el teléfono (respecto de la posición neutra).
            Vector3 felt = _neutral * ToBoardAxes(_filtered.normalized);
            Quaternion tilt = SafeFromTo(felt, Vector3.down);

            float angle = Tilt.Magnitude(tilt);
            if (angle <= _settings.DeadZone) return _lastTilt = Quaternion.identity;
            float effective = (angle - _settings.DeadZone) * _settings.Sensitivity;
            tilt = Quaternion.SlerpUnclamped(Quaternion.identity, tilt, effective / angle);

            _lastTilt = Tilt.Clamp(tilt, _settings.MaxAngle);
            return _lastTilt;
        }

        /// <summary>Espera a que el teléfono esté quieto y promedia esa posición.</summary>
        private void UpdateCalibration(Vector3 gravity)
        {
            float dt = Time.deltaTime;
            _calibrationElapsed += dt;

            bool steady = _calibrationAnchor != Vector3.zero &&
                          Vector3.Angle(_calibrationAnchor, gravity) <= _settings.CalibrationTolerance;
            if (steady)
            {
                _stableTime += dt;
                _calibrationSum += gravity;
            }
            else
            {
                // Se movió: arranca de nuevo desde la posición actual.
                _calibrationAnchor = gravity;
                _calibrationSum = gravity;
                _stableTime = 0f;
            }

            bool done = _stableTime >= _settings.CalibrationTime;
            bool timedOut = _settings.CalibrationTimeout > 0f && _calibrationElapsed >= _settings.CalibrationTimeout;
            if (!done && !timedOut) return;

            _neutral = SafeFromTo(ToBoardAxes(_calibrationSum.normalized), Vector3.down);
            _filtered = gravity;
            _calibrating = false;
        }

        /// <summary>
        /// Como Quaternion.FromToRotation, pero sin ambigüedad cuando los vectores son opuestos
        /// (teléfono boca abajo): en ese caso gira siempre alrededor del eje X del tablero.
        /// </summary>
        private static Quaternion SafeFromTo(Vector3 from, Vector3 to)
        {
            return Vector3.Dot(from.normalized, to.normalized) < -0.9995f
                ? Quaternion.AngleAxis(180f, Vector3.right)
                : Quaternion.FromToRotation(from, to);
        }

        /// <summary>
        /// Ejes del teléfono → ejes del tablero. Teléfono acostado boca arriba: x = derecha, y = borde superior,
        /// z = hacia la pantalla. Tablero: x = derecha, z = adelante (lejos de la cámara), y = arriba.
        /// </summary>
        private Vector3 ToBoardAxes(Vector3 device)
        {
            float x = _settings.InvertX ? -device.x : device.x;
            float y = _settings.InvertY ? -device.y : device.y;
            return new Vector3(x, device.z, y);
        }

        private bool TryReadGravity(out Vector3 gravity)
        {
            GravitySensor gravitySensor = GravitySensor.current;
            if (Ready(gravitySensor))
            {
                gravity = gravitySensor.gravity.ReadValue();
                if (gravity.sqrMagnitude > 0.0001f)
                {
                    _source = $"GravitySensor ({gravitySensor.displayName})";
                    return true;
                }
            }

            Accelerometer accelerometer = Accelerometer.current;
            if (Ready(accelerometer))
            {
                gravity = accelerometer.acceleration.ReadValue();
                if (gravity.sqrMagnitude > 0.0001f)
                {
                    _source = $"Accelerometer ({accelerometer.displayName})";
                    return true;
                }
            }

            _source = gravitySensor == null && accelerometer == null
                ? "ninguno conectado"
                : "conectado, esperando datos";
            gravity = Vector3.zero;
            return false;
        }

        /// <summary>Activa el sensor si existe y todavía no está activo.</summary>
        private static bool Ready(InputDevice device)
        {
            if (device == null) return false;
            if (!device.enabled) InputSystem.EnableDevice(device);
            return device.enabled;
        }
    }
}
