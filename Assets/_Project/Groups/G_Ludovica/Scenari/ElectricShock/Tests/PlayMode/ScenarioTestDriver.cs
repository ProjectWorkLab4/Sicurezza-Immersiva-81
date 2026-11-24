using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SI81.Ludovica.TempCore;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SI81.Ludovica.ElectricShock.Tests
{
    /// <summary>
    /// Pilota dello scenario per i test Play Mode: carica la scena (che carica da sola il rig temporaneo, modalità desktop),
    /// legge i campi privati dello scenario e simula le azioni del giocatore senza input reali.
    /// </summary>
    public class ScenarioTestDriver
    {
        /// <summary>Percorso della scena dello scenario.</summary>
        public const string SCENE = "Assets/_Project/Groups/G_Ludovica/Scenari/ElectricShock/SCN_ElectricShock.unity";
        private const BindingFlags NP = BindingFlags.NonPublic | BindingFlags.Instance;

        /// <summary>Scenario in corso.</summary>
        public ElectricShockScenario Scenario => Object.FindFirstObjectByType<ElectricShockScenario>();
        /// <summary>Rig temporaneo.</summary>
        public TempRig Rig => TempRig.Instance;

        /// <summary>Carica la scena da zero (Guided) e aspetta che lo scenario parta.</summary>
        public IEnumerator Load()
        {
#if UNITY_EDITOR
            MissionSession.Abort();
            var op = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(SCENE, new LoadSceneParameters(LoadSceneMode.Single));
            while (!op.isDone) yield return null;
            yield return WaitRunning();
#else
            Assert.Ignore("I test dello scenario girano solo nell'Editor.");
            yield break;
#endif
        }

        /// <summary>Aspetta che uno scenario sia in corso (max 10 s reali).</summary>
        public IEnumerator WaitRunning()
        {
            float until = Time.realtimeSinceStartup + 10f;
            while (Time.realtimeSinceStartup < until)
            {
                var s = Scenario;
                if (s != null && s.IsRunning && Rig != null) yield break;
                yield return null;
            }
            Assert.Fail("Lo scenario non è partito entro 10 s");
        }

        /// <summary>Aspetta la fine della missione (max secondi di gioco) e restituisce il risultato.</summary>
        public IEnumerator WaitEnd(float maxGameSeconds)
        {
            float until = Time.time + maxGameSeconds;
            while (Scenario.IsRunning && Time.time < until) yield return null;
        }

        /// <summary>Legge un campo privato dello scenario.</summary>
        public T Get<T>(string field) => (T)typeof(ElectricShockScenario).GetField(field, NP).GetValue(Scenario);

        /// <summary>Scrive un campo privato dello scenario.</summary>
        public void Set(string field, object value) => typeof(ElectricShockScenario).GetField(field, NP).SetValue(Scenario, value);

        /// <summary>Il giocatore parla (B/Y o Q).</summary>
        public void Shout() => typeof(ElectricShockScenario).GetMethod("OnShout", NP).Invoke(Scenario, new object[] { Rig.Head.position, Rig.Head.forward });

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

        /// <summary>Esito dell'ultima missione chiusa.</summary>
        public OutcomeType? Outcome => MissionSession.LastResult?.Outcome;

        /// <summary>true se il Registro delle scelte contiene l'ID.</summary>
        public bool Logged(string id) => Scenario.Log.Has(id);
    }
}
