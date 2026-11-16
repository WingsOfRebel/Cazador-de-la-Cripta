using UnityEngine;

// Proyectil de la ballesta. Se mueve por su cuenta con un poco de gravedad y, en cada paso,
// lanza un rayo para saber exactamente qué tocó: una diana, un enemigo o una pared.
public class Flecha : MonoBehaviour
{
    public static int EnVuelo;              // flechas que siguen volando (para saber si se acabó la munición)

    public float m_Gravedad = 3f;
    public int m_Dano = 2;
    public float m_VidaMaxima = 6f;
    public bool m_ClavarEnPared = true;    // la flecha se queda clavada; el orbe mágico se deshace
    public Color m_ColorImpacto = new Color(0.9f, 0.9f, 0.85f);

    private Vector3 m_Velocidad;
    private bool m_Volando;
    private float m_Reloj;
    private int m_Mascara;

    private void Awake()
    {
        // El jugador está en la capa "Ignore Raycast" para que sus propias flechas no lo golpeen
        m_Mascara = ~LayerMask.GetMask("Ignore Raycast");
    }

    public void Lanzar(Vector3 velocidad)
    {
        m_Velocidad = velocidad;
        m_Volando = true;
        EnVuelo++;
    }

    private void Update()
    {
        m_Reloj += Time.deltaTime;
        if (!m_Volando)
        {
            if (m_Reloj > 4f) Destroy(gameObject);   // clavada en la pared un rato
            return;
        }
        if (m_Reloj > m_VidaMaxima) { Destroy(gameObject); return; }

        m_Velocidad += Vector3.down * m_Gravedad * Time.deltaTime;
        Vector3 paso = m_Velocidad * Time.deltaTime;

        if (Physics.Raycast(transform.position, paso.normalized, out RaycastHit hit, paso.magnitude + 0.05f, m_Mascara, QueryTriggerInteraction.Ignore))
        {
            Impacto(hit);
            return;
        }
        transform.position += paso;
        if (m_Velocidad.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(m_Velocidad);
    }

    private void Impacto(RaycastHit hit)
    {
        Parar();

        // ¿Qué tocamos?
        var objetivo = hit.collider.GetComponentInParent<Objetivo>();
        if (objetivo != null)
        {
            objetivo.Alcanzado(hit.point);
            Destroy(gameObject);   // se destruyen la diana y la flecha
            return;
        }
        var enemigo = hit.collider.GetComponentInParent<EnemigoEsqueleto>();
        if (enemigo != null)
        {
            // Tiro a la cabeza: daño crítico (el doble)
            bool critico = enemigo.EsCabeza(hit.point);
            enemigo.RecibirDano(critico ? m_Dano * 2 : m_Dano, hit.point, critico);
            Destroy(gameObject);
            return;
        }

        // Pared, piso u otra cosa: la flecha se queda clavada y el orbe se deshace
        Juego.Instancia.Audio.Sonar("sfx_impacto_madera", 0.5f);
        if (!m_ClavarEnPared)
        {
            Astillas.Crear(hit.point, m_ColorImpacto, 15);
            Destroy(gameObject);
            return;
        }
        transform.position = hit.point - transform.forward * 0.15f;
        transform.SetParent(hit.collider.transform, true);
    }

    private void Parar()
    {
        if (!m_Volando) return;
        m_Volando = false;
        m_Reloj = 0f;
        EnVuelo = Mathf.Max(0, EnVuelo - 1);
    }

    private void OnDestroy()
    {
        Parar();
    }
}
