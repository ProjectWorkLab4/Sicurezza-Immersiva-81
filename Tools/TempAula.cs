using System.Text;
using UnityEditor;
using UnityEngine;

public static class TempAula
{
    const string FbxPath = "Assets/_Project/Groups/G_Simone/Assets/Level/Prefabs/P_W_4_aula.fbx";

    public static void Instanzia()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);

        if (prefab == null)
        {
            Debug.LogError("FBX non trovato: " + FbxPath);
            return;
        }

        var esistenti = GameObject.Find("Aula");

        if (esistenti != null)
        {
            Object.DestroyImmediate(esistenti);
        }

        var istanza = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        istanza.name = "Aula";

        // Non toccare rotazione/scala: il nodo radice dell'FBX contiene la
        // conversione assi (rot 270, scala 100). Si sposta solo la posizione.
        Debug.Log($"[Aula] root pos={istanza.transform.position} rot={istanza.transform.eulerAngles} scale={istanza.transform.localScale}");

        var renderers = istanza.GetComponentsInChildren<Renderer>(true);
        var bounds = new Bounds();
        bool first = true;

        foreach (var r in renderers)
        {
            if (first) { bounds = r.bounds; first = false; }
            else bounds.Encapsulate(r.bounds);
        }

        Debug.Log($"[Aula] bounds size={bounds.size} center={bounds.center} min={bounds.min} max={bounds.max} renderers={renderers.Length}");
        Selection.activeGameObject = istanza;
    }

    public static void Mappa()
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

            if (r == null)
            {
                continue;
            }

            if (first) { bounds = r.bounds; first = false; }
            else bounds.Encapsulate(r.bounds);

            var c = f.gameObject.GetComponent<MeshCollider>();

            if (c == null)
            {
                c = f.gameObject.AddComponent<MeshCollider>();
            }

            c.sharedMesh = f.sharedMesh;
            c.convex = false;
            collider = c;
        }

        const float passo = 0.5f;
        const float alto = 8f;
        float pavimento = bounds.min.y;

        int nx = Mathf.CeilToInt(bounds.size.x / passo);
        int nz = Mathf.CeilToInt(bounds.size.z / passo);

        var sb = new StringBuilder();
        sb.AppendLine($"# MAPPA x {bounds.min.x:F1}..{bounds.max.x:F1}  z {bounds.min.z:F1}..{bounds.max.z:F1}  y {bounds.min.y:F2}..{bounds.max.y:F2}  passo {passo} ({nx}x{nz})");

        for (int iz = nz - 1; iz >= 0; iz--)
        {
            float z = bounds.min.z + (iz + 0.5f) * passo;
            var riga = new StringBuilder();
            riga.Append($"{z,7:F1} ");

            for (int ix = 0; ix < nx; ix++)
            {
                float x = bounds.min.x + (ix + 0.5f) * passo;
                var origine = new Vector3(x, bounds.max.y + alto, z);

                if (!Physics.Raycast(origine, Vector3.down, out var hit, alto * 2.5f, ~0, QueryTriggerInteraction.Ignore))
                {
                    riga.Append(' ');
                    continue;
                }

                riga.Append(hit.point.y > pavimento + 1.6f ? '#' : '.');
            }

            sb.AppendLine(riga.ToString());
        }

        // Riga delle ascisse (ogni 2 m)
        var asse = new StringBuilder("        ");
        for (int ix = 0; ix < nx; ix++)
        {
            float x = bounds.min.x + (ix + 0.5f) * passo;
            bool multiplo = Mathf.Abs(Mathf.Repeat(x, 2f)) < passo * 0.5f || Mathf.Abs(Mathf.Repeat(x, 2f) - 2f) < passo * 0.5f;
            asse.Append(multiplo ? '|' : ' ');
        }
        sb.AppendLine(asse.ToString());

        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        Debug.Log(sb.ToString());
    }
}
