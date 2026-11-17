using EcoTilt.Waste;
using UnityEditor;
using UnityEngine;

namespace EcoTilt.EditorTools
{
    internal sealed class WasteCategories
    {
        public WasteCategory Plastic;
        public WasteCategory Metal;
        public WasteCategory Cardboard;

        public WasteCategory[] All => new[] { Plastic, Metal, Cardboard };
    }

    /// <summary>Crea las categorías de reciclaje la primera vez; después se editan libremente en el Inspector.</summary>
    internal static class WasteCategoryFactory
    {
        public static WasteCategories LoadOrCreate(string folder)
        {
            EditorAssetUtility.EnsureFolder(folder);
            return new WasteCategories
            {
                Plastic = Get($"{folder}/Categoria_Plastico.asset", "Plástico", "amarillo", new Color(0.98f, 0.78f, 0.12f), new[]
                {
                    "Enjuagá y aplastá las botellas antes de tirarlas: ocupan menos lugar.",
                    "Separá las tapitas: muchas campañas solidarias las juntan.",
                    "El plástico limpio y seco se recicla mucho mejor."
                }),
                Metal = Get($"{folder}/Categoria_Metal.asset", "Metal", "gris", new Color(0.72f, 0.80f, 0.88f), new[]
                {
                    "Las latas de aluminio pueden reciclarse una y otra vez sin perder calidad.",
                    "Aplastá las latas para que ocupen menos lugar en el contenedor.",
                    "Enjuagá las latas de conserva antes de tirarlas."
                }),
                Cardboard = Get($"{folder}/Categoria_Carton.asset", "Cartón y papel", "azul", new Color(0.22f, 0.52f, 1.00f), new[]
                {
                    "Desarmá las cajas: así entran más en el contenedor.",
                    "Mantené el cartón seco: mojado o con grasa ya no se puede reciclar.",
                    "Las cajas de pizza con grasa van a la basura común."
                })
            };
        }

        private static WasteCategory Get(string path, string name, string colorName, Color color, string[] tips)
        {
            WasteCategory category = AssetDatabase.LoadAssetAtPath<WasteCategory>(path);
            if (category != null) return category;
            category = EditorAssetUtility.LoadOrCreate<WasteCategory>(path);
            category.Setup(name, colorName, color, tips);
            EditorUtility.SetDirty(category);
            return category;
        }
    }
}
