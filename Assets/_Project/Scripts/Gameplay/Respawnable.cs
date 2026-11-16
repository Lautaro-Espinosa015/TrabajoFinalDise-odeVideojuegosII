using System;
using System.Collections;
using UnityEngine;

namespace EcoTilt.Gameplay
{
    /// <summary>
    /// Oculta el objeto, espera un tiempo (1 s por defecto) y lo vuelve a colocar en el punto de inicio
    /// con velocidad cero. El punto de inicio puede ser hijo del tablero, así sigue su inclinación.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class Respawnable : MonoBehaviour, IRespawnable
    {
        [SerializeField] private Transform spawnPoint;
        [SerializeField, Min(0f)] private float respawnDelay = 1f;

        /// <summary>Se dispara cuando el objeto ya reapareció.</summary>
        public event Action<Respawnable> Respawned;

        private Rigidbody _body;
        private Renderer[] _renderers;
        private Collider[] _colliders;
        private Quaternion _spawnRotationOffset;
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
            _spawnRotationOffset = Quaternion.Inverse(reference) * transform.rotation;
        }

        private void OnDisable()
        {
            if (_routine == null) return;
            StopCoroutine(_routine);
            _routine = null;
            SetPresence(true);
        }

        public void RequestRespawn()
        {
            if (IsRespawning || !isActiveAndEnabled) return;
            _routine = StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            SetPresence(false);
            yield return new WaitForSeconds(respawnDelay);
            PlaceAtSpawn();
            SetPresence(true);
            _body.linearVelocity = Vector3.zero;
            _body.angularVelocity = Vector3.zero;
            _routine = null;
            Respawned?.Invoke(this);
        }

        private void PlaceAtSpawn()
        {
            if (spawnPoint == null)
            {
                Debug.LogWarning($"{name}: no tiene punto de reaparición asignado.", this);
                return;
            }

            Vector3 position = spawnPoint.position;
            Quaternion rotation = spawnPoint.rotation * _spawnRotationOffset;
            transform.SetPositionAndRotation(position, rotation);
            _body.position = position;
            _body.rotation = rotation;
        }

        private void SetPresence(bool present)
        {
            foreach (Renderer meshRenderer in _renderers) meshRenderer.enabled = present;
            foreach (Collider meshCollider in _colliders) meshCollider.enabled = present;
            _body.isKinematic = !present;
        }
    }
}
