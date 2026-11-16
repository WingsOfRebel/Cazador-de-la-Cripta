using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Herramienta del editor: menú "FPS > Construir todo".
// Configura los modelos, crea materiales, animaciones y prefabs,
// y arma las escenas 00 Menu, 01 Galeria de disparos, 02 Mazmorra y 03 Jefe.
public static class ConstruirJuego
{
    const string Modelos = "Assets/Modelos";
    const string Generado = "Assets/Generado";
    const string Prefabs = Generado + "/Prefabs";
    const string Materiales = Generado + "/Materiales";
    const string Plantilla = "Assets/Scenes/SampleScene.unity";

    static readonly StringBuilder s_Reporte = new StringBuilder();
    static Material s_MatMazmorra, s_MatDianas, s_MatEsqueleto, s_MatJefe, s_MatArmas, s_MatMagia;
    static AnimatorController s_CtrlEsqueleto;

    [MenuItem("FPS/Construir todo")]
    public static void ConstruirTodo()
    {
        s_Reporte.Clear();
        if (!File.Exists(Plantilla))
        {
            EditorUtility.DisplayDialog("FPS", "No encuentro " + Plantilla + " (la escena de ejemplo de la plantilla).", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Carpeta(Generado); Carpeta(Prefabs); Carpeta(Materiales); Carpeta("Assets/Resources");
        ConfigurarModelos();
        CrearMateriales();
        CrearAnimaciones();
        var p = CrearPrefabs();

        CrearMenu(p);
        CrearGaleria(p);
        CrearMazmorra(p);
        CrearJefe(p);

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/00 Menu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/01 Galeria de disparos.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/02 Mazmorra.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/03 Jefe.unity", true),
        };
        Log("Escenas en Build Settings: 00 Menu, 01 Galeria de disparos, 02 Mazmorra, 03 Jefe");
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene("Assets/Scenes/00 Menu.unity", OpenSceneMode.Single);
        File.WriteAllText("FPS_construccion.txt", s_Reporte.ToString());
        Log("LISTO. Abre la escena 00 Menu y dale a Play.");
    }

    static void Log(string t) { s_Reporte.AppendLine(t); Debug.Log("FPS: " + t); }

    static void Carpeta(string ruta)
    {
        if (AssetDatabase.IsValidFolder(ruta)) return;
        string padre = Path.GetDirectoryName(ruta).Replace('\\', '/');
        Carpeta(padre);
        AssetDatabase.CreateFolder(padre, Path.GetFileName(ruta));
    }

    // ---------------- Modelos ----------------

    static bool EsBucle(string clip)
    {
        string c = clip.ToLowerInvariant();
        return c.Contains("idle") || c.Contains("walking") || c.Contains("running") || c.Contains("relax");
    }

    static void ConfigurarModelos()
    {
        int cambiados = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { Modelos }))
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            var mi = AssetImporter.GetAtPath(ruta) as ModelImporter;
            if (mi == null) continue;
            bool animado = ruta.Contains("/Esqueletos/") || ruta.Contains("/Animaciones/") || ruta.Contains("/Brazos/");
            bool conClips = ruta.Contains("/Animaciones/") || ruta.Contains("/Brazos/");
            bool cambio = false;

            if (mi.importCameras || mi.importLights) { mi.importCameras = false; mi.importLights = false; cambio = true; }
            var tipo = animado ? ModelImporterAnimationType.Generic : ModelImporterAnimationType.None;
            if (mi.animationType != tipo) { mi.animationType = tipo; cambio = true; }
            if (mi.importAnimation != conClips) { mi.importAnimation = conClips; cambio = true; }
            if (cambio) { mi.SaveAndReimport(); cambiados++; mi = AssetImporter.GetAtPath(ruta) as ModelImporter; }

