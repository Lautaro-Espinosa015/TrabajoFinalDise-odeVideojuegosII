using EcoTilt.Board;
using UnityEditor;
using UnityEngine;

namespace EcoTilt.EditorTools
{
    /// <summary>
    /// Crea el asset de tema del tablero (estilo "santuario": pizarra, zócalos arena, barandas oscuras,
    /// acentos naranjas) con sus materiales. Si el tema ya existe solo completa lo que falte.
    /// </summary>
    internal static class BoardThemeFactory
    {
        public static BoardTheme LoadOrCreate(string path, LevelMaterialLibrary library, Mesh cube)
        {
            BoardTheme theme = EditorAssetUtility.LoadOrCreate<BoardTheme>(path);

            if (theme.Tiles == null || theme.Tiles.Length == 0)
                theme.Tiles = new[]
                {
                    library.Lit("M_Baldosa_A", new Color(0.23f, 0.33f, 0.37f), 0.30f),
                    library.Lit("M_Baldosa_B", new Color(0.20f, 0.30f, 0.34f), 0.30f),
                    library.Lit("M_Baldosa_C", new Color(0.26f, 0.36f, 0.39f), 0.30f)
                };
            if (theme.SlabStone == null) theme.SlabStone = library.Lit("M_LosaFlotante", new Color(0.11f, 0.17f, 0.20f), 0.10f);
            if (theme.Plinth == null) theme.Plinth = library.Lit("M_Zocalo", new Color(0.80f, 0.71f, 0.48f), 0.20f);
            if (theme.Railing == null) theme.Railing = library.Lit("M_Baranda", new Color(0.12f, 0.17f, 0.19f), 0.35f);
            if (theme.RampStone == null) theme.RampStone = library.Lit("M_Rampa_Piedra", new Color(0.29f, 0.38f, 0.41f), 0.25f);
            if (theme.Glow == null) theme.Glow = library.Emissive("M_BrilloNaranja", theme.GlowColor, 3.0f);
            if (theme.DarkStone == null) theme.DarkStone = library.Lit("M_PiedraOscura", new Color(0.14f, 0.19f, 0.21f), 0.20f);
            if (theme.Surface == null) theme.Surface = library.Physics("PM_Tablero", 0.4f, 0.4f, 0.05f);
            if (theme.Cube == null) theme.Cube = cube;

            EditorUtility.SetDirty(theme);
            return theme;
        }
    }
}
