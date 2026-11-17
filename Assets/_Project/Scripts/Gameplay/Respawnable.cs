using System;
using System.Collections;
using UnityEngine;

namespace EcoTilt.Gameplay
{
    /// <summary>
    /// Oculta el objeto, espera un tiempo (1 s por defecto) y lo vuelve a colocar en el punto de inicio
    /// con velocidad cero. El punto de inicio es hijo del tablero, así sigue su inclinación.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class Respawnable : MonoBehaviour, IRespawnable
    {
        [SerializeField] private Transform spawnPoint;
        [Tooltip("Desplazamiento respecto del punto de inicio (para que varios objetos no aparezcan encimados).")]
        [SerializeField] private Vector3 spawnOffset;
        [SerializeField, Min(0f)] private float respawnDelay = 1f;

        /// <summary>Se dispara cuando el objeto ya reapareció.</summary>
        public event Action<Respawnable> Respawned;

        private Rigidbody _body;
        private Renderer[] _renderers;
        private Collider[] _colliders;
        private Quaternion _restRotation;
        private Coroutine _routine;

        public bool IsRespawning => _routine != null;

        public void SetSpawnPoint(Transform point)
        {
            spawnPoint = point;
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _renderers = GetComponentsInChildren<Renderer>();
            _colliders = GetComponentsInChildren<Collider>();

            // Conserva la orientación inicial del objeto (por ejemplo, la botella acostada).
            Quaternion reference = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;
            _restRotation = Quaternion.Inverse(reference) * transform.rotation;
        }

        private void OnDisable()
        {
            StopRoutine();
        }

        public bool RequestRespawn()
        {
            if (IsRespawning || !isActiveAndEnabled) return false;
            _routine = StartCoroutine(RespawnRoutine());
            return true;
        }

        /// <summary>Asigna punto de inicio y desplazamiento, y toma la orientación actual como la de aparición.</summary>
        public void Configure(Transform point, Vector3 offset)
        {
            spawnPoint = point;
            spawnOffset = offset;
            _restRotation = Quaternion.Inverse(point.rotation) * transform.rotation;
        }

        /// <summary>Oculta el objeto y lo deja quieto (por ejemplo, mientras se regenera el mapa).</summary>
        public void Suspend()
        {
            StopRoutine();
            SetPresence(false);
        }

        /// <summary>Lo coloca ya mismo en el punto de inicio, visible y quieto.</summary>
        public void ResetToSpawn()
        {
            StopRoutine();
            PlaceAtSpawn();
            Show();
        }

        private IEnumerator RespawnRoutine()
        {
            SetPresence(false);
            yield return new WaitForSeconds(respawnDelay);
            PlaceAtSpawn();
            Show();
            _routine = null;
            Respawned?.Invoke(this);
        }

        private void StopRoutine()
        {
            if (_routine == null) return;
            StopCoroutine(_routine);
            _routine = null;
        }

        private void PlaceAtSpawn()
        {
            if (spawnPoint == null)
            {
                Debug.LogWarning($"{name}: no tiene punto de reaparición asignado.", this);
                return;
            }

            Vector3 position = spawnPoint.position + spawnPoint.rotation * spawnOffset;
            Quaternion rotation = spawnPoint.rotation * _restRotation;
            _body.isKinematic = true;
            transform.SetPositionAndRotation(position, rotation);
            _body.position = position;
            _body.rotation = rotation;
        }

        private void Show()
        {
            SetPresence(true);
            _body.linearVelocity = Vector3.zero;
            _body.angularVelocity = Vector3.zero;
        }

        private void SetPresence(bool present)
        {
            foreach (Renderer meshRenderer in _renderers) meshRenderer.enabled = present;
            foreach (Collider meshCollider in _colliders) meshCollider.enabled = present;
            _body.isKinematic = !present;
        }
    }
}
