using System.Collections.Generic;
using UnityEngine;

namespace EcoTilt.Controls
{
    /// <summary>
    /// Punto de composición del input: arma las fuentes según la configuración del Inspector
    /// y las expone como un único ITiltInput. Es el único lugar que conoce las clases concretas.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class TiltInputProvider : MonoBehaviour, ITiltInput, ICalibratable, ITiltDiagnostics, ITiltStatus,
                                            ITiltResponseSelector
    {
        [Header("Teléfono: el tablero copia su orientación")]
        [SerializeField] private bool useSensors = true;
        [Tooltip("Respuesta \"Fiel\": el tablero copia al teléfono casi sin demora.")]
        [SerializeField] private TiltResponseSettings fiel = TiltResponseSettings.Faithful;
        [Tooltip("Respuesta \"Suave\": movimientos más tranquilos.")]
        [SerializeField] private TiltResponseSettings suave = TiltResponseSettings.Smooth;
        [Header("Calibración (posición inicial = tablero horizontal)")]
        [Tooltip("Segundos que el teléfono tiene que estar quieto para tomar su posición como horizontal.")]
        [SerializeField, Range(0.2f, 3f)] private float calibrationTime = 0.8f;
        [Tooltip("Cuánto puede moverse el teléfono (grados) y seguir considerándose quieto.")]
        [SerializeField, Range(1f, 15f)] private float calibrationTolerance = 4f;
        [Tooltip("Si nunca queda quieto, se calibra igual pasado este tiempo (segundos).")]
        [SerializeField, Range(1f, 15f)] private float calibrationTimeout = 6f;
        [Tooltip("Recalibrar automáticamente si la pantalla gira (por ejemplo, el teléfono se da vuelta 180°).")]
        [SerializeField] private bool recalibrateOnScreenRotation = true;
        [SerializeField] private bool invertSensorX;
        [SerializeField] private bool invertSensorY;
        [Tooltip("Toma como horizontal la posición en la que se sostiene el teléfono al iniciar.")]
        [SerializeField] private bool calibrateOnStart = true;

        [Header("PC / Editor")]
        [SerializeField] private bool useKeyboard = true;
        [SerializeField, Range(5f, 90f)] private float keyboardMaxAngle = 25f;
        [SerializeField, Min(10f)] private float keyboardDegreesPerSecond = 90f;
        [SerializeField] private bool useMouse = true;
        [SerializeField, Range(5f, 90f)] private float mouseMaxAngle = 45f;
        [SerializeField, Range(0f, 0.5f)] private float mouseDeadZone = 0.1f;

        private ITiltInput _input;
        private SensorTiltInput _sensor;
        private Quaternion _lastTilt = Quaternion.identity;
        private ScreenOrientation _orientation;
        private TiltResponseMode _mode;

        /// <summary>Respuesta elegida por el jugador (se guarda entre partidas).</summary>
        public TiltResponseMode ResponseMode
        {
            get => _mode;
            set
            {
                _mode = value;
                TiltResponsePreference.Save(value);
                _sensor?.SetResponse(CurrentResponse);
            }
        }

        private TiltResponseSettings CurrentResponse =>
            _mode == TiltResponseMode.Suave ? suave : fiel;

        public bool HasData => _sensor != null && _sensor.HasData;
        public bool IsCalibrating => _sensor != null && _sensor.IsCalibrating;
        public float CalibrationProgress => _sensor != null ? _sensor.CalibrationProgress : 1f;
        public Quaternion LastTilt => _lastTilt;

        /// <summary>Sensor que se está usando (por ejemplo, "Giroscopio (...)").</summary>
        public string SensorName => _sensor != null ? _sensor.SourceName : "Sensores desactivados";

        public string Diagnostics =>
            (_sensor != null ? _sensor.Diagnostics : "Sensores desactivados") +
            $"\nTablero recibe: {Tilt.Magnitude(_lastTilt):0}°";

        private void Awake()
        {
            if (Application.isMobilePlatform)
            {
                Screen.autorotateToPortrait = false;
                Screen.autorotateToPortraitUpsideDown = false;
                Screen.autorotateToLandscapeLeft = false;
                Screen.autorotateToLandscapeRight = false;
                Screen.orientation = ScreenOrientation.LandscapeLeft;
            }
            _mode = TiltResponsePreference.Load();
            Build();
            _orientation = Screen.orientation;
        }

        private void Update()
        {
            if (!recalibrateOnScreenRotation || Screen.orientation == _orientation) return;
            _orientation = Screen.orientation;
            Calibrate();
        }

        public Quaternion ReadTilt()
        {
            _lastTilt = _input != null ? _input.ReadTilt() : Quaternion.identity;
            return _lastTilt;
        }

        public void Calibrate()
        {
            _sensor?.Calibrate();
        }

        private void Build()
        {
            var sources = new List<ITiltInput>();

            // Siempre se agrega: los sensores pueden aparecer después (Unity Remote) y se activan al leer.
            if (useSensors)
            {
                _sensor = new SensorTiltInput(new SensorTiltInput.Settings
                {
                    CalibrationTime = calibrationTime,
                    CalibrationTolerance = calibrationTolerance,
                    CalibrationTimeout = calibrationTimeout,
                    InvertX = invertSensorX,
                    InvertY = invertSensorY,
                    CalibrateOnFirstSample = calibrateOnStart
                });
                _sensor.SetResponse(CurrentResponse);
                sources.Add(_sensor);
            }

            if (useKeyboard) sources.Add(new KeyboardTiltInput(keyboardMaxAngle, keyboardDegreesPerSecond));
            if (useMouse) sources.Add(new MouseTiltInput(mouseDeadZone, mouseMaxAngle));

            _input = new CompositeTiltInput(sources);
        }
    }
}
