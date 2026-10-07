using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Practica5
{
    /// <summary>
    /// HUD en espacio de mundo (visible en el visor y en el Game View) con FPS y el estado
    /// de cada fuente de audio 3D: distancia, ganancia estimada por la atenuación logarítmica
    /// y dirección relativa a la cabeza. Registra las muestras de FPS en un CSV para el reporte.
    /// Tecla H: mostrar/ocultar HUD.
    /// </summary>
    public class PerformanceHUD : MonoBehaviour
    {
        [SerializeField] AudioSource[] m_TrackedSources = new AudioSource[0];
        [SerializeField] Vector3 m_LocalOffset = new Vector3(0f, 0.2f, 0.9f);
        [SerializeField] float m_SampleInterval = 1f;

        readonly List<string> m_CsvRows = new List<string>();
        readonly StringBuilder m_Text = new StringBuilder(512);

        TextMesh m_TextMesh;
        Transform m_Head;
        float m_Accum;
        int m_Frames;
        float m_IntervalStart;
        float m_CurrentFps;
        float m_MinFps = float.MaxValue;
        float m_MaxFrameMs;
        double m_FpsSum;
        int m_FpsSamples;
        string m_Message = string.Empty;
        float m_MessageUntil;

        void OnEnable() => HiddenSoundTarget.Found += OnTargetFound;
        void OnDisable() => HiddenSoundTarget.Found -= OnTargetFound;

        void Start()
        {
            m_Head = transform;
            var go = new GameObject("HUD_Text");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = m_LocalOffset;
            m_TextMesh = go.AddComponent<TextMesh>();
            m_TextMesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            m_TextMesh.GetComponent<MeshRenderer>().sharedMaterial = m_TextMesh.font.material;
            m_TextMesh.fontSize = 48;
            m_TextMesh.characterSize = 0.0045f;
            m_TextMesh.anchor = TextAnchor.LowerCenter;
            m_TextMesh.alignment = TextAlignment.Left;
            m_TextMesh.color = new Color(1f, 1f, 1f, 0.9f);
            m_IntervalStart = Time.unscaledTime;
            m_CsvRows.Add("tiempo_s,fps,frame_ms_max,fps_promedio");
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.hKey.wasPressedThisFrame)
                m_TextMesh.gameObject.SetActive(!m_TextMesh.gameObject.activeSelf);

            float dt = Time.unscaledDeltaTime;
            m_Accum += dt;
            m_Frames++;
            m_MaxFrameMs = Mathf.Max(m_MaxFrameMs, dt * 1000f);

            if (Time.unscaledTime - m_IntervalStart >= m_SampleInterval)
            {
                m_CurrentFps = m_Frames / m_Accum;
                // Se descartan los primeros 3 s (carga de escena) para no sesgar el mínimo.
                if (Time.timeSinceLevelLoad > 3f)
                {
                    m_MinFps = Mathf.Min(m_MinFps, m_CurrentFps);
                    m_FpsSum += m_CurrentFps;
                    m_FpsSamples++;
                    m_CsvRows.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                        "{0:F1},{1:F1},{2:F2},{3:F1}", Time.timeSinceLevelLoad, m_CurrentFps, m_MaxFrameMs, AverageFps));
                }
                m_Accum = 0f;
                m_Frames = 0;
                m_MaxFrameMs = 0f;
                m_IntervalStart = Time.unscaledTime;
            }

            if (m_TextMesh.gameObject.activeSelf)
                m_TextMesh.text = BuildText();
        }

        float AverageFps => m_FpsSamples > 0 ? (float)(m_FpsSum / m_FpsSamples) : m_CurrentFps;

        string BuildText()
        {
            m_Text.Clear();
            m_Text.AppendFormat("FPS {0:F0}  (prom {1:F0} | min {2:F0})\n", m_CurrentFps, AverageFps,
                m_MinFps == float.MaxValue ? m_CurrentFps : m_MinFps);
            foreach (var src in m_TrackedSources)
            {
                if (src == null)
                    continue;
                Vector3 toSrc = src.transform.position - m_Head.position;
                float dist = toSrc.magnitude;
                float gain = EstimateGain(src, dist);
                m_Text.AppendFormat("{0}: {1:F1} m  vol {2:P0}  {3}{4}\n", src.name, dist, gain, Direction(toSrc),
                    SourceState(src));
            }
            if (AudioObjectVisibility.IsHidden)
                m_Text.Append("Audio_Object OCULTO: ubícalo por el sonido (V)\n");
            if (Time.time < m_MessageUntil)
                m_Text.Append(m_Message);
            return m_Text.ToString();
        }

        static string SourceState(AudioSource src)
        {
            if (src.TryGetComponent(out RadioInteractable radio) && !radio.IsOn)
                return "  (apagada)";
            if (src.TryGetComponent(out HiddenSoundTarget target) && target.IsRevealed)
                return "  (encontrado)";
            return string.Empty;
        }

        /// <summary>Ganancia aproximada de la curva logarítmica de Unity: minDistance / distancia.</summary>
        static float EstimateGain(AudioSource src, float dist)
        {
            float d = Mathf.Clamp(dist, src.minDistance, src.maxDistance);
            float rolloff = src.rolloffMode == AudioRolloffMode.Logarithmic
                ? src.minDistance / d
                : 1f - Mathf.InverseLerp(src.minDistance, src.maxDistance, d);
            return src.volume * Mathf.Lerp(1f, rolloff, src.spatialBlend);
        }

        string Direction(Vector3 toSrc)
        {
            Vector3 local = m_Head.InverseTransformDirection(toSrc.normalized);
            string side = local.x > 0.35f ? "derecha" : local.x < -0.35f ? "izquierda" : "centro";
            string depth = local.z >= 0f ? "delante" : "detras";
            string height = local.y > 0.5f ? " arriba" : local.y < -0.5f ? " abajo" : string.Empty;
            return depth + "-" + side + height;
        }

        void OnTargetFound(HiddenSoundTarget target)
        {
            m_Message = $"<< OBJETO INVISIBLE ENCONTRADO x{target.TimesFound} >>";
            m_MessageUntil = Time.time + 3f;
        }

        void OnDestroy() => SaveCsv();

        void SaveCsv()
        {
            if (m_FpsSamples == 0)
                return;
            string dir = Application.isEditor
                ? Path.Combine(Application.dataPath, "..", "Reportes")
                : Application.persistentDataPath;
            try
            {
                Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, "fps_log.csv");
                File.WriteAllLines(path, m_CsvRows);
                File.WriteAllText(Path.Combine(dir, "fps_resumen.txt"), string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "Plataforma: {0}\nDispositivo: {1} | GPU: {2}\nMuestras: {3}\nFPS promedio: {4:F1}\nFPS minimo: {5:F1}\n",
                    Application.platform, SystemInfo.deviceModel, SystemInfo.graphicsDeviceName,
                    m_FpsSamples, AverageFps, m_MinFps));
                Debug.Log($"[Practica5] Registro de FPS guardado en {Path.GetFullPath(path)}");
            }
            catch (IOException e)
            {
                Debug.LogWarning($"[Practica5] No se pudo guardar el registro de FPS: {e.Message}");
            }
        }
    }
}
