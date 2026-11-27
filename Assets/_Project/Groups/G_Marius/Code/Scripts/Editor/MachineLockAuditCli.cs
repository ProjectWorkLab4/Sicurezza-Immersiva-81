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
    /// Invoked via <c>scripts/unity-run.ps1 inspect-scene</c> or <c>capture-views</c>, or via Unity Pipeline.
    /// </summary>
    public static class MachineLockAuditCli
    {
        private const string LOG_PREFIX = "[MachineLockAudit] ";
        private const int PREVIEW_WIDTH = 1280;
        private const int PREVIEW_HEIGHT = 720;

        /// <summary>
        /// Logs total triangle count, top-25 MeshFilter triangle consumers in SCN_MachineLock, and
        /// MESH_WorkshopShell bounds.
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

                InspectWorkshopShell(scene);
            }
            finally
            {
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
        }

        /// <summary>
        /// Renders eight offscreen 1280x720 PNG views of SCN_MachineLock into Logs/previews/ using a
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
                ("01_spawn_view.png",       new Vector3(-7.6f, 1.65f,  0.0f), new Vector3( 0.0f, 1.55f,  0.0f)),
                ("02_electrical_bay.png",   new Vector3( 5.3f, 1.55f,  0.0f), new Vector3( 8.8f, 1.45f,  0.0f)),
                ("03_ppe_and_iso_south.png",new Vector3( 1.2f, 1.60f, -1.3f), new Vector3( 1.5f, 1.45f, -5.4f)),
                ("04_oil_splat_valve.png",  new Vector3( 3.0f, 1.80f,  3.5f), new Vector3( 2.2f, 0.70f,  0.2f)),
                ("05_west_exit_door.png",   new Vector3(-6.3f, 1.55f,  0.3f), new Vector3(-9.0f, 1.50f,  0.1f)),
                ("06_spill_kit_props.png",  new Vector3(-4.2f, 1.35f, -2.0f), new Vector3(-5.2f, 0.55f, -4.35f)),
                ("07_north_extinguisher.png",new Vector3( 2.8f, 1.65f,  2.2f), new Vector3( 3.2f, 1.55f,  5.45f)),
                ("08_hall_overview.png",    new Vector3(-7.2f, 4.60f, -4.2f), new Vector3( 4.5f, 1.30f,  0.8f))
            };

            try
            {
                cam.targetTexture = rt;
                RenderTexture prevActive = RenderTexture.active;
                RenderTexture.active = rt;

                // Initialize diegetic Digital Meter to its unlit OFF faceplate before capturing room shots.
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    MachineLockDigitalMeterDisplay initMeter = root.GetComponentInChildren<MachineLockDigitalMeterDisplay>(true);
                    if (initMeter == null) continue;
                    initMeter.SetPoweredOff();
                    break;
                }

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

                // Shot 09: Open PPE cabinet sliding doors temporarily to inspect all 6 hero PPE 3D models up close.
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    MachineLockPpeCabinet cab = root.GetComponentInChildren<MachineLockPpeCabinet>(true);
                    if (cab == null) continue;
                    Transform doorL = cab.transform.Find("MESH_PpeCabinet_DoorLeft");
                    Transform doorR = cab.transform.Find("MESH_PpeCabinet_DoorRight");
                    if (doorL != null) doorL.gameObject.SetActive(false);
                    if (doorR != null) doorR.gameObject.SetActive(false);
                    cam.fieldOfView = 52f;
                    Vector3 cabPos = cab.transform.position;
                    camObj.transform.position = cabPos + new Vector3(0f, 1.25f, 1.85f);
                    camObj.transform.LookAt(cabPos + new Vector3(0f, 1.12f, 0f), Vector3.up);
                    cam.Render();
                    tex.ReadPixels(new Rect(0, 0, PREVIEW_WIDTH, PREVIEW_HEIGHT), 0, 0);
                    tex.Apply(false, false);
                    string ppeFile = Path.Combine(outDir, "09_ppe_cabinet_closeup.png");
                    File.WriteAllBytes(ppeFile, tex.EncodeToPNG());
                    Debug.Log($"{LOG_PREFIX}PREVIEW {ppeFile}");
                    if (doorL != null) doorL.gameObject.SetActive(true);
                    if (doorR != null) doorR.gameObject.SetActive(true);
                    break;
                }

                // Shots 10 & 11: Close-up of the Voltage Tester Digital Meter (OFF vs LIVE 222 V / 0.00 A / 0.0 W).
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    MachineLockDigitalMeterDisplay meter = root.GetComponentInChildren<MachineLockDigitalMeterDisplay>(true);
                    if (meter == null) continue;
                    cam.fieldOfView = 34f;
                    Vector3 meterPos = meter.transform.position;
                    camObj.transform.position = meterPos + new Vector3(-0.62f, 0.02f, 0f);
                    camObj.transform.LookAt(meterPos, Vector3.up);

                    meter.SetPoweredOff();
                    cam.Render();
                    tex.ReadPixels(new Rect(0, 0, PREVIEW_WIDTH, PREVIEW_HEIGHT), 0, 0);
                    tex.Apply(false, false);
                    string meterOffFile = Path.Combine(outDir, "10_tester_meter_off.png");
                    File.WriteAllBytes(meterOffFile, tex.EncodeToPNG());
                    Debug.Log($"{LOG_PREFIX}PREVIEW {meterOffFile}");

                    meter.SetPoweredOn(222f, 0f, 0f);
                    cam.Render();
                    tex.ReadPixels(new Rect(0, 0, PREVIEW_WIDTH, PREVIEW_HEIGHT), 0, 0);
                    tex.Apply(false, false);
                    string meterOnFile = Path.Combine(outDir, "11_tester_meter_live.png");
                    File.WriteAllBytes(meterOnFile, tex.EncodeToPNG());
                    Debug.Log($"{LOG_PREFIX}PREVIEW {meterOnFile}");

                    meter.SetPoweredOff();
                    break;
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

        private static void InspectWorkshopShell(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name != "MESH_WorkshopShell") continue;
                    foreach (MeshRenderer mr in t.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        MeshFilter mf = mr.GetComponent<MeshFilter>();
                        if (mf == null || mf.sharedMesh == null) continue;
                        Bounds b = mr.bounds;
                        int tris = mf.sharedMesh.triangles.Length / 3;
                        Debug.Log($"{LOG_PREFIX}SHELL '{mr.name}' Tris={tris} WorldMin={b.min} WorldMax={b.max} Size={b.size}");
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
