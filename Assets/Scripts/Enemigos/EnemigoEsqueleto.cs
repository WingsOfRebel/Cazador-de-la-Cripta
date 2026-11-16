using System.Collections;
using UnityEngine;

// Esqueleto enemigo: espera hasta ver al jugador, lo persigue y, si lo alcanza,
// le quita una vida. Las flechas le bajan la vida; al llegar a 0 cae y desaparece.
// El jefe (Rey Esqueleto) usa este mismo script con más vida, más tamaño y esbirros.
[RequireComponent(typeof(CharacterController))]
public class EnemigoEsqueleto : MonoBehaviour
{
    public string m_Nombre = "Esqueleto";
    public int m_Vida = 1;
    public int m_Puntos = 50;
    public float m_Velocidad = 2.5f;
    public float m_DistanciaVision = 18f;
    public float m_AlcanceGolpe = 1.4f;
    public float m_EsperaGolpe = 1.5f;
    public bool m_CuentaParaNivel = true;

    [Header("Solo para el jefe")]
    public bool m_EsJefe;
    public GameObject m_PrefabEsbirro;
    public float m_CadaCuantoInvoca = 9f;
    public int m_MaxEsbirros = 4;

    public bool Muerto { get; private set; }

    private CharacterController m_Cc;
    private Animator m_Anim;
    private Transform m_Jugador;
    private JugadorFPS m_JugadorFPS;
    private int m_VidaMaxima;
    private float m_VelY;
    private float m_RelojGolpe;
    private float m_RelojAturdido;
    private float m_RelojInvocar;
    private bool m_Despierto;
    private string m_EstadoActual;
    private float m_BloqueoAnim;     // deja terminar las animaciones de ataque y golpe

    private void Awake()
    {
        m_Cc = GetComponent<CharacterController>();
        m_Anim = GetComponentInChildren<Animator>();
        m_VidaMaxima = m_Vida;
        m_RelojInvocar = m_CadaCuantoInvoca * 0.5f;
    }

    private void Start()
    {
        var j = FindAnyObjectByType<JugadorFPS>();
        if (j != null) { m_Jugador = j.transform; m_JugadorFPS = j; }
        Animar("Quieto");
    }

    private void Animar(string estado)
    {
        if (m_Anim == null || m_EstadoActual == estado || m_BloqueoAnim > 0f) return;
        m_EstadoActual = estado;
        m_Anim.CrossFadeInFixedTime(estado, 0.15f);
    }

    // Pone una animación de inmediato y la protege un rato para que se vea completa
    private void AnimarYa(string estado, float bloqueo)
    {
        if (m_Anim == null) return;
        m_Anim.CrossFadeInFixedTime(estado, 0.05f);
        m_EstadoActual = estado;
        m_BloqueoAnim = bloqueo;
    }

    private void Update()
    {
        if (Muerto || m_Jugador == null) return;
        var juego = Juego.Instancia;
        if (m_BloqueoAnim > 0f) m_BloqueoAnim -= Time.deltaTime;

        // Gravedad
        m_VelY = m_Cc.isGrounded ? -1f : m_VelY - 18f * Time.deltaTime;

        if (!juego.Jugable)
        {
            m_Cc.Move(Vector3.up * m_VelY * Time.deltaTime);
            Animar("Quieto");
            return;
        }

        Vector3 hacia = m_Jugador.position - transform.position;
        hacia.y = 0f;
        float distancia = hacia.magnitude;

        if (!m_Despierto && distancia < m_DistanciaVision) Despertar();

        if (m_RelojGolpe > 0f) m_RelojGolpe -= Time.deltaTime;
        if (m_RelojAturdido > 0f)
        {
            m_RelojAturdido -= Time.deltaTime;
            m_Cc.Move(Vector3.up * m_VelY * Time.deltaTime);
            return;
        }

        Vector3 movimiento = Vector3.zero;
        if (m_Despierto)
        {
            // Mirar al jugador
            if (distancia > 0.1f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(hacia), Time.deltaTime * 6f);

            if (distancia > m_AlcanceGolpe * 0.8f)
            {
                movimiento = hacia.normalized * m_Velocidad;
                Animar(m_Velocidad > 3f ? "Correr" : "Caminar");
            }
            else
            {
                Animar("Quieto");
            }

            // ¿Alcanza al jugador?
            if (distancia < m_AlcanceGolpe && m_RelojGolpe <= 0f && Mathf.Abs(m_Jugador.position.y - transform.position.y) < 2.5f)
            {
                m_RelojGolpe = m_EsperaGolpe;
                AnimarYa("Atacar", 0.6f);
                m_JugadorFPS.RecibirGolpe(transform.position, m_EsJefe ? 14f : 9f);
            }

            if (m_EsJefe) Invocar();
        }

        m_Cc.Move((movimiento + Vector3.up * m_VelY) * Time.deltaTime);
    }

