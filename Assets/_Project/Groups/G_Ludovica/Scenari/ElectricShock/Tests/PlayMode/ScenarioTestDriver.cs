using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SI81.Input;
using SI81.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SI81.Ludovica.ElectricShock.Tests
{
    /// <summary>
    /// Pilota dello scenario per i test Play Mode: carica la scena (che carica da sola SCN_Core con UI e rig condiviso),
    /// sostituisce testa e mano del giocatore con trasform di prova (PlayerLocator) e simula grilletto / grip chiamando
    /// gli stessi metodi che usa il raggio condiviso (BeginSelect / BeginGrab).
    /// </summary>
    public class ScenarioTestDriver
    {
        /// <summary>Percorso della scena dello scenario.</summary>
        public const string SCENE = "Assets/_Project/Groups/G_Ludovica/Scenari/ElectricShock/SCN_ElectricShock.unity";
        private const BindingFlags NP = BindingFlags.NonPublic | BindingFlags.Instance;

        private ElectricShockScenario scenario;
        private Transform body;

        /// <summary>Scenario caricato (resta leggibile anche dopo lo scaricamento della scena).</summary>
        public ElectricShockScenario Scenario => scenario;
        /// <summary>Testa di prova del giocatore.</summary>
        public Transform Head { get; private set; }
        /// <summary>Mano destra di prova (davanti alla testa, segue lo sguardo).</summary>
        public Transform RightHand { get; private set; }

        /// <summary>Carica la scena da zero (Guided), aspetta lo scenario e crea il giocatore di prova.</summary>
        public IEnumerator Load()
        {
#if UNITY_EDITOR
            MissionSession.Clear();
            PlayerLocator.SetOverride(null, null, null);
            var op = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(SCENE, new LoadSceneParameters(LoadSceneMode.Single));
            while (!op.isDone) yield return null;
            CreateTestPlayer();
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

        /// <summary>Toglie il giocatore di prova.</summary>
        public void Unload()
        {
            PlayerLocator.SetOverride(null, null, null);
            if (body != null) Object.Destroy(body.gameObject);
        }

        /// <summary>Corpo, testa (1,65 m) e mani di prova, senza camera: la camera è quella del rig di SCN_Core.</summary>
        private void CreateTestPlayer()
        {
            body = new GameObject("TEST_Player").transform;
            body.position = new Vector3(8f, 0f, 8f);
            Head = new GameObject("Head").transform;
            Head.SetParent(body, false);
            Head.localPosition = new Vector3(0f, 1.65f, 0f);
            RightHand = new GameObject("RightHand").transform;
            RightHand.SetParent(Head, false);
            RightHand.localPosition = new Vector3(0.2f, -0.2f, 0.55f);
            var left = new GameObject("LeftHand").transform;
            left.SetParent(Head, false);
            left.localPosition = new Vector3(-0.2f, -0.2f, 0.55f);
            PlayerLocator.SetOverride(Head, left, RightHand);
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

        /// <summary>Il giocatore parla (come il grilletto su un collega).</summary>
        public void Shout() => ShoutTarget.RaiseShout(Head.position, Head.forward);

        /// <summary>La mano destra afferra l'oggetto (grip).</summary>
        public void Grab(ScenarioInteractable item) => item.BeginGrab(Hand.Right);

        /// <summary>La mano destra lascia l'oggetto.</summary>
        public void Release(ScenarioInteractable item) => item.EndGrab(Hand.Right);

        /// <summary>Estintore del tipo indicato.</summary>
        public Extinguisher Extinguisher(ExtinguisherAgent agent) => Get<Extinguisher[]>("extinguishers").First(e => e.Agent == agent);

        /// <summary>Porta il giocatore in un punto della mappa (x, y) guardando verso yaw.</summary>
        public void Teleport(float x, float y, float yaw)
        {
            body.SetPositionAndRotation(new Vector3(x, 0f, y), Quaternion.Euler(0f, yaw, 0f));
            Head.localRotation = Quaternion.identity;
        }

        /// <summary>Guarda cavo e pozza abbastanza a lungo da osservarli (LG-01).</summary>
        public IEnumerator ObserveScene()
        {
            Teleport(17f, 6f, 90f);
            yield return null;
            Head.LookAt(Get<LiveCable>("cable").transform.position);
            yield return new WaitForSeconds(1.2f);
            Head.LookAt(Get<Transform>("fuelPool").position);
            yield return new WaitForSeconds(1.2f);
        }

        /// <summary>Chiude il varco con le barriere.</summary>
        public void CloseBarriers()
        {
            var line = Get<BarrierSnapLine>("barrierLine");
            var b = Object.FindObjectsByType<BarrierPanel>(FindObjectsSortMode.None).OrderBy(x => x.name).ToArray();
            for (int i = 0; i < b.Length; i++) line.Place(b[i], i);
        }

        /// <summary>Si mette accanto al fuoco con l'estintore in mano e spruzza per alcuni secondi.</summary>
        public IEnumerator SprayFire(Extinguisher ext, float distance, float seconds)
        {
            var fire = Get<FireSource>("fire").transform.position;
            Teleport(fire.x - distance, fire.z - 0.3f, 90f);
            yield return null;
            Head.LookAt(fire + Vector3.up * 0.3f);
            ext.StartSpray();
            yield return new WaitForSeconds(seconds);
            ext.StopSpray();
        }

        /// <summary>Ferma il generatore e chiude il varco: la pozza prende fuoco 2 s dopo (nuovo ordine).</summary>
        public void StopAndIsolate()
        {
            Get<EmergencyStopButton>("emergencyStop").Press();
            CloseBarriers();
        }

        /// <summary>
        /// Afferra il collega col grip e cammina all'indietro verso il punto (x, z del mondo) finché il collega non è entro
        /// <paramref name="radius"/> m; poi lo lascia.
        /// </summary>
        public IEnumerator DragVictimTo(Vector3 target, float radius = 1.2f, float maxSeconds = 30f)
        {
            var victim = Get<InjuredPerson>("victim");
            Vector3 v = victim.transform.position;
            Vector3 dir = new Vector3(target.x - v.x, 0f, target.z - v.z).normalized;
            // Il giocatore sta davanti al collega, dalla parte della meta, e lo guarda.
            body.position = new Vector3(v.x, 0f, v.z) + dir * 1.0f;
            Head.rotation = Quaternion.LookRotation(-dir);
            yield return null;
            victim.BeginGrab(Hand.Right);
            float until = Time.time + maxSeconds;
            while (Time.time < until)
            {
                Vector3 vp = victim.transform.position;
                if (new Vector2(vp.x - target.x, vp.z - target.z).magnitude <= radius) break;
                body.position += dir * 0.7f * Time.deltaTime;
                yield return null;
            }
            victim.EndGrab(Hand.Right);
        }

        /// <summary>Esito dell'ultima missione chiusa (dalla sessione della UI comune).</summary>
        public OutcomeType? Outcome => MissionSession.LastResult != null ? MissionSession.LastResult.Outcome : (OutcomeType?)null;

        /// <summary>true se il Registro delle scelte contiene l'ID.</summary>
        public bool Logged(string id) => scenario.Log.Has(id);
    }
}
