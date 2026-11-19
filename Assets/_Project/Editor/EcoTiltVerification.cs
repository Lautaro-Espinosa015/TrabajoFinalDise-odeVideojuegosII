using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using EcoTilt.Board;
using EcoTilt.Controls;
using EcoTilt.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

namespace EcoTilt.EditorTools
{
    // Pruebas de integración optativas. Ejecutar en Play; al salir se restaura la escena.
    public static class EcoTiltVerification
    {
        private static IEnumerator run;
        private static readonly List<string> report = new List<string>();
        private static readonly List<GameObject> temporary = new List<GameObject>();
        private static BoardTiltController controller;
        private sealed class TestInput : ITiltInput
        {
            public Quaternion Value;
            public Quaternion ReadTilt() => Value;
        }

        [MenuItem("EcoTilt/Verificar cambios en Play")]
        public static void Start()
        {
            if (!EditorApplication.isPlaying || run != null)
            {
                Debug.LogWarning("Entrá en Play antes de verificar EcoTilt.");
                return;
            }
            report.Clear();
            report.Add("Unity " + Application.unityVersion + " - pruebas automáticas en Play (no prueba en celular).");
            run = Check();
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            try { if (EditorApplication.isPlaying && run.MoveNext()) return; }
            catch (Exception e) { report.Add("FAIL: " + e); Debug.LogException(e); }
            finally { }
            EditorApplication.update -= Tick;
            (run as IDisposable)?.Dispose();
            run = null;
            if (controller != null) controller.SetInput(Object.FindFirstObjectByType<TiltInputProvider>());
            Time.timeScale = 1f;
            AudioListener.pause = false;
            foreach (GameObject go in temporary) if (go != null) Object.Destroy(go);
            temporary.Clear();
            Directory.CreateDirectory("Verificacion");
            File.WriteAllLines("Verificacion/Resultado_Play.txt", report);
            Debug.Log("EcoTilt: verificación terminada. Ver Verificacion/Resultado_Play.txt");
        }

        private static void Assert(bool ok, string message)
        {
            if (!ok) throw new Exception(message);
            report.Add("PASS: " + message);
        }

        private static object Field(object target, string name) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);

