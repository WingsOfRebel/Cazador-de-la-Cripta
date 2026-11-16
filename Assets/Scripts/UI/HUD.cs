using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Interfaz hecha por código: mira, vidas, flechas, puntos, objetivo del nivel, nombre del alumno,
// barra del jefe, textos de puntos que flotan, destello rojo al recibir daño y el menú final.
public class HUD : MonoBehaviour
{
    private Font m_Fuente;
    private GameObject m_Marcadores, m_FondoMensaje, m_PanelFinal;
    private Text m_Vidas, m_Flechas, m_Puntos, m_Objetivo, m_Titulo, m_Subtitulo, m_JefeTexto;
    private Text m_FinalTitulo, m_FinalDatos;
    private Image m_Destello, m_JefeBarra;
    private GameObject m_Jefe;
    private Transform m_ZonaFlotantes;
    private readonly List<(Text texto, float tiempo)> m_Flotantes = new List<(Text, float)>();
    private string m_NombreArma = "BALLESTA";

    private void Awake()
    {
        m_Fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var canvasGO = new GameObject("HUD");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var escala = canvasGO.AddComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920, 1080);
        escala.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();
        var raiz = canvasGO.transform;

        // Destello rojo de daño (detrás de todo)
        m_Destello = Panel(raiz, "Destello", new Color(0.8f, 0f, 0f, 0f), Vector2.zero, Vector2.zero, true);

        // Nombre y matrícula siempre visibles
        Texto(raiz, "Edwin Flete - 1-22-1805", 32, TextAnchor.UpperRight, new Vector2(1, 1), new Vector2(-30, -20), new Vector2(700, 50)).color = new Color(1f, 0.92f, 0.6f);

        m_Marcadores = new GameObject("Marcadores", typeof(RectTransform));
        m_Marcadores.transform.SetParent(raiz, false);
        Estirar(m_Marcadores.GetComponent<RectTransform>());
        var m = m_Marcadores.transform;

        // Mira en el centro
        Texto(m, "+", 56, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(80, 80));

        m_Puntos = Texto(m, "", 44, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(30, -20), new Vector2(600, 56));
        m_Objetivo = Texto(m, "", 36, TextAnchor.UpperCenter, new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(1000, 50));
        m_Vidas = Texto(m, "", 48, TextAnchor.LowerLeft, new Vector2(0, 0), new Vector2(30, 25), new Vector2(500, 60));
        m_Vidas.color = new Color(1f, 0.45f, 0.45f);
        m_Flechas = Texto(m, "", 44, TextAnchor.LowerRight, new Vector2(1, 0), new Vector2(-30, 25), new Vector2(900, 60));
        m_Flechas.color = new Color(1f, 0.85f, 0.4f);

