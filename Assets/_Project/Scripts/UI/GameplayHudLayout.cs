using UnityEngine;

namespace EcoTilt.UI
{
    /// <summary>Alinea los controles superiores y respeta los recortes de pantalla del teléfono.</summary>
    public static class GameplayHudLayout
    {
        public static Rect PauseButton(float scale)
        {
            Rect safe = Screen.safeArea;
            return new Rect(safe.xMax - 100f * scale, Screen.height - safe.yMax + 16f * scale,
                84f * scale, 70f * scale);
        }

        public static Rect CalibrationButton(float scale)
        {
            Rect pause = PauseButton(scale);
            return new Rect(pause.x - 192f * scale, pause.y, 180f * scale, pause.height);
        }
    }
}
