using System;
using UnityEditor;
using UnityEngine;

namespace EcoTilt.EditorTools
{
    /// <summary>
    /// Crea (una sola vez) materiales visuales y materiales físicos.
    /// Si un asset ya existe se reutiliza, así los ajustes hechos a mano en el Inspector no se pierden.
    /// </summary>
    internal sealed class LevelMaterialLibrary
    {
        private readonly string _materialsFolder;
        private readonly string _physicsFolder;

        public LevelMaterialLibrary(string materialsFolder, string physicsFolder)
        {
            _materialsFolder = materialsFolder;
            _physicsFolder = physicsFolder;
            EditorAssetUtility.EnsureFolder(materialsFolder);
            EditorAssetUtility.EnsureFolder(physicsFolder);
        }

        public Material Lit(string name, Color color, float smoothness = 0.15f)
        {
            return GetOrCreate(name, "Universal Render Pipeline/Lit", material =>
            {
                SetColor(material, color);
                if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            });
        }

        /// <summary>Material que brilla (emisión HDR) para los acentos luminosos.</summary>
        public Material Emissive(string name, Color color, float intensity)
        {
            return GetOrCreate(name, "Universal Render Pipeline/Lit", material =>
            {
                SetColor(material, color);
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * intensity);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            });
        }

        public PhysicsMaterial Physics(string name, float dynamicFriction, float staticFriction, float bounciness, PhysicsMaterialCombine frictionCombine = PhysicsMaterialCombine.Average)
        {
            string path = $"{_physicsFolder}/{name}.asset";
            PhysicsMaterial material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            if (material != null) return material;

            material = new PhysicsMaterial(name)
            {
                dynamicFriction = dynamicFriction,
                staticFriction = staticFriction,
                bounciness = bounciness,
                frictionCombine = frictionCombine,
                bounceCombine = PhysicsMaterialCombine.Average
            };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private Material GetOrCreate(string name, string shaderName, Action<Material> setup)
        {
            string path = $"{_materialsFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;

            Shader shader = Shader.Find(shaderName);
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            material = new Material(shader) { name = name };
            setup(material);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void SetColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        }
    }
}