    private void Despertar()
    {
        m_Despierto = true;
        if (m_EsJefe)
        {
            Juego.Instancia.Audio.Sonar("sfx_jefe");
            Juego.Instancia.Hud.ActualizarJefe(m_Vida, m_VidaMaxima);
        }
    }

    // El jefe llama esqueletos pequeños cada cierto tiempo
    private void Invocar()
    {
        if (m_PrefabEsbirro == null) return;
        m_RelojInvocar -= Time.deltaTime;
        if (m_RelojInvocar > 0f) return;
        m_RelojInvocar = m_CadaCuantoInvoca;

        int vivos = 0;
        foreach (var e in FindObjectsByType<EnemigoEsqueleto>()) if (!e.m_EsJefe && !e.Muerto) vivos++;
        if (vivos >= m_MaxEsbirros) return;

        for (int i = 0; i < 2; i++)
        {
            Vector3 lado = transform.right * (i == 0 ? -3f : 3f) + transform.forward * 1.5f;
            var go = Instantiate(m_PrefabEsbirro, transform.position + lado, transform.rotation);
            var e = go.GetComponent<EnemigoEsqueleto>();
            e.m_CuentaParaNivel = false;
            e.m_Despierto = true;
            Astillas.Crear(go.transform.position + Vector3.up, new Color(0.6f, 0.4f, 1f), 30);
        }
        Juego.Instancia.Hud.MostrarPuntosFlotantes("¡El Rey llama a sus esbirros!", false);
    }

    // ¿El punto está en la cabeza? (el 22% de arriba del cuerpo)
    public bool EsCabeza(Vector3 punto)
    {
        float alto = m_Cc.height * transform.lossyScale.y;
        float base_ = transform.position.y + (m_Cc.center.y * transform.lossyScale.y) - alto / 2f;
        return punto.y > base_ + alto * 0.78f;
    }

    public void RecibirDano(int dano, Vector3 punto, bool critico = false)
    {
        if (Muerto) return;
        if (!m_Despierto) Despertar();
        m_Vida -= dano;
        Juego.Instancia.Audio.Sonar("sfx_impacto_hueso");
        if (critico)
        {
            Juego.Instancia.Audio.Sonar("sfx_diana", 0.8f);
            Juego.Instancia.SumarPuntos(25, "¡CRÍTICO en la cabeza!", punto);
        }
        Astillas.Crear(punto, new Color(0.9f, 0.9f, 0.85f), 12);
        if (m_EsJefe) Juego.Instancia.Hud.ActualizarJefe(Mathf.Max(0, m_Vida), m_VidaMaxima);

        if (m_Vida <= 0)
        {
            Morir();
            return;
        }
        // Se tambalea un momento
        m_RelojAturdido = m_EsJefe ? 0.15f : 0.4f;
        AnimarYa("Golpeado", 0.4f);
    }

    private void Morir()
    {
        Muerto = true;
        m_Cc.enabled = false;
        AnimarYa("Morir", 10f);
        Juego.Instancia.Audio.Sonar("sfx_muerte_esqueleto");
        Juego.Instancia.SumarPuntos(m_Puntos, m_Nombre, transform.position);
        if (m_EsJefe)
        {
            Juego.Instancia.Hud.ActualizarJefe(-1, 1);
            // Al caer el Rey, sus esbirros caen con él
            foreach (var e in FindObjectsByType<EnemigoEsqueleto>()) if (e != this && !e.Muerto) e.Invoke(nameof(MorirSinPuntos), 0.3f);
        }
        var nivel = Juego.Instancia.NivelActual;
        if (nivel != null) nivel.Invoke(nameof(Nivel.Revisar), 0.1f);
        StartCoroutine(Desaparecer());
    }

    private void MorirSinPuntos()
    {
        if (Muerto) return;
        m_Puntos = 0;
        Morir();
    }

    private IEnumerator Desaparecer()
    {
        yield return new WaitForSeconds(2.5f);
        Astillas.Crear(transform.position + Vector3.up * 0.5f, new Color(0.8f, 0.8f, 0.75f), 20);
        Destroy(gameObject);
    }
}
