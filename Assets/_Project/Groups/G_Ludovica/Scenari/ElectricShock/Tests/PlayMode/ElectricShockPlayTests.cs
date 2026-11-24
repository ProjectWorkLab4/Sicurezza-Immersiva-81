using System.Collections;
using System.Linq;
using NUnit.Framework;
using SI81.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace SI81.Ludovica.ElectricShock.Tests
{
    /// <summary>
    /// Test Play Mode dello scenario 10: percorsi completi ed esiti (specifica §7-§8). Il tempo di gioco scorre 3 volte più veloce.
    /// </summary>
    public class ElectricShockPlayTests
    {
        private const float TIME_SCALE = 3f;
        private ScenarioTestDriver d;

        /// <summary>Carica la scena e accelera il tempo.</summary>
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            d = new ScenarioTestDriver();
            yield return d.Load();
            Time.timeScale = TIME_SCALE;
        }

        /// <summary>Ripristina il tempo e lascia la mano vuota.</summary>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            if (d.Rig != null) d.Rig.RightHand.Release();
            yield return null;
        }

        /// <summary>Percorso corretto in Guided: Positivo, tutte le scelte corrette, nessun errore.</summary>
        [UnityTest]
        public IEnumerator CorrectPath_IsPositive()
        {
            yield return d.ObserveScene();
            d.Shout();
            d.CloseBarriers();
            d.Get<EmergencyRadio>("radio").Call();
            var powder = d.Extinguisher(ExtinguisherAgent.Powder);
            d.Grab(powder);
            powder.ReadLabel();
            d.Get<EmergencyStopButton>("emergencyStop").Press();
            yield return new WaitForSeconds(2.5f);
            Assert.IsTrue(d.Get<FireSource>("fire").IsBurning, "La pozza deve accendersi 2 s dopo l'arresto");
            yield return d.SprayFire(powder, 2f, 6f);
            Assert.IsTrue(d.Get<FireSource>("fire").WasExtinguished);
            d.Rig.RightHand.Release();

            var victim = d.Get<InjuredPerson>("victim");
            d.Teleport(16f, 6.6f, 0f);
            victim.CheckConsciousness();
            victim.PlaceInPosition();
            yield return new WaitForSeconds(11f);
            d.Get<ParamedicInShock>("paramedicInShock").FollowTarget(d.Rig.Head);
            d.Rig.TeleportTo(d.Get<Transform>("safePoint").position, 0f);
            yield return new WaitForSeconds(12f);
            Assert.IsTrue(d.Get<ParamedicInShock>("paramedicInShock").IsSafe, "PM-1 deve arrivare al punto sicuro");

            d.Set("paramedicArrivalTime", d.Scenario.ElapsedSeconds + 0.5f);
            yield return d.WaitEnd(40f);
            Assert.AreEqual(OutcomeType.Positive, d.Outcome);
            foreach (var id in new[] { "LG-01", "LG-03", "LG-05", "LG-06", "LG-09", "LG-12", "LG-13", "LG-14", "LG-18", "LG-20" })
                Assert.IsTrue(d.Logged(id), id + " non registrato");
            Assert.IsFalse(d.Scenario.Log.Entries.Any(e => e.Outcome != ChoiceOutcome.Correct), "Nessuna scelta sbagliata nel percorso corretto");
        }

        /// <summary>Toccare il collega con la corrente presente: Negativo (LG-08).</summary>
        [UnityTest]
        public IEnumerator TouchVictimWithPowerOn_IsNegative()
        {
            d.Get<InjuredPerson>("victim").OnContact(d.Rig.RightHand);
            yield return null;
            Assert.AreEqual(OutcomeType.Negative, d.Outcome);
            Assert.IsTrue(d.Logged("LG-08"));
        }

        /// <summary>Toccare il cavo sotto tensione: Negativo (LG-24); dopo l'arresto invece non succede nulla.</summary>
        [UnityTest]
        public IEnumerator TouchCable_OnlyDangerousWithPower()
        {
            d.Get<EmergencyStopButton>("emergencyStop").Press();
            d.Get<LiveCable>("cable").OnContact(d.Rig.RightHand);
            yield return null;
            Assert.IsTrue(d.Scenario.IsRunning, "Senza corrente il cavo non è pericoloso");
            Assert.IsFalse(d.Logged("LG-24"));
        }

        /// <summary>Estintore ad acqua sul fuoco: Negativo (LG-11).</summary>
        [UnityTest]
        public IEnumerator WaterExtinguisher_IsNegative()
        {
            var water = d.Extinguisher(ExtinguisherAgent.Water);
            d.Grab(water);
            d.Get<EmergencyStopButton>("emergencyStop").Press();
            yield return new WaitForSeconds(2.5f);
            yield return d.SprayFire(water, 2f, 1.5f);
            Assert.AreEqual(OutcomeType.Negative, d.Outcome);
            Assert.IsTrue(d.Logged("LG-11"));
        }

        /// <summary>Spruzzo con l'ugello già dentro la zona del fuoco: il fuoco si spegne lo stesso.</summary>
        [UnityTest]
        public IEnumerator CloseRangeSpray_Extinguishes()
        {
            var co2 = d.Extinguisher(ExtinguisherAgent.CO2);
            d.Grab(co2);
            d.Get<EmergencyStopButton>("emergencyStop").Press();
            yield return new WaitForSeconds(2.5f);
            yield return d.SprayFire(co2, 0.6f, 6f);
            Assert.IsTrue(d.Get<FireSource>("fire").WasExtinguished);
        }

        /// <summary>Colleghi lasciati fare con la corrente presente: nuova vittima, Negativo (LG-15).</summary>
        [UnityTest]
        public IEnumerator IgnoredColleagues_TouchVictim_IsNegative()
        {
            d.Set("bystanderAttemptTime", d.Scenario.ElapsedSeconds + 0.5f);
            yield return d.WaitEnd(30f);
            Assert.AreEqual(OutcomeType.Negative, d.Outcome);
            Assert.IsTrue(d.Logged("LG-15"));
        }

        /// <summary>Gridare durante l'esitazione ferma i colleghi, che escono fino al punto di raccolta (LG-14).</summary>
        [UnityTest]
        public IEnumerator ShoutStopsColleagues_TheyEvacuate()
        {
            d.Set("bystanderAttemptTime", d.Scenario.ElapsedSeconds + 0.2f);
            yield return new WaitForSeconds(2f);
            d.Teleport(14f, 5f, 90f);
            d.Shout();
            yield return new WaitForSeconds(20f);
            Assert.IsTrue(d.Scenario.IsRunning);
            Assert.IsTrue(d.Logged("LG-14"));
            Assert.IsTrue(d.Get<Bystander[]>("bystanders").All(b => b.Current == Bystander.State.Evacuated));
        }

        /// <summary>
        /// PM-2 con varco chiuso e zona non sicura si ferma; quando la zona è sicura entra (le barriere si aprono e si richiudono),
        /// porta via PM-1 e raggiunge il collega.
        /// </summary>
        [UnityTest]
        public IEnumerator Pm2_WaitsAtClosedBarrier_ThenLeadsPm1()
        {
            d.CloseBarriers();
            var barrier = Object.FindObjectsByType<BarrierPanel>(FindObjectsSortMode.None).First(b => b.SlotIndex == 0);
            Quaternion closed = barrier.transform.rotation;
            d.Set("paramedicArrivalTime", d.Scenario.ElapsedSeconds + 0.2f);
            yield return new WaitForSeconds(12f);
            Assert.AreEqual("WaitingAtGate", d.Get<object>("pm2").ToString());

            var powder = d.Extinguisher(ExtinguisherAgent.Powder);
            d.Grab(powder);
            d.Get<EmergencyStopButton>("emergencyStop").Press();
            yield return new WaitForSeconds(2.5f);
            yield return d.SprayFire(powder, 2f, 6f);
            d.Rig.RightHand.Release();
            d.Teleport(10f, 12f, 0f);

            float maxOpen = 0f;
            var pm1 = d.Get<ParamedicInShock>("paramedicInShock");
            float until = Time.time + 40f;
            while (Time.time < until && d.Get<object>("pm2").ToString() != "AtVictim" && d.Scenario.IsRunning)
            {
                maxOpen = Mathf.Max(maxOpen, Quaternion.Angle(closed, barrier.transform.rotation));
                yield return null;
            }
            Assert.Greater(maxOpen, 60f, "Le barriere devono aprirsi per far passare PM-2");
            Assert.IsTrue(pm1.IsSafe, "PM-2 deve portare PM-1 al punto sicuro");
            Assert.AreEqual("pm2", pm1.SavedBy);
            yield return new WaitForSeconds(2f);
            Assert.Less(Quaternion.Angle(closed, barrier.transform.rotation), 1f, "Le barriere devono richiudersi");
            Assert.IsTrue(d.Get<BarrierSnapLine>("barrierLine").IsClosed);
        }

        /// <summary>Varco aperto e zona non sicura all'arrivo di PM-2: seconda vittima, Negativo (LG-23).</summary>
        [UnityTest]
        public IEnumerator Pm2_EntersOpenGateWhileUnsafe_IsNegative()
        {
            d.Set("paramedicArrivalTime", d.Scenario.ElapsedSeconds + 0.2f);
            yield return d.WaitEnd(20f);
            Assert.AreEqual(OutcomeType.Negative, d.Outcome);
            Assert.IsTrue(d.Logged("LG-04"));
            Assert.IsTrue(d.Logged("LG-23"));
        }

        /// <summary>Esplosione con il giocatore vicino al generatore: Negativo (LG-17).</summary>
        [UnityTest]
        public IEnumerator Explosion_PlayerInsideRadius_IsNegative()
        {
            d.Get<EmergencyStopButton>("emergencyStop").Press();
            yield return new WaitForSeconds(2.5f);
            d.Set("explosionDelay", d.Scenario.ElapsedSeconds - d.Get<float>("ignitedAt") + 2f);
            d.Teleport(19f, 7f, 0f);
            yield return d.WaitEnd(10f);
            Assert.AreEqual(OutcomeType.Negative, d.Outcome);
            Assert.IsTrue(d.Logged("LG-17"));
        }

        /// <summary>Esplosione con il collega trascinato fuori dal raggio durante l'avviso: nessun ferito, Parziale, LG-16 corretto.</summary>
        [UnityTest]
        public IEnumerator Explosion_VictimDraggedOut_IsPartial()
        {
            d.Teleport(15f, 5f, 90f);
            d.Shout();
            d.Get<EmergencyStopButton>("emergencyStop").Press();
            yield return new WaitForSeconds(2.5f);
            d.Set("explosionDelay", d.Scenario.ElapsedSeconds - d.Get<float>("ignitedAt") + 13f);
            yield return new WaitForSeconds(2f);
            d.Teleport(14.8f, 6.6f, 270f);
            yield return null;
            d.Rig.Head.localRotation = Quaternion.identity;
            var victim = d.Get<InjuredPerson>("victim");
            victim.OnUseStart(d.Rig.RightHand);
            yield return new WaitForSeconds(4f);
            victim.OnUseEnd(d.Rig.RightHand);
            Assert.IsTrue(victim.WasMoved);
            yield return d.WaitEnd(20f);
            Assert.AreEqual(OutcomeType.Partial, d.Outcome);
            var lg16 = d.Scenario.Log.Entries.First(e => e.ActionId == "LG-16");
            Assert.AreEqual(ChoiceOutcome.Correct, lg16.Outcome);
        }

        /// <summary>PM-1 che segue il giocatore gira attorno al generatore invece di attraversarlo (NavMesh).</summary>
        [UnityTest]
        public IEnumerator Pm1_FollowsAroundGenerator()
        {
            d.Get<EmergencyStopButton>("emergencyStop").Press();
            var pm1 = d.Get<ParamedicInShock>("paramedicInShock");
            var generatorBounds = new Bounds(new Vector3(20f, 0.5f, 9.5f), new Vector3(1.8f, 2f, 1.0f));
            d.Teleport(20f, 12.5f, 180f);
            pm1.FollowTarget(d.Rig.Head);
            bool crossed = false;
            float until = Time.time + 20f;
            while (Time.time < until)
            {
                var p = pm1.transform.position;
                if (generatorBounds.Contains(new Vector3(p.x, 0.5f, p.z))) crossed = true;
                yield return null;
            }
            Assert.IsFalse(crossed, "PM-1 ha attraversato il generatore");
            Assert.Less(Vector3.Distance(pm1.transform.position, new Vector3(20f, 0f, 12.5f)), 2.5f, "PM-1 deve raggiungere il giocatore");
        }
    }
}
