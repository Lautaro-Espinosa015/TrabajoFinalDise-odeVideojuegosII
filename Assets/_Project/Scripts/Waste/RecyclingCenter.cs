using System;
using System.Collections;
using System.Collections.Generic;
using EcoTilt.Board;
using UnityEngine;

namespace EcoTilt.Waste
{
    /// <summary>Resultado de separar un residuo.</summary>
    public readonly struct WasteSortResult
    {
        public readonly WasteItem Item;
        public readonly RecyclingBin ReceivedBy;   // contenedor al que llegó
        public readonly RecyclingBin Destination;  // contenedor donde queda (el correcto)
        public readonly bool Correct;

        public WasteSortResult(WasteItem item, RecyclingBin receivedBy, RecyclingBin destination, bool correct)
        {
            Item = item;
            ReceivedBy = receivedBy;
            Destination = destination;
            Correct = correct;
        }
    }

    /// <summary>
    /// Reglas de separación: si el residuo llegó a su contenedor, se acomoda y queda pegado;
    /// si no, vuela solo al contenedor correcto. Avisa el resultado a quien escuche (puntaje, mensajes).
    /// </summary>
    public sealed class RecyclingCenter : MonoBehaviour
    {
        [SerializeField] private BoardBuilder board;
        [SerializeField, Min(0.05f)] private float settleDuration = 0.35f;
        [SerializeField, Min(0.1f)] private float relocateDuration = 1.2f;
        [Tooltip("Altura del arco (metros) cuando un residuo vuela al contenedor correcto.")]
        [SerializeField, Min(0f)] private float relocateArc = 2.5f;

        private readonly List<RecyclingBin> _bins = new List<RecyclingBin>();

        public event Action<WasteSortResult> Sorted;

        private void OnEnable()
        {
            if (board != null) board.Rebuilt += RefreshBins;
            RefreshBins();
        }

        private void OnDisable()
        {
            if (board != null) board.Rebuilt -= RefreshBins;
            foreach (RecyclingBin bin in _bins)
                if (bin != null) bin.Received -= OnReceived;
            _bins.Clear();
        }

        /// <summary>Los contenedores se recrean al regenerar el tablero: hay que volver a escucharlos.</summary>
        private void RefreshBins()
        {
            foreach (RecyclingBin bin in _bins)
                if (bin != null) bin.Received -= OnReceived;
            _bins.Clear();
            if (board == null) return;
            _bins.AddRange(board.GetComponentsInChildren<RecyclingBin>());
            foreach (RecyclingBin bin in _bins) bin.Received += OnReceived;
        }

        private void OnReceived(WasteItem item, RecyclingBin bin)
        {
            if (item.IsSorted) return;
            item.MarkSorted();

            bool correct = bin.Category == item.Category;
            RecyclingBin destination = correct ? bin : FindBin(item.Category) ?? bin;
            Sorted?.Invoke(new WasteSortResult(item, bin, destination, correct));
            StartCoroutine(MoveInto(item, destination, correct ? settleDuration : relocateDuration, correct ? 0f : relocateArc));
        }

        private RecyclingBin FindBin(WasteCategory category)
        {
            foreach (RecyclingBin bin in _bins)
                if (bin != null && bin.Category == category) return bin;
            return null;
        }

        private IEnumerator MoveInto(WasteItem item, RecyclingBin bin, float duration, float arc)
        {
            Rigidbody body = item.Body;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.None;

            Vector3 start = item.transform.position;
            Vector3 localTarget = bin.NextStoragePoint();
            float t = 0f;
            while (t < 1f)
            {
                if (item == null) yield break;
                if (bin == null)
                {
                    Destroy(item.gameObject); // el tablero se regeneró durante el vuelo
                    yield break;
                }

                t = Mathf.Min(1f, t + Time.deltaTime / duration);
                float eased = t * t * (3f - 2f * t);
                Vector3 end = bin.transform.TransformPoint(localTarget);
                item.transform.position = Vector3.Lerp(start, end, eased) + Vector3.up * (arc * 4f * t * (1f - t));
                yield return null;
            }

            if (item != null && bin != null) bin.Store(item, localTarget);
        }
    }
}
