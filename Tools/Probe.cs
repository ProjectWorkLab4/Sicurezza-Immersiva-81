using System.Text;
using UnityEditor;
using UnityEngine;

public static class Probe
{
    public static void Run()
    {
        const string path = "Assets/_Project/Groups/G_Simone/Assets/Level/Prefabs/P_W_4_aula.fbx";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

        if (prefab == null)
        {
            Debug.Log(path + " -> MISSING");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("=== " + path + " ===");

        var renderers = prefab.GetComponentsInChildren<Renderer>(true);
        var total = new Bounds();
        var first = true;

        foreach (var r in renderers)
        {
            if (first) { total = r.bounds; first = false; }
            else total.Encapsulate(r.bounds);
        }

        sb.AppendLine("TOTAL size " + total.size.ToString("F3") + " center " + total.center.ToString("F3") +
                      " min " + total.min.ToString("F3") + " max " + total.max.ToString("F3"));
        sb.AppendLine("renderers=" + renderers.Length);

        foreach (var t in prefab.GetComponentsInChildren<Transform>(true))
        {
            int depth = 0;
            var p = t;
            while (p != prefab.transform) { depth++; p = p.parent; }

            var extra = new StringBuilder();

            var mr = t.GetComponent<MeshFilter>();
            if (mr != null && mr.sharedMesh != null)
            {
                extra.Append(" mesh=").Append(mr.sharedMesh.name).Append(" verts=").Append(mr.sharedMesh.vertexCount);
            }

            var r = t.GetComponent<Renderer>();
            if (r != null)
            {
                extra.Append(" bounds=").Append(r.bounds.size.ToString("F2")).Append('@').Append(r.bounds.center.ToString("F2"));
            }

            if (t.GetComponent<Collider>() != null)
            {
                extra.Append(" [collider]");
            }

            sb.AppendLine(new string(' ', depth * 2) + t.name +
                          " pos=" + t.localPosition.ToString("F2") +
                          " rot=" + t.localEulerAngles.ToString("F1") +
                          " scale=" + t.localScale.ToString("F2") + extra);
        }

        Debug.Log(sb.ToString());
    }
}
