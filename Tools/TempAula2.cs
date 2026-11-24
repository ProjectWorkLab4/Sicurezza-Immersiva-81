using UnityEditor;
using UnityEngine;

public static class TempAula2
{
    public static void Diagnostica()
    {
        var aula = GameObject.Find("Aula");
        var piano = GameObject.Find("Plane");

        if (aula != null)
        {
            foreach (var r in aula.GetComponentsInChildren<Renderer>(true))
            {
                Debug.Log($"[Aula] {r.name} worldBounds size={r.bounds.size} center={r.bounds.center} enabled={r.enabled}");
            }

            foreach (var t in aula.GetComponentsInChildren<Transform>(true))
            {
                Debug.Log($"[AulaT] {t.name} pos={t.position} rot={t.eulerAngles} scale={t.lossyScale}");
            }
        }
        else
        {
            Debug.LogError("[Aula] non trovata");
        }

        if (piano != null)
        {
            foreach (var r in piano.GetComponentsInChildren<Renderer>(true))
            {
                Debug.Log($"[Piano] {r.name} worldBounds size={r.bounds.size} center={r.bounds.center}");
            }
        }
    }

    public static void VistaProspettica()
    {
        var aula = GameObject.Find("Aula");

        if (aula == null)
        {
            Debug.LogError("[Aula] non trovata");
            return;
        }

        var bounds = new Bounds();
        bool first = true;

        foreach (var r in aula.GetComponentsInChildren<Renderer>(true))
        {
            if (first) { bounds = r.bounds; first = false; }
            else bounds.Encapsulate(r.bounds);
        }

        var piano = GameObject.Find("Plane");

        if (piano != null)
        {
            piano.SetActive(false);
        }

        var vista = SceneView.lastActiveSceneView;

        if (vista == null)
        {
            return;
        }

        var rotazione = Quaternion.Euler(35f, 135f, 0f);
        vista.orthographic = false;
        vista.LookAt(bounds.center, rotazione, bounds.extents.magnitude * 1.4f);
        vista.Repaint();
    }
}
