using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Armas equipadas. Clic izquierdo usa el arma actual: la ballesta y el báculo disparan hacia
// donde apunta la mira (gastan munición) y la espada golpea de cerca (no gasta munición).
// Clic derecho acerca la vista y con 1 / 2 / 3 (o la rueda del ratón) se cambia de arma.
public class Ballesta : MonoBehaviour
{
    [Serializable]
    public class Arma
    {
        public string m_Nombre = "BALLESTA";
        public GameObject m_Modelo;          // lo que se ve en pantalla
        public GameObject m_Proyectil;       // flecha u orbe (vacío en la espada)
        public Transform m_Salida;           // punto de donde sale
        public GameObject m_Cargada;         // flecha puesta en la ballesta (opcional)
        public float m_Velocidad = 70f;
        public float m_Recarga = 0.4f;
        public string m_Sonido = "sfx_disparo";

        [Header("Cuerpo a cuerpo")]
        public bool m_CuerpoACuerpo;         // true = espada
        public float m_Alcance = 2.4f;
        public int m_Dano = 3;
    }

    public Arma[] m_Armas;
    public float m_CampoVisionNormal = 70f;
    public float m_CampoVisionZoom = 40f;
    public float m_RetrasoGolpe = 0.13f;     // el golpe cuenta cuando la espada pasa por el centro

    public Arma Actual => m_Armas[m_Indice];

    private Camera m_Cam;
    private VistaArma m_Vista;
    private float m_Reloj;
    private int m_Indice;
    private int m_Mascara;
    private float m_GolpePendiente = -1f;

    private void Awake()
    {
        m_Cam = GetComponentInParent<Camera>();
        if (m_Cam == null) m_Cam = Camera.main;
        m_Vista = GetComponentInParent<VistaArma>();
        m_Mascara = ~LayerMask.GetMask("Ignore Raycast");   // no apuntarse a uno mismo
    }

    private void Start()
    {
        Equipar(0);
    }

    private void Equipar(int indice)
    {
        if (m_Armas == null || m_Armas.Length == 0) return;
        m_Indice = Mathf.Clamp(indice, 0, m_Armas.Length - 1);
        for (int i = 0; i < m_Armas.Length; i++)
            if (m_Armas[i].m_Modelo != null) m_Armas[i].m_Modelo.SetActive(i == m_Indice);
        m_Reloj = 0.25f;   // pequeña pausa al cambiar de arma
        m_GolpePendiente = -1f;
        Juego.Instancia.Hud.ActualizarArma(Actual.m_Nombre);
        Juego.Instancia.ActualizarHud();
        Juego.Instancia.Audio.Sonar("sfx_recarga", 0.6f);
    }

    private void Update()
    {
        if (m_Armas == null || m_Armas.Length == 0) return;
        var juego = Juego.Instancia;
        var raton = Mouse.current;
        var kb = Keyboard.current;

        // Cambiar de arma
        if (juego.Jugable)
        {
            if (kb != null && kb.digit1Key.wasPressedThisFrame && m_Indice != 0) Equipar(0);
            if (kb != null && kb.digit2Key.wasPressedThisFrame && m_Armas.Length > 1 && m_Indice != 1) Equipar(1);
            if (kb != null && kb.digit3Key.wasPressedThisFrame && m_Armas.Length > 2 && m_Indice != 2) Equipar(2);
            float rueda = raton != null ? raton.scroll.ReadValue().y : 0f;
            if (Mathf.Abs(rueda) > 0.1f && m_Armas.Length > 1)
                Equipar((m_Indice + (rueda > 0f ? 1 : m_Armas.Length - 1)) % m_Armas.Length);
        }

        // El golpe de la espada se comprueba un instante después del clic
        if (m_GolpePendiente >= 0f)
        {
            m_GolpePendiente -= Time.deltaTime;
            if (m_GolpePendiente < 0f) Golpear(Actual);
        }

        if (m_Reloj > 0f)
        {
            m_Reloj -= Time.deltaTime;
            if (m_Reloj <= 0f && juego.Flechas > 0 && Actual.m_Cargada != null) Actual.m_Cargada.SetActive(true);
        }
        if (Actual.m_Cargada != null && juego.Flechas <= 0) Actual.m_Cargada.SetActive(false);

        // Zoom con clic derecho (no con la espada)
        if (m_Cam != null)
        {
            bool zoom = juego.Jugable && raton != null && raton.rightButton.isPressed && !Actual.m_CuerpoACuerpo;
            m_Cam.fieldOfView = Mathf.Lerp(m_Cam.fieldOfView, zoom ? m_CampoVisionZoom : m_CampoVisionNormal, Time.deltaTime * 10f);
        }

        if (!juego.Jugable || raton == null || !raton.leftButton.wasPressedThisFrame || m_Reloj > 0f) return;

        if (Actual.m_CuerpoACuerpo)
        {
            // Espada: no gasta munición
            m_Reloj = Actual.m_Recarga;
            m_GolpePendiente = m_RetrasoGolpe;
            var golpe = Actual.m_Modelo != null ? Actual.m_Modelo.GetComponent<GolpeEspada>() : null;
            if (golpe != null) golpe.Animar();
            juego.Audio.Sonar(Actual.m_Sonido);
            return;
        }

        if (!juego.UsarFlecha()) return;
        Disparar(Actual);
    }

    private void Disparar(Arma arma)
    {
        m_Reloj = arma.m_Recarga;
        if (arma.m_Cargada != null) arma.m_Cargada.SetActive(false);

        // Apuntar a lo que está en el centro de la pantalla (la mira)
        Ray rayo = m_Cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 destino = rayo.origin + rayo.direction * 100f;
        if (Physics.Raycast(rayo, out RaycastHit hit, 100f, m_Mascara, QueryTriggerInteraction.Ignore)) destino = hit.point;

        Vector3 origen = arma.m_Salida != null ? arma.m_Salida.position : rayo.origin + rayo.direction * 0.5f;
        // Si algo está muy cerca, el proyectil sale desde la cámara para no atravesarlo
        if (Vector3.Distance(rayo.origin, destino) < 1.5f) origen = rayo.origin;
        Vector3 direccion = (destino - origen).normalized;

        var go = Instantiate(arma.m_Proyectil, origen, Quaternion.LookRotation(direccion));
        go.GetComponent<Flecha>().Lanzar(direccion * arma.m_Velocidad);

        Juego.Instancia.Audio.Sonar(arma.m_Sonido);
        if (m_Vista != null) m_Vista.Retroceso();
    }

    // Golpe de espada: una esfera corta desde la cámara hacia la mira
    private void Golpear(Arma arma)
    {
        if (Juego.Instancia.Terminado) return;
        Ray rayo = m_Cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.SphereCast(rayo, 0.35f, out RaycastHit hit, arma.m_Alcance, m_Mascara, QueryTriggerInteraction.Ignore)) return;

        var objetivo = hit.collider.GetComponentInParent<Objetivo>();
        if (objetivo != null)
        {
            objetivo.Alcanzado(hit.point);
            return;
        }
        var enemigo = hit.collider.GetComponentInParent<EnemigoEsqueleto>();
        if (enemigo != null)
        {
            // Golpe a la cabeza: daño crítico (el doble), igual que con las flechas
            bool critico = enemigo.EsCabeza(hit.point);
            enemigo.RecibirDano(critico ? arma.m_Dano * 2 : arma.m_Dano, hit.point, critico);
            return;
        }
        Juego.Instancia.Audio.Sonar("sfx_impacto_madera", 0.6f);   // golpe a la pared
    }
}
