using System.Text;
using UnityEditor;
using UnityEngine;

public static class Analisi2
{
    static MeshCollider Attiva()
    {
        var aula = GameObject.Find("Aula");

        if (aula == null) return null;

        var f = aula.GetComponentInChildren<MeshFilter>();

        if (f == null) return null;

        var c = f.gameObject.GetComponent<MeshCollider>();

        if (c == null)
        {
            c = f.gameObject.AddComponent<MeshCollider>();
        }

        c.sharedMesh = f.sharedMesh;
        c.convex = false;
        return c;
    }

    public static void PortaEst()
    {
        var c = Attiva();

        if (c == null) return;

        var sb = new StringBuilder();
        sb.AppendLine("# PORTA EST (muro x=-11.0)  z -5.2..-3.3  y -1.0..1.1");

        for (float y = 1.1f; y >= -1.0f; y -= 0.1f)
        {
            var riga = new StringBuilder($"{y,6:F1} ");

            for (float z = -5.2f; z <= -3.3f; z += 0.1f)
            {
                var o = new Vector3(-16f, y, z);
                riga.Append(Physics.Raycast(o, Vector3.right, out _, 8f, ~0, QueryTriggerInteraction.Ignore) ? '#' : '.');
            }

            sb.AppendLine(riga.ToString());
        }

        Object.DestroyImmediate(c);
        Debug.Log(sb.ToString());
    }

    public static void PavimentoEst()
    {
        var c = Attiva();

        if (c == null) return;

        var sb = new StringBuilder();
        sb.AppendLine("# PAVIMENTO EST x -12..-1 z -7..-2 (X=pavimento, .=vuoto)");

        for (float z = -2f; z >= -7f; z -= 0.5f)
        {
            var riga = new StringBuilder($"{z,6:F1} ");

            for (float x = -12f; x <= -1f; x += 0.5f)
            {
                var o = new Vector3(x, 3f, z);
                riga.Append(Physics.Raycast(o, Vector3.down, out var h, 6f, ~0, QueryTriggerInteraction.Ignore) && h.point.y < -0.5f ? 'X' : '.');
            }

            sb.AppendLine(riga.ToString());
        }

        Object.DestroyImmediate(c);
        Debug.Log(sb.ToString());
    }
}
