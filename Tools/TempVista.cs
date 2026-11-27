using UnityEditor;
using UnityEngine;

public static class TempVista
{
    public static void Stanza()
    {
        var centro = new Vector3(-17.0f, -1.08f, -8.4f);
        var vista = SceneView.lastActiveSceneView;

        if (vista == null) return;

        // Nasconde il piano gigante per non confondere la lettura
        var piano = GameObject.Find("Plane");

        if (piano != null) piano.SetActive(false);

        vista.orthographic = true;
        vista.LookAt(centro, Quaternion.Euler(90f, 0f, 0f), 6.5f);
        vista.Repaint();
    }

    public static void Destra()
    {
        var centro = new Vector3(3.0f, -1.08f, -6.0f);
        var vista = SceneView.lastActiveSceneView;

        if (vista == null) return;

        var piano = GameObject.Find("Plane");

        if (piano != null) piano.SetActive(false);

        vista.orthographic = true;
        vista.LookAt(centro, Quaternion.Euler(90f, 0f, 0f), 12f);
        vista.Repaint();
    }
}
