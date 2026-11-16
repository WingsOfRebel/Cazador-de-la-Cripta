using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Menú de inicio: título, botones JUGAR, CONTROLES y SALIR. La cámara gira despacio por la cripta.
public class MenuPrincipal : MonoBehaviour
{
    public string m_Titulo = "CAZADOR DE LA CRIPTA";
    public Transform m_CamaraGiratoria;

    private Font m_Fuente;
    private GameObject m_Controles;

    private void Start()
    {
        Juego.Instancia.EntrarMenu();
        m_Fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var canvasGO = new GameObject("MenuCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var escala = canvasGO.AddComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920, 1080);
        escala.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();
        var raiz = canvasGO.transform;

        Texto(raiz, m_Titulo, 110, new Vector2(0, 320), new Vector2(1800, 160)).color = new Color(1f, 0.8f, 0.35f);
        Texto(raiz, "Un juego de disparos en primera persona con ballesta", 40, new Vector2(0, 210), new Vector2(1600, 60));

        Boton(raiz, "JUGAR", new Vector2(0, 50), () => Juego.Instancia.IniciarPartida());
        Boton(raiz, "CONTROLES", new Vector2(0, -80), () => m_Controles.SetActive(!m_Controles.activeSelf));
#if !UNITY_WEBGL
        Boton(raiz, "SALIR", new Vector2(0, -210), Application.Quit);
#endif

        m_Controles = new GameObject("Controles", typeof(RectTransform), typeof(Image));
        m_Controles.transform.SetParent(raiz, false);
        m_Controles.GetComponent<RectTransform>().sizeDelta = new Vector2(1200, 640);
        m_Controles.GetComponent<Image>().color = new Color(0, 0, 0, 0.88f);
        Texto(m_Controles.transform,
            "CONTROLES\n\n<size=40>Moverse: flechas del teclado (o W A S D)\nApuntar: ratón\nDisparar / golpear: clic izquierdo      Zoom: clic derecho\nArmas: 1 ballesta, 2 báculo, 3 espada (o rueda del ratón)\nGolpe a la cabeza: daño crítico\n" +
            "Correr: Shift      Saltar: Espacio\nPausa: Esc\n\nHaz clic en CONTROLES para cerrar</size>", 56, Vector2.zero, new Vector2(1150, 620));
        m_Controles.SetActive(false);
    }

    private void Update()
    {
        if (m_CamaraGiratoria != null) m_CamaraGiratoria.Rotate(0f, 6f * Time.deltaTime, 0f, Space.World);
        var kb = Keyboard.current;
        if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)) Juego.Instancia.IniciarPartida();
    }

    private Text Texto(Transform padre, string texto, int tamano, Vector2 pos, Vector2 caja)
    {
        var go = new GameObject("Texto", typeof(RectTransform));
        go.transform.SetParent(padre, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos; rt.sizeDelta = caja;
        var t = go.AddComponent<Text>();
        t.font = m_Fuente; t.text = texto; t.fontSize = tamano; t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter; t.color = Color.white; t.supportRichText = true;
        t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        var borde = go.AddComponent<Outline>();
        borde.effectColor = new Color(0, 0, 0, 0.9f); borde.effectDistance = new Vector2(3, -3);
        return t;
    }

    private void Boton(Transform padre, string texto, Vector2 pos, UnityEngine.Events.UnityAction accion)
    {
        var go = new GameObject("Boton_" + texto, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(padre, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(460, 100);
        go.GetComponent<Image>().color = new Color(0.45f, 0.12f, 0.12f, 0.95f);
        var b = go.GetComponent<Button>();
        var col = b.colors; col.highlightedColor = new Color(1f, 0.7f, 0.7f); b.colors = col;
        b.onClick.AddListener(accion);
        Texto(go.transform, texto, 52, Vector2.zero, rt.sizeDelta);
    }
}
