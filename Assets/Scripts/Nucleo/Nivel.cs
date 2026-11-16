using UnityEngine;

// Va en cada nivel. Sabe cuántas flechas da, qué hay que hacer para ganar
// y revisa si te quedaste sin flechas.
public class Nivel : MonoBehaviour
{
    public enum Meta { Dianas, Enemigos, Jefe }

    public int m_Numero = 1;
    public string m_Nombre = "Galería de disparos";
    [TextArea] public string m_Instrucciones = "Dispara a las dianas";
    public Meta m_Meta = Meta.Dianas;
    public int m_FlechasIniciales = 15;
    public bool m_EsJefe;

    private bool m_Completado;

    private void Start()
    {
        Juego.Instancia.EntrarNivel(this);
    }

    public int DianasRestantes()
    {
        int n = 0;
        foreach (var o in FindObjectsByType<Objetivo>())
            if (o.m_CuentaParaNivel && !o.Destruido) n++;
        return n;
    }

    public int EnemigosRestantes()
    {
        int n = 0;
        foreach (var e in FindObjectsByType<EnemigoEsqueleto>())
            if (e.m_CuentaParaNivel && !e.Muerto) n++;
        return n;
    }

    public string TextoObjetivo()
    {
        switch (m_Meta)
        {
            case Meta.Dianas: return "Dianas restantes: " + DianasRestantes();
            case Meta.Enemigos: return "Esqueletos restantes: " + EnemigosRestantes();
            default: return "Derrota al Rey Esqueleto";
        }
    }

    // Lo llaman las dianas y los enemigos al ser destruidos
    public void Revisar()
    {
        if (m_Completado || Juego.Instancia.Terminado) return;
        Juego.Instancia.ActualizarHud();
        bool listo = m_Meta == Meta.Dianas ? DianasRestantes() == 0 : EnemigosRestantes() == 0;
        if (listo)
        {
            m_Completado = true;
            Juego.Instancia.NivelCompletado();
        }
    }

    private void Update()
    {
        if (m_Completado || Juego.Instancia.Terminado || !Juego.Instancia.Jugable) return;
        // Sin flechas, sin flechas en el aire y sin carcajes a la mano: se acabó
        if (Juego.Instancia.Flechas <= 0 && Flecha.EnVuelo == 0 && FindAnyObjectByType<Recogible>() == null)
            Juego.Instancia.Derrota("Te quedaste sin flechas");
    }
}
