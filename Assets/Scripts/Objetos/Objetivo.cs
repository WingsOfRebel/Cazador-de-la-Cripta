using UnityEngine;

// Cualquier cosa a la que se puede disparar para ganar (o perder) puntos:
// dianas grandes, pequeñas, móviles, de pared, botellas y barriles.
// Al recibir la flecha da sus puntos y se destruye junto con la flecha.
public class Objetivo : MonoBehaviour
{
    public string m_Nombre = "Diana";
    public int m_Puntos = 10;
    public bool m_CuentaParaNivel = true;   // las dianas cuentan; barriles y botellas no

    public bool Destruido { get; private set; }

    public void Alcanzado(Vector3 punto)
    {
        if (Destruido) return;
        Destruido = true;

        var juego = Juego.Instancia;
        juego.SumarPuntos(m_Puntos, m_Nombre, punto);
        juego.Audio.Sonar(m_Puntos < 0 ? "sfx_penalizacion" : "sfx_diana");
        juego.Audio.Sonar("sfx_romper", 0.6f);
        Astillas.Crear(punto, m_Puntos < 0 ? new Color(0.6f, 0.35f, 0.2f) : new Color(1f, 0.85f, 0.3f));

        if (juego.NivelActual != null) juego.NivelActual.Invoke(nameof(Nivel.Revisar), 0f);
        Destroy(gameObject);
    }
}
