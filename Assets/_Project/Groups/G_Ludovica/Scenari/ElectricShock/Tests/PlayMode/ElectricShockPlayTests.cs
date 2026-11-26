using System.Collections;
using System.Linq;
using NUnit.Framework;
using SI81.Input;
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
            d.Unload();
            yield return null;
        }

        /// <summary>
        /// Percorso corretto in Guided (nuovo ordine): generatore, colleghi, 112, 3 barriere, estintore pronto, incendio spento,
        /// collega trascinato nella zona sicura, PM-1 al sicuro. Positivo, nessuna scelta sbagliata.
        /// </summary>
        [UnityTest]
        public IEnumerator CorrectPath_IsPositive()
        {
            yield return d.ObserveScene();
            d.Get<EmergencyStopButton>("emergencyStop").Press();
            Assert.IsFalse(d.Get<GeneratorUnit>("generator").IsRunning);
            d.Teleport(14f, 6f, 90f);
            d.Shout();
            d.Get<EmergencyRadio>("radio").Call();
            var powder = d.Extinguisher(ExtinguisherAgent.Powder);
            d.Grab(powder);
            powder.ReadLabel();
            yield return new WaitForSeconds(3f);
            Assert.IsFalse(d.Get<FireSource>("fire").IsBurning, "Senza varco chiuso l'incendio non parte subito");
            d.CloseBarriers();
            yield return new WaitForSeconds(2.5f);
            Assert.IsTrue(d.Get<FireSource>("fire").IsBurning, "La pozza deve accendersi 2 s dopo l'isolamento");
            yield return d.SprayFire(powder, 2f, 6f);
            Assert.IsTrue(d.Get<FireSource>("fire").WasExtinguished);
            d.Release(powder);

            var victim = d.Get<InjuredPerson>("victim");
            d.Teleport(16f, 6.6f, 0f);
            victim.BeginSelect(Hand.Right);
            Assert.IsTrue(victim.ConsciousnessChecked);
            var safe = d.Get<Transform>("safePoint").position;
            yield return d.DragVictimTo(safe);
            yield return null;
            Assert.IsTrue(d.Logged("LG-27"), "Il collega deve essere nella zona sicura");

            d.Teleport(safe.x, safe.z - 1.5f, 0f);

            d.Set("paramedicArrivalTime", d.Scenario.ElapsedSeconds + 0.5f);
            yield return d.WaitEnd(60f);
            Assert.AreEqual(OutcomeType.Positive, d.Outcome);
            foreach (var id in new[] { "LG-01", "LG-03", "LG-05", "LG-06", "LG-09", "LG-12", "LG-13", "LG-14", "LG-18", "LG-27" })
                Assert.IsTrue(d.Logged(id), id + " non registrato");
            Assert.IsFalse(d.Scenario.Log.Entries.Any(e => e.Outcome != ChoiceOutcome.Correct), "Nessuna scelta sbagliata nel percorso corretto");
        }

        /// <summary>Toccare il collega con la corrente presente: Negativo (LG-08).</summary>
        [UnityTest]
        public IEnumerator TouchVictimWithPowerOn_IsNegative()
        {
            d.Get<InjuredPerson>("victim").BeginSelect(Hand.Right);
            yield return null;
            Assert.AreEqual(OutcomeType.Negative, d.Outcome);
            Assert.IsTrue(d.Logged("LG-08"));
        }

        /// <summary>Toccare il cavo sotto tensione: Negativo (LG-24); dopo l'arresto invece non succede nulla.</summary>
        [UnityTest]
        public IEnumerator TouchCable_OnlyDangerousWithPower()
        {
            d.Get<EmergencyStopButton>("emergencyStop").Press();
            d.Get<LiveCable>("cable").BeginSelect(Hand.Right);
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
            d.StopAndIsolate();
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
            d.StopAndIsolate();
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
        /// PM-2 con varco chiuso e zona non sicura si ferma; a incendio spento entra dal passaggio (le barriere non si aprono),
        /// mette in sicurezza il generatore e poi va dal collega.
        /// </summary>
        [UnityTest]
        public IEnumerator Pm2_WaitsAtClosedBarrier_ThenSecuresGenerator()
        {
            d.CloseBarriers();
            var barrier = Object.FindObjectsByType<BarrierPanel>(FindObjectsSortMode.None).First(b => b.SlotIndex == 0);
            Quaternion closed = barrier.transform.rotation;
            d.Set("paramedicArrivalTime", d.Scenario.ElapsedSeconds + 0.2f);
            yield return new WaitForSeconds(12f);
            Assert.AreEqual("WaitingAtGate", d.Get<object>("pm2").ToString());

            var powder = d.Extinguisher(ExtinguisherAgent.Powder);
            d.Grab(powder);
            d.StopAndIsolate();
            yield return new WaitForSeconds(2.5f);
            yield return d.SprayFire(powder, 2f, 6f);
            d.Release(powder);
            d.Teleport(10f, 12f, 0f);

            float maxOpen = 0f;
            bool securing = false;
            var pm2 = d.Get<NpcMover>("arrivingParamedic");
            var line = d.Get<BarrierSnapLine>("barrierLine");
            bool crossedPanel = false;
            float until = Time.time + 60f;
            while (Time.time < until && d.Scenario.IsRunning && d.Get<object>("pm2").ToString() != "AtVictim")
            {
                maxOpen = Mathf.Max(maxOpen, Quaternion.Angle(closed, barrier.transform.rotation));
                if (d.Get<object>("pm2").ToString() == "SecuringGenerator" && pm2.GetComponent<CharacterAnimator>().CurrentKey == "Work") securing = true;
                if (!line.AllowPass(pm2.transform.position, pm2.transform.position)) crossedPanel = true;
                yield return null;
            }
            Assert.Less(maxOpen, 1f, "Le barriere posate non si aprono");
            Assert.IsFalse(crossedPanel, "PM-2 non deve passare sopra una barriera");
            Assert.IsTrue(securing, "PM-2 deve mettere in sicurezza il generatore");
            Assert.AreEqual("AtVictim", d.Get<object>("pm2").ToString(), "Poi PM-2 va dal collega");
            Assert.IsTrue(line.IsClosed);
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
            d.StopAndIsolate();
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
            d.StopAndIsolate();
            yield return new WaitForSeconds(2.5f);
            d.Set("explosionDelay", d.Scenario.ElapsedSeconds - d.Get<float>("ignitedAt") + 13f);
            yield return new WaitForSeconds(2f);
            d.Teleport(14.8f, 6.6f, 270f);
            yield return null;
            var victim = d.Get<InjuredPerson>("victim");
            victim.BeginGrab(Hand.Right);
            yield return new WaitForSeconds(4f);
            victim.EndGrab(Hand.Right);
            Assert.IsTrue(victim.WasMoved);
            yield return d.WaitEnd(20f);
            Assert.AreEqual(OutcomeType.Partial, d.Outcome);
            var lg16 = d.Scenario.Log.Entries.First(e => e.ActionId == "LG-16");
            Assert.AreEqual(ChoiceOutcome.Correct, lg16.Outcome);
        }

        /// <summary>Grilletto sull'estintore al rack legge l'etichetta; afferrato va in mano con l'ugello in avanti.</summary>
        [UnityTest]
        public IEnumerator Extinguisher_SelectReadsLabel_GrabPutsItInHand()
        {
            var co2 = d.Extinguisher(ExtinguisherAgent.CO2);
            co2.BeginSelect(Hand.Right);
            Assert.IsTrue(co2.LabelRead, "Il grilletto sull'estintore deve leggere l'etichetta");
            d.Grab(co2);
            yield return null;
            Assert.IsTrue(co2.IsHeld);
            Assert.AreEqual(d.RightHand, co2.transform.parent, "Afferrato deve stare nella mano");
            Assert.Greater(Vector3.Dot(co2.transform.forward, d.Head.forward), 0.9f, "L'ugello deve puntare dove guarda il giocatore");
            d.Release(co2);
            Assert.IsFalse(co2.IsHeld);
            Assert.AreEqual(0f, co2.transform.position.y, 0.01f, "Lasciato deve tornare dritto a terra");
        }

        /// <summary>Grilletto su un collega = il giocatore parla: i colleghi escono.</summary>
        [UnityTest]
        public IEnumerator SelectOnColleague_Shouts()
        {
            var by = d.Get<Bystander[]>("bystanders");
            d.Teleport(14f, 5f, 90f);
            by[0].GetComponent<ShoutTarget>().BeginSelect(Hand.Right);
            yield return new WaitForSeconds(1f);
            Assert.IsTrue(d.Logged("LG-14"));
            Assert.AreEqual(Bystander.State.Evacuating, by[0].Current);
        }

        /// <summary>Simula il RayInteractor condiviso: chiama i metodi interni di Interactable come fa il raggio.</summary>
        private static void Ray(Component target, string method, Hand hand)
        {
            var interactable = target.GetComponent<SI81.Interaction.Interactable>();
            Assert.IsNotNull(interactable, target.name + " senza Interactable");
            typeof(SI81.Interaction.Interactable).GetMethod(method, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(interactable, new object[] { hand });
        }

        /// <summary>
        /// Catena completa raggio condiviso -> Interactable -> oggetti dello scenario: grilletto sulla chiave ferma il generatore,
        /// grip sull'estintore lo mette in mano, permessi Select/Grab corretti per ogni oggetto.
        /// </summary>
        [UnityTest]
        public IEnumerator SharedInteractable_EventsReachScenarioObjects()
        {
            var key = d.Get<EmergencyStopButton>("emergencyStop");
            var keyI = key.GetComponent<SI81.Interaction.Interactable>();
            Assert.IsTrue(keyI.CanSelect(Hand.Right));
            Assert.IsFalse(keyI.CanGrab(Hand.Right), "La chiave non si afferra");
            Assert.IsFalse(keyI.FollowsHand);

            var barrier = Object.FindObjectsByType<BarrierPanel>(FindObjectsSortMode.None)[0].GetComponent<SI81.Interaction.Interactable>();
            Assert.IsFalse(barrier.CanSelect(Hand.Right), "Il grilletto non fa nulla sulla barriera");
            Assert.IsTrue(barrier.CanGrab(Hand.Right));

            var ext = d.Extinguisher(ExtinguisherAgent.Powder);
            Ray(ext, "GrabEnter", Hand.Right);
            yield return null;
            Assert.IsTrue(ext.IsHeld, "Grip dal raggio: estintore in mano");
            Ray(ext, "GrabExit", Hand.Right);
            Assert.IsFalse(ext.IsHeld);

            Ray(key, "SelectEnter", Hand.Right);
            Ray(key, "SelectExit", Hand.Right);
            yield return null;
            Assert.IsFalse(d.Get<GeneratorUnit>("generator").IsRunning, "Grilletto dal raggio sulla chiave: generatore fermo");
        }

        /// <summary>
        /// Primo avvio: un solo rig, una sola camera attiva, un solo EventSystem, ponte UI registrato,
        /// rig portato su MNG_SpawnPoint e fermo (non cade).
        /// </summary>
        [UnityTest]
        public IEnumerator FirstRun_SharedRigIsSetUpOnce()
        {
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(1, Object.FindObjectsByType<SI81.Rig.RigRegistration>(FindObjectsSortMode.None).Length, "Un solo rig");
            var rig = SI81.Rig.RigRegistration.Instance;
            Assert.IsNotNull(rig);
            Assert.IsNotNull(rig.Bridge, "Ponte rig-UI non registrato");
            Assert.AreEqual(1, Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(c => c.isActiveAndEnabled), "Una sola camera attiva");
            Assert.AreEqual(1, Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None).Length, "Un solo EventSystem");
            var spawn = Object.FindFirstObjectByType<SI81.Rig.SpawnPoint>();
            Assert.IsNotNull(spawn, "Manca SpawnPoint");
            Assert.AreEqual("MNG_SpawnPoint", spawn.name);
            Vector3 origin = rig.TrackingOrigin.position;
            Vector2 d2 = new Vector2(origin.x - spawn.transform.position.x, origin.z - spawn.transform.position.z);
            Assert.Less(d2.magnitude, 0.5f, "Rig non portato sullo spawn");
            yield return new WaitForSeconds(1f);
            Assert.Less(Mathf.Abs(rig.TrackingOrigin.position.y - origin.y), 0.05f, "Il rig sta cadendo");
            Assert.Greater(rig.TrackingOrigin.position.y, -0.1f, "Rig sotto il pavimento");
        }

        /// <summary>
        /// Raggio vero (come il RayInteractor: vince il collider più vicino) da 1,5 m davanti al generatore, anche di sbieco:
        /// colpisce la chiave, non il generatore.
        /// </summary>
        [UnityTest]
        public IEnumerator Ray_FromFrontOfGenerator_HitsKeyFirst()
        {
            yield return null;
            var key = d.Get<EmergencyStopButton>("emergencyStop");
            var gen = d.Get<GeneratorUnit>("generator");
            Vector3 keyPos = key.transform.position;
            Vector3 outward = keyPos - gen.GetComponent<Collider>().bounds.center;
            outward.y = 0f;
            outward = Mathf.Abs(outward.x) > Mathf.Abs(outward.z) ? new Vector3(Mathf.Sign(outward.x), 0f, 0f) : new Vector3(0f, 0f, Mathf.Sign(outward.z));
            foreach (float yaw in new[] { 0f, 30f, -30f })
            {
                Vector3 from = keyPos + Quaternion.Euler(0f, yaw, 0f) * outward * 1.5f + Vector3.up * 0.6f;
                var hits = Physics.RaycastAll(from, (keyPos - from).normalized, 5f, ~0, QueryTriggerInteraction.Ignore);
                Assert.Greater(hits.Length, 0, "Il raggio non colpisce nulla (yaw " + yaw + ")");
                var nearest = hits.OrderBy(h => h.distance).First();
                Assert.AreEqual(key.gameObject, nearest.collider.gameObject, "Yaw " + yaw + ": il raggio colpisce " + nearest.collider.name);
            }
        }

        /// <summary>I colleghi fermati a voce escono senza sovrapporsi (distanza minima 0,45 m nei primi 6 s).</summary>
        [UnityTest]
        public IEnumerator Shout_ColleaguesLeaveWithoutOverlapping()
        {
            var by = Object.FindObjectsByType<Bystander>(FindObjectsSortMode.None).Where(b => b.isActiveAndEnabled).ToArray();
            Assert.GreaterOrEqual(by.Length, 2);
            d.Teleport(12f, 8f, 90f);
            yield return null;
            d.Shout();
            float minDist = float.MaxValue;
            float until = Time.time + 6f;
            while (Time.time < until)
            {
                for (int i = 0; i < by.Length; i++)
                    for (int j = i + 1; j < by.Length; j++)
                    {
                        Vector3 a = by[i].transform.position, b = by[j].transform.position;
                        minDist = Mathf.Min(minDist, new Vector2(a.x - b.x, a.z - b.z).magnitude);
                    }
                yield return null;
            }
            Assert.Greater(minDist, 0.45f, "Colleghi sovrapposti: distanza minima " + minDist.ToString("F2") + " m");
        }

        /// <summary>
        /// Fine con PM-2: dopo aver portato via PM-1 raggiunge il collega, resta inginocchiato (testa sotto 1,3 m) e si muove
        /// (non una statua) per almeno 4 s prima che il livello si chiuda.
        /// </summary>
        [UnityTest]
        public IEnumerator Pm2_AtVictim_KneelsAndKeepsMoving()
        {
            d.Shout();
            var powder = d.Extinguisher(ExtinguisherAgent.Powder);
            d.Grab(powder);
            d.StopAndIsolate();
            yield return new WaitForSeconds(2.5f);
            yield return d.SprayFire(powder, 2f, 6f);
            d.Release(powder);
            yield return d.DragVictimTo(d.Get<Transform>("safePoint").position);
            d.Teleport(10f, 12f, 0f);
            d.Set("paramedicArrivalTime", d.Scenario.ElapsedSeconds + 0.2f);
            float until = Time.time + 90f;
            while (Time.time < until && d.Scenario.IsRunning && d.Get<object>("pm2").ToString() != "AtVictim") yield return null;
            Assert.AreEqual("AtVictim", d.Get<object>("pm2").ToString(), "PM-2 non ha raggiunto il collega");

            var pm2 = d.Get<NpcMover>("arrivingParamedic");
            var anim = pm2.GetComponent<CharacterAnimator>();
            Assert.AreEqual("Kneel", anim.CurrentKey);
            // Il tratto in ginocchio di Anim_Pm2_Rescue va da 0,8 a 3,4 s: la clip deve andare avanti e indietro lì dentro
            // (prima restava ferma sull'ultimo fotogramma, in piedi).
            yield return new WaitForSeconds(1.5f);
            double minT = double.MaxValue, maxT = double.MinValue;
            float sampleUntil = Time.time + 6f;
            while (Time.time < sampleUntil)
            {
                Assert.IsTrue(d.Scenario.IsRunning, "Il livello si è chiuso subito: PM-2 non si vede al lavoro");
                double t = anim.ClipTime;
                minT = System.Math.Min(minT, t); maxT = System.Math.Max(maxT, t);
                yield return null;
                if (Time.time - (sampleUntil - 6f) > 2f) break;
            }
            Assert.GreaterOrEqual(minT, 0.75d, "La clip è uscita dal tratto in ginocchio");
            Assert.LessOrEqual(maxT, 3.6d, "La clip è arrivata alla parte in cui si rialza (da 3,7 s)");
            Assert.Greater(maxT - minT, 0.3d, "La clip è ferma (statua)");
            yield return d.WaitEnd(10f);
            Assert.AreEqual(OutcomeType.Positive, d.Outcome, "Tutto fatto: Positivo");
        }

        /// <summary>Nuovo ordine: dopo l'arresto la pozza non si accende finché la zona non è isolata (al massimo 40 s).</summary>
        [UnityTest]
        public IEnumerator Fire_StartsAfterIsolation_OrFallback()
        {
            d.Get<EmergencyStopButton>("emergencyStop").Press();
            yield return new WaitForSeconds(10f);
            Assert.IsFalse(d.Get<FireSource>("fire").IsBurning, "Niente incendio prima dell'isolamento");
            yield return new WaitForSeconds(33f);
            Assert.IsTrue(d.Get<FireSource>("fire").IsBurning, "Senza isolamento la pozza si accende comunque dopo 40 s");
        }

        /// <summary>Collega trascinato nella zona sicura: LG-27, obiettivo 4 attivo/completo.</summary>
        [UnityTest]
        public IEnumerator VictimDraggedToSafeZone_IsRecorded()
        {
            d.Get<EmergencyStopButton>("emergencyStop").Press();
            yield return null;
            yield return d.DragVictimTo(d.Get<Transform>("safePoint").position);
            yield return null;
            Assert.IsTrue(d.Logged("LG-27"));
            Assert.IsTrue(d.Scenario.IsRunning);
        }

        /// <summary>Il collega trascinato verso una parete si ferma al bordo del pavimento percorribile: non la attraversa.</summary>
        [UnityTest]
        public IEnumerator DraggedVictim_DoesNotCrossWalls()
        {
            d.Get<EmergencyStopButton>("emergencyStop").Press();
            yield return null;
            // Parete Middle_A sulla x = 13 (z 2-6): la linea dal collega a (9; 2,8) la incrocia a z = 5,6, sotto il varco.
            yield return d.DragVictimTo(new Vector3(9f, 0f, 2.8f), 0.3f, 12f);
            var v = d.Get<InjuredPerson>("victim").transform.position;
            Assert.Greater(v.x, 13.1f, "Il collega e' passato attraverso la parete (x = " + v.x.ToString("F2") + ")");
        }
    }
}
