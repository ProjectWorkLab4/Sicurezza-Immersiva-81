using UnityEditor;
using UnityEngine;

public static class PortaAltezza
{
    public static void Esegui()
    {
        var aula = GameObject.Find("Aula");

        if (aula == null) return;

        var f = aula.GetComponentInChildren<MeshFilter>();

        if (f == null) return;

        var c = f.gameObject.GetComponent<MeshCollider>();

        if (c == null) c = f.gameObject.AddComponent<MeshCollider>();

        c.sharedMesh = f.sharedMesh;
        c.convex = false;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# PORTA EST: muro presente? (raggio corto da x=-11.6 verso +X, 1.2m)");

        for (float y = 1.05f; y >= -1.0f; y -= 0.1f)
        {
            var o = new Vector3(-11.6f, y, -4.3f);
            bool hit = Physics.Raycast(o, Vector3.right, out var h, 1.2f, ~0, QueryTriggerInteraction.Ignore);
            sb.AppendLine($"y={y:F2} -> {(hit ? "MURO x=" + h.point.x.ToString("F2") : "APERTO")}");
        }

        Object.DestroyImmediate(c);
        Debug.Log(sb.ToString());
    }
}
