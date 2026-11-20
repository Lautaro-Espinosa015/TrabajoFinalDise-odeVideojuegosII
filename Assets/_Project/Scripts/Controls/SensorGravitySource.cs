using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EcoTilt.Controls
{
    /// <summary>
    /// Gravedad medida por el sensor de gravedad de Android (acelerómetro filtrado por el sistema);
    /// si no existe, el acelerómetro. Es la fuente de referencia: estable, pero con algo de demora
    /// en los giros rápidos.
    /// </summary>
    public sealed class SensorGravitySource : IGravitySource
    {
        private string _name = "ninguno";

        public string Name => _name;

        public bool TryRead(out Vector3 gravity)
        {
            GravitySensor gravitySensor = GravitySensor.current;
            if (SensorUtility.Ready(gravitySensor))
            {
                gravity = gravitySensor.gravity.ReadValue();
                if (gravity.sqrMagnitude > 0.0001f)
                {
                    _name = $"Gravedad ({gravitySensor.displayName})";
                    return true;
                }
            }

            Accelerometer accelerometer = Accelerometer.current;
            if (SensorUtility.Ready(accelerometer))
            {
                gravity = accelerometer.acceleration.ReadValue();
                if (gravity.sqrMagnitude > 0.0001f)
                {
                    _name = $"Acelerómetro ({accelerometer.displayName})";
                    return true;
                }
            }

            _name = gravitySensor == null && accelerometer == null ? "ninguno conectado" : "conectado, esperando datos";
            gravity = Vector3.zero;
            return false;
        }
    }

    /// <summary>Activación de sensores del Input System.</summary>
    public static class SensorUtility
    {
        /// <summary>Lecturas por segundo que se piden a cada sensor.</summary>
        public const float SamplingFrequency = 100f;

        private static readonly HashSet<int> Configured = new HashSet<int>();

        /// <summary>Activa el sensor si existe y pide más lecturas por segundo (una sola vez por sensor).</summary>
        public static bool Ready(InputDevice device)
        {
            if (device == null) return false;
            if (!device.enabled) InputSystem.EnableDevice(device);
            if (!device.enabled) return false;

            if (device is Sensor sensor && Configured.Add(device.deviceId))
            {
                try
                {
                    if (sensor.samplingFrequency < SamplingFrequency) sensor.samplingFrequency = SamplingFrequency;
                }
                catch (System.Exception)
                {
                    // Algunos sensores (o Unity Remote) no permiten cambiar la frecuencia: se usa la que traen.
                }
            }
            return true;
        }
    }
}
