using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Practica5.Editor
{
    /// <summary>
    /// Mide el tiempo de cada bakeo de iluminación y lo registra en Reportes/bake_report.txt
    /// junto con la configuración usada y los lightmaps generados.
    /// </summary>
    [InitializeOnLoad]
    static class BakeReporter
    {
        static readonly Stopwatch s_Watch = new Stopwatch();

        static BakeReporter()
        {
            Lightmapping.bakeStarted += OnBakeStarted;
            Lightmapping.bakeCompleted += OnBakeCompleted;
        }

        static string ReportDir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Reportes"));

        static void OnBakeStarted() => s_Watch.Restart();

        static void OnBakeCompleted()
        {
            if (!s_Watch.IsRunning)
                return;
            s_Watch.Stop();
            var ls = Lightmapping.lightingSettings;
            var sb = new StringBuilder();
            sb.AppendLine($"=== Bakeo {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
            sb.AppendLine($"Escena: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().path}");
            sb.AppendLine($"Tiempo de bakeo: {s_Watch.Elapsed.TotalSeconds:F2} s");
            if (ls != null)
            {
                sb.AppendLine($"Lightmapper: {ls.lightmapper}");
                sb.AppendLine($"Resolucion: {ls.lightmapResolution} texels/u | Tamaño max: {ls.lightmapMaxSize} | Padding: {ls.lightmapPadding}");
                sb.AppendLine($"Muestras directas/indirectas/entorno: {ls.directSampleCount}/{ls.indirectSampleCount}/{ls.environmentSampleCount} | Rebotes: {ls.maxBounces}");
                sb.AppendLine($"Direccionalidad: {ls.directionalityMode} | Compresion: {ls.lightmapCompression} | AO: {ls.ao}");
            }
            var maps = LightmapSettings.lightmaps;
            sb.AppendLine($"Lightmaps generados: {maps.Length}");
            for (int i = 0; i < maps.Length; i++)
            {
                var tex = maps[i].lightmapColor;
                if (tex != null)
                    sb.AppendLine($"  [{i}] {AssetDatabase.GetAssetPath(tex)} ({tex.width}x{tex.height})");
            }
            sb.AppendLine();

            try
            {
                Directory.CreateDirectory(ReportDir);
                File.AppendAllText(Path.Combine(ReportDir, "bake_report.txt"), sb.ToString());
            }
            catch (IOException e)
            {
                Debug.LogWarning($"[Practica5] No se pudo escribir bake_report.txt: {e.Message}");
            }
            Debug.Log($"[Practica5] Bakeo completado en {s_Watch.Elapsed.TotalSeconds:F2} s, {maps.Length} lightmap(s).");
        }

        [MenuItem("Practica5/Exportar Lightmaps a PNG", priority = 20)]
        static void ExportLightmaps()
        {
            var maps = LightmapSettings.lightmaps;
            if (maps.Length == 0)
            {
                Debug.LogWarning("[Practica5] No hay lightmaps. Ejecuta primero el bakeo.");
                return;
            }
            Directory.CreateDirectory(ReportDir);
            for (int i = 0; i < maps.Length; i++)
            {
                var src = maps[i].lightmapColor;
                if (src == null)
                    continue;
                var rt = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                Graphics.Blit(src, rt);
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                var png = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
                png.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
                png.Apply();
                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(rt);
                string path = Path.Combine(ReportDir, $"Lightmap_{i}.png");
                File.WriteAllBytes(path, png.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(png);
                Debug.Log($"[Practica5] Lightmap exportado: {path}");
            }
        }
    }
}
