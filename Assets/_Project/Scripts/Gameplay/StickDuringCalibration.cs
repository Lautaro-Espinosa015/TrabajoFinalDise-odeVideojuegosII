using System.Collections.Generic;
using EcoTilt.Controls;
using EcoTilt.Waste;
using UnityEngine;

namespace EcoTilt.Gameplay
{
    /// <summary>
    /// Mientras se calibra (al empezar, al recalibrar o al volver de la pausa), los residuos que están sobre
    /// el tablero quedan pegados a él y se mueven con él; al terminar se sueltan, quietos, donde estaban.
    /// Así el tablero puede nivelarse sin lanzarlos por el aire.
    /// </summary>
    public sealed class StickDuringCalibration : MonoBehaviour
    {
        [Tooltip("Componente que implemente ITiltStatus (por ejemplo, TiltInputProvider).")]
        [SerializeField] private MonoBehaviour statusSource;
        [SerializeField] private Transform board;
        [SerializeField] private WasteSpawner spawner;
        [Tooltip("Solo se pegan los residuos que están por debajo de esta altura sobre el tablero (los que caen siguen cayendo).")]
        [SerializeField, Min(0.1f)] private float stickHeight = 0.6f;

        private struct Anchor
        {
            public Vector3 LocalPosition;
            public Quaternion LocalRotation;
        }

        private readonly Dictionary<WasteItem, Anchor> _stuck = new Dictionary<WasteItem, Anchor>();
        private readonly List<WasteItem> _release = new List<WasteItem>();
        private ITiltStatus _status;

        private void Awake()
        {
            _status = statusSource as ITiltStatus;
        }

        private void FixedUpdate()
        {
            bool calibrating = _status != null && _status.IsCalibrating;
            if (calibrating)
            {
                StickNewItems();
                FollowBoard();
            }
            else if (_stuck.Count > 0)
            {
                ReleaseAll();
            }
        }

        private void StickNewItems()
        {
            foreach (WasteItem item in spawner.Active)
            {
                if (item == null || item.IsSorted || _stuck.ContainsKey(item) || item.Respawnable.IsRespawning) continue;
                Vector3 local = board.InverseTransformPoint(item.transform.position);
                if (local.y > stickHeight) continue;

                item.Body.linearVelocity = Vector3.zero;
                item.Body.angularVelocity = Vector3.zero;
                item.Body.isKinematic = true;
                _stuck.Add(item, new Anchor
                {
                    LocalPosition = local,
                    LocalRotation = Quaternion.Inverse(board.rotation) * item.transform.rotation
                });
            }
        }

        private void FollowBoard()
        {
            foreach (KeyValuePair<WasteItem, Anchor> pair in _stuck)
            {
                if (pair.Key == null) continue;
                pair.Key.Body.MovePosition(board.TransformPoint(pair.Value.LocalPosition));
                pair.Key.Body.MoveRotation(board.rotation * pair.Value.LocalRotation);
            }
        }

        private void ReleaseAll()
        {
            _release.Clear();
            _release.AddRange(_stuck.Keys);
            foreach (WasteItem item in _release)
            {
                if (item == null || item.IsSorted || item.Respawnable.IsRespawning) continue;
                item.Body.isKinematic = false;
                item.Body.linearVelocity = Vector3.zero;
                item.Body.angularVelocity = Vector3.zero;
            }
            _stuck.Clear();
        }
    }
}
