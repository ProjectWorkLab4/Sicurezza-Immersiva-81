using System.Text;
using UnityEditor;
using UnityEngine;

public static class AnalisiMuri
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

    public static void FinestreSud()
    {
        var c = Attiva();

        if (c == null) return;

        var sb = new StringBuilder();
        sb.AppendLine("# FINESTRE muro sud (z=-13.4)  x -18.2..-10.8  y -1.1..1.2");
        sb.AppendLine("        y\\x  0         1         2         3         4         5         6         7");
        sb.AppendLine("        4567890123456789012345678901234567890123456789012345678901234567890123456");

        for (float y = 1.2f; y >= -1.1f; y -= 0.1f)
        {
            var riga = new StringBuilder($"{y,7:F1}  ");

            for (float x = -18.2f; x <= -10.8f; x += 0.1f)
            {
                var o = new Vector3(x, y, -12.0f);
                riga.Append(Physics.Raycast(o, Vector3.back, out var hit, 3f, ~0, QueryTriggerInteraction.Ignore) ? '#' : '.');
            }

            sb.AppendLine(riga.ToString());
        }

        Object.DestroyImmediate(c);
        Debug.Log(sb.ToString());
    }

    public static void AperturaEst()
    {
        var c = Attiva();

        if (c == null) return;

        var sb = new StringBuilder();
        sb.AppendLine("# APERTURA est: raggio +X da x=-17.5, y=0.0, z -13.5..-3.3 ('.'=nessun muro entro 8m)");

        for (float z = -3.3f; z >= -13.5f; z -= 0.25f)
        {
            var o = new Vector3(-17.5f, 0f, z);
            bool colpito = Physics.Raycast(o, Vector3.right, out var hit, 8f, ~0, QueryTriggerInteraction.Ignore);
            sb.AppendLine($"{z,7:F2}  {(colpito ? "# hit x=" + hit.point.x.ToString("F2") : ".")}");
        }

        Object.DestroyImmediate(c);
        Debug.Log(sb.ToString());
    }

    public static void AltezzeParete()
    {
        var c = Attiva();

        if (c == null) return;

        var sb = new StringBuilder();
        sb.AppendLine("# ALTEZZE: raggio -Y da y=3 a varie x/z");
        float[] xs = { -17f, -15f, -13f, -12f, -11.5f };
        float[] zs = { -5f, -8f, -11f, -13f };

        foreach (float x in xs)
        {
            foreach (float z in zs)
            {
                var o = new Vector3(x, 3f, z);
                bool hit = Physics.Raycast(o, Vector3.down, out var h, 6f, ~0, QueryTriggerInteraction.Ignore);
                sb.AppendLine($"x={x:F1} z={z:F1} -> {(hit ? h.point.y.ToString("F2") : "nessun hit")}");
            }
        }

        Object.DestroyImmediate(c);
        Debug.Log(sb.ToString());
    }
}
