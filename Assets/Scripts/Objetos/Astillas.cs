using UnityEngine;

// Pequeña explosión de pedacitos cuando se rompe una diana o muere un esqueleto.
// Usa un ParticleSystem creado por código, así no hace falta ningún prefab.
public static class Astillas
{
    private static Material s_Material;

    public static void Crear(Vector3 lugar, Color color, int cantidad = 25)
    {
        var go = new GameObject("Astillas");
        go.transform.position = lugar;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 0.5f;
        main.loop = false;
        main.startLifetime = 0.8f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        main.startColor = color;
        main.gravityModifier = 1.5f;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emision = ps.emission;
        emision.rateOverTime = 0f;
        emision.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)cantidad) });

        var forma = ps.shape;
        forma.shapeType = ParticleSystemShapeType.Sphere;
        forma.radius = 0.2f;

        var render = go.GetComponent<ParticleSystemRenderer>();
        if (s_Material == null) s_Material = Resources.Load<Material>("MaterialAstillas");   // lo crea la herramienta
        if (s_Material == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            s_Material = new Material(shader);
        }
        render.sharedMaterial = s_Material;
        ps.Play();
    }
}
