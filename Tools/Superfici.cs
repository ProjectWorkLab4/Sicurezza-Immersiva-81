using UnityEditor;
using UnityEngine;

public static class Superfici
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
        sb.AppendLine("# SUPERFICI INTERNE");

        Prova(sb, new Vector3(-15f, 0f, -8f), Vector3.back, "sud (z-)");
        Prova(sb, new Vector3(-15f, 0f, -8f), Vector3.forward, "nord (z+)");
        Prova(sb, new Vector3(-15f, 0f, -8f), Vector3.right, "est (x+)");
        Prova(sb, new Vector3(-15f, 0f, -8f), Vector3.left, "ovest (x-)");

        // Apertura finestra centrale: bordo interno
        Prova(sb, new Vector3(-14.45f, 0.45f, -12f), Vector3.back, "finestra centro");

        // Soglia porta est
        Prova(sb, new Vector3(-13f, 0f, -4.3f), Vector3.right, "porta est");

        Object.DestroyImmediate(c);
        Debug.Log(sb.ToString());
    }

    static void Prova(System.Text.StringBuilder sb, Vector3 o, Vector3 d, string nome)
    {
        if (Physics.Raycast(o, d, out var hit, 10f, ~0, QueryTriggerInteraction.Ignore))
        {
            sb.AppendLine($"{nome}: hit {hit.point} normale {hit.normal}");
        }
        else
        {
            sb.AppendLine($"{nome}: nessun hit");
        }
    }
}
