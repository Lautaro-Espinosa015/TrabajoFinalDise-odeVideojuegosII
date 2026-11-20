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
            float max = (float)typeof(TiltInputProvider).GetField("maxBoardAngle", Private).GetValue(provider);
            float sensitivity = (float)typeof(TiltInputProvider).GetField("sensitivity", Private).GetValue(provider);
            Check(max == 90f && sensitivity == 1f, "Escena: sensibilidad 1 y límite 90°, como el merge #2");
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
                    MaxAngle = max, Sensitivity = sensitivity, DeadZone = 2f, Smoothing = 0f
                });
                var reduced = new SensorTiltInput(new SensorTiltInput.Settings
                {
                    MaxAngle = 30f, Sensitivity = 0.6f, DeadZone = 2f, Smoothing = 0f
                });
                foreach (float angle in new[] { 30f, 60f })
                {
                    float radians = angle * Mathf.Deg2Rad;
                    InputSystem.QueueDeltaStateEvent(device.gravity, new Vector3(Mathf.Sin(radians), 0f, -Mathf.Cos(radians)));
                    InputSystem.Update();
                    float actual = Tilt.Magnitude(restored.ReadTilt());
                    float before = Tilt.Magnitude(reduced.ReadTilt());
                    Check(Mathf.Abs(actual - (angle - 2f)) < 0.2f && actual > before,
                        $"Teléfono {angle}°: restaurado {actual:0.0}°, merge #3 {before:0.0}°");
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
