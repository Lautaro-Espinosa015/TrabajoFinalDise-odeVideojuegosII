using UnityEngine.InputSystem;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MenuManager : MonoBehaviour
{
    private bool cargandoPartida;
    #region Variables
    public GameObject menuPrincipal;
    public GameObject menuOpciones;
    public GameObject menuTutorial;
    public GameObject menuCreditos;
    public DateTime InicioPanelUtc { get; private set; } = DateTime.UtcNow;
    #endregion

    private void Start()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Volver();
        var music = GetComponent<AudioSource>();
        if (music != null) music.volume = PlayerPrefs.GetFloat("VolumenMusica", 0.35f);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Volver();
    }

    #region Botones Menu
    public void Jugar()
    {
        if (cargandoPartida) return;
        cargandoPartida = true;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        PlayerPrefs.Save();
        SceneManager.LoadScene("Assets/_Project/Scenes/Nivel1.unity");
    }

    public void Opciones()
    {
        MostrarPanel(menuOpciones);
    }

    public void Tutorial()
    {
        MostrarPanel(menuTutorial);
    }

    public void Creditos()
    {
        MostrarPanel(menuCreditos);
    }

    public void Salir()
    {
        PlayerPrefs.Save();
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
    #endregion

    public void Volver()
    {
        MostrarPanel(menuPrincipal);
    }

    // Método robusto para evitar overlapping y bugs visuales
    private void MostrarPanel(GameObject panelDestino)
    {
        InicioPanelUtc = DateTime.UtcNow;
        // 1. Apagamos TODOS los paneles primero
        if (menuPrincipal) menuPrincipal.SetActive(false);
        if (menuOpciones) menuOpciones.SetActive(false);
        if (menuTutorial) menuTutorial.SetActive(false);
        if (menuCreditos) menuCreditos.SetActive(false);

        // 2. Encendemos únicamente el que necesitamos
        if (panelDestino) panelDestino.SetActive(true);
        if (panelDestino && EventSystem.current != null)
        {
            var first = panelDestino.GetComponentInChildren<Selectable>();
            EventSystem.current.SetSelectedGameObject(first != null ? first.gameObject : null);
        }
    }
}
