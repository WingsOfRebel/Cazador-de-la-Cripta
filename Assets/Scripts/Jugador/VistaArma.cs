using UnityEngine;
using UnityEngine.InputSystem;

// Mueve el arma en pantalla: balanceo al caminar, un poco de inercia al girar
// y el retroceso al disparar. Las posiciones se pueden ajustar en el Inspector mientras se juega.
public class VistaArma : MonoBehaviour
{
    [Header("Posición en pantalla (relativa a la cámara)")]
    public Vector3 m_Posicion = new Vector3(0.28f, -0.32f, 0.55f);
    public Vector3 m_Rotacion = new Vector3(0f, 0f, 0f);

    [Header("Efectos")]
    public float m_Balanceo = 0.025f;
    public float m_Inercia = 0.02f;
    public float m_FuerzaRetroceso = 0.12f;

    private JugadorFPS m_Jugador;
    private float m_Fase;
    private Vector3 m_Retroceso;
    private Vector3 m_InerciaActual;

    private void Awake()
    {
        m_Jugador = GetComponentInParent<JugadorFPS>();
    }

    public void Retroceso()
    {
        m_Retroceso = new Vector3(0f, 0.03f, -m_FuerzaRetroceso);
    }

    private void LateUpdate()
    {
        // Balanceo al caminar
        bool camina = m_Jugador != null && m_Jugador.SeMueve;
        m_Fase += Time.deltaTime * (camina ? 9f : 1.5f);
        float amp = camina ? m_Balanceo : m_Balanceo * 0.25f;
        Vector3 balanceo = new Vector3(Mathf.Cos(m_Fase * 0.5f) * amp, Mathf.Sin(m_Fase) * amp * 0.6f, 0f);

        // Inercia: el arma se atrasa un poco al girar con el ratón
        Vector2 d = (Mouse.current != null && Juego.Instancia.Jugable) ? Mouse.current.delta.ReadValue() : Vector2.zero;
        Vector3 objetivo = new Vector3(-d.x, -d.y, 0f) * m_Inercia * 0.01f;
        m_InerciaActual = Vector3.Lerp(m_InerciaActual, Vector3.ClampMagnitude(objetivo, 0.05f), Time.deltaTime * 8f);

        m_Retroceso = Vector3.Lerp(m_Retroceso, Vector3.zero, Time.deltaTime * 10f);

        transform.localPosition = m_Posicion + balanceo + m_InerciaActual + m_Retroceso;
        transform.localRotation = Quaternion.Euler(m_Rotacion + new Vector3(-m_Retroceso.z * 60f, 0f, 0f));
    }
}
