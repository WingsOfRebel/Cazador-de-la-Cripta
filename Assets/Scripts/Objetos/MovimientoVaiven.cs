using UnityEngine;

// Mueve un objeto de ida y vuelta (dianas móviles).
public class MovimientoVaiven : MonoBehaviour
{
    public Vector3 m_Direccion = Vector3.right;
    public float m_Distancia = 3f;
    public float m_Velocidad = 1.5f;

    private Vector3 m_Origen;
    private float m_Fase;

    private void Start()
    {
        m_Origen = transform.position;
    }

    private void Update()
    {
        if (!Juego.Instancia.Jugable) return;
        m_Fase += Time.deltaTime * m_Velocidad;
        float d = Mathf.PingPong(m_Fase, m_Distancia * 2f) - m_Distancia;
        transform.position = m_Origen + m_Direccion.normalized * d;
    }
}
