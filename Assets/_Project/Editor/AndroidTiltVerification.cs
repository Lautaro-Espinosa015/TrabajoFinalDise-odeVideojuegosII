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
            var faithful = (TiltResponseSettings)typeof(TiltInputProvider).GetField("faithfulResponse", Private).GetValue(provider);
            var smooth = (TiltResponseSettings)typeof(TiltInputProvider).GetField("smoothResponse", Private).GetValue(provider);
            Check(faithful.MaxAngle == 35f && Mathf.Approximately(faithful.Sensitivity, 0.7f) && faithful.MaxDegreesPerSecond == 180f,
                "Respuesta Fiel moderada: 35°, sensibilidad 0,7 y giro limitado a 180°/s");
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
                    Check(Mathf.Abs(actual - Mathf.Min((angle - faithful.DeadZone) * faithful.Sensitivity, faithful.MaxAngle)) < 0.2f && actual > before && actual <= before * 1.35f,
                        $"Teléfono {angle}°: Fiel {actual:0.0}°, Suave {before:0.0}°");
                }
            }
            finally { InputSystem.RemoveDevice(device); }

            bool hadSensitivity = PlayerPrefs.HasKey("SensibilidadFiel");
            bool hadMode = PlayerPrefs.HasKey("RespuestaTablero");
            float savedSensitivity = TiltResponsePreference.LoadSensitivity();
            var savedMode = TiltResponsePreference.Load();
            try
            {
                provider.ResponseMode = TiltResponseMode.Fiel;
                provider.FaithfulSensitivity = 5f;
                Check(provider.FaithfulSensitivity == 0.8f && TiltResponsePreference.LoadSensitivity() == 0.8f,
                    "Variador limita y guarda el máximo en 0,8");
                provider.FaithfulSensitivity = 0f;
                Check(provider.FaithfulSensitivity == 0.6f, "Variador limita el mínimo en 0,6");
                provider.ResponseMode = TiltResponseMode.Suave;
                provider.FaithfulSensitivity = 0.75f;
                var response = (TiltResponseSettings)typeof(TiltInputProvider).GetProperty("CurrentResponse", Private).GetValue(provider);
                Check(response.Sensitivity == smooth.Sensitivity && response.MaxDegreesPerSecond == smooth.MaxDegreesPerSecond,
                    "Ajustar Fiel conserva el modo Suave");
                provider.ResponseMode = TiltResponseMode.Fiel;
                response = (TiltResponseSettings)typeof(TiltInputProvider).GetProperty("CurrentResponse", Private).GetValue(provider);
                Check(response.Sensitivity == 0.75f, "Cambiar a Fiel aplica la sensibilidad elegida");
            }
            finally
            {
                TiltResponsePreference.SaveSensitivity(savedSensitivity);
                TiltResponsePreference.Save(savedMode);
                if (!hadSensitivity) PlayerPrefs.DeleteKey("SensibilidadFiel");
                if (!hadMode) PlayerPrefs.DeleteKey("RespuestaTablero");
                PlayerPrefs.Save();
            }

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
