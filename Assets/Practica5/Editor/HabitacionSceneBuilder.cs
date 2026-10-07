using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace Practica5.Editor
{
    /// <summary>
    /// Construye la escena de la Práctica 5: habitación estática con iluminación bakeada,
    /// audio espacial 3D (radio interactuable + objeto invisible) y rig XR con simulador para PC.
    /// </summary>
    static class HabitacionSceneBuilder
    {
        const string k_Root = "Assets/Practica5";
        const string k_ScenePath = "Assets/Scenes/Practica05.unity"; // ruta indicada en el Paso 1 del tutorial
        const string k_MatDir = k_Root + "/Materials";
        const string k_SettingsDir = k_Root + "/Settings";
        const string k_AudioDir = k_Root + "/Audio";
        const string k_XriSamples = "Assets/Samples/XR Interaction Toolkit/3.6.1";
        const string k_RigPrefab = k_XriSamples + "/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
        const string k_SimulatorPrefab = k_XriSamples + "/XR Interaction Simulator/XR Interaction Simulator.prefab";

        const StaticEditorFlags k_StaticFlags = StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic |
            StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic;

        static readonly Dictionary<string, Material> s_Materials = new Dictionary<string, Material>();
        static LightmapParameters s_LightmapParams;

        [MenuItem("Practica5/Construir Escena", priority = 0)]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            s_Materials.Clear();
            CreateMaterials();
            s_LightmapParams = CreateLightmapParameters();

            var arquitectura = new GameObject("Arquitectura").transform;
            var muebles = new GameObject("Muebles").transform;
            var luces = new GameObject("Iluminacion").transform;
            var audio = new GameObject("Audio").transform;

            BuildRoom(arquitectura);
            BuildFurniture(muebles);
            BuildLights(luces);
            BuildProbes(luces);
            var tracked = BuildAudio(audio, muebles);
            BuildPostProcessing();
            BuildXR(tracked);
            ConfigureLighting();

            EditorSceneManager.SaveScene(scene, k_ScenePath);
            AddSceneToBuild();
            AssetDatabase.SaveAssets();
            Debug.Log("[Practica5] Escena construida: " + k_ScenePath + ". Siguiente paso: Practica5/Bakear Iluminacion.");
        }

        [MenuItem("Practica5/Bakear Iluminacion", priority = 10)]
        public static void Bake()
        {
            if (!Lightmapping.BakeAsync())
                Debug.LogWarning("[Practica5] No se pudo iniciar el bakeo (¿ya hay uno en curso?).");
        }

        // ---------------------------------------------------------------- Materiales

        static void CreateMaterials()
        {
            Mat("Piso_Madera", new Color(0.55f, 0.36f, 0.22f), 0.35f);
            Mat("Pared", new Color(0.86f, 0.83f, 0.76f), 0.1f);
            Mat("Pared_Acento", new Color(0.33f, 0.47f, 0.55f), 0.1f);
            Mat("Techo", new Color(0.95f, 0.95f, 0.93f), 0.05f);
            Mat("Madera_Oscura", new Color(0.32f, 0.2f, 0.12f), 0.4f);
            Mat("Madera_Clara", new Color(0.72f, 0.55f, 0.36f), 0.35f);
            Mat("Tela_Cama", new Color(0.2f, 0.32f, 0.5f), 0.05f);
            Mat("Tela_Blanca", new Color(0.92f, 0.92f, 0.9f), 0.05f);
            Mat("Alfombra", new Color(0.62f, 0.2f, 0.18f), 0.02f);
            Mat("Metal", new Color(0.6f, 0.6f, 0.62f), 0.7f, 0.9f);
            Mat("Cubo_Rojo", new Color(0.85f, 0.38f, 0.25f), 0.3f);
            Mat("Plastico_Radio", new Color(0.12f, 0.12f, 0.13f), 0.55f);
            Mat("Rejilla_Radio", new Color(0.4f, 0.4f, 0.42f), 0.3f, 0.6f);
            Mat("Maceta", new Color(0.6f, 0.32f, 0.2f), 0.2f);
            Mat("Planta", new Color(0.2f, 0.5f, 0.22f), 0.15f);
            Mat("Cuadro", new Color(0.9f, 0.62f, 0.25f), 0.2f);
            Mat("Libro_Rojo", new Color(0.65f, 0.12f, 0.1f), 0.2f);
            Mat("Libro_Verde", new Color(0.14f, 0.42f, 0.24f), 0.2f);
            Mat("Libro_Azul", new Color(0.12f, 0.24f, 0.55f), 0.2f);
            Mat("Emisivo_Lampara", new Color(1f, 0.85f, 0.6f), 0.1f, 0f, new Color(1f, 0.8f, 0.55f) * 2.5f);
            Mat("Emisivo_Techo", new Color(1f, 0.97f, 0.9f), 0.1f, 0f, new Color(1f, 0.95f, 0.85f) * 2f);
            Mat("Emisivo_Indicador", new Color(0.2f, 1f, 0.3f), 0.3f, 0f, new Color(0.2f, 1f, 0.3f) * 2f);
            Mat("Emisivo_Objetivo", new Color(1f, 0.85f, 0.1f), 0.3f, 0f, new Color(1f, 0.8f, 0.1f) * 4f);
        }

        static void Mat(string name, Color baseColor, float smoothness, float metallic = 0f, Color? emission = null)
        {
            string path = $"{k_MatDir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", baseColor);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", metallic);
            if (emission.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission.Value);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.black);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }
            EditorUtility.SetDirty(mat);
            s_Materials[name] = mat;
        }

        // ---------------------------------------------------------------- Geometría

        /// <summary>Crea una primitiva estática con material y Lightmap Parameters para móvil.</summary>
        static GameObject Static(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale,
            string material, float lightmapScale = 1f, Vector3? euler = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            if (euler.HasValue)
                go.transform.localEulerAngles = euler.Value;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = s_Materials[material];
            mr.receiveGI = ReceiveGI.Lightmaps;
            mr.shadowCastingMode = ShadowCastingMode.On;
            GameObjectUtility.SetStaticEditorFlags(go, k_StaticFlags);

            var so = new SerializedObject(mr);
            so.FindProperty("m_ScaleInLightmap").floatValue = lightmapScale;
            so.FindProperty("m_LightmapParameters").objectReferenceValue = s_LightmapParams;
            so.ApplyModifiedPropertiesWithoutUndo();
            return go;
        }

        static void BuildRoom(Transform root)
        {
            // Habitación de 6 x 5 m y 2.8 m de alto. Muros de 20 cm para evitar fugas de luz.
            // Paso 4: piso con Plane (10 x 10 u escalado a 6.2 x 5.2 m) y paredes con Cubes.
            var floor = Static(PrimitiveType.Plane, "Floor", root, Vector3.zero, new Vector3(0.62f, 1f, 0.52f), "Piso_Madera");
            floor.AddComponent<TeleportationArea>();
            Static(PrimitiveType.Cube, "Techo", root, new Vector3(0f, 2.85f, 0f), new Vector3(6.4f, 0.1f, 5.4f), "Techo", 0.6f);
            Static(PrimitiveType.Cube, "Wall_West", root, new Vector3(-3.1f, 1.4f, 0f), new Vector3(0.2f, 2.8f, 5.4f), "Pared_Acento", 0.8f);
            Static(PrimitiveType.Cube, "Wall_East", root, new Vector3(3.1f, 1.4f, 0f), new Vector3(0.2f, 2.8f, 5.4f), "Pared", 0.8f);
            Static(PrimitiveType.Cube, "Wall_South", root, new Vector3(0f, 1.4f, -2.6f), new Vector3(6.4f, 2.8f, 0.2f), "Pared", 0.8f);

            // Pared norte con hueco de ventana (x: -1..1, y: 0.9..2.1).
            var norte = new GameObject("Wall_North").transform;
            norte.SetParent(root, false);
            Static(PrimitiveType.Cube, "Izquierda", norte, new Vector3(-2.1f, 1.4f, 2.6f), new Vector3(2.2f, 2.8f, 0.2f), "Pared", 0.8f);
            Static(PrimitiveType.Cube, "Derecha", norte, new Vector3(2.1f, 1.4f, 2.6f), new Vector3(2.2f, 2.8f, 0.2f), "Pared", 0.8f);
            Static(PrimitiveType.Cube, "Bajo_Ventana", norte, new Vector3(0f, 0.45f, 2.6f), new Vector3(2f, 0.9f, 0.2f), "Pared", 0.8f);
            Static(PrimitiveType.Cube, "Sobre_Ventana", norte, new Vector3(0f, 2.45f, 2.6f), new Vector3(2f, 0.7f, 0.2f), "Pared", 0.8f);
            Static(PrimitiveType.Cube, "Alfeizar", norte, new Vector3(0f, 0.92f, 2.48f), new Vector3(2.1f, 0.04f, 0.12f), "Madera_Clara", 0.5f);
            Static(PrimitiveType.Cube, "Marco_Centro", norte, new Vector3(0f, 1.5f, 2.6f), new Vector3(0.05f, 1.2f, 0.06f), "Madera_Clara", 0.3f);

            // Zócalos.
            Static(PrimitiveType.Cube, "Zocalo_Sur", root, new Vector3(0f, 0.05f, -2.48f), new Vector3(6f, 0.1f, 0.03f), "Madera_Oscura", 0.3f);
            Static(PrimitiveType.Cube, "Zocalo_Este", root, new Vector3(2.98f, 0.05f, 0f), new Vector3(0.03f, 0.1f, 5f), "Madera_Oscura", 0.3f);
        }

        static void BuildFurniture(Transform root)
        {
            // Cama contra la pared oeste.
            var cama = Group("Cama", root);
            Static(PrimitiveType.Cube, "Estructura", cama, new Vector3(-2.3f, 0.2f, -1.5f), new Vector3(1.4f, 0.4f, 2f), "Madera_Oscura", 0.6f);
            Static(PrimitiveType.Cube, "Colchon", cama, new Vector3(-2.3f, 0.5f, -1.5f), new Vector3(1.3f, 0.2f, 1.9f), "Tela_Cama", 0.6f);
            Static(PrimitiveType.Cube, "Almohada", cama, new Vector3(-2.3f, 0.66f, -2.2f), new Vector3(1f, 0.12f, 0.4f), "Tela_Blanca", 0.4f);
            Static(PrimitiveType.Cube, "Cabecera", cama, new Vector3(-2.3f, 0.6f, -2.46f), new Vector3(1.4f, 1.2f, 0.08f), "Madera_Oscura", 0.5f);

            // Velador con lámpara.
            var velador = Group("Velador", root);
            Static(PrimitiveType.Cube, "Mueble", velador, new Vector3(-1.3f, 0.3f, -2.2f), new Vector3(0.5f, 0.6f, 0.5f), "Madera_Clara", 0.5f);
            Static(PrimitiveType.Cylinder, "Lampara_Base", velador, new Vector3(-1.3f, 0.65f, -2.2f), new Vector3(0.12f, 0.05f, 0.12f), "Metal", 0.3f);
            Static(PrimitiveType.Cylinder, "Lampara_Varilla", velador, new Vector3(-1.3f, 0.75f, -2.2f), new Vector3(0.02f, 0.1f, 0.02f), "Metal", 0.2f);
            Static(PrimitiveType.Cylinder, "Lampara_Pantalla", velador, new Vector3(-1.3f, 0.9f, -2.2f), new Vector3(0.26f, 0.08f, 0.26f), "Emisivo_Lampara", 0.3f);

            // Escritorio y silla contra la pared este.
            var escritorio = Group("Escritorio", root);
            Static(PrimitiveType.Cube, "Tablero", escritorio, new Vector3(2.6f, 0.75f, 0.5f), new Vector3(0.8f, 0.05f, 1.6f), "Madera_Clara", 0.6f);
            foreach (var (x, z) in new[] { (2.25f, -0.25f), (2.95f, -0.25f), (2.25f, 1.25f), (2.95f, 1.25f) })
                Static(PrimitiveType.Cube, "Pata", escritorio, new Vector3(x, 0.36f, z), new Vector3(0.05f, 0.72f, 0.05f), "Metal", 0.2f);

            var silla = Group("Silla", root);
            Static(PrimitiveType.Cube, "Asiento", silla, new Vector3(1.95f, 0.45f, 0.5f), new Vector3(0.45f, 0.05f, 0.45f), "Madera_Oscura", 0.4f);
            Static(PrimitiveType.Cube, "Respaldo", silla, new Vector3(1.75f, 0.77f, 0.5f), new Vector3(0.05f, 0.6f, 0.45f), "Madera_Oscura", 0.4f);
            foreach (var (x, z) in new[] { (1.76f, 0.31f), (2.14f, 0.31f), (1.76f, 0.69f), (2.14f, 0.69f) })
                Static(PrimitiveType.Cube, "Pata", silla, new Vector3(x, 0.21f, z), new Vector3(0.04f, 0.42f, 0.04f), "Metal", 0.2f);

            // Estante con libros en la pared sur.
            var estante = Group("Estante", root);
            foreach (float y in new[] { 0.4f, 0.9f, 1.4f })
                Static(PrimitiveType.Cube, "Repisa", estante, new Vector3(1.2f, y, -2.33f), new Vector3(1.6f, 0.04f, 0.3f), "Madera_Oscura", 0.4f);
            Static(PrimitiveType.Cube, "Lateral_Izq", estante, new Vector3(0.42f, 0.8f, -2.33f), new Vector3(0.04f, 1.2f, 0.3f), "Madera_Oscura", 0.4f);
            Static(PrimitiveType.Cube, "Lateral_Der", estante, new Vector3(1.98f, 0.8f, -2.33f), new Vector3(0.04f, 1.2f, 0.3f), "Madera_Oscura", 0.4f);
            string[] libros = { "Libro_Rojo", "Libro_Verde", "Libro_Azul" };
            for (int i = 0; i < 9; i++)
            {
                float y = i < 5 ? 0.54f : 1.04f;
                float x = i < 5 ? 0.55f + i * 0.09f : 0.75f + (i - 5) * 0.1f;
                float h = 0.2f + (i % 3) * 0.03f;
                Static(PrimitiveType.Cube, "Libro", estante, new Vector3(x, y - 0.12f + h * 0.5f, -2.33f),
                    new Vector3(0.06f, h, 0.2f), libros[i % 3], 0.2f);
            }

            // Ropero en la esquina sureste.
            var ropero = Group("Ropero", root);
            Static(PrimitiveType.Cube, "Cuerpo", ropero, new Vector3(2.68f, 1f, -1.8f), new Vector3(0.6f, 2f, 1.2f), "Madera_Oscura", 0.6f);
            Static(PrimitiveType.Cube, "Division_Puertas", ropero, new Vector3(2.37f, 1f, -1.8f), new Vector3(0.02f, 1.9f, 0.02f), "Madera_Clara", 0.2f);
            Static(PrimitiveType.Cube, "Tirador_A", ropero, new Vector3(2.36f, 1.05f, -1.86f), new Vector3(0.02f, 0.18f, 0.02f), "Metal", 0.2f);
            Static(PrimitiveType.Cube, "Tirador_B", ropero, new Vector3(2.36f, 1.05f, -1.74f), new Vector3(0.02f, 0.18f, 0.02f), "Metal", 0.2f);

            // Alfombra, cuadro y planta.
            Static(PrimitiveType.Cube, "Alfombra", root, new Vector3(0f, 0.005f, 0f), new Vector3(2.2f, 0.01f, 1.6f), "Alfombra", 0.6f);
            var cuadro = Group("Cuadro", root);
            Static(PrimitiveType.Cube, "Marco", cuadro, new Vector3(-2.985f, 1.6f, 0.8f), new Vector3(0.03f, 0.7f, 1f), "Madera_Oscura", 0.3f);
            Static(PrimitiveType.Cube, "Lienzo", cuadro, new Vector3(-2.965f, 1.6f, 0.8f), new Vector3(0.01f, 0.6f, 0.9f), "Cuadro", 0.3f);
            var planta = Group("Planta", root);
            Static(PrimitiveType.Cylinder, "Maceta", planta, new Vector3(-2.5f, 0.2f, 2.0f), new Vector3(0.35f, 0.2f, 0.35f), "Maceta", 0.3f);
            Static(PrimitiveType.Sphere, "Hojas", planta, new Vector3(-2.5f, 0.65f, 2.0f), new Vector3(0.6f, 0.6f, 0.6f), "Planta", 0.3f);

            // Lámpara de techo (emisiva) y ventilador de caja.
            Static(PrimitiveType.Cylinder, "Lampara_Techo", root, new Vector3(0f, 2.77f, 0f), new Vector3(0.45f, 0.03f, 0.45f), "Emisivo_Techo", 0.3f);
            var ventilador = Group("Ventilador", root);
            Static(PrimitiveType.Cube, "Carcasa", ventilador, new Vector3(-2.6f, 0.25f, 1.1f), new Vector3(0.4f, 0.5f, 0.18f), "Metal", 0.3f, new Vector3(0f, 60f, 0f));
        }

        static Transform Group(string name, Transform parent)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, k_StaticFlags);
            return t;
        }

        // ---------------------------------------------------------------- Iluminación

        static void BuildLights(Transform root)
        {
            var sol = BakedLight(root, "Sol_Ventana", LightType.Directional, new Vector3(0f, 6f, 6f), new Vector3(38f, 200f, 0f),
                new Color(1f, 0.93f, 0.8f), 1f); // Paso 6: Baked con intensidad moderada (1.0)
            sol.shadows = LightShadows.Soft;
            sol.shadowAngle = 1.5f;
            RenderSettings.sun = sol;

            var ventana = BakedLight(root, "Luz_Area_Ventana", LightType.Rectangle, new Vector3(0f, 1.5f, 2.46f), new Vector3(0f, 180f, 0f),
                new Color(0.75f, 0.85f, 1f), 3f);
            ventana.areaSize = new Vector2(2f, 1.2f);
            ventana.range = 8f;

            var techo = BakedLight(root, "Luz_Techo", LightType.Point, new Vector3(0f, 2.55f, 0f), Vector3.zero,
                new Color(1f, 0.95f, 0.85f), 1.4f);
            techo.range = 6f;
            techo.shadows = LightShadows.Soft;
            techo.shadowRadius = 0.15f;

            var velador = BakedLight(root, "Luz_Velador", LightType.Point, new Vector3(-1.3f, 0.9f, -2.2f), Vector3.zero,
                new Color(1f, 0.75f, 0.45f), 0.9f);
            velador.range = 3f;
            velador.shadows = LightShadows.Soft;
            velador.shadowRadius = 0.12f;

            var escritorio = BakedLight(root, "Spot_Escritorio", LightType.Spot, new Vector3(2.5f, 2.7f, 0.5f), new Vector3(90f, 0f, 0f),
                new Color(1f, 0.97f, 0.9f), 2.5f);
            escritorio.range = 4f;
            escritorio.spotAngle = 70f;
            escritorio.innerSpotAngle = 40f;
            escritorio.shadows = LightShadows.Soft;
            escritorio.shadowRadius = 0.1f;
        }

        static Light BakedLight(Transform parent, string name, LightType type, Vector3 pos, Vector3 euler, Color color, float intensity)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = euler;
            var light = go.AddComponent<Light>();
            light.type = type;
            light.color = color;
            light.intensity = intensity;
            light.lightmapBakeType = LightmapBakeType.Baked; // Realtime -> Baked (paso 2 de la guía)
            light.shadows = LightShadows.Soft;
            return light;
        }

        static void BuildProbes(Transform root)
        {
            // Light Probes para que los objetos dinámicos (radio, objetivo, manos) reciban luz bakeada.
            var probes = new GameObject("Light_Probes");
            probes.transform.SetParent(root, false);
            var group = probes.AddComponent<LightProbeGroup>();
            var positions = new List<Vector3>();
            for (int x = 0; x < 5; x++)
            for (int z = 0; z < 5; z++)
            foreach (float y in new[] { 0.3f, 1.3f, 2.4f })
                positions.Add(new Vector3(-2.6f + x * 1.3f, y, -2.2f + z * 1.1f));
            group.probePositions = positions.ToArray();

            var reflection = new GameObject("Reflection_Probe");
            reflection.transform.SetParent(root, false);
            reflection.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            var rp = reflection.AddComponent<ReflectionProbe>();
            rp.mode = ReflectionProbeMode.Baked;
            rp.size = new Vector3(6f, 2.8f, 5f);
            rp.boxProjection = true;
            rp.resolution = 128;
            rp.hdr = true;
        }

        static LightmapParameters CreateLightmapParameters()
        {
            string path = k_SettingsDir + "/LP_Movil_Quest.giparams";
            var lp = AssetDatabase.LoadAssetAtPath<LightmapParameters>(path);
            if (lp == null)
            {
                lp = new LightmapParameters();
                AssetDatabase.CreateAsset(lp, path);
            }
            // Ajustes pensados para visores móviles (Quest): poco antialiasing y lightmaps acotados.
            lp.antiAliasingSamples = 4;
            lp.blurRadius = 2;
            lp.directLightQuality = 64;
            lp.backFaceTolerance = 0.9f;
            lp.pushoff = 0.0001f;
            lp.limitLightmapCount = true;
            lp.maxLightmapCount = 2;
            lp.bakedLightmapTag = 0;
            EditorUtility.SetDirty(lp);
            return lp;
        }

        static void ConfigureLighting()
        {
            string path = k_SettingsDir + "/LS_HabitacionXR.lighting";
            var ls = AssetDatabase.LoadAssetAtPath<LightingSettings>(path);
            if (ls == null)
            {
                ls = new LightingSettings { name = "LS_HabitacionXR" };
                AssetDatabase.CreateAsset(ls, path);
            }
            ls.bakedGI = true;
            ls.realtimeGI = false;
            ls.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
            ls.mixedBakeMode = MixedLightingMode.Subtractive;
            ls.lightmapResolution = 20f;
            ls.lightmapPadding = 2;
            ls.lightmapMaxSize = 1024;
            ls.lightmapCompression = LightmapCompression.NormalQuality;
            ls.directionalityMode = LightmapsMode.NonDirectional;
            ls.ao = true;
            ls.aoMaxDistance = 0.8f;
            ls.directSampleCount = 32;
            ls.indirectSampleCount = 256;
            ls.environmentSampleCount = 128;
            ls.maxBounces = 2;
            ls.filteringMode = LightingSettings.FilterMode.Auto;
            EditorUtility.SetDirty(ls);
            Lightmapping.lightingSettings = ls;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.62f, 0.75f);
            RenderSettings.ambientEquatorColor = new Color(0.42f, 0.4f, 0.37f);
            RenderSettings.ambientGroundColor = new Color(0.2f, 0.17f, 0.14f);
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.fog = false;
        }

        // ---------------------------------------------------------------- Audio

        static AudioSource[] BuildAudio(Transform root, Transform muebles)
        {
            var radioClip = Clip("Radio_Melodia");
            var beaconClip = Clip("Objeto_Invisible_Beacon");
            var chimeClip = Clip("Encontrado_Chime");
            var ambientClip = Clip("Ambiente_Ventilador");

            // Radio interactuable sobre el escritorio (objeto dinámico: no es Static).
            var radio = GameObject.CreatePrimitive(PrimitiveType.Cube);
            radio.name = "Audio_Object"; // nombre del Paso 4/9 del tutorial (radio con sonido)
            radio.transform.SetParent(root, false);
            radio.transform.localPosition = new Vector3(2.6f, 0.88f, 0.1f);
            radio.transform.localEulerAngles = new Vector3(0f, 90f, 0f); // frente (-Z local) mirando hacia la habitación
            radio.transform.localScale = new Vector3(0.32f, 0.2f, 0.12f);
            radio.GetComponent<MeshRenderer>().sharedMaterial = s_Materials["Plastico_Radio"];
            Child(PrimitiveType.Cylinder, "Parlante", radio.transform, new Vector3(-0.2f, 0f, -0.52f), new Vector3(0.45f, 0.05f, 0.75f), "Rejilla_Radio", new Vector3(90f, 0f, 0f));
            var indicator = Child(PrimitiveType.Sphere, "Indicador", radio.transform, new Vector3(0.32f, 0.25f, -0.52f), new Vector3(0.08f, 0.13f, 0.2f), "Emisivo_Indicador", Vector3.zero);
            Child(PrimitiveType.Cylinder, "Antena", radio.transform, new Vector3(0.38f, 1.6f, 0f), new Vector3(0.03f, 1.3f, 0.08f), "Metal", Vector3.zero);

            var rb = radio.AddComponent<Rigidbody>();
            rb.mass = 1.2f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var radioSource = radio.AddComponent<AudioSource>();
            Configure3D(radioSource, radioClip, 0.8f, 1f, 15f); // Paso 10: Min 1 m / Max 15 m
            var grab = radio.AddComponent<XRGrabInteractable>();
            grab.useDynamicAttach = true;
            grab.throwOnDetach = false;
            var radioComp = radio.AddComponent<RadioInteractable>();
            SetField(radioComp, "m_IndicatorLight", indicator.GetComponent<Renderer>());
            radio.AddComponent<AudioObjectVisibility>(); // Paso 12: segunda prueba ocultando Audio_Object (tecla V)

            // Objeto móvil (no Static, Paso 5): cubo agarrable con el mando.
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Cube_Interactuable";
            cube.transform.SetParent(root, false);
            cube.transform.localPosition = new Vector3(0.7f, 0.125f, -0.3f);
            cube.transform.localScale = Vector3.one * 0.25f;
            cube.GetComponent<MeshRenderer>().sharedMaterial = s_Materials["Cubo_Rojo"];
            var cubeRb = cube.AddComponent<Rigidbody>();
            cubeRb.mass = 0.8f;
            cubeRb.interpolation = RigidbodyInterpolation.Interpolate;
            cube.AddComponent<XRGrabInteractable>().useDynamicAttach = true;

            // Objeto invisible: sin renderer, solo se localiza por el sonido.
            var target = new GameObject("Objeto_Invisible");
            target.transform.SetParent(root, false);
            var beacon = target.AddComponent<AudioSource>();
            Configure3D(beacon, beaconClip, 0.9f, 0.3f, 10f);
            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Revelado";
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.transform.SetParent(target.transform, false);
            visual.transform.localScale = Vector3.one * 0.15f;
            visual.GetComponent<MeshRenderer>().sharedMaterial = s_Materials["Emisivo_Objetivo"];
            visual.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            visual.SetActive(false);

            var spawnRoot = new GameObject("Puntos_Objeto_Invisible").transform;
            spawnRoot.SetParent(root, false);
            var spawns = new List<Transform>();
            var spawnData = new (string, Vector3)[]
            {
                ("Bajo_Cama", new Vector3(-2.3f, 0.12f, -0.9f)),
                ("Sobre_Ropero", new Vector3(2.68f, 2.2f, -1.8f)),
                ("Detras_Planta", new Vector3(-2.75f, 0.6f, 2.3f)),
                ("Estante_Bajo", new Vector3(1.6f, 0.15f, -2.3f)),
                ("Esquina_Ventana", new Vector3(2.75f, 0.3f, 2.3f)),
            };
            foreach (var (n, p) in spawnData)
            {
                var s = new GameObject(n).transform;
                s.SetParent(spawnRoot, false);
                s.localPosition = p;
                spawns.Add(s);
            }
            target.transform.localPosition = spawns[0].localPosition;
            var hidden = target.AddComponent<HiddenSoundTarget>();
            SetField(hidden, "m_RevealVisual", visual);
            SetField(hidden, "m_FoundClip", chimeClip);
            var so = new SerializedObject(hidden);
            var arr = so.FindProperty("m_SpawnPoints");
            arr.arraySize = spawns.Count;
            for (int i = 0; i < spawns.Count; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = spawns[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            // Ambiente: ventilador de caja (fuente 3D suave).
            var fan = muebles.Find("Ventilador/Carcasa");
            var ambientGo = new GameObject("Ventilador_Audio");
            ambientGo.transform.SetParent(root, false);
            ambientGo.transform.position = fan != null ? fan.position : new Vector3(-2.6f, 0.25f, 1.1f);
            var ambient = ambientGo.AddComponent<AudioSource>();
            Configure3D(ambient, ambientClip, 0.35f, 0.5f, 6f);

            // Sala acústica de Meta XR Audio con las medidas reales (reflexiones/reverb coherentes con el HRTF).
            var roomGo = new GameObject("Meta_XR_Audio_Room");
            roomGo.transform.SetParent(root, false);
            var room = roomGo.AddComponent<MetaXRAudioRoomAcousticProperties>();
            room.lockPositionToListener = false;
            roomGo.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            room.width = 6f;
            room.height = 2.8f;
            room.depth = 5f;
            room.floorMaterial = MetaXRAudioRoomAcousticProperties.MaterialPreset.WoodFloor;
            room.ceilingMaterial = MetaXRAudioRoomAcousticProperties.MaterialPreset.PlasterOnConcreteBlock;
            room.leftMaterial = MetaXRAudioRoomAcousticProperties.MaterialPreset.PlasterOnBrick;
            room.rightMaterial = MetaXRAudioRoomAcousticProperties.MaterialPreset.PlasterOnBrick;
            room.frontMaterial = MetaXRAudioRoomAcousticProperties.MaterialPreset.Glass;
            room.backMaterial = MetaXRAudioRoomAcousticProperties.MaterialPreset.PlasterOnBrick;
            room.clutterFactor = 0.6f;

            return new[] { radioSource, beacon, ambient };
        }

        /// <summary>Audio 3D completo (Spatial Blend = 1), atenuación logarítmica y HRTF (spatialize).</summary>
        static void Configure3D(AudioSource src, AudioClip clip, float volume, float minDistance, float maxDistance)
        {
            src.clip = clip;
            src.loop = true;
            src.playOnAwake = true;
            src.volume = volume;
            src.spatialBlend = 1f;
            src.spatialize = true;
            src.spatializePostEffects = true;
            src.rolloffMode = AudioRolloffMode.Logarithmic;
            src.minDistance = minDistance;
            src.maxDistance = maxDistance;
            src.dopplerLevel = 0f;
            src.spread = 0f;
            src.reverbZoneMix = 1f;
        }

        static AudioClip Clip(string name)
        {
            string path = $"{k_AudioDir}/{name}.wav";
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null)
                Debug.LogWarning("[Practica5] Falta el clip de audio: " + path);
            return clip;
        }

        static GameObject Child(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 localScale, string material, Vector3 euler)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localEulerAngles = euler;
            go.transform.localScale = localScale;
            go.GetComponent<MeshRenderer>().sharedMaterial = s_Materials[material];
            return go;
        }

        static void SetField(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- Post-procesado

        static void BuildPostProcessing()
        {
            // Solo efectos baratos y sin movimiento: Bloom suave, Tonemapping y Color Grading.
            // Se evitan Motion Blur, Chromatic Aberration, Lens Distortion y Vignette fuerte (mareo en XR).
            string path = k_SettingsDir + "/PP_HabitacionXR.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }
            foreach (var c in profile.components.ToArray())
            {
                profile.Remove(c.GetType());
                Object.DestroyImmediate(c, true);
            }

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(1.1f);
            bloom.intensity.Override(0.25f);
            bloom.scatter.Override(0.6f);
            bloom.highQualityFiltering.Override(false);
            var tonemap = profile.Add<Tonemapping>(true);
            tonemap.mode.Override(TonemappingMode.Neutral);
            var color = profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(0.15f);
            color.contrast.Override(6f);
            color.saturation.Override(8f);
            var wb = profile.Add<WhiteBalance>(true);
            wb.temperature.Override(4f);
            foreach (var c in profile.components)
                if (!AssetDatabase.IsSubAsset(c))
                    AssetDatabase.AddObjectToAsset(c, profile);
            EditorUtility.SetDirty(profile);

            var go = new GameObject("Global_Volume_PostProcess");
            var vol = go.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 1f;
            vol.sharedProfile = profile;
        }

        // ---------------------------------------------------------------- XR

        static void BuildXR(AudioSource[] tracked)
        {
            new GameObject("XR Interaction Manager").AddComponent<XRInteractionManager>();

            var rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(k_RigPrefab);
            Camera cam;
            if (rigPrefab != null)
            {
                var rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab);
                rig.transform.position = new Vector3(0f, 0f, 1.2f);
                rig.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                cam = rig.GetComponentInChildren<Camera>(true);
                FixRigMaterialsForURP(rig);
                rig.AddComponent<SimulatorHapticsGuard>(); // sin vibración en el simulador de PC (evita el aviso de hápticos)
            }
            else
            {
                Debug.LogWarning("[Practica5] No se encontró el XR Origin de Starter Assets; se crea una cámara simple.");
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                camGo.transform.SetPositionAndRotation(new Vector3(0f, 1.6f, 1.2f), Quaternion.Euler(0f, 180f, 0f));
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }

            if (cam != null)
            {
                cam.nearClipPlane = 0.02f;
                cam.farClipPlane = 50f;
                var data = cam.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = true;
                data.antialiasing = AntialiasingMode.None; // en XR se usa MSAA del pipeline
                var hud = cam.gameObject.GetComponent<PerformanceHUD>();
                if (hud == null)
                    hud = cam.gameObject.AddComponent<PerformanceHUD>();
                var so = new SerializedObject(hud);
                var arr = so.FindProperty("m_TrackedSources");
                arr.arraySize = tracked.Length;
                for (int i = 0; i < tracked.Length; i++)
                    arr.GetArrayElementAtIndex(i).objectReferenceValue = tracked[i];
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // Simulador XR: permite probar en PC sin visor (WASD + ratón).
            var simPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(k_SimulatorPrefab);
            if (simPrefab != null)
                PrefabUtility.InstantiatePrefab(simPrefab);
            else
                Debug.LogWarning("[Practica5] No se encontró el XR Interaction Simulator.");
        }

        /// <summary>
        /// Los materiales del sample Starter Assets usan shaders Standard / Legacy Particles que URP no soporta
        /// (se ven magenta). Se convierten a URP Lit y las líneas de los rayos a URP Particles Unlit.
        /// </summary>
        static void FixRigMaterialsForURP(GameObject rig)
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { k_XriSamples }))
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (mat == null || mat.shader == null || mat.shader.name != "Standard")
                    continue;
                var color = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
                float smooth = mat.HasProperty("_Glossiness") ? mat.GetFloat("_Glossiness") : 0.5f;
                mat.shader = lit;
                mat.SetColor("_BaseColor", color);
                mat.SetFloat("_Smoothness", smooth);
                EditorUtility.SetDirty(mat);
            }

            string linePath = k_MatDir + "/Linea_Rayo_XR.mat";
            var lineMat = AssetDatabase.LoadAssetAtPath<Material>(linePath);
            if (lineMat == null)
            {
                lineMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(lineMat, linePath);
            }
            // Transparente con mezcla alfa para que el gradiente del rayo se vea correctamente.
            lineMat.SetFloat("_Surface", 1f);
            lineMat.SetFloat("_Blend", 0f);
            lineMat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            lineMat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            lineMat.SetFloat("_ZWrite", 0f);
            lineMat.SetColor("_BaseColor", Color.white);
            lineMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            lineMat.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(lineMat);

            foreach (var line in rig.GetComponentsInChildren<LineRenderer>(true))
            {
                var mats = line.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                    if (mats[i] == null || mats[i].shader == null || !mats[i].shader.name.StartsWith("Universal"))
                        mats[i] = lineMat;
                line.sharedMaterials = mats;
            }
        }

        static void AddSceneToBuild()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != k_ScenePath).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(k_ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
