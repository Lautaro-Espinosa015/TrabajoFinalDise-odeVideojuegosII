using UnityEngine;
using UnityEngine.UI;

public class OpcionesMenu : MonoBehaviour
{
    #region Variables volumen
    public Slider volumenMusica;
    public Slider volumenEfectos;
    public float valueMusica;
    public float valueEfectos;
    public Image mute;
    #endregion
    #region Variables PC
    public Toggle fullscreen;
    public TMPro.TextMeshProUGUI fpsText;
    #endregion

    void Start()
    {
        #region Start Volumen
        valueMusica = EcoTilt.Audio.AudioPreferences.Music;
        valueEfectos = EcoTilt.Audio.AudioPreferences.Effects;
        
        if (volumenMusica != null) volumenMusica.SetValueWithoutNotify(valueMusica);
        if (volumenEfectos != null) volumenEfectos.SetValueWithoutNotify(valueEfectos);
        
        checkMute();
        #endregion
        #region Start PC
        if (fullscreen != null)
        {
            fullscreen.SetIsOnWithoutNotify(Screen.fullScreen);
            fullscreen.interactable = !Application.isMobilePlatform;
        }
        int currentFPS = LimiteFPS.Leer();
        if (fpsText != null) fpsText.text = $"FPS: {currentFPS}";
        #endregion
    }

    private void OnDisable() => PlayerPrefs.Save();

    #region Volumen
    public void changeVolumenMusica(float valor)
    {
        valueMusica = Mathf.Clamp01(valor);
        if (volumenMusica != null) volumenMusica.SetValueWithoutNotify(valueMusica);
        EcoTilt.Audio.AudioPreferences.Music = valueMusica;
        var manager = FindFirstObjectByType<MenuManager>();
        var music = manager != null ? manager.GetComponent<AudioSource>() : null;
        if (music != null) music.volume = valueMusica;
        checkMute();
    }

    public void changeVolumenEfectos(float valor)
    {
        valueEfectos = Mathf.Clamp01(valor);
        if (volumenEfectos != null) volumenEfectos.SetValueWithoutNotify(valueEfectos);
        EcoTilt.Audio.AudioPreferences.Effects = valueEfectos;
        checkMute();
    }

    public void checkMute()
    {
        if (mute == null) return;
        mute.enabled = (valueMusica == 0 && valueEfectos == 0);
    }
    #endregion

    #region Pantalla Completa
    public void changeFullscreen(bool isFullscreen)
    {
        if (!Application.isMobilePlatform) Screen.fullScreen = isFullscreen;
    }
    #endregion
    #region FPS
    public void CycleFPS()
    {
        int currentFPS = LimiteFPS.Leer();
        SetFPS(currentFPS switch
        {
            30 => 60,
            60 when !LimiteFPS.EsAndroid => 90,
            90 => 120,
            _ => 30
        });
    }
    
    public void SetFPS(int currentFPS)
    {
        currentFPS = LimiteFPS.GuardarYAplicar(currentFPS);
        
        // Actualizar texto del botÃ³n si existe
        if (fpsText != null) fpsText.text = $"FPS: {currentFPS}";
        
        Debug.Log($"[Sistema] FPS cambiados a: {currentFPS}");
    }
    #endregion
}
