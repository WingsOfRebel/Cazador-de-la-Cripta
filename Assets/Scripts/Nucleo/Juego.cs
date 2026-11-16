using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Cerebro del juego. Vive entre escenas y guarda vidas, puntos y flechas.
// Decide qué pasa al ganar un nivel, al perder (sin vidas o sin flechas) y al ganar el juego.
public class Juego : MonoBehaviour
{
    public const string EscenaMenu = "00 Menu";
    public static readonly string[] EscenasNiveles = { "01 Galeria de disparos", "02 Mazmorra", "03 Jefe" };
    public const int VidasIniciales = 3;

    private static Juego s_Instancia;
    public static Juego Instancia
    {
        get
        {
            if (s_Instancia == null) s_Instancia = new GameObject("Juego").AddComponent<Juego>();
            return s_Instancia;
        }
    }

    public int Vidas { get; private set; } = VidasIniciales;
    public int Puntos { get; private set; }
    public int Flechas { get; private set; }

    public bool Jugable { get; private set; }     // el jugador puede moverse y disparar
    public bool Pausado { get; private set; }
    public bool Terminado { get; private set; }   // se muestra el panel final

    public Nivel NivelActual { get; private set; }
    public GestorAudio Audio { get; private set; }
    public HUD Hud { get; private set; }

    private bool m_EnTransicion;

