using UnityEngine;

// Objeto que se recoge al pasar cerca: carcaj (+flechas) o poción (+1 vida).
// Gira y flota para que se vea desde lejos.
public class Recogible : MonoBehaviour
{
    public enum Tipo { Carcaj, Pocion }
    public Tipo m_Tipo = Tipo.Carcaj;
    public int m_Cantidad = 5;
    public float m_Distancia = 1.6f;

    private Transform m_Jugador;
    private Vector3 m_Inicio;

    private void Start()
    {
        var j = FindAnyObjectByType<JugadorFPS>();
        if (j != null) m_Jugador = j.transform;
        m_Inicio = transform.position;
    }

    private void Update()
    {
        transform.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World);
        transform.position = m_Inicio + Vector3.up * (Mathf.Sin(Time.time * 2f) * 0.15f);
        if (m_Jugador == null || !Juego.Instancia.Jugable) return;

        Vector3 d = m_Jugador.position - transform.position;
        d.y *= 0.5f;
        if (d.magnitude > m_Distancia) return;

        if (m_Tipo == Tipo.Carcaj) Juego.Instancia.SumarFlechas(m_Cantidad);
        else Juego.Instancia.SumarVida();
        Destroy(gameObject);
    }
}