        // Barra de vida del jefe
        m_Jefe = new GameObject("Jefe", typeof(RectTransform));
        m_Jefe.transform.SetParent(m, false);
        var rj = m_Jefe.GetComponent<RectTransform>();
        rj.anchorMin = rj.anchorMax = new Vector2(0.5f, 1f);
        rj.anchoredPosition = new Vector2(0, -110);
        rj.sizeDelta = new Vector2(800, 34);
        Panel(m_Jefe.transform, "Fondo", new Color(0, 0, 0, 0.6f), Vector2.zero, Vector2.zero, true);
        m_JefeBarra = Panel(m_Jefe.transform, "Barra", new Color(0.8f, 0.15f, 0.15f), Vector2.zero, Vector2.zero, true);
        m_JefeBarra.rectTransform.pivot = new Vector2(0f, 0.5f);
        m_JefeTexto = Texto(m_Jefe.transform, "REY ESQUELETO", 28, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800, 34));
        m_Jefe.SetActive(false);

        // Textos que flotan (+25 Diana pequeña)
        var zona = new GameObject("Flotantes", typeof(RectTransform));
        zona.transform.SetParent(raiz, false);
        var rz = zona.GetComponent<RectTransform>();
        rz.anchorMin = rz.anchorMax = new Vector2(0.5f, 0.5f);
        rz.anchoredPosition = new Vector2(0, 120);
        m_ZonaFlotantes = zona.transform;

        // Mensaje central
        m_FondoMensaje = new GameObject("Mensaje", typeof(RectTransform), typeof(Image));
        m_FondoMensaje.transform.SetParent(raiz, false);
        var rm = m_FondoMensaje.GetComponent<RectTransform>();
        rm.anchorMin = rm.anchorMax = new Vector2(0.5f, 0.5f);
        rm.sizeDelta = new Vector2(1400, 300);
        m_FondoMensaje.GetComponent<Image>().color = new Color(0, 0, 0, 0.65f);
        m_FondoMensaje.GetComponent<Image>().raycastTarget = false;
        m_Titulo = Texto(m_FondoMensaje.transform, "", 72, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 50), new Vector2(1350, 100));
        m_Subtitulo = Texto(m_FondoMensaje.transform, "", 38, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(1350, 120));
        m_FondoMensaje.SetActive(false);

        CrearPanelFinal(raiz);

        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            DontDestroyOnLoad(es);
        }
    }

    private void CrearPanelFinal(Transform raiz)
    {
        m_PanelFinal = new GameObject("PanelFinal", typeof(RectTransform), typeof(Image));
        m_PanelFinal.transform.SetParent(raiz, false);
        Estirar(m_PanelFinal.GetComponent<RectTransform>());
        m_PanelFinal.GetComponent<Image>().color = new Color(0.05f, 0.03f, 0.08f, 0.88f);
        var t = m_PanelFinal.transform;
        m_FinalTitulo = Texto(t, "", 110, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 300), new Vector2(1600, 150));
        m_FinalDatos = Texto(t, "", 46, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(1500, 300));
        Boton(t, "VOLVER A JUGAR", new Vector2(-260, -220), () => Juego.Instancia.IniciarPartida());
        Boton(t, "MENÚ PRINCIPAL", new Vector2(260, -220), () => Juego.Instancia.IrAlMenu());
        Texto(t, "o presiona Enter para jugar de nuevo", 30, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, -330), new Vector2(1200, 50)).color = new Color(1, 1, 1, 0.6f);
        m_PanelFinal.SetActive(false);
    }

    private void Update()
    {
        // Desvanecer el destello rojo
        if (m_Destello.color.a > 0f)
        {
            var c = m_Destello.color; c.a = Mathf.Max(0f, c.a - Time.unscaledDeltaTime * 1.2f); m_Destello.color = c;
        }
        // Textos flotantes: suben y se desvanecen
        for (int i = m_Flotantes.Count - 1; i >= 0; i--)
        {
            var (texto, tiempo) = m_Flotantes[i];
            float edad = Time.unscaledTime - tiempo;
            if (edad > 1.6f || texto == null) { if (texto != null) Destroy(texto.gameObject); m_Flotantes.RemoveAt(i); continue; }
            texto.rectTransform.anchoredPosition += Vector2.up * 60f * Time.unscaledDeltaTime;
            var col = texto.color; col.a = 1f - edad / 1.6f; texto.color = col;
        }
        if (m_PanelFinal.activeSelf && Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame))
            Juego.Instancia.IniciarPartida();
    }

    // ---------- API ----------

    public void MostrarHud(bool visible) => m_Marcadores.SetActive(visible);

    public void ActualizarMarcadores(int vidas, int flechas, int puntos)
    {
        m_Vidas.text = "VIDAS  " + vidas;
        m_Flechas.text = m_NombreArma + "   MUNICIÓN  " + flechas;
        m_Puntos.text = "PUNTOS  " + puntos;
    }

    public void ActualizarObjetivo(string texto) => m_Objetivo.text = texto;

    public void ActualizarArma(string nombre) => m_NombreArma = nombre;

    public void ActualizarJefe(int vida, int maxima)
    {
        m_Jefe.SetActive(vida >= 0);
        if (vida < 0) return;
        float f = Mathf.Clamp01(vida / (float)Mathf.Max(1, maxima));
        m_JefeBarra.rectTransform.anchorMax = new Vector2(f, 1f);
        m_JefeTexto.text = "REY ESQUELETO  " + vida + " / " + maxima;
    }

    public void MostrarPuntosFlotantes(string texto, bool bueno)
    {
        var t = Texto(m_ZonaFlotantes, texto, 46, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, -m_Flotantes.Count * 10f), new Vector2(900, 60));
        t.color = bueno ? new Color(0.5f, 1f, 0.5f) : new Color(1f, 0.45f, 0.35f);
        m_Flotantes.Add((t, Time.unscaledTime));
    }

    public void DestelloDano()
    {
        var c = m_Destello.color; c.a = 0.45f; m_Destello.color = c;
    }

    public void MostrarMensaje(string titulo, string subtitulo)
    {
        m_Titulo.text = titulo;
        m_Subtitulo.text = subtitulo;
        m_FondoMensaje.SetActive(true);
    }

    public void OcultarMensaje() => m_FondoMensaje.SetActive(false);

    public void MostrarFinal(bool gano, string motivo, int puntos, int vidas, int nivel)
    {
        m_FinalTitulo.text = gano ? "¡GANASTE!" : "PERDISTE";
        m_FinalTitulo.color = gano ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 0.35f, 0.3f);
        m_FinalDatos.text = motivo + "\n\nPuntuación: " + puntos + "\nVidas restantes: " + Mathf.Max(0, vidas) + "\nNivel alcanzado: " + nivel + " de 3";
        m_PanelFinal.SetActive(true);
    }

    public void OcultarFinal() => m_PanelFinal.SetActive(false);

    // ---------- Ayudas ----------

    private static void Estirar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private Image Panel(Transform padre, string nombre, Color color, Vector2 pos, Vector2 tam, bool estirar)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(padre, false);
        var rt = go.GetComponent<RectTransform>();
        if (estirar) Estirar(rt); else { rt.anchoredPosition = pos; rt.sizeDelta = tam; }
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private Text Texto(Transform padre, string texto, int tamano, TextAnchor alineacion, Vector2 ancla, Vector2 pos, Vector2 caja)
    {
        var go = new GameObject("Texto", typeof(RectTransform));
        go.transform.SetParent(padre, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = ancla;
        rt.anchoredPosition = pos;
        rt.sizeDelta = caja;
        var t = go.AddComponent<Text>();
        t.font = m_Fuente; t.text = texto; t.fontSize = tamano; t.fontStyle = FontStyle.Bold;
        t.alignment = alineacion; t.color = Color.white; t.supportRichText = true;
        t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        var borde = go.AddComponent<Outline>();
        borde.effectColor = new Color(0, 0, 0, 0.9f); borde.effectDistance = new Vector2(2, -2);
        return t;
    }

    private void Boton(Transform padre, string texto, Vector2 pos, UnityEngine.Events.UnityAction accion)
    {
        var go = new GameObject("Boton", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(padre, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(460, 100);
        go.GetComponent<Image>().color = new Color(0.45f, 0.12f, 0.12f, 0.95f);
        var b = go.GetComponent<Button>();
        var col = b.colors; col.highlightedColor = new Color(1f, 0.7f, 0.7f); b.colors = col;
        b.onClick.AddListener(accion);
        Texto(go.transform, texto, 40, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, rt.sizeDelta);
    }
}
