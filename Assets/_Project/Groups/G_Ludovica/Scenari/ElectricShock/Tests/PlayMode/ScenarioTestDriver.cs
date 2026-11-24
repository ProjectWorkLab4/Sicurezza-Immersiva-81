using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SI81.Ludovica.TempCore;
using SI81.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SI81.Ludovica.ElectricShock.Tests
{
    /// <summary>
    /// Pilota dello scenario per i test Play Mode: carica la scena (che carica da sola SCN_Core con la UI comune),
    /// crea un rig di prova senza camera (solo per simulare le azioni del giocatore) e legge i campi privati dello scenario.
    /// </summary>
    public class ScenarioTestDriver
    {
        /// <summary>Percorso della scena dello scenario.</summary>
        public const string SCENE = "Assets/_Project/Groups/G_Ludovica/Scenari/ElectricShock/SCN_ElectricShock.unity";
        private const BindingFlags NP = BindingFlags.NonPublic | BindingFlags.Instance;

        private ElectricShockScenario scenario;

        /// <summary>Scenario caricato (resta leggibile anche dopo lo scaricamento della scena).</summary>
        public ElectricShockScenario Scenario => scenario;
        /// <summary>Rig di prova.</summary>
        public TempRig Rig => TempRig.Instance;

        /// <summary>Carica la scena da zero (Guided), aspetta lo scenario e crea il rig di prova.</summary>
        public IEnumerator Load()
        {
#if UNITY_EDITOR
            MissionSession.Clear();
            var op = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(SCENE, new LoadSceneParameters(LoadSceneMode.Single));
            while (!op.isDone) yield return null;
            CreateTestRig();
            float until = Time.realtimeSinceStartup + 20f;
            while (Time.realtimeSinceStartup < until)
            {
                scenario = Object.FindFirstObjectByType<ElectricShockScenario>();
                if (scenario != null && scenario.IsRunning) yield break;
                yield return null;
            }
            Assert.Fail("Lo scenario non è partito entro 20 s (SCN_Core caricata?)");
#else
            Assert.Ignore("I test dello scenario girano solo nell'Editor.");
            yield break;
#endif
        }

        /// <summary>
        /// Crea il rig temporaneo senza camera né AudioListener (la camera è quella di SCN_Core): serve solo come "corpo"
        /// del giocatore per osservare, afferrare e parlare nei test.
        /// </summary>
        private static void CreateTestRig()
        {
            if (TempRig.Instance != null) return;
            var go = new GameObject("TEST_TempRig");
            go.SetActive(false);
            go.transform.position = new Vector3(8f, 0f, 8f);
            go.AddComponent<CharacterController>();
            var head = new GameObject("Head").transform;
            head.SetParent(go.transform, false);
            var left = new GameObject("LeftHand").AddComponent<TempHand>();
            left.transform.SetParent(go.transform, false);
            typeof(TempHand).GetField("isLeft", NP).SetValue(left, true);
            var right = new GameObject("RightHand").AddComponent<TempHand>();
            right.transform.SetParent(go.transform, false);
            var rig = go.AddComponent<TempRig>();
            typeof(TempRig).GetField("head", NP).SetValue(rig, head);
            typeof(TempRig).GetField("leftHand", NP).SetValue(rig, left);
            typeof(TempRig).GetField("rightHand", NP).SetValue(rig, right);
            go.SetActive(true);
        }

        /// <summary>Aspetta la fine della missione (max secondi di gioco).</summary>
        public IEnumerator WaitEnd(float maxGameSeconds)
        {
            float until = Time.time + maxGameSeconds;
            while (scenario != null && scenario.IsRunning && Time.time < until) yield return null;
        }

        /// <summary>Legge un campo privato dello scenario.</summary>
        public T Get<T>(string field) => (T)typeof(ElectricShockScenario).GetField(field, NP).GetValue(scenario);

        /// <summary>Scrive un campo privato dello scenario.</summary>
        public void Set(string field, object value) => typeof(ElectricShockScenario).GetField(field, NP).SetValue(scenario, value);

        /// <summary>Il giocatore parla (B/Y o Q).</summary>
        public void Shout() => typeof(ElectricShockScenario).GetMethod("OnShout", NP).Invoke(scenario, new object[] { Rig.Head.position, Rig.Head.forward });

        /// <summary>La mano destra afferra l'oggetto.</summary>
        public void Grab(TempInteractable item) => typeof(TempHand).GetMethod("TryGrab", NP).Invoke(Rig.RightHand, new object[] { item });

        /// <summary>Estintore del tipo indicato.</summary>
        public Extinguisher Extinguisher(ExtinguisherAgent agent) => Get<Extinguisher[]>("extinguishers").First(e => e.Agent == agent);

        /// <summary>Porta il giocatore in un punto della mappa (x, y) guardando verso yaw.</summary>
        public void Teleport(float x, float y, float yaw) => Rig.TeleportTo(new Vector3(x, 0f, y), yaw);

        /// <summary>Guarda cavo e pozza abbastanza a lungo da osservarli (LG-01).</summary>
        public IEnumerator ObserveScene()
        {
            Teleport(17f, 6f, 90f);
            yield return null;
            Rig.Head.LookAt(Get<LiveCable>("cable").transform.position);
            yield return new WaitForSeconds(1.2f);
            Rig.Head.LookAt(Get<Transform>("fuelPool").position);
            yield return new WaitForSeconds(1.2f);
        }

        /// <summary>Chiude il varco con le due barriere.</summary>
        public void CloseBarriers()
        {
            var line = Get<BarrierSnapLine>("barrierLine");
            var b = Object.FindObjectsByType<BarrierPanel>(FindObjectsSortMode.None).OrderBy(x => x.name).ToArray();
            line.Place(b[0], 0);
            line.Place(b[1], 1);
        }

        /// <summary>Si mette accanto al fuoco con l'estintore in mano e spruzza per alcuni secondi.</summary>
        public IEnumerator SprayFire(Extinguisher ext, float distance, float seconds)
        {
            var fire = Get<FireSource>("fire").transform.position;
            Rig.TeleportTo(new Vector3(fire.x - distance, 0f, fire.z - 0.3f), 90f);
            yield return null;
            Rig.Head.LookAt(fire + Vector3.up * 0.3f);
            ext.OnUseStart(Rig.RightHand);
            yield return new WaitForSeconds(seconds);
            ext.OnUseEnd(Rig.RightHand);
        }

        /// <summary>Esito dell'ultima missione chiusa (dalla sessione della UI comune).</summary>
        public OutcomeType? Outcome => MissionSession.LastResult != null ? MissionSession.LastResult.Outcome : (OutcomeType?)null;

        /// <summary>true se il Registro delle scelte contiene l'ID.</summary>
        public bool Logged(string id) => scenario.Log.Has(id);
    }
}
