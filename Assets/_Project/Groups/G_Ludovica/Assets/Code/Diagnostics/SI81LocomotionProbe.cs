using System.Text;
using SI81.Input;
using SI81.Rig;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace G_Ludovica.ForkliftChaos
{
    /// <summary>
    /// DIAGNOSTICA TEMPORANEA della locomozione. Si crea da sola a runtime: nessuna scena condivisa viene modificata.
    /// In Editor: logga nella Console e mostra un overlay nella Game view. Menu SI81/Debug per dump e toggle.
    /// DA RIMUOVERE dopo la diagnosi.
    /// </summary>
    public sealed class SI81LocomotionProbe : MonoBehaviour
    {
        private const string PREFIX = "[SI81-PROBE]";
        private const float INTERVALLO = 1f;

        private float prossimoLog;
        private string ultimaRiga = "in attesa...";
        private bool mostraOverlay = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreaSonda()
        {
            if (Object.FindFirstObjectByType<SI81LocomotionProbe>() != null) return;
            GameObject go = new GameObject("DIAG_SI81_LocomotionProbe");
            go.AddComponent<SI81LocomotionProbe>();
            Object.DontDestroyOnLoad(go);
            Debug.Log(PREFIX + " sonda attiva. LEGENDA: lock=MovementLock | supp=World sospeso | "
                            + "locoEn=action abilitate | rawMove=action grezza | readMove=valore usato dal rig | "
                            + "devL/devR=stick letti dal dispositivo | rigPos=posizione radice rig");
        }

        private void Update()
        {
            if (Time.unscaledTime < prossimoLog) return;
            prossimoLog = Time.unscaledTime + INTERVALLO;
            DumpOra();
        }

        /// <summary>Stampa subito lo stato corrente (usabile anche dal menu SI81/Debug).</summary>
        public static void DumpOra()
        {
            SI81LocomotionProbe sonda = Object.FindFirstObjectByType<SI81LocomotionProbe>();
            if (sonda == null) return;
            sonda.ultimaRiga = sonda.CostruisciRiga();
            Debug.Log(sonda.ultimaRiga);
        }

        private string CostruisciRiga()
        {
            StringBuilder sb = new StringBuilder(768);
            sb.Append(PREFIX).Append(" scene=").Append(NomiScene());

            RigRegistration rig = RigRegistration.Instance;
            RigLocomotion loco = Object.FindFirstObjectByType<RigLocomotion>();
            CharacterController cc = rig != null ? rig.GetComponent<CharacterController>() : null;
            InputService input = InputService.Instance;

            sb.Append(" | ts=").Append(Time.timeScale.ToString("F2"));
            sb.Append(" rig=").Append(rig != null ? rig.name : "NULL");
            sb.Append(" loco=").Append(loco != null ? "yes" : "NO");
            sb.Append(" lock=").Append(loco != null && loco.MovementLock);
            sb.Append(" cc=").Append(cc != null ? (cc.enabled ? "on" : "OFF") : "NULL");
            sb.Append(" grounded=").Append(cc != null && cc.isGrounded);
            if (rig != null) sb.Append(" rigPos=").Append(Fmt(rig.transform.position));

            if (loco != null)
            {
                System.Reflection.BindingFlags bf = System.Reflection.BindingFlags.NonPublic
                                                    | System.Reflection.BindingFlags.Instance;
                System.Reflection.FieldInfo fSpeed = typeof(RigLocomotion).GetField("moveSpeed", bf);
                System.Reflection.FieldInfo fDead = typeof(RigLocomotion).GetField("deadzone", bf);
                System.Reflection.FieldInfo fRig = typeof(RigLocomotion).GetField("rig", bf);
                System.Reflection.FieldInfo fVv = typeof(RigLocomotion).GetField("verticalVelocity", bf);

                sb.Append(" locoOn=").Append(loco.enabled);
                if (fSpeed != null) sb.Append(" speed=").Append(fSpeed.GetValue(loco));
                if (fDead != null) sb.Append(" dead=").Append(fDead.GetValue(loco));
                RigRegistration rigField = fRig != null ? fRig.GetValue(loco) as RigRegistration : null;
                sb.Append(" rigEq=").Append(rigField != null && rigField == RigRegistration.Instance);
                if (fVv != null) sb.Append(" vv=").Append(fVv.GetValue(loco));

                System.Reflection.MethodInfo metodo = typeof(RigLocomotion).GetMethod("ComputeMoveVelocity", bf);
                if (metodo != null && input != null)
                    sb.Append(" vel=").Append(Fmt((Vector3)metodo.Invoke(loco, new object[] { input.ReadMove() })));
            }
            if (cc != null)
            {
                sb.Append(" ccVel=").Append(Fmt(cc.velocity));
                sb.Append(" ccPar=").Append(cc.radius.ToString("F2")).Append('/')
                  .Append(cc.height.ToString("F2")).Append("/slope").Append(cc.slopeLimit);
                if (rig != null)
                {
                    Collider[] hits = Physics.OverlapSphere(rig.transform.position + Vector3.up * 0.9f,
                        0.6f, ~0, QueryTriggerInteraction.Ignore);
                    StringBuilder names = new StringBuilder();
                    foreach (Collider hit in hits)
                    {
                        if (hit == cc) continue;
                        if (names.Length > 0) names.Append(',');
                        names.Append(hit.name);
                    }
                    sb.Append(" near=[").Append(names).Append(']');
                }
            }

            sb.Append(" || input=").Append(input != null ? "yes" : "NULL");
            if (input != null)
            {
                sb.Append(" supp=").Append(input.IsWorldSuppressed);
                sb.Append(" locoEn=").Append(input.IsLocomotionEnabled);

                InputAction move = input.WorldMap != null
                    ? input.WorldMap.FindAction(InputBindings.Move, false) : null;
                InputAction turn = input.WorldMap != null
                    ? input.WorldMap.FindAction(InputBindings.Turn, false) : null;

                sb.Append(" moveAct=").Append(move != null ? (move.enabled ? "on" : "OFF") : "NULL");
                sb.Append(" turnAct=").Append(turn != null ? (turn.enabled ? "on" : "OFF") : "NULL");
                sb.Append(" rawMove=").Append(move != null ? Fmt(move.ReadValue<Vector2>()) : "-");
                sb.Append(" rawTurn=").Append(turn != null ? Fmt(turn.ReadValue<Vector2>()) : "-");
                sb.Append(" readMove=").Append(Fmt(input.ReadMove()));
                sb.Append(" readTurn=").Append(Fmt(input.ReadTurn()));
            }

            sb.Append(" || ").Append(StickDispositivi());
            return sb.ToString();
        }

        /// <summary>Valore grezzo dei due stick letto direttamente dai dispositivi XR.</summary>
        private static string StickDispositivi()
        {
            XRController left = XRController.leftHand;
            XRController right = XRController.rightHand;
            Vector2Control ls = left != null ? left.TryGetChildControl<Vector2Control>("primary2DAxis") : null;
            Vector2Control rs = right != null ? right.TryGetChildControl<Vector2Control>("primary2DAxis") : null;
            return "devL=" + (ls != null ? Fmt(ls.ReadValue()) : "NONE")
                 + " devR=" + (rs != null ? Fmt(rs.ReadValue()) : "NONE");
        }

        private void OnGUI()
        {
            if (!mostraOverlay) return;
            GUI.Box(new Rect(8f, 8f, 1180f, 58f), GUIContent.none);
            GUI.Label(new Rect(18f, 14f, 1160f, 46f), ultimaRiga);
        }

        private static string NomiScene()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene s = SceneManager.GetSceneAt(i);
                if (i > 0) sb.Append('+');
                sb.Append(s.name);
                if (!s.isLoaded) sb.Append("(!)");
            }
            return sb.ToString();
        }

        private static string Fmt(Vector2 v)
        {
            return "(" + v.x.ToString("F2") + "," + v.y.ToString("F2") + ")";
        }

        private static string Fmt(Vector3 v)
        {
            return "(" + v.x.ToString("F2") + "," + v.y.ToString("F2") + "," + v.z.ToString("F2") + ")";
        }

#if UNITY_EDITOR
        [MenuItem("SI81/Debug/Log locomozione ora")]
        private static void MenuDump()
        {
            DumpOra();
        }

        [MenuItem("SI81/Debug/Probe overlay: on/off")]
        private static void MenuOverlay()
        {
            SI81LocomotionProbe sonda = Object.FindFirstObjectByType<SI81LocomotionProbe>();
            if (sonda != null) sonda.mostraOverlay = !sonda.mostraOverlay;
        }
#endif
    }
}