            // Las animaciones de caminar, correr y quieto se repiten en bucle
            if (conClips)
            {
                var clips = mi.clipAnimations.Length > 0 ? mi.clipAnimations : mi.defaultClipAnimations;
                bool tocar = mi.clipAnimations.Length == 0;
                foreach (var c in clips)
                {
                    bool bucle = EsBucle(c.name);
                    if (c.loopTime != bucle) { c.loopTime = bucle; tocar = true; }
                }
                if (tocar) { mi.clipAnimations = clips; mi.SaveAndReimport(); cambiados++; }
            }
        }
        Log("Modelos configurados ahora: " + cambiados);
    }

    static GameObject M(string carpeta, string nombre)
    {
        var go = AssetDatabase.LoadAssetAtPath<GameObject>(Modelos + "/" + carpeta + "/" + nombre + ".fbx");
        if (go == null) Log("AVISO: falta el modelo " + carpeta + "/" + nombre);
        return go;
    }

    // ---------------- Materiales ----------------

    static Material Mat(string nombre, string textura, Color color, bool emision = false)
    {
        string ruta = Materiales + "/" + nombre + ".mat";
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, ruta); }
        m.shader = shader;
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(textura);
        if (tex == null) Log("AVISO: falta la textura " + textura);
        m.SetTexture("_BaseMap", tex);
        m.SetColor("_BaseColor", color);
        m.SetFloat("_Smoothness", 0.1f);
        if (emision)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", new Color(0.35f, 0.05f, 0.05f));
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    static void CrearMateriales()
    {
        s_MatMazmorra = Mat("Mazmorra", Modelos + "/Mazmorra/dungeon_texture.png", Color.white);
        s_MatDianas = Mat("Dianas", Modelos + "/Dianas/prototypebits_texture.png", Color.white);
        s_MatEsqueleto = Mat("Esqueleto", Modelos + "/Esqueletos/skeleton_texture.png", Color.white);
        s_MatJefe = Mat("Jefe", Modelos + "/Esqueletos/skeleton_texture.png", new Color(1f, 0.55f, 0.45f), true);
        s_MatArmas = Mat("Armas", Modelos + "/Armas/skeleton_texture.png", Color.white);
        s_MatMagia = Mat("Magia", Modelos + "/Armas/skeleton_texture.png", new Color(0.7f, 0.3f, 1f));
        s_MatMagia.SetTexture("_BaseMap", null);
        s_MatMagia.EnableKeyword("_EMISSION");
        s_MatMagia.SetColor("_EmissionColor", new Color(1.2f, 0.4f, 2.5f));

        // Material de las astillas (partículas), en Resources para que también exista en el build
        string ruta = "Assets/Resources/MaterialAstillas.mat";
        var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        var ma = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (ma == null && sh != null) { ma = new Material(sh); AssetDatabase.CreateAsset(ma, ruta); }
        Log("Materiales creados en " + Materiales);
    }

    static void PonerMaterial(GameObject go, Material m)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            var mats = new Material[Mathf.Max(1, r.sharedMaterials.Length)];
            for (int i = 0; i < mats.Length; i++) mats[i] = m;
            r.sharedMaterials = mats;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }
    }

    // ---------------- Animaciones ----------------

    static AnimationClip Clip(string ruta, string nombre)
    {
        var clips = AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToList();
        var clip = clips.FirstOrDefault(c => c.name == nombre) ?? clips.FirstOrDefault(c => c.name.EndsWith(nombre));
        if (clip == null) Log("AVISO: no encuentro la animación " + nombre + " en " + ruta + " (hay: " + string.Join(", ", clips.Select(c => c.name)) + ")");
        return clip;
    }

    static AnimatorController Controlador(string nombre, (string estado, AnimationClip clip)[] estados)
    {
        string ruta = Generado + "/" + nombre + ".controller";
        AssetDatabase.DeleteAsset(ruta);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ruta);
        var sm = ctrl.layers[0].stateMachine;
        foreach (var (estado, clip) in estados)
        {
            var st = sm.AddState(estado);
            st.motion = clip;
            if (sm.defaultState == null || estado == "Quieto") sm.defaultState = st;
        }
        return ctrl;
    }

    static void CrearAnimaciones()
    {
        string gen = Modelos + "/Animaciones/Rig_Medium_General.fbx";
        string mov = Modelos + "/Animaciones/Rig_Medium_MovementBasic.fbx";
        s_CtrlEsqueleto = Controlador("Esqueleto", new[]
        {
            ("Quieto", Clip(gen, "Idle_A")),
            ("Caminar", Clip(mov, "Walking_A")),
            ("Correr", Clip(mov, "Running_A")),
            ("Atacar", Clip(gen, "Throw")),
            ("Golpeado", Clip(gen, "Hit_A")),
            ("Morir", Clip(gen, "Death_A")),
        });
        Log("Animator Controller creado (Esqueleto)");
    }

    // ---------------- Prefabs ----------------

    static GameObject Instancia(GameObject modelo, Transform padre)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(modelo);
        if (padre != null) go.transform.SetParent(padre, false);
        return go;
    }

    static Bounds Limites(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    // Caja de colisión del tamaño del modelo
    static BoxCollider CajaAjustada(GameObject raiz)
    {
        var b = Limites(raiz);
        var c = raiz.AddComponent<BoxCollider>();
        c.center = raiz.transform.InverseTransformPoint(b.center);
        var t = raiz.transform.InverseTransformVector(b.size);
        c.size = new Vector3(Mathf.Abs(t.x), Mathf.Abs(t.y), Mathf.Abs(t.z));
        return c;
    }

    static GameObject Guardar(GameObject go, string nombre)
    {
        var p = PrefabUtility.SaveAsPrefabAsset(go, Prefabs + "/" + nombre + ".prefab");
        Object.DestroyImmediate(go);
        return p;
    }

    static GameObject CrearObjetivo(string prefab, GameObject modelo, string nombre, int puntos, bool cuenta, float escala = 1f, bool movil = false)
    {
        var raiz = new GameObject(prefab);
        var m = Instancia(modelo, raiz.transform);
        m.transform.localScale = Vector3.one * escala;
        PonerMaterial(m, modelo.name.StartsWith("target") ? s_MatDianas : s_MatMazmorra);
        CajaAjustada(raiz);
        var o = raiz.AddComponent<Objetivo>();
        o.m_Nombre = nombre;
        o.m_Puntos = puntos;
        o.m_CuentaParaNivel = cuenta;
        if (movil) raiz.AddComponent<MovimientoVaiven>();
        return Guardar(raiz, prefab);
    }

    static GameObject CrearEsqueleto(string prefab, string modelo, string nombre, int vida, int puntos, float velocidad, float escala, Material mat, bool jefe = false)
    {
        var raiz = new GameObject(prefab);
        var m = Instancia(M("Esqueletos", modelo), raiz.transform);
        m.transform.localScale = Vector3.one * escala;
        PonerMaterial(m, mat);
        var anim = m.GetComponentInChildren<Animator>();
        if (anim == null) anim = m.AddComponent<Animator>();
        anim.runtimeAnimatorController = s_CtrlEsqueleto;
        anim.applyRootMotion = false;
        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        var b = Limites(raiz);
        var cc = raiz.AddComponent<CharacterController>();
        float alto = Mathf.Max(1f, b.size.y);
        cc.height = alto;
        cc.radius = Mathf.Clamp(Mathf.Min(b.size.x, b.size.z) * 0.35f, 0.3f, 1.4f);
        cc.center = new Vector3(0f, alto / 2f + 0.02f, 0f);
        cc.skinWidth = 0.05f;

        var e = raiz.AddComponent<EnemigoEsqueleto>();
        e.m_Nombre = nombre;
        e.m_Vida = vida;
        e.m_Puntos = puntos;
        e.m_Velocidad = velocidad;
        e.m_EsJefe = jefe;
        e.m_AlcanceGolpe = cc.radius + 1.1f;
        if (jefe)
        {
            e.m_DistanciaVision = 30f;
            var luz = new GameObject("LuzJefe").AddComponent<Light>();
            luz.transform.SetParent(raiz.transform, false);
            luz.transform.localPosition = new Vector3(0f, alto * 0.6f, 1.5f);
            luz.type = LightType.Point; luz.color = new Color(1f, 0.25f, 0.2f); luz.range = 8f; luz.intensity = 3f;
        }
        Log("Prefab " + prefab + ": alto " + alto.ToString("0.0") + " m, vida " + vida);
        return Guardar(raiz, prefab);
    }

    static GameObject CrearRecogible(string prefab, GameObject modelo, Material mat, Recogible.Tipo tipo, int cantidad, float escala, Color luz)
    {
        var raiz = new GameObject(prefab);
        var m = Instancia(modelo, raiz.transform);
        m.transform.localScale = Vector3.one * escala;
        PonerMaterial(m, mat);
        var r = raiz.AddComponent<Recogible>();
        r.m_Tipo = tipo;
        r.m_Cantidad = cantidad;
        var l = new GameObject("Brillo").AddComponent<Light>();
        l.transform.SetParent(raiz.transform, false);
        l.transform.localPosition = Vector3.up * 0.6f;
        l.type = LightType.Point; l.color = luz; l.range = 3f; l.intensity = 2f;
        return Guardar(raiz, prefab);
    }

    static Dictionary<string, GameObject> CrearPrefabs()
    {
        var p = new Dictionary<string, GameObject>();

        // Flecha (proyectil)
        {
            var raiz = new GameObject("Flecha");
            var m = Instancia(M("Armas", "Skeleton_Arrow"), raiz.transform);
            m.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);   // la punta del modelo mira hacia -Y: se gira para que mire al frente
            m.transform.localScale = Vector3.one * 1.3f;
            PonerMaterial(m, s_MatArmas);
            raiz.AddComponent<Flecha>();
            p["Flecha"] = Guardar(raiz, "Flecha");
        }

        // Orbe mágico (proyectil del báculo): esfera brillante sin gravedad
        {
            var raiz = new GameObject("OrbeMagico");
            var esfera = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.DestroyImmediate(esfera.GetComponent<Collider>());
            esfera.transform.SetParent(raiz.transform, false);
            esfera.transform.localScale = Vector3.one * 0.28f;
            esfera.GetComponent<Renderer>().sharedMaterial = s_MatMagia;
            var luz = new GameObject("Luz").AddComponent<Light>();
            luz.transform.SetParent(raiz.transform, false);
            luz.type = LightType.Point; luz.color = new Color(0.7f, 0.4f, 1f); luz.range = 4f; luz.intensity = 3f; luz.shadows = LightShadows.None;
            var f = raiz.AddComponent<Flecha>();
            f.m_Gravedad = 0f;
            f.m_Dano = 1;
            f.m_ClavarEnPared = false;
            f.m_ColorImpacto = new Color(0.7f, 0.4f, 1f);
            p["Orbe"] = Guardar(raiz, "OrbeMagico");
        }

        // Dianas y objetos con puntaje
        p["Diana"] = CrearObjetivo("Diana", M("Dianas", "target_stand_A"), "Diana", 10, true);
        p["DianaLejana"] = CrearObjetivo("DianaLejana", M("Dianas", "target_stand_B"), "Diana lejana", 15, true);
        p["DianaPequena"] = CrearObjetivo("DianaPequena", M("Dianas", "target_small"), "Diana pequeña", 25, true);
        p["DianaMovil"] = CrearObjetivo("DianaMovil", M("Dianas", "target"), "Diana móvil", 50, true, 1.2f, true);
        p["DianaPared"] = CrearObjetivo("DianaPared", M("Dianas", "target_wall_large_A"), "Diana de pared", 15, true);
        p["DianaParedPequena"] = CrearObjetivo("DianaParedPequena", M("Dianas", "target_wall_small"), "Diana de pared pequeña", 30, true);
        p["Barril"] = CrearObjetivo("Barril", M("Mazmorra", "barrel_large"), "Barril (¡no dispares!)", -10, false);
        p["Botella"] = CrearObjetivo("Botella", M("Mazmorra", "bottle_A_brown"), "Botella", 5, false);

        // Enemigos
        p["Esbirro"] = CrearEsqueleto("Esbirro", "Skeleton_Minion", "Esqueleto esbirro", 2, 50, 3.4f, 1f, s_MatEsqueleto);
        p["Picaro"] = CrearEsqueleto("Picaro", "Skeleton_Rogue", "Esqueleto pícaro", 3, 75, 4.2f, 1f, s_MatEsqueleto);
        p["Guerrero"] = CrearEsqueleto("Guerrero", "Skeleton_Warrior", "Esqueleto guerrero", 5, 100, 2.2f, 1f, s_MatEsqueleto);
        p["Mago"] = CrearEsqueleto("Mago", "Skeleton_Mage", "Esqueleto mago", 3, 100, 2.6f, 1f, s_MatEsqueleto);
        p["Jefe"] = CrearEsqueleto("Jefe", "Skeleton_Warrior", "Rey Esqueleto", 30, 1000, 2.4f, 2.6f, s_MatJefe, true);
        {
            // El jefe invoca esbirros
            var jefe = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(p["Jefe"]));
            jefe.GetComponent<EnemigoEsqueleto>().m_PrefabEsbirro = p["Esbirro"];
            PrefabUtility.SaveAsPrefabAsset(jefe, AssetDatabase.GetAssetPath(p["Jefe"]));
            PrefabUtility.UnloadPrefabContents(jefe);
        }

        // Objetos para recoger
        p["Carcaj"] = CrearRecogible("Carcaj", M("Armas", "Skeleton_Quiver"), s_MatArmas, Recogible.Tipo.Carcaj, 5, 1.6f, new Color(1f, 0.85f, 0.4f));
        p["Pocion"] = CrearRecogible("Pocion", M("Mazmorra", "bottle_A_green"), s_MatMazmorra, Recogible.Tipo.Pocion, 1, 1.3f, new Color(0.4f, 1f, 0.4f));

        // Jugador: cuerpo, cámara, ballesta y brazos
        {
            var raiz = new GameObject("Jugador");
            raiz.layer = 2;   // Ignore Raycast: así sus flechas y la mira no lo detectan
            raiz.tag = "Player";
            var cc = raiz.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.35f; cc.center = new Vector3(0f, 0.92f, 0f); cc.skinWidth = 0.05f;
            var jug = raiz.AddComponent<JugadorFPS>();

            var camGO = new GameObject("Camara");
            camGO.tag = "MainCamera";
            camGO.transform.SetParent(raiz.transform, false);
            camGO.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            var cam = camGO.AddComponent<Camera>();
            cam.nearClipPlane = 0.03f;
            cam.fieldOfView = 70f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.03f, 0.06f);
            camGO.AddComponent<AudioListener>();
            jug.m_Camara = camGO.transform;

            // Arma: todo lo que se ve en primera persona va dentro de "Arma" y se mueve junto.
            // No se usan brazos: sus animaciones no servían para sostener armas, así que solo se ve el arma.
            var arma = new GameObject("Arma");
            arma.transform.SetParent(camGO.transform, false);
            var vista = arma.AddComponent<VistaArma>();
            vista.m_Posicion = Vector3.zero;

            // Ballesta abajo a la derecha, apuntando al frente (el modelo ya mira hacia +Z)
            var ballestaGO = new GameObject("ModeloBallesta");
            ballestaGO.transform.SetParent(arma.transform, false);
            ballestaGO.transform.localPosition = new Vector3(0.2f, -0.2f, 0.35f);
            ballestaGO.transform.localRotation = Quaternion.Euler(0f, -3f, 0f);
            var ballestaModelo = Instancia(M("Armas", "Skeleton_Crossbow"), ballestaGO.transform);
            ballestaModelo.transform.localRotation = Quaternion.identity;
            ballestaModelo.transform.localScale = Vector3.one * 0.45f;
            PonerMaterial(ballestaModelo, s_MatArmas);
            var salidaB = new GameObject("Salida");
            salidaB.transform.SetParent(ballestaGO.transform, false);
            salidaB.transform.localPosition = new Vector3(0f, 0.06f, 0.55f);
            var cargada = Instancia(M("Armas", "Skeleton_Arrow"), ballestaGO.transform);
            cargada.name = "FlechaCargada";
            cargada.transform.localPosition = new Vector3(0f, 0.065f, 0.23f);
            cargada.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);   // la flecha viene de pie: se acuesta
            cargada.transform.localScale = Vector3.one * 0.65f;
            PonerMaterial(cargada, s_MatArmas);

            // Báculo mágico abajo a la derecha, inclinado hacia delante
            var baculoGO = new GameObject("ModeloBaculo");
            baculoGO.transform.SetParent(arma.transform, false);
            baculoGO.transform.localPosition = new Vector3(0.28f, -0.55f, 0.6f);
            baculoGO.transform.localRotation = Quaternion.Euler(25f, 0f, 8f);
            var baculoModelo = Instancia(M("Armas", "Skeleton_Staff"), baculoGO.transform);
            baculoModelo.transform.localRotation = Quaternion.identity;   // el báculo ya viene de pie
            baculoModelo.transform.localScale = Vector3.one * 0.42f;
            PonerMaterial(baculoModelo, s_MatArmas);
            var salidaM = new GameObject("Salida");
            salidaM.transform.SetParent(baculoGO.transform, false);
            salidaM.transform.localPosition = new Vector3(-0.07f, 0.55f, 0f);

            // Espada (cuerpo a cuerpo): de pie, inclinada hacia delante y hacia el centro
            var espadaGO = new GameObject("ModeloEspada");
            espadaGO.transform.SetParent(arma.transform, false);
            espadaGO.transform.localPosition = new Vector3(0.3f, -0.38f, 0.5f);
            espadaGO.transform.localRotation = Quaternion.Euler(30f, 0f, 20f);
            var espadaModelo = Instancia(M("Armas", "Skeleton_Blade"), espadaGO.transform);
            espadaModelo.transform.localRotation = Quaternion.identity;   // la espada ya viene de pie
            espadaModelo.transform.localScale = Vector3.one * 0.45f;
            PonerMaterial(espadaModelo, s_MatArmas);
            espadaGO.AddComponent<GolpeEspada>();

            foreach (var r in arma.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var armas = arma.AddComponent<Ballesta>();
            armas.m_Armas = new[]
            {
                new Ballesta.Arma { m_Nombre = "BALLESTA", m_Modelo = ballestaGO, m_Proyectil = p["Flecha"], m_Salida = salidaB.transform,
                                    m_Cargada = cargada, m_Velocidad = 70f, m_Recarga = 0.4f, m_Sonido = "sfx_disparo" },
                new Ballesta.Arma { m_Nombre = "BÁCULO", m_Modelo = baculoGO, m_Proyectil = p["Orbe"], m_Salida = salidaM.transform,
                                    m_Velocidad = 40f, m_Recarga = 0.22f, m_Sonido = "sfx_magia" },
                new Ballesta.Arma { m_Nombre = "ESPADA", m_Modelo = espadaGO, m_CuerpoACuerpo = true, m_Alcance = 2.4f, m_Dano = 3,
                                    m_Recarga = 0.5f, m_Sonido = "sfx_espada" },
            };

            // Una luz suave que acompaña al jugador para que nunca esté todo negro
            var luz = new GameObject("Farol").AddComponent<Light>();
            luz.transform.SetParent(camGO.transform, false);
            luz.type = LightType.Point; luz.range = 7f; luz.intensity = 1.2f; luz.color = new Color(1f, 0.85f, 0.65f);
            luz.shadows = LightShadows.None;

            foreach (var t in raiz.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 2;
            p["Jugador"] = Guardar(raiz, "Jugador");
        }

        Log("Prefabs creados en " + Prefabs + ": " + p.Count);
        return p;
    }

    // ---------------- Escenario ----------------

    static GameObject Poner(GameObject modelo, Vector3 pos, float rotY, Transform padre, Material mat = null, bool colision = true, float escala = 1f)
    {
        if (modelo == null) return null;
        var go = Instancia(modelo, padre);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
        go.transform.localScale = Vector3.one * escala;
        if (mat != null) PonerMaterial(go, mat);
        if (colision)
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
                if (mf.GetComponent<Collider>() == null) mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
        return go;
    }

    static GameObject PonerPrefab(GameObject prefab, Vector3 pos, float rotY, Transform padre)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.SetParent(padre, true);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
        return go;
    }

    // Sala rectangular de "ancho x largo" baldosas de 4 m, con muros, columnas en las esquinas y antorchas
    static void Sala(int ancho, int largo, Transform padre, int cadaAntorcha = 2)
    {
        var piso = M("Mazmorra", "floor_tile_large");
        var pisoRoto = M("Mazmorra", "floor_tile_large_rocks");
        var muro = M("Mazmorra", "wall");
        var muroRoto = M("Mazmorra", "wall_cracked");
        float W = ancho * 4f, L = largo * 4f;
        int n = 0;
        for (int x = 0; x < ancho; x++)
            for (int z = 0; z < largo; z++)
                Poner((x * 7 + z * 3) % 9 == 0 ? pisoRoto : piso, new Vector3(x * 4 + 2, 0, z * 4 + 2), 0, padre, s_MatMazmorra);
        for (int x = 0; x < ancho; x++)
        {
            Poner(n++ % 5 == 3 ? muroRoto : muro, new Vector3(x * 4 + 2, 0, 0), 0, padre, s_MatMazmorra);
            Poner(n++ % 5 == 3 ? muroRoto : muro, new Vector3(x * 4 + 2, 0, L), 180, padre, s_MatMazmorra);
        }
        for (int z = 0; z < largo; z++)
        {
            Poner(n++ % 5 == 3 ? muroRoto : muro, new Vector3(0, 0, z * 4 + 2), 90, padre, s_MatMazmorra);
            Poner(n++ % 5 == 3 ? muroRoto : muro, new Vector3(W, 0, z * 4 + 2), -90, padre, s_MatMazmorra);
        }
        foreach (var esquina in new[] { new Vector3(0, 0, 0), new Vector3(W, 0, 0), new Vector3(0, 0, L), new Vector3(W, 0, L) })
            Poner(M("Mazmorra", "pillar"), esquina, 0, padre, s_MatMazmorra);

        // Antorchas en los muros largos (con luz que parpadea)
        for (int z = 1; z < largo; z += cadaAntorcha)
        {
            Antorcha(new Vector3(0.5f, 2.2f, z * 4f), 90, padre);
            Antorcha(new Vector3(W - 0.5f, 2.2f, z * 4f), -90, padre);
        }
        Antorcha(new Vector3(W / 2f, 2.2f, L - 0.5f), 180, padre);
    }

    static void Antorcha(Vector3 pos, float rotY, Transform padre)
    {
        var t = Poner(M("Mazmorra", "torch_mounted"), pos, rotY, padre, s_MatMazmorra, false);
        if (t == null) return;
        var luz = new GameObject("Luz").AddComponent<Light>();
        luz.transform.SetParent(t.transform, false);
        luz.transform.localPosition = new Vector3(0f, 0.7f, 0.6f);
        luz.type = LightType.Point;
        luz.color = new Color(1f, 0.6f, 0.3f);
        luz.range = 10f;
        luz.intensity = 3f;
        luz.shadows = LightShadows.None;
        luz.gameObject.AddComponent<Parpadeo>();
    }

    static Scene NuevaEscena(string nombre, bool quitarCamara)
    {
        string ruta = "Assets/Scenes/" + nombre + ".unity";
        AssetDatabase.DeleteAsset(ruta);
        AssetDatabase.CopyAsset(Plantilla, ruta);
        var escena = EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);
        if (quitarCamara)
            foreach (var c in Object.FindObjectsByType<Camera>()) Object.DestroyImmediate(c.gameObject);
        foreach (var l in Object.FindObjectsByType<Light>())
            if (l.type == LightType.Directional) { l.intensity = 0.35f; l.color = new Color(0.7f, 0.65f, 0.9f); }
        // Ambiente oscuro de cripta con algo de niebla
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.28f, 0.26f, 0.34f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.018f;
        RenderSettings.fogColor = new Color(0.05f, 0.04f, 0.07f);
        RenderSettings.skybox = null;
        return escena;
    }

    static void Guardar(Scene escena, string descripcion)
    {
        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        Log(escena.name + ": " + descripcion);
    }

    static Nivel NuevoNivel(int numero, string nombre, string instrucciones, Nivel.Meta meta, int flechas, bool jefe = false)
    {
        var n = new GameObject("Nivel").AddComponent<Nivel>();
        n.m_Numero = numero; n.m_Nombre = nombre; n.m_Instrucciones = instrucciones;
        n.m_Meta = meta; n.m_FlechasIniciales = flechas; n.m_EsJefe = jefe;
        return n;
    }

    // Poner objetos sobre una mesa (a la altura de su superficie)
    static float AlturaDe(GameObject go) => go == null ? 0f : Limites(go).max.y;

    // ---------------- Escenas ----------------

    static void CrearMenu(Dictionary<string, GameObject> p)
    {
        var escena = NuevaEscena("00 Menu", false);
        var mapa = new GameObject("Mapa").transform;
        Sala(5, 5, mapa, 1);
        var cam = Object.FindAnyObjectByType<Camera>();
        cam.transform.position = new Vector3(10f, 1.8f, 10f);
        cam.transform.rotation = Quaternion.Euler(8f, 0f, 0f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.03f, 0.06f);

        // Esqueletos de adorno alrededor de la cámara (con su animación de reposo)
        string[] tipos = { "Esbirro", "Picaro", "Guerrero", "Mago" };
        for (int i = 0; i < 4; i++)
        {
            float a = i * Mathf.PI / 2f + 0.4f;
            var pos = new Vector3(10f + Mathf.Sin(a) * 5f, 0f, 10f + Mathf.Cos(a) * 5f);
            var e = PonerPrefab(p[tipos[i]], pos, Mathf.Atan2(10f - pos.x, 10f - pos.z) * Mathf.Rad2Deg, mapa);
            Object.DestroyImmediate(e.GetComponent<EnemigoEsqueleto>());
        }
        Poner(M("Mazmorra", "chest_gold"), new Vector3(10f, 0f, 16f), 180, mapa, s_MatMazmorra);
        Poner(M("Mazmorra", "banner_shield_red"), new Vector3(10f, 0f, 19.4f), 180, mapa, s_MatMazmorra, false);

        var menu = new GameObject("Menu").AddComponent<MenuPrincipal>();
        menu.m_CamaraGiratoria = cam.transform;
        Guardar(escena, "menú con la cripta de fondo");
    }

    static void CrearGaleria(Dictionary<string, GameObject> p)
    {
        var escena = NuevaEscena("01 Galeria de disparos", true);
        var mapa = new GameObject("Mapa").transform;
        Sala(6, 10, mapa);    // 24 x 40 metros
        NuevoNivel(1, "Galería de disparos", "Rompe las 10 dianas: cada una da distintos puntos.\n¡No le dispares a los barriles! Tienes 15 flechas.", Nivel.Meta.Dianas, 15);
        PonerPrefab(p["Jugador"], new Vector3(12f, 0.1f, 3f), 0, null);

        var dianas = new GameObject("Dianas").transform;
        // Las dianas miran hacia +Z, por eso se giran 180° para que miren al jugador
        PonerPrefab(p["Diana"], new Vector3(6f, 0f, 14f), 180, dianas);
        PonerPrefab(p["Diana"], new Vector3(18f, 0f, 14f), 180, dianas);
        PonerPrefab(p["DianaLejana"], new Vector3(12f, 0f, 22f), 180, dianas);
        PonerPrefab(p["DianaPequena"], new Vector3(4f, 0f, 26f), 180, dianas);
        PonerPrefab(p["DianaPequena"], new Vector3(20f, 0f, 26f), 180, dianas);
        var movil1 = PonerPrefab(p["DianaMovil"], new Vector3(12f, 1.8f, 29f), 180, dianas);
        var v1 = movil1.GetComponent<MovimientoVaiven>(); v1.m_Direccion = Vector3.right; v1.m_Distancia = 8f; v1.m_Velocidad = 3f;
        var movil2 = PonerPrefab(p["DianaMovil"], new Vector3(7f, 2.2f, 33f), 180, dianas);
        var v2 = movil2.GetComponent<MovimientoVaiven>(); v2.m_Direccion = Vector3.up; v2.m_Distancia = 1.2f; v2.m_Velocidad = 2f;
        PonerPrefab(p["DianaPared"], new Vector3(12f, 2.4f, 39.4f), 180, dianas);
        PonerPrefab(p["DianaParedPequena"], new Vector3(0.55f, 2f, 20f), 90, dianas);
        PonerPrefab(p["DianaParedPequena"], new Vector3(23.45f, 2.2f, 31f), -90, dianas);

        // Barriles que restan puntos y botellas que dan pocos
        PonerPrefab(p["Barril"], new Vector3(9f, 0f, 19f), 0, dianas);
        PonerPrefab(p["Barril"], new Vector3(15.5f, 0f, 30f), 0, dianas);
        PonerPrefab(p["Barril"], new Vector3(3f, 0f, 34f), 0, dianas);
        var mesa = Poner(M("Mazmorra", "table_long"), new Vector3(18f, 0f, 9f), 90, mapa, s_MatMazmorra);
        float h = AlturaDe(mesa);
        for (int i = 0; i < 3; i++) PonerPrefab(p["Botella"], new Vector3(18f, h, 7.8f + i * 1.2f), 0, dianas);

        // Mostrador de entrenamiento y adornos
        for (int i = 0; i < 3; i++) Poner(M("Mazmorra", "table_medium"), new Vector3(7f + i * 5f, 0f, 6f), 0, mapa, s_MatMazmorra);
        Poner(M("Mazmorra", "crates_stacked"), new Vector3(2.5f, 0f, 3f), 30, mapa, s_MatMazmorra);
        Poner(M("Mazmorra", "box_large"), new Vector3(21.5f, 0f, 3f), 0, mapa, s_MatMazmorra);
        Poner(M("Mazmorra", "banner_patternA_red"), new Vector3(6f, 0f, 39.4f), 180, mapa, s_MatMazmorra, false);
        Poner(M("Mazmorra", "banner_patternA_red"), new Vector3(18f, 0f, 39.4f), 180, mapa, s_MatMazmorra, false);
        Guardar(escena, "10 dianas, 3 barriles de penalización, 3 botellas, 15 flechas");
    }

    static void CrearMazmorra(Dictionary<string, GameObject> p)
    {
        var escena = NuevaEscena("02 Mazmorra", true);
        var mapa = new GameObject("Mapa").transform;
        Sala(10, 10, mapa);   // 40 x 40 metros
        NuevoNivel(2, "La Mazmorra", "Acaba con todos los esqueletos. Si te tocan, pierdes una vida.\nRecoge carcajes para tener más flechas.", Nivel.Meta.Enemigos, 20);
        PonerPrefab(p["Jugador"], new Vector3(20f, 0.1f, 3f), 0, null);

        foreach (var c in new[] { new Vector3(12, 0, 12), new Vector3(28, 0, 12), new Vector3(12, 0, 28), new Vector3(28, 0, 28) })
            Poner(M("Mazmorra", "pillar_decorated"), c, 0, mapa, s_MatMazmorra);
        Poner(M("Mazmorra", "crates_stacked"), new Vector3(6f, 0f, 16f), 20, mapa, s_MatMazmorra);
        Poner(M("Mazmorra", "crates_stacked"), new Vector3(33f, 0f, 18f), -30, mapa, s_MatMazmorra);
        Poner(M("Mazmorra", "barrel_large"), new Vector3(20f, 0f, 14f), 0, mapa, s_MatMazmorra);
        Poner(M("Mazmorra", "rubble_large"), new Vector3(24f, 0f, 22f), 45, mapa, s_MatMazmorra);
        Poner(M("Mazmorra", "keg"), new Vector3(15f, 0f, 36f), 0, mapa, s_MatMazmorra);
        Poner(M("Mazmorra", "chest"), new Vector3(36f, 0f, 37f), -45, mapa, s_MatMazmorra);

        var enemigos = new GameObject("Enemigos").transform;
        var lista = new (string, Vector3)[]
        {
            ("Esbirro", new Vector3(8, 0, 20)), ("Esbirro", new Vector3(32, 0, 22)), ("Esbirro", new Vector3(16, 0, 30)), ("Esbirro", new Vector3(25, 0, 34)),
            ("Picaro", new Vector3(6, 0, 34)), ("Picaro", new Vector3(34, 0, 34)),
            ("Guerrero", new Vector3(14, 0, 24)), ("Guerrero", new Vector3(27, 0, 26)),
            ("Mago", new Vector3(20, 0, 36)),
        };
        foreach (var (tipo, pos) in lista) PonerPrefab(p[tipo], pos, 180, enemigos);

        var objetos = new GameObject("Objetos").transform;
        foreach (var c in new[] { new Vector3(4, 0.8f, 10), new Vector3(36, 0.8f, 10), new Vector3(20, 0.8f, 20), new Vector3(6, 0.8f, 28), new Vector3(34, 0.8f, 28) })
            PonerPrefab(p["Carcaj"], c, 0, objetos);
        PonerPrefab(p["Pocion"], new Vector3(20f, 0.8f, 30f), 0, objetos);
        Guardar(escena, lista.Length + " esqueletos, 5 carcajes, 1 poción, 20 flechas");
    }

    static void CrearJefe(Dictionary<string, GameObject> p)
    {
        var escena = NuevaEscena("03 Jefe", true);
        var mapa = new GameObject("Mapa").transform;
        Sala(8, 12, mapa);    // 32 x 48 metros
        NuevoNivel(3, "El Trono del Rey Esqueleto", "Derrota al Rey Esqueleto (30 de vida): la ballesta quita 2 y el báculo 1.\nApunta a la cabeza para hacer el doble. Cuidado con sus esbirros.", Nivel.Meta.Jefe, 25, true);
        PonerPrefab(p["Jugador"], new Vector3(16f, 0.1f, 3f), 0, null);

        for (int i = 0; i < 3; i++)
        {
            Poner(M("Mazmorra", "pillar_decorated"), new Vector3(8f, 0f, 12f + i * 12f), 0, mapa, s_MatMazmorra);
            Poner(M("Mazmorra", "pillar_decorated"), new Vector3(24f, 0f, 12f + i * 12f), 0, mapa, s_MatMazmorra);
        }
        // Trono del rey al fondo
        Poner(M("Mazmorra", "chest_gold"), new Vector3(16f, 0f, 45f), 180, mapa, s_MatMazmorra);
        Poner(M("Mazmorra", "coin_stack_large"), new Vector3(13f, 0f, 45f), 0, mapa, s_MatMazmorra);
        Poner(M("Mazmorra", "coin_stack_large"), new Vector3(19f, 0f, 45f), 0, mapa, s_MatMazmorra);
        Poner(M("Mazmorra", "banner_shield_red"), new Vector3(10f, 0f, 47.4f), 180, mapa, s_MatMazmorra, false);
        Poner(M("Mazmorra", "banner_shield_red"), new Vector3(22f, 0f, 47.4f), 180, mapa, s_MatMazmorra, false);

        var enemigos = new GameObject("Enemigos").transform;
        PonerPrefab(p["Jefe"], new Vector3(16f, 0f, 38f), 180, enemigos);
        PonerPrefab(p["Esbirro"], new Vector3(10f, 0f, 30f), 180, enemigos).GetComponent<EnemigoEsqueleto>().m_CuentaParaNivel = false;
        PonerPrefab(p["Esbirro"], new Vector3(22f, 0f, 30f), 180, enemigos).GetComponent<EnemigoEsqueleto>().m_CuentaParaNivel = false;

        var objetos = new GameObject("Objetos").transform;
        foreach (var c in new[] { new Vector3(4, 0.8f, 8), new Vector3(28, 0.8f, 8), new Vector3(4, 0.8f, 24), new Vector3(28, 0.8f, 24), new Vector3(4, 0.8f, 40), new Vector3(28, 0.8f, 40) })
            PonerPrefab(p["Carcaj"], c, 0, objetos);
        PonerPrefab(p["Pocion"], new Vector3(16f, 0.8f, 14f), 0, objetos);
        PonerPrefab(p["Pocion"], new Vector3(16f, 0.8f, 26f), 0, objetos);
        Guardar(escena, "jefe con 30 de vida, 2 esbirros, 6 carcajes, 2 pociones, 25 flechas");
    }
}
