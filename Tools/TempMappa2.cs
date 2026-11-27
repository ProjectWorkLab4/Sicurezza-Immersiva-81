using System.Text;
using UnityEditor;
using UnityEngine;

public static class TempMappa2
{
    public static void ZonaSinistra()
    {
        Mappa("SINISTRA", -25.6f, -8.8f, -13.6f, -2.9f, 0.5f);
    }

    public static void ZonaDestra()
    {
        Mappa("DESTRA", -2.2f, 7.9f, -32.1f, 7.6f, 1.0f);
    }

    static void Mappa(string nome, float xmin, float xmax, float zmin, float zmax, float passo)
    {
        var aula = GameObject.Find("Aula");

        if (aula == null)
        {
            Debug.LogError("[Mappa] Aula non trovata");
            return;
        }

        var filtri = aula.GetComponentsInChildren<MeshFilter>(true);
        var bounds = new Bounds();
        bool first = true;
        MeshCollider collider = null;

        foreach (var f in filtri)
        {
            var r = f.GetComponent<Renderer>();

            if (r == null) continue;

            if (first) { bounds = r.bounds; first = false; }
            else bounds.Encapsulate(r.bounds);

            var c = f.gameObject.GetComponent<MeshCollider>();

            if (c == null) c = f.gameObject.AddComponent<MeshCollider>();

            c.sharedMesh = f.sharedMesh;
            c.convex = false;
            collider = c;
        }

        float pavimento = bounds.min.y;
        int nx = Mathf.CeilToInt((xmax - xmin) / passo);
        int nz = Mathf.CeilToInt((zmax - zmin) / passo);

        var sb = new StringBuilder();
        sb.AppendLine($"# {nome} x {xmin}..{xmax} z {zmin}..{zmax} passo {passo} pavimento {pavimento:F2}");

        var asse = new StringBuilder("        ");
        for (int ix = 0; ix < nx; ix++)
        {
            float x = xmin + (ix + 0.5f) * passo;
            asse.Append(ix % 2 == 0 ? '|' : ' ');
        }
        sb.AppendLine(asse.ToString());

        for (int iz = nz - 1; iz >= 0; iz--)
        {
            float z = zmin + (iz + 0.5f) * passo;
            var riga = new StringBuilder();
            riga.Append($"{z,7:F1} ");

            for (int ix = 0; ix < nx; ix++)
            {
                float x = xmin + (ix + 0.5f) * passo;
                var origine = new Vector3(x, bounds.max.y + 8f, z);

                if (!Physics.Raycast(origine, Vector3.down, out var hit, 20f, ~0, QueryTriggerInteraction.Ignore))
                {
                    riga.Append(' ');
                    continue;
                }

                riga.Append(hit.point.y > pavimento + 1.6f ? '#' : '.');
            }

            sb.AppendLine(riga.ToString());
        }

        if (collider != null) Object.DestroyImmediate(collider);

        Debug.Log(sb.ToString());
    }
}
