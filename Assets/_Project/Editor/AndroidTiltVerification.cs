using System;
using System.Reflection;
using EcoTilt.Board;
using EcoTilt.Controls;
using EcoTilt.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EcoTilt.EditorTools
{
    public static class AndroidTiltVerification
    {
        private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [MenuItem("EcoTilt/Verificar respuesta de inclinación")]
        public static void Verify()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Nivel1.unity");
            var provider = UnityEngine.Object.FindFirstObjectByType<TiltInputProvider>();
            Check(provider != null, "Nivel1 tiene proveedor de sensores");
            var faithful = (TiltResponseSettings)typeof(TiltInputProvider).GetField("fiel", Private).GetValue(provider);
            var smooth = (TiltResponseSettings)typeof(TiltInputProvider).GetField("suave", Private).GetValue(provider);
            Check(faithful.MaxAngle == 90f && faithful.Sensitivity == 1f && faithful.MaxDegreesPerSecond == 0f,
                "Respuesta Fiel: 1 a 1, hasta 90°, sin límite de giro");
            Check(smooth.MaxAngle == 30f && smooth.Sensitivity < 1f && smooth.MaxDegreesPerSecond > 0f,
                "Respuesta Suave: 30°, sensibilidad reducida y giro limitado");
            Check(UnityEngine.Object.FindFirstObjectByType<BoardTiltController>() != null, "Control físico del tablero presente");
            var calibration = UnityEngine.Object.FindFirstObjectByType<TiltDebugOverlay>();
            Check(calibration != null && (typeof(TiltDebugOverlay).GetField("source", Private).GetValue(calibration) as MonoBehaviour) == provider,
                "Botón de calibrar conectado al proveedor");

            var device = InputSystem.AddDevice<GravitySensor>();
            try
            {
                InputSystem.EnableDevice(device);
                var restored = new SensorTiltInput(new SensorTiltInput.Settings
                {
                    MaxAngle = faithful.MaxAngle, Sensitivity = faithful.Sensitivity, DeadZone = faithful.DeadZone, Smoothing = 0f
                });
                var reduced = new SensorTiltInput(new SensorTiltInput.Settings
                {
                    MaxAngle = smooth.MaxAngle, Sensitivity = smooth.Sensitivity, DeadZone = smooth.DeadZone, Smoothing = 0f
                });
                foreach (float angle in new[] { 30f, 60f })
                {
                    float radians = angle * Mathf.Deg2Rad;
                    InputSystem.QueueDeltaStateEvent(device.gravity, new Vector3(Mathf.Sin(radians), 0f, -Mathf.Cos(radians)));
                    InputSystem.Update();
                    float actual = Tilt.Magnitude(restored.ReadTilt());
                    float before = Tilt.Magnitude(reduced.ReadTilt());
                    Check(Mathf.Abs(actual - (angle - faithful.DeadZone)) < 0.2f && actual > before,
                        $"Teléfono {angle}°: Fiel {actual:0.0}°, Suave {before:0.0}°");
                }
            }
            finally { InputSystem.RemoveDevice(device); }

            foreach (float scale in new[] { 0.75f, 1f, 1.5f })
            {
                Rect pause = GameplayHudLayout.PauseButton(scale);
                Rect calibrate = GameplayHudLayout.CalibrationButton(scale);
                Check(calibrate.xMax < pause.xMin && calibrate.y == pause.y && calibrate.height == pause.height,
                    $"Calibrar alineado a la izquierda de Pausa, escala {scale}");
            }
            Debug.Log("ECOTILT_ANDROID_TILT_VERIFIED: sensores simulados y controles de interfaz OK; pendiente tacto real en Android.");
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
            Debug.Log("TILT_PASS: " + message);
        }
    }
}
