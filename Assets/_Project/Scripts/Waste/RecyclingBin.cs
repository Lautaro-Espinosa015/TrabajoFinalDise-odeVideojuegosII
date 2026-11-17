using System;
using System.Collections.Generic;
using UnityEngine;

namespace EcoTilt.Waste
{
    /// <summary>
    /// Contenedor: detecta los residuos que entran (trigger) y guarda los que le corresponden, pegados al fondo.
    /// No decide si el residuo es correcto: solo avisa que lo recibió (eso lo resuelve RecyclingCenter).
    /// </summary>
    public sealed class RecyclingBin : MonoBehaviour
    {
        [SerializeField] private WasteCategory category;
        [Tooltip("Centro de la zona donde se apilan los residuos guardados (coordenadas locales).")]
        [SerializeField] private Vector3 storageCenter = new Vector3(0f, -0.75f, 0f);
        [Tooltip("Mitad del ancho y del fondo de esa zona.")]
        [SerializeField] private Vector2 storageHalfSize = new Vector2(0.55f, 0.55f);
        [Tooltip("Cantidad máxima de residuos guardados; al superarla se quita el más viejo.")]
        [SerializeField, Min(1)] private int capacity = 8;

        private readonly Queue<GameObject> _stored = new Queue<GameObject>();

        public WasteCategory Category => category;

        /// <summary>Se dispara cuando un residuo todavía no separado entra al contenedor.</summary>
        public event Action<WasteItem, RecyclingBin> Received;

        /// <summary>Lo usa el editor al preparar los prefabs.</summary>
        public void Setup(WasteCategory binCategory, Vector3 center, Vector2 halfSize)
        {
            category = binCategory;
            storageCenter = center;
            storageHalfSize = halfSize;
        }

        private void OnTriggerEnter(Collider other)
        {
            WasteItem item = other.GetComponentInParent<WasteItem>();
            if (item != null && !item.IsSorted) Received?.Invoke(item, this);
        }

        /// <summary>Punto (local) donde acomodar el próximo residuo; se van apilando.</summary>
        public Vector3 NextStoragePoint()
        {
            float layer = (_stored.Count / 4) * 0.25f;
            return storageCenter + new Vector3(
                UnityEngine.Random.Range(-storageHalfSize.x, storageHalfSize.x),
                layer,
                UnityEngine.Random.Range(-storageHalfSize.y, storageHalfSize.y));
        }

        /// <summary>Deja el residuo pegado al contenedor (se mueve con el tablero).</summary>
        public void Store(WasteItem item, Vector3 localPoint)
        {
            item.transform.SetParent(transform, true);
            item.transform.localPosition = localPoint;
            _stored.Enqueue(item.gameObject);
            while (_stored.Count > capacity)
            {
                GameObject oldest = _stored.Dequeue();
                if (oldest != null) Destroy(oldest);
            }
        }
    }
}
