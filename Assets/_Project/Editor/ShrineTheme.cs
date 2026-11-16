using UnityEngine;

namespace EcoTilt.EditorTools
{
    /// <summary>
    /// Paleta del tablero: baldosas de piedra pizarra, zócalos color arena, barandas oscuras
    /// y acentos naranjas luminosos. Cambiar el estilo = cambiar solo esta clase.
    /// </summary>
    internal sealed class ShrineTheme
    {
        public Material[] Tiles;
        public Material SlabStone;
        public Material Plinth;
        public Material Railing;
        public Material RampStone;
        public Material Glow;
        public Material DarkStone;
        public PhysicsMaterial Surface;

        public static readonly Color AmbientColor = new Color(0.30f, 0.40f, 0.44f);
        public static readonly Color BackgroundColor = new Color(0.16f, 0.30f, 0.34f);
        public static readonly Color SunColor = new Color(0.82f, 0.95f, 1.00f);
        public static readonly Color GlowColor = new Color(1.00f, 0.52f, 0.14f);

        public static ShrineTheme Create(LevelMaterialLibrary library)
        {
            return new ShrineTheme
            {
                Tiles = new[]
                {
                    library.Lit("M_Baldosa_A", new Color(0.23f, 0.33f, 0.37f), 0.30f),
                    library.Lit("M_Baldosa_B", new Color(0.20f, 0.30f, 0.34f), 0.30f),
                    library.Lit("M_Baldosa_C", new Color(0.26f, 0.36f, 0.39f), 0.30f)
                },
                SlabStone = library.Lit("M_LosaFlotante", new Color(0.11f, 0.17f, 0.20f), 0.10f),
                Plinth = library.Lit("M_Zocalo", new Color(0.80f, 0.71f, 0.48f), 0.20f),
                Railing = library.Lit("M_Baranda", new Color(0.12f, 0.17f, 0.19f), 0.35f),
                RampStone = library.Lit("M_Rampa_Piedra", new Color(0.29f, 0.38f, 0.41f), 0.25f),
                Glow = library.Emissive("M_BrilloNaranja", GlowColor, 3.0f),
                DarkStone = library.Lit("M_PiedraOscura", new Color(0.14f, 0.19f, 0.21f), 0.20f),
                Surface = library.Physics("PM_Tablero", 0.4f, 0.4f, 0.05f)
            };
        }
    }
}
