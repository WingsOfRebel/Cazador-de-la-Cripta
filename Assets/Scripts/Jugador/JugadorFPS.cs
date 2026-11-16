using UnityEngine;
using UnityEngine.InputSystem;

// Jugador en primera persona: se mueve con las flechas (o WASD), apunta con el ratón
// y salta con Espacio. Si un enemigo lo toca, pierde una vida y sale empujado.
[RequireComponent(typeof(CharacterController))]
public class JugadorFPS : MonoBehaviour
{
    public float m_Velocidad = 5f;
    public float m_VelocidadCorrer = 8f;     // con Shift
    public float m_FuerzaSalto = 5f;
    public float m_Gravedad = -18f;
    public float m_Sensibilidad = 0.07f;     // grados por píxel de ratón
    public Transform m_Camara;
    public float m_TiempoInvencible = 1.5f;  // tras recibir un golpe

    public bool Invencible => m_RelojInvencible > 0f;
    public bool SeMueve { get; private set; }

    private CharacterController m_Cc;
    private float m_Pitch;
    private float m_VelY;
    private Vector3 m_Empuje;
    private float m_RelojInvencible;

    private void Awake()
    {
        m_Cc = GetComponent<CharacterController>();
        if (m_Camara == null) m_Camara = GetComponentInChildren<Camera>().transform;
    }

    private void Update()
    {
        if (m_RelojInvencible > 0f) m_RelojInvencible -= Time.deltaTime;
        var juego = Juego.Instancia;
        var kb = Keyboard.current;
        var raton = Mouse.current;

        Vector2 mover = Vector2.zero;
        bool saltar = false, correr = false;
        if (juego.Jugable && kb != null)
        {
            if (kb.upArrowKey.isPressed || kb.wKey.isPressed) mover.y += 1f;
            if (kb.downArrowKey.isPressed || kb.sKey.isPressed) mover.y -= 1f;
            if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) mover.x += 1f;
            if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) mover.x -= 1f;
            saltar = kb.spaceKey.wasPressedThisFrame;
            correr = kb.leftShiftKey.isPressed;
        }

        // Apuntar con el ratón
        if (juego.Jugable && raton != null)
        {
            Vector2 d = raton.delta.ReadValue() * m_Sensibilidad;
            transform.Rotate(0f, d.x, 0f);
            m_Pitch = Mathf.Clamp(m_Pitch - d.y, -80f, 80f);
            m_Camara.localRotation = Quaternion.Euler(m_Pitch, 0f, 0f);
        }

        // Movimiento relativo a hacia dónde mira
        Vector3 dir = (transform.right * mover.x + transform.forward * mover.y);
        if (dir.sqrMagnitude > 1f) dir.Normalize();
        SeMueve = dir.sqrMagnitude > 0.01f && m_Cc.isGrounded;

        if (m_Cc.isGrounded)
        {
            if (m_VelY < 0f) m_VelY = -2f;
            if (saltar) m_VelY = m_FuerzaSalto;
        }
        m_VelY += m_Gravedad * Time.deltaTime;

        Vector3 vel = dir * (correr ? m_VelocidadCorrer : m_Velocidad) + Vector3.up * m_VelY + m_Empuje;
        m_Empuje = Vector3.Lerp(m_Empuje, Vector3.zero, Time.deltaTime * 6f);
        m_Cc.Move(vel * Time.deltaTime);
    }

    // Lo llama un enemigo al tocarlo
    public void RecibirGolpe(Vector3 desde, float fuerza = 9f)
    {
        if (Invencible || Juego.Instancia.Terminado) return;
        m_RelojInvencible = m_TiempoInvencible;
        Vector3 lado = transform.position - desde;
        lado.y = 0f;
        m_Empuje = lado.normalized * fuerza;
        m_VelY = 3f;
        Juego.Instancia.PerderVida();
    }
}
