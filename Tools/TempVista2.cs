using UnityEditor;
using UnityEngine;

public static class TempVista2
{
    static void GuardaDa(Vector3 pos, Vector3 verso, float fov)
    {
        var vista = SceneView.lastActiveSceneView;

        if (vista == null) return;

        var piano = GameObject.Find("Plane");

        if (piano != null) piano.SetActive(false);

        vista.orthographic = false;
        vista.LookAt(pos + verso * 5f, Quaternion.LookRotation(verso), 1f);
        vista.pivot = pos;
        vista.size = 5f;
        vista.Repaint();
    }

    public static void DentroNord() { GuardaDa(new Vector3(-16.5f, 0.6f, -4.5f), Vector3.back, 70f); }
    public static void DentroSud() { GuardaDa(new Vector3(-16.5f, 0.6f, -12.0f), Vector3.forward, 70f); }
    public static void DentroEst() { GuardaDa(new Vector3(-17.5f, 0.6f, -8.0f), Vector3.right, 70f); }
}
