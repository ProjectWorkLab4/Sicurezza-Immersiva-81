using System.Text;
using UnityEditor;
using UnityEngine;

public static class Props
{
    static readonly string[] Percorsi =
    {
        "Assets/_Project/Shared/Oggetti/Telefono/Telefono.fbx",
        "Assets/_Project/Shared/Oggetti/Quadro Elettrico/Quadro Elettrico.fbx",
        "Assets/_Project/Shared/Oggetti/Estintore/Estintore.fbx",
        "Assets/_Project/Shared/Oggetti/Cestino/Cestino.fbx",
        "Assets/_Project/Shared/Oggetti/Bottiglia/Bottiglia.fbx",
        "Assets/_Project/Shared/Oggetti/Presa/Presa.fbx"
    };

    public static void Esegui()
    {
        var sb = new StringBuilder();

        foreach (var percorso in Percorsi)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(percorso);

            if (prefab == null)
            {
                sb.AppendLine(percorso + " -> MANCANTE");
                continue;
            }

            var renderers = prefab.GetComponentsInChildren<Renderer>(true);
            var b = new Bounds();
            bool primo = true;

            foreach (var r in renderers)
            {
                if (primo) { b = r.bounds; primo = false; }
                else b.Encapsulate(r.bounds);
            }

            sb.AppendLine($"{System.IO.Path.GetFileName(percorso)} size={b.size} center={b.center} min={b.min} max={b.max} renderers={renderers.Length} rootScale={prefab.transform.localScale}");

            foreach (var t in prefab.GetComponentsInChildren<Transform>(true))
            {
                sb.AppendLine($"    {t.name} local={t.localPosition} scale={t.localScale} mesh={(t.GetComponent<MeshFilter>() != null && t.GetComponent<MeshFilter>().sharedMesh != null ? t.GetComponent<MeshFilter>().sharedMesh.name : "-")}");
            }
        }

        Debug.Log(sb.ToString());
    }
}