        private static IEnumerator Check()
        {
            var sensor = new SensorTiltInput(new SensorTiltInput.Settings
            {
                CalibrateOnFirstSample = true, CalibrationTime = 0.8f,
                CalibrationTolerance = 4f, MaxAngle = 30f, Sensitivity = 0.6f
            });
            sensor.ReadTilt();
            Assert(!sensor.HasData && !sensor.IsCalibrating, "PC sin sensor: disponible=false, calibrando=false");
            var device = InputSystem.AddDevice<GravitySensor>();
            try
            {
                InputSystem.EnableDevice(device);
                InputSystem.QueueDeltaStateEvent(device.gravity, new Vector3(0, 0, -1));
                InputSystem.Update();
                sensor.ReadTilt();
                Assert(sensor.HasData && sensor.IsCalibrating, "Sensor conectado inicia calibración");
            }
            finally { InputSystem.RemoveDevice(device); }
            sensor.ReadTilt();
            Assert(!sensor.HasData && !sensor.IsCalibrating, "Desconectar sensor libera calibración y disponibilidad");

            var mouse = InputSystem.AddDevice<Mouse>();
            try
            {
                var input = new MouseTiltInput(0.1f, 45f);
                InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(Screen.width, Screen.height * 0.5f) }.WithButton(MouseButton.Left));
                InputSystem.Update();
                Assert(Quaternion.Angle(input.ReadTilt(), Quaternion.identity) > 10f, "Clic izquierdo inclina");
                InputSystem.QueueStateEvent(mouse, new MouseState());
                InputSystem.Update();
                Assert(Quaternion.Angle(input.ReadTilt(), Quaternion.identity) < 0.01f, "Soltar clic pide tablero nivelado");
            }
            finally { InputSystem.RemoveDevice(mouse); }

            foreach (string name in new[] { "Botella", "Lata", "Caja" })
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Prefabs/Residuo_{name}.prefab");
                GameObject item = Object.Instantiate(prefab, new Vector3(0, 100, 0), Quaternion.identity);
                temporary.Add(item);
                Rigidbody body = item.GetComponent<Rigidbody>();
                body.useGravity = false;
                MonoBehaviour sound = Array.Find(item.GetComponents<MonoBehaviour>(), c => c.GetType().Name == "SonidoArrastre");
                AudioSource source = item.GetComponent<AudioSource>();
                Assert(sound != null && sound.enabled && source.clip != null && !source.mute &&
                    item.GetComponents<AudioSource>().Length == 1, name + ": componente habilitado, audio asignado y sin mute ni fuente duplicada");
                MethodInfo update = sound.GetType().GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
                body.linearVelocity = Vector3.right;
                update.Invoke(sound, null);
                yield return null;
                Assert(source.isPlaying && source.loop, name + ": reproduce en movimiento");
                source.time = source.clip.length * 0.4f;
                body.linearVelocity = Vector3.zero;
                update.Invoke(sound, null);
                float paused = source.time;
                Assert(!source.isPlaying, name + ": pausa al detenerse");
                body.linearVelocity = Vector3.right;
                update.Invoke(sound, null);
                Assert(source.isPlaying && Mathf.Abs(source.time - paused) < 0.1f, name + ": reanuda desde el punto conservado");
                body.isKinematic = true;
                update.Invoke(sound, null);
                Assert(!source.isPlaying, name + ": pausa al quedar cinemático");
                body.isKinematic = false;
                body.linearVelocity = Vector3.right;
                update.Invoke(sound, null);
                Time.timeScale = 0;
                update.Invoke(sound, null);
                Assert(!source.isPlaying, name + ": pausa con timeScale=0");
                Time.timeScale = 1;
                if (name == "Caja")
                {
                    PhysicsMaterial material = item.GetComponent<Collider>().sharedMaterial;
                    Assert(Mathf.Approximately(body.linearDamping, 0.01f) && material.name == "PM_CajaRapida" &&
                        Mathf.Approximately(material.dynamicFriction, 0.05f) && Mathf.Approximately(material.staticFriction, 0.08f) &&
                        material.frictionCombine == PhysicsMaterialCombine.Minimum, "Caja: damping y material propio correctos");
                }
                Object.Destroy(item);
            }

            var victory = Object.FindFirstObjectByType<SonidoVictoria>();
            if (victory != null)
            {
                var progress = Object.FindFirstObjectByType<LevelProgress>();
                var completed = (Action<int, int, int>)Field(progress, "LevelCompleted");
                completed.Invoke(progress.Level, 6, 6);
                AudioSource source = (AudioSource)Field(victory, "source");
                Assert(source.isPlaying && Field(victory, "generated") != null, "Evento LevelCompleted reproduce melodía generada en fuente propia");
                Assert(source.gameObject != victory.gameObject, "Fuente de victoria separada de música");
            }

            BoardBuilder builder = Object.FindFirstObjectByType<BoardBuilder>();
            controller = builder.GetComponent<BoardTiltController>();
            var testInput = new TestInput { Value = Quaternion.Euler(20, 0, 20) };
            controller.SetInput(testInput);
            double until = EditorApplication.timeSinceStartup + 1;
            while (EditorApplication.timeSinceStartup < until) yield return null;
            for (int round = 0; round < 2; round++)
            {
                Transform root = builder.transform.Find("Generado") ?? builder.transform;
                Transform limits = root.Find("LimitesInvisibles");
                Assert(limits != null && limits.GetComponentsInChildren<BoxCollider>().Length == 5, "Mapa " + round + ": cuatro paredes y techo, sin piso que tape pozos");
                builder.EnsureContainment();
                Assert(limits.GetComponentsInChildren<BoxCollider>().Length == 5, "Crear límites dos veces no duplica colliders");
                var probes = new List<Rigidbody>();
                foreach (BoxCollider wall in limits.GetComponentsInChildren<BoxCollider>())
                {
                    Vector3 direction = wall.name == "Techo" ? Vector3.up :
                        wall.name == "Izquierda" ? Vector3.left : wall.name == "Derecha" ? Vector3.right :
                        wall.name == "Frente" ? Vector3.back : Vector3.forward;
                    Vector3 local = wall.transform.localPosition - direction * (0.3f + 1f);
                    if (wall.name != "Techo") local.y = 3f;
                    var probe = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    temporary.Add(probe);
                    probe.transform.localScale = Vector3.one * 0.3f;
                    probe.transform.position = limits.TransformPoint(local);
                    var body = probe.AddComponent<Rigidbody>();
                    body.useGravity = false;
                    body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                    body.linearVelocity = limits.TransformDirection(direction) * 30f;
                    probes.Add(body);
                }
                until = EditorApplication.timeSinceStartup + 0.7;
                while (EditorApplication.timeSinceStartup < until) yield return null;
                float left = limits.Find("Izquierda").localPosition.x;
                float right = limits.Find("Derecha").localPosition.x;
                float front = limits.Find("Frente").localPosition.z;
                float back = limits.Find("Fondo").localPosition.z;
                float top = limits.Find("Techo").localPosition.y;
                foreach (Rigidbody body in probes)
                {
                    Vector3 p = limits.InverseTransformPoint(body.position);
                    Assert(p.x > left && p.x < right && p.z > front && p.z < back && p.y < top,
                        "Mapa " + round + ": proyectil a 30 m/s contenido con tablero inclinado");
                    Object.Destroy(body.gameObject);
                }
                if (builder.Layout == null) break; // Escena histórica: no regenerar su diseño.
                if (round == 0) builder.Rebuild(builder.Layout.CreateGrid());
            }
            report.Add("No verifica tacto/sensores físicos, escucha humana, recorrido completo ni todas las velocidades posibles.");
        }
    }
}
