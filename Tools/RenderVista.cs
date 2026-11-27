using System.IO;
using UnityEditor;
using UnityEngine;

public static class RenderVista
{
    const string Cartella = @"C:\Users\CastelliGiovanniKaro\AppData\Local\Temp\opencode";

    public static void Tutte()
    {
        var piano = GameObject.Find("Plane");

        if (piano != null) piano.SetActive(false);

        var aula = GameObject.Find("Aula");
        var b = new Bounds(new Vector3(-17f, -1f, -8f), new Vector3(18f, 3f, 12f));

        if (aula != null)
        {
            foreach (var r in aula.GetComponentsInChildren<Renderer>(true))
            {
                b = r.bounds;
            }
        }

        Debug.Log($"[Render] bounds {b.min} .. {b.max}");

        // Vista dall'alto ortografica della zona sinistra
        Scatta("alto_sinistra", new Vector3(-17f, 12f, -8.4f), Quaternion.Euler(90f, 0f, 0f), true, 7f);

        // Prospettive interne
        Scatta("p_nord", new Vector3(-16.5f, 0.6f, -4.2f), Quaternion.LookRotation(Vector3.back, Vector3.up), false, 60f);
        Scatta("p_sud", new Vector3(-16.5f, 0.6f, -12.5f), Quaternion.LookRotation(Vector3.forward, Vector3.up), false, 60f);
        Scatta("p_est", new Vector3(-17.6f, 0.6f, -8.0f), Quaternion.LookRotation(Vector3.right, Vector3.up), false, 60f);
        Scatta("p_ovest", new Vector3(-11.8f, 0.6f, -8.0f), Quaternion.LookRotation(Vector3.left, Vector3.up), false, 60f);

        // Vista dall'alto della zona destra
        Scatta("alto_destra", new Vector3(3f, 14f, -10f), Quaternion.Euler(90f, 0f, 0f), true, 11f);
    }

    static void Scatta(string nome, Vector3 pos, Quaternion rot, bool ortho, float size)
    {
        var go = new GameObject("CamTmp_" + nome);
        var cam = go.AddComponent<Camera>();
        cam.transform.SetPositionAndRotation(pos, rot);
        cam.orthographic = ortho;
        cam.orthographicSize = ortho ? size : 1f;
        cam.fieldOfView = ortho ? 60f : size;
        cam.nearClipPlane = 0.02f;
        cam.farClipPlane = 200f;
        cam.clearFlags = CameraClearFlags.Skybox;

        const int w = 1200;
        const int h = 800;
        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        RenderTexture.active = null;

        File.WriteAllBytes(Path.Combine(Cartella, nome + ".png"), tex.EncodeToPNG());

        cam.targetTexture = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(go);
    }
}
