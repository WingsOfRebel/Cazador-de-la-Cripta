using UnityEngine;

// Animación del tajo de la espada hecha por código: sube un poco hacia atrás,
// corta en diagonal de derecha a izquierda y vuelve a su sitio.
public class GolpeEspada : MonoBehaviour
{
    public float m_TiempoPreparar = 0.07f;
    public float m_TiempoCorte = 0.12f;
    public float m_TiempoVolver = 0.25f;

    [Header("Poses (relativas a la posición de reposo)")]
    public Vector3 m_PosPreparar = new Vector3(0.08f, 0.1f, -0.05f);
    public Vector3 m_RotPreparar = new Vector3(-15f, 0f, -25f);
    public Vector3 m_PosCorte = new Vector3(-0.35f, -0.12f, 0.15f);
    public Vector3 m_RotCorte = new Vector3(75f, -20f, 70f);

    private Vector3 m_PosReposo;
    private Quaternion m_RotReposo;
    private float m_Reloj = -1f;

    private void Awake()
    {
        m_PosReposo = transform.localPosition;
        m_RotReposo = transform.localRotation;
    }

    private void OnDisable()
    {
        // Si se cambia de arma a mitad del tajo, vuelve a su sitio
        m_Reloj = -1f;
        transform.localPosition = m_PosReposo;
        transform.localRotation = m_RotReposo;
    }

    public void Animar()
    {
        m_Reloj = 0f;
    }

    private void Update()
    {
        if (m_Reloj < 0f) return;
        m_Reloj += Time.deltaTime;

        Quaternion rotPrep = m_RotReposo * Quaternion.Euler(m_RotPreparar);
        Quaternion rotCorte = m_RotReposo * Quaternion.Euler(m_RotCorte);
        Vector3 posPrep = m_PosReposo + m_PosPreparar;
        Vector3 posCorte = m_PosReposo + m_PosCorte;

        float t = m_Reloj;
        if (t < m_TiempoPreparar)
        {
            float k = t / m_TiempoPreparar;
            Poner(Vector3.Lerp(m_PosReposo, posPrep, k), Quaternion.Slerp(m_RotReposo, rotPrep, k));
        }
        else if ((t -= m_TiempoPreparar) < m_TiempoCorte)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / m_TiempoCorte);
            Poner(Vector3.Lerp(posPrep, posCorte, k), Quaternion.Slerp(rotPrep, rotCorte, k));
        }
        else if ((t -= m_TiempoCorte) < m_TiempoVolver)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / m_TiempoVolver);
            Poner(Vector3.Lerp(posCorte, m_PosReposo, k), Quaternion.Slerp(rotCorte, m_RotReposo, k));
        }
        else
        {
            Poner(m_PosReposo, m_RotReposo);
            m_Reloj = -1f;
        }
    }

    private void Poner(Vector3 pos, Quaternion rot)
    {
        transform.localPosition = pos;
        transform.localRotation = rot;
    }
}
