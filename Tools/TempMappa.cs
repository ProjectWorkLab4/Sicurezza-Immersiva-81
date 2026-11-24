using System.Text;
using UnityEditor;
using UnityEngine;

public static class TempMappa
{
    public static void Esegui()
    {
        var aula = GameObject.Find("Aula");

        if (aula == null)
        {
            Debug.LogError("[Mappa] Aula non trovata");
            return;
        }

        var renderer = aula.GetComponentInChildren<Renderer>();

        if (renderer == null)
        {
            Debug.LogError("[Mappa] Renderer non trovato");
            return;
        }

        var collider = renderer.gameObject.GetComponent<MeshCollider>();

        if (collider == null)
        {
            collider = renderer.gameObject.AddComponent<MeshCollider>();
        }

        var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
        collider.sharedMesh = mesh;
        collider.convex = false;

        var b = renderer.bounds;
        const float passo = 0.5f;
        const float alto = 6f;

        int nx = Mathf.CeilToInt(b.size.x / passo);
        int nz = Mathf.CeilToInt(b.size.z / passo);

        var sb = new StringBuilder();
        sb.AppendLine($"# Mappa aula: x {b.min.x:F1}..{b.max.x:F1}  z {b.min.z:F1}..{b.max.z:F1}  passo {passo}  ({nx}x{nz})");
        sb.AppendLine("# legenda: '.' pavimento  '#' muro/ostacolo  ' ' vuoto");

        // Header con coordinate ogni 2 metri
        var header = new StringBuilder("        ");
        for (int ix = 0; ix < nx; ix++)
        {
            float x = b.min.x + (ix + 0.5f) * passo;
            header.Append(ix % 4 == 0 ? '|' : ' ');
        }
        sb.AppendLine(header.ToString());

        for (int iz = nz - 1; iz >= 0; iz--)
        {
            float z = b.min.z + (iz + 0.5f) * passo;
            var riga = new StringBuilder();
            riga.Append($"{z,7:F1} ");

            for (int ix = 0; ix < nx; ix++)
            {
                float x = b.min.x + (ix + 0.5f) * passo;
                var origine = new Vector3(x, b.max.y + alto, z);

                if (!Physics.Raycast(origine, Vector3.down, out var hit, alto * 2f, ~0, QueryTriggerInteraction.Ignore))
                {
                    riga.Append(' ');
                    continue;
                }

                // Il pavimento e' la superficie piu' bassa; se il primo hit e' molto in alto e' un muro/soffitto.
                riga.Append(hit.point.y > b.min.y + 1.6f ? '#' : '.');
            }

            sb.AppendLine(riga.ToString());
        }

        Object.DestroyImmediate(collider);
        Debug.Log(sb.ToString());
    }
}
