using UnityEditor;
using UnityEngine;

public static class Ispeziona
{
    public static void Oggetti()
    {
        var nomi = new[] { "Plane", "OVRCameraRig", "Timer", "Quadro Elettrico", "Teleport Hotspot", "EventSystem", "Aula" };

        foreach (var n in nomi)
        {
            var go = GameObject.Find(n);

            if (go == null)
            {
                Debug.Log($"[Obj] {n} -> MANCANTE");
                continue;
            }

            var t = go.transform;
            var sb = new System.Text.StringBuilder();
            sb.Append($"[Obj] {n} pos={t.position} rot={t.eulerAngles} scale={t.lossyScale}");

            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                sb.Append($"\n    R {r.name} bounds={r.bounds.size}@{r.bounds.center}");
            }

            foreach (var c in go.GetComponents<Component>())
            {
                sb.Append($"\n    C {c.GetType().Name}");
            }

            Debug.Log(sb.ToString());
        }
    }
}
