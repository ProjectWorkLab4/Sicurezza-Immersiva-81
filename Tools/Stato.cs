using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Stato
{
    public static void Esegui()
    {
        var sb = new StringBuilder();
        var scena = SceneManager.GetActiveScene();
        sb.AppendLine("=== SCENA " + scena.path + " dirty=" + scena.isDirty + " ===");

        foreach (var root in scena.GetRootGameObjects())
        {
            Descrivi(sb, root.transform, 0);
        }

        sb.AppendLine("=== RISORSE ===");
        sb.AppendLine("TMP_Settings: " + (Resources.Load("TMP Settings") != null));
        var font = Resources.Load<TMPro.TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        sb.AppendLine("TMP font default: " + (font != null));
        sb.AppendLine("URP asset: " + (UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline != null
            ? UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline.GetType().Name
            : "nessuno"));

        var cartelle = AssetDatabase.FindAssets("t:Material", new[] { "Assets/_Project/Groups/G_Simone" });
        sb.AppendLine("Materiali G_Simone: " + cartelle.Length);

        Debug.Log(sb.ToString());
    }

    static void Descrivi(StringBuilder sb, Transform t, int depth)
    {
        var comp = new StringBuilder();

        foreach (var c in t.GetComponents<Component>())
        {
            if (c == null) { comp.Append(" NULL"); continue; }
            comp.Append(' ').Append(c.GetType().Name);
        }

        sb.AppendLine(new string(' ', depth * 2) + t.name + " [" + comp.ToString().Trim() + "]");

        foreach (Transform figlio in t)
        {
            Descrivi(sb, figlio, depth + 1);
        }
    }
}