    private void Awake()
    {
        if (s_Instancia != null && s_Instancia != this) { Destroy(gameObject); return; }
        s_Instancia = this;
        DontDestroyOnLoad(gameObject);
        Audio = gameObject.AddComponent<GestorAudio>();
        Hud = gameObject.AddComponent<HUD>();
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || NivelActual == null || Terminado || m_EnTransicion) return;
        if (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame) CambiarPausa(!Pausado);
        // Al hacer clic durante la pausa se vuelve al juego
        if (Pausado && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) CambiarPausa(false);
    }

    private void CambiarPausa(bool pausa)
    {
        Pausado = pausa;
        Time.timeScale = pausa ? 0f : 1f;
        Jugable = !pausa;
        BloquearCursor(!pausa);
        if (pausa) Hud.MostrarMensaje("PAUSA", "Haz clic o presiona Esc para continuar");
        else Hud.OcultarMensaje();
    }

    public static void BloquearCursor(bool bloquear)
    {
        Cursor.lockState = bloquear ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !bloquear;
    }

    // ---------- Flujo ----------

    public void IniciarPartida()
    {
        if (m_EnTransicion) return;
        Vidas = VidasIniciales;
        Puntos = 0;
        Terminado = false;
        Audio.Sonar("sfx_select");
        StartCoroutine(Cargar(EscenasNiveles[0]));
    }

    public void IrAlMenu()
    {
        Terminado = false;
        StartCoroutine(Cargar(EscenaMenu));
    }

    public void EntrarMenu()
    {
        NivelActual = null;
        m_EnTransicion = false;
        Jugable = false;
        Terminado = false;
        Time.timeScale = 1f;
        BloquearCursor(false);
        Audio.PonerMusica("musica_mazmorra");
        Hud.MostrarHud(false);
        Hud.OcultarMensaje();
        Hud.OcultarFinal();
    }

    public void EntrarNivel(Nivel nivel)
    {
        NivelActual = nivel;
        m_EnTransicion = false;
        Terminado = false;
        Pausado = false;
        Time.timeScale = 1f;
        Flechas = nivel.m_FlechasIniciales;        // cada nivel te da un carcaj nuevo
        Audio.PonerMusica(nivel.m_EsJefe ? "musica_jefe" : "musica_mazmorra");
        Hud.MostrarHud(true);
        Hud.OcultarFinal();
        Hud.ActualizarJefe(-1, 1);
        ActualizarHud();
        StartCoroutine(Anuncio(nivel));
    }

    private IEnumerator Anuncio(Nivel nivel)
    {
        Jugable = false;
        BloquearCursor(true);
        Hud.MostrarMensaje("NIVEL " + nivel.m_Numero + ": " + nivel.m_Nombre, nivel.m_Instrucciones);
        yield return new WaitForSecondsRealtime(2.5f);
        Hud.OcultarMensaje();
        if (!Terminado) Jugable = true;
    }

    // ---------- Marcadores ----------

    public void SumarPuntos(int puntos, string motivo, Vector3 lugar)
    {
        Puntos = Mathf.Max(0, Puntos + puntos);
        Hud.MostrarPuntosFlotantes((puntos >= 0 ? "+" : "") + puntos + "  " + motivo, puntos >= 0);
        Debug.Log("Juego: " + motivo + " " + puntos + " puntos. Total: " + Puntos);
        ActualizarHud();
    }

    public bool UsarFlecha()
    {
        if (Flechas <= 0) return false;
        Flechas--;
        ActualizarHud();
        return true;
    }

    public void SumarFlechas(int cantidad)
    {
        Flechas += cantidad;
        Audio.Sonar("sfx_recoger");
        Hud.MostrarPuntosFlotantes("+" + cantidad + " flechas", true);
        ActualizarHud();
    }

    public void SumarVida()
    {
        Vidas++;
        Audio.Sonar("sfx_recoger");
        Hud.MostrarPuntosFlotantes("+1 vida", true);
        ActualizarHud();
    }

    public void ActualizarHud()
    {
        Hud.ActualizarMarcadores(Vidas, Flechas, Puntos);
        if (NivelActual != null) Hud.ActualizarObjetivo(NivelActual.TextoObjetivo());
    }

    // ---------- Final ----------

    // Un enemigo tocó al jugador
    public void PerderVida()
    {
        if (Terminado || m_EnTransicion) return;
        Vidas--;
        Audio.Sonar("sfx_dano");
        Hud.DestelloDano();
        ActualizarHud();
        if (Vidas <= 0) Derrota("Los esqueletos acabaron contigo");
    }

    public void Derrota(string motivo)
    {
        if (Terminado) return;
        Terminado = true;
        Jugable = false;
        Audio.Sonar("sfx_derrota");
        int nivel = NivelActual != null ? NivelActual.m_Numero : 0;
        Debug.Log("Juego: PERDISTE. " + motivo + ". Puntos: " + Puntos + ", vidas: " + Vidas + ", nivel " + nivel);
        StartCoroutine(PanelFinal(false, motivo));
    }

    public void NivelCompletado()
    {
        if (Terminado || m_EnTransicion || NivelActual == null) return;
        int bono = Flechas * 10;   // las flechas que sobran dan puntos
        if (bono > 0) SumarPuntos(bono, "flechas sobrantes", Vector3.zero);
        int siguiente = NivelActual.m_Numero;      // índice del próximo nivel
        if (siguiente >= EscenasNiveles.Length) { Victoria(); return; }
        m_EnTransicion = true;
        Jugable = false;
        Audio.Sonar("sfx_victoria");
        StartCoroutine(Siguiente(siguiente));
    }

    private IEnumerator Siguiente(int indice)
    {
        Hud.MostrarMensaje("¡NIVEL COMPLETADO!", indice == EscenasNiveles.Length - 1 ? "Ahora te espera el Rey Esqueleto..." : "Sigue: " + EscenasNiveles[indice].Substring(3));
        yield return new WaitForSecondsRealtime(3f);
        yield return Cargar(EscenasNiveles[indice]);
    }

    private void Victoria()
    {
        Terminado = true;
        Jugable = false;
        Audio.Sonar("sfx_victoria");
        Debug.Log("Juego: ¡GANASTE! Puntos: " + Puntos + ", vidas restantes: " + Vidas);
        StartCoroutine(PanelFinal(true, "Derrotaste al Rey Esqueleto"));
    }

    private IEnumerator PanelFinal(bool gano, string motivo)
    {
        yield return new WaitForSecondsRealtime(gano ? 1.5f : 1f);
        Hud.OcultarMensaje();
        BloquearCursor(false);
        int nivel = NivelActual != null ? NivelActual.m_Numero : 0;
        Hud.MostrarFinal(gano, motivo, Puntos, Vidas, nivel);
    }

    private IEnumerator Cargar(string escena)
    {
        m_EnTransicion = true;
        Jugable = false;
        Time.timeScale = 1f;
        Hud.OcultarMensaje();
        Hud.OcultarFinal();
        yield return null;
        SceneManager.LoadScene(escena);
    }
}
