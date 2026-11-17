using EcoTilt.Gameplay;
using UnityEngine;

namespace EcoTilt.Waste
{
    /// <summary>Un residuo: a qué categoría pertenece, cómo se llama en los mensajes y si ya fue separado.</summary>
    [RequireComponent(typeof(Respawnable))]
    public sealed class WasteItem : MonoBehaviour
    {
        [SerializeField] private WasteCategory category;
        [Tooltip("Cómo se lo nombra en los mensajes (por ejemplo, \"La botella\").")]
        [SerializeField] private string displayName = "El residuo";
        [Tooltip("Rotación con la que aparece (por ejemplo, la botella acostada: 0, 0, 90).")]
        [SerializeField] private Vector3 restEuler;

        public WasteCategory Category => category;
        public string DisplayName => displayName;
        public Quaternion RestRotation => Quaternion.Euler(restEuler);
        public Respawnable Respawnable { get; private set; }
        public Rigidbody Body { get; private set; }
        public bool IsSorted { get; private set; }

        private void Awake()
        {
            Respawnable = GetComponent<Respawnable>();
            Body = GetComponent<Rigidbody>();
        }

        /// <summary>Lo usa el editor al preparar los prefabs.</summary>
        public void Setup(WasteCategory wasteCategory, string name, Vector3 restRotationEuler)
        {
            category = wasteCategory;
            displayName = name;
            restEuler = restRotationEuler;
        }

        /// <summary>Ya entró en un contenedor: deja de reaparecer y de contar como caída.</summary>
        public void MarkSorted()
        {
            IsSorted = true;
            Respawnable.enabled = false;
        }
    }
}
