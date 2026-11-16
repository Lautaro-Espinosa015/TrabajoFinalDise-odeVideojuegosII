using System.Collections.Generic;
using UnityEngine;

namespace EcoTilt.Controls
{
    /// <summary>
    /// Punto de composición del input: arma las fuentes según la configuración del Inspector
    /// y las expone como un único ITiltInput. Es el único lugar que conoce las clases concretas.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class TiltInputProvider : MonoBehaviour, ITiltInput, ICalibratable, ITiltDiagnostics
    {
        [Header("Sensores del teléfono (acelerómetro / giroscopio)")]
        [SerializeField] private bool useSensors = true;
        [Tooltip("Grados que hay que inclinar el teléfono para llegar a la inclinación máxima del tablero.")]
        [SerializeField, Range(5f, 45f)] private float deviceTiltForFullInput = 20f;
        [SerializeField, Range(1f, 30f)] private float sensorResponsiveness = 12f;
        [SerializeField, Range(0f, 0.3f)] private float sensorDeadZone = 0.05f;
        [SerializeField] private bool invertSensorX;
        [SerializeField] private bool invertSensorY;
        [Tooltip("Toma como neutra la posición en la que se sostiene el teléfono al iniciar.")]
        [SerializeField] private bool calibrateOnStart = true;

        [Header("PC / Editor")]
        [SerializeField] private bool useKeyboard = true;
        [SerializeField] private bool useMouse = true;
        [SerializeField, Range(0f, 0.5f)] private float mouseDeadZone = 0.1f;

        private ITiltInput _input;
        private SensorTiltInput _sensor;
        private Vector2 _lastTilt;

        public string Diagnostics =>
            (_sensor != null ? _sensor.Diagnostics : "Sensores desactivados") +
            $"\nTablero recibe: ({_lastTilt.x:0.00}, {_lastTilt.y:0.00})";

        private void Awake()
        {
            Build();
        }

        public Vector2 ReadTilt()
        {
            _lastTilt = _input != null ? _input.ReadTilt() : Vector2.zero;
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
                    DeviceTiltForFullInput = deviceTiltForFullInput,
                    Responsiveness = sensorResponsiveness,
                    DeadZone = sensorDeadZone,
                    InvertX = invertSensorX,
                    InvertY = invertSensorY,
                    CalibrateOnFirstSample = calibrateOnStart
                });
                sources.Add(_sensor);
            }

            if (useKeyboard) sources.Add(new KeyboardTiltInput());
            if (useMouse) sources.Add(new MouseTiltInput(mouseDeadZone));

            _input = new CompositeTiltInput(sources);
        }
    }
}
