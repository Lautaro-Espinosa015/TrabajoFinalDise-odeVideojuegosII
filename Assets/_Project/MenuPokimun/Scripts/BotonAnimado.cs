using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class BotonAnimado : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{

    [Header("Ajustes de Escala")]
    [Tooltip("El tamaÃ±o que tendrÃ¡ el botÃ³n al pasar el mouse por encima (1.1 = 10% mÃ¡s grande)")]
    public float multiplicadorHover = 1.1f;
    [Tooltip("El tamaÃ±o que tendrÃ¡ el botÃ³n al hacer click (0.95 = 5% mÃ¡s pequeÃ±o)")]
    public float multiplicadorClick = 0.95f;
    
    [Header("Velocidad de AnimaciÃ³n")]
    public float velocidadTransicion = 12f;

    private Vector3 escalaOriginal;
    private Vector3 escalaObjetivo;
    private bool mouseEncima = false;
    private Button botonConSonido;

    private void Awake()
    {
        // Guardamos la escala original del botÃ³n (normalmente 1,1,1)
        botonConSonido = GetComponent<Button>();
        escalaOriginal = transform.localScale;
        escalaObjetivo = escalaOriginal;
    }

    private void OnDisable()
    {
        mouseEncima = false;
        escalaObjetivo = escalaOriginal;
        transform.localScale = escalaOriginal;
    }

    private void Update()
    {
        // Interpola suavemente la escala actual hacia la escala objetivo
        // Usamos unscaledDeltaTime para que la animaciÃ³n funcione incluso si el juego estÃ¡ pausado
        transform.localScale = Vector3.Lerp(transform.localScale, escalaObjetivo, Time.unscaledDeltaTime * velocidadTransicion);
    }

    private static AudioClip hoverClip;
    private static AudioClip clickClip;
    private void PlayTone(float frequency)
    {
        if (hoverClip == null) hoverClip = CreateTone(650f);
        if (clickClip == null) clickClip = CreateTone(850f);
        AudioSource.PlayClipAtPoint(frequency < 800f ? hoverClip : clickClip,
            Camera.main != null ? Camera.main.transform.position : Vector3.zero,
            EcoTilt.Audio.AudioPreferences.Effects * 0.2f);
    }

    private static AudioClip CreateTone(float frequency)
    {
        const int rate = 22050;
        var data = new float[1102];
        for (int i = 0; i < data.Length; i++)
            data[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / rate) * (1f - i / (float)data.Length);
        var clip = AudioClip.Create("MenuTone", data.Length, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        mouseEncima = true;
        escalaObjetivo = escalaOriginal * multiplicadorHover;
        if (eventData is ExtendedPointerEventData { device: Mouse } &&
            botonConSonido != null && botonConSonido.IsInteractable())
            PlayTone(650f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        mouseEncima = false;
        escalaObjetivo = escalaOriginal;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        escalaObjetivo = escalaOriginal * multiplicadorClick;
        if (eventData.button == PointerEventData.InputButton.Left &&
            botonConSonido != null && botonConSonido.IsInteractable())
            PlayTone(850f);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (mouseEncima)
        {
            escalaObjetivo = escalaOriginal * multiplicadorHover;
        }
        else
        {
            escalaObjetivo = escalaOriginal;
        }
    }
}
