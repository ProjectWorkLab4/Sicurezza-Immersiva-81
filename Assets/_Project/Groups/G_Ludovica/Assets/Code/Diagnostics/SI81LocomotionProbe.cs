using System.Text;
using SI81.Input;
using SI81.Rig;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;

namespace G_Ludovica.ForkliftChaos
{
    /// <summary>
    /// DIAGNOSTICA TEMPORANEA della locomozione. Si crea da sola a runtime: nessuna scena condivisa viene modificata.
    /// Logga ogni secondo con il prefisso [SI81-PROBE], sia nel mock sia nella missione 07.
    /// DA RIMUOVERE dopo la diagnosi.
    /// </summary>
    public sealed class SI81LocomotionProbe : MonoBehaviour
    {
        private const string PREFIX = "[SI81-PROBE]";
        private const float INTERVALLO = 1f;

        private float prossimoLog;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreaSonda()
        {
            if (Object.FindFirstObjectByType<SI81LocomotionProbe>() != null) return;
            GameObject go = new GameObject("DIAG_SI81_LocomotionProbe");
            go.AddComponent<SI81LocomotionProbe>();
            Object.DontDestroyOnLoad(go);
        }

        private void Update()
        {
            if (Time.unscaledTime < prossimoLog) return;
            prossimoLog = Time.unscaledTime + INTERVALLO;
            LogStato();
        }

        private static void LogStato()
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
            Debug.Log(sb.ToString());
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
    }
}
