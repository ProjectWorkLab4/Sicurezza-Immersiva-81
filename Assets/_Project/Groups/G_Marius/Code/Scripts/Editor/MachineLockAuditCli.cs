#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SI81.MachineLock.EditorTools
{
    /// <summary>
    /// Read-only Editor CLI auditing utilities for Scenario 08 (Machine Lock): geometry &amp; triangle-budget
    /// inspection and offscreen PNG camera captures for visual verification before building an APK.
    /// Invoked via <c>scripts/unity-run.ps1 inspect-scene</c> or <c>capture-views</c>.
    /// </summary>
    public static class MachineLockAuditCli
    {
        private const string LOG_PREFIX = "[MachineLockAudit] ";
        private const int PREVIEW_WIDTH = 1280;
        private const int PREVIEW_HEIGHT = 720;

        /// <summary>
        /// Logs total triangle count, top-25 MeshFilter triangle consumers in SCN_MachineLock, and
        /// PFB_OldIndustrialBuilding horizontal slab elevations and inner wall clearances.
        /// </summary>
        [MenuItem("SI81/MachineLock/Audit/Inspect Scene Geometry & Triangles")]
        public static void InspectScene()
        {
            Scene scene = EditorSceneManager.OpenScene(MachineLockSceneBuilder.SCENE_PATH, OpenSceneMode.Single);
            try
            {
                var counts = new List<KeyValuePair<string, int>>();
                long total = 0;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>(true))
                    {
                        if (mf.sharedMesh == null) continue;
                        int tris = mf.sharedMesh.triangles.Length / 3;
                        total += tris;
                        counts.Add(new KeyValuePair<string, int>(GetHierarchyPath(mf.transform), tris));
                    }
                }
                counts.Sort((a, b) => b.Value.CompareTo(a.Value));
                Debug.Log($"{LOG_PREFIX}SCENE_TRIS Total={total}");
                int top = Math.Min(25, counts.Count);
                for (int i = 0; i < top; i++)
                    Debug.Log($"{LOG_PREFIX}TOP{i + 1:00}: {counts[i].Value,6} tris | {counts[i].Key}");

                InspectBuildingShell(scene);
            }
            finally
            {
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
        }

        /// <summary>
        /// Renders six offscreen 1280x720 PNG views of SCN_MachineLock into Logs/previews/ using a
        /// temporary camera that is destroyed before closing the scene. Must run without -nographics.
        /// </summary>
        [MenuItem("SI81/MachineLock/Audit/Capture Scene Preview PNGs")]
        public static void CaptureSceneViews()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                Debug.LogError(LOG_PREFIX + "CaptureSceneViews requires a graphics device (omit -nographics).");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }

            string outDir = Path.GetFullPath(Path.Combine("Logs", "previews"));
            Directory.CreateDirectory(outDir);

            Scene scene = EditorSceneManager.OpenScene(MachineLockSceneBuilder.SCENE_PATH, OpenSceneMode.Single);
            var camObj = new GameObject("__TempAuditPreviewCamera");
            camObj.hideFlags = HideFlags.HideAndDontSave;
            var cam = camObj.AddComponent<Camera>();
            cam.fieldOfView = 68f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 80f;
            cam.clearFlags = CameraClearFlags.Skybox;

            var rt = new RenderTexture(PREVIEW_WIDTH, PREVIEW_HEIGHT, 24, RenderTextureFormat.ARGB32);
            var tex = new Texture2D(PREVIEW_WIDTH, PREVIEW_HEIGHT, TextureFormat.RGB24, false);

            var shots = new (string FileName, Vector3 Position, Vector3 LookAt)[]
            {
                ("01_spawn_view.png",     new Vector3(-7.6f, 1.65f,  0.0f), new Vector3( 0.0f, 1.50f,  0.0f)),
                ("02_electrical_bay.png", new Vector3( 5.4f, 1.55f,  0.0f), new Vector3( 8.7f, 1.35f,  0.0f)),
                ("03_ppe_cabinet.png",    new Vector3( 2.8f, 1.45f, -2.2f), new Vector3( 2.8f, 1.20f, -5.1f)),
                ("04_valve_and_pipe.png", new Vector3( 3.2f, 1.65f,  3.1f), new Vector3( 4.6f, 0.95f,  0.55f)),
                ("05_west_exit.png",      new Vector3(-4.6f, 1.65f,  0.8f), new Vector3(-9.0f, 1.75f,  0.6f)),
                ("06_topdown_bay.png",    new Vector3( 0.0f, 14.5f, -0.5f), new Vector3( 0.0f, 0.00f,  0.0f))
            };

            try
            {
                cam.targetTexture = rt;
                RenderTexture prevActive = RenderTexture.active;
                RenderTexture.active = rt;

                foreach (var shot in shots)
                {
                    camObj.transform.position = shot.Position;
                    camObj.transform.LookAt(shot.LookAt, Vector3.up);
                    cam.Render();
                    tex.ReadPixels(new Rect(0, 0, PREVIEW_WIDTH, PREVIEW_HEIGHT), 0, 0);
                    tex.Apply(false, false);

                    string filePath = Path.Combine(outDir, shot.FileName);
                    File.WriteAllBytes(filePath, tex.EncodeToPNG());
                    Debug.Log($"{LOG_PREFIX}PREVIEW {filePath}");
                }

                RenderTexture.active = prevActive;
                cam.targetTexture = null;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tex);
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
                UnityEngine.Object.DestroyImmediate(camObj);
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
        }

        private static void InspectBuildingShell(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name != "PFB_OldIndustrialBuilding") continue;
                    foreach (MeshRenderer mr in t.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        MeshFilter mf = mr.GetComponent<MeshFilter>();
                        if (mf == null || mf.sharedMesh == null) continue;
                        Bounds b = mr.bounds;
                        Debug.Log($"{LOG_PREFIX}SHELL '{mr.name}' WorldMin={b.min} WorldMax={b.max} Size={b.size}");
                    }
                    return;
                }
            }
        }

        private static string GetHierarchyPath(Transform t)
        {
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }
    }
}
#endif
