using System.Collections.Generic;
using System.Linq;
using SI81.Ludovica.TempCore; // rig e interazioni temporanee (finché non arriva il VR Interaction System)
using SI81.UI;
using UnityEngine;

namespace SI81.Ludovica.ElectricShock
{
    /// <summary>
    /// Scenario 10 · Electric Shock (Folgorazione), versione LITE. Va su MNG_Scenario.
    /// Ordine obbligato dalla scena: prima la corrente (PB-01), poi il fuoco (la pozza si accende all'arresto), poi il soccorso.
    /// Gestisce tempi, eventi EV-10-01...07, Registro delle scelte LG-01...21, esiti (Positivo / Parziale / Negativo)
    /// e chiude con EndMission una sola volta.
    /// </summary>
    public class ElectricShockScenario : ScenarioController
    {
        [Header("Dati")]
        [SerializeField] private ElectricShockSettings settings;
        [Tooltip("Tabella delle frasi del debrief (JSON).")]
        [SerializeField] private TextAsset debriefPhrases;

        [Header("Generatore, corrente e fuoco")]
        [SerializeField] private GeneratorUnit generator;
        [SerializeField] private EmergencyStopButton emergencyStop;
        [SerializeField] private LiveCable cable;
        [Tooltip("Pozza di benzina PZ-01 (radice: si sposta con le varianti di replay).")]
        [SerializeField] private Transform fuelPool;
        [Tooltip("Tre posizioni possibili della pozza sotto il generatore (replay).")]
        [SerializeField] private Transform[] fuelPoolPositions;
        [SerializeField] private FireSource fire;
        [SerializeField] private FuelTankHazard fuelTank;
        [Tooltip("Zona di fumo / aria nociva ZN-11.")]
        [SerializeField] private BoxCollider harmfulAirZone;
        [Tooltip("Fumo di Crisis che copre la pozza (facoltativo).")]
        [SerializeField] private ParticleSystem crisisSmoke;

        [Header("Attrezzature")]
        [SerializeField] private Extinguisher[] extinguishers;
        [Tooltip("Posizioni del rack RK-02 (ordine casuale in Crisis).")]
        [SerializeField] private Transform[] extinguisherSlots;
        [SerializeField] private BarrierSnapLine barrierLine;
        [SerializeField] private EmergencyRadio radio;

        [Header("Persone")]
        [SerializeField] private InjuredPerson victim;
        [SerializeField] private Bystander[] bystanders;
        [Tooltip("Terzo collega (solo Crisis).")]
        [SerializeField] private Bystander extraBystander;
        [SerializeField] private ParamedicInShock paramedicInShock;
        [Tooltip("PM-2 (inattivo fino all'arrivo).")]
        [SerializeField] private NpcMover arrivingParamedic;

        [Header("Punti")]
        [SerializeField] private Transform safePoint;
        [Tooltip("Distanza dal centro di SAFE-01 entro cui PM-1 è al sicuro (m).")]
        [SerializeField] private float safePointRadius = 2f;
        [Tooltip("Punto di raccolta AP-02.")]
        [SerializeField] private Transform assemblyPoint;
        [Tooltip("Percorso di uscita dei colleghi verso AP-02.")]
        [SerializeField] private Transform[] exitPath;
        [Tooltip("Percorso di PM-2 dallo spawn al varco DR-03 (ultimo punto davanti al varco).")]
        [SerializeField] private Transform[] paramedicPath;
        [Tooltip("Percorso di PM-2 dal varco all'interno della zona, che gira attorno alla vittima.")]
        [SerializeField] private Transform[] zoneEntryPath;
        [Tooltip("Punto accanto alla vittima dove si fermano colleghi e PM-2.")]
        [SerializeField] private Transform victimApproachPoint;
        [Tooltip("Zona del generatore ZN-10 (x 13-30, y 2-14).")]
        [SerializeField] private Bounds generatorZone = new Bounds(new Vector3(21.5f, 1.5f, 8f), new Vector3(17f, 3f, 12f));

        [Header("Osservazione")]
        [SerializeField] private ObservableHazard[] observables;

        // Stato generale
        private ElectricShockSettings.LevelSettings level;
        private readonly ChoiceLog log = new ChoiceLog();
        private DebriefPhraseTable phrases;
        private bool finished;

        // Tempi casuali (replay)
        private float ignitionTime;
        private float explosionDelay;
        private float bystanderAttemptTime;
        private float paramedicArrivalTime = -1f;

        // Flag di scena
        private float stopTime = -1f;
        private float pendingIgnitionAt = -1f;
        private float ignitedAt = -1f;
        private bool warningStarted;
        private float warningAt = -1f;
        private bool exploded;
        private bool bystandersAttempted;
        private bool bystandersStopped;
        private float callTime = -1f;
        private float harmfulAirExposure;
        private bool poolSmellAnnounced;
        private float stayTimer;
        private bool stayedWithVictim;
        private int hintsGiven;
        private readonly HashSet<string> hintsShown = new HashSet<string>();
        private float nextDistractorAt = 30f;
        private bool waterUsed;
        private bool playerInjured;
        private bool victimInjured;
        private bool otherInjured;
        private bool secondVictim;
        private ChoiceLogEntry preparedEntry;
        private ChoiceLogEntry callEntry;

        // PM-2
        private enum ParamedicState { NotArrived, Walking, WaitingAtGate, Entering, LeadingPm1, GoingToVictim, AtVictim }
        private ParamedicState pm2 = ParamedicState.NotArrived;
        private bool pm2ToldToStop;
        private float pm2AtVictimSince = -1f;
        /// <summary>Secondi massimi di attesa di PM-2 accanto al collega prima di chiudere il livello.</summary>
        private const float PM2_MAX_WAIT = 30f;

        // Snapshot dell'avviso di esplosione (LG-16)
        private bool warnPlayerInside;
        private bool warnPm1Safe;
        private bool warnVictimMoved;

        /// <summary>Registro delle scelte (lettura, per test e debrief).</summary>
        public ChoiceLog Log => log;

        // =====================================================================
        // Avvio
        // =====================================================================

        /// <summary>Prepara la scena al livello in corso, sceglie le varianti di replay e avvia EV-10-01.</summary>
        protected override void OnScenarioStarted()
        {
            if (settings == null)
            {
                Debug.LogError("[SI81 ElectricShock] Manca SO_ElectricShockSettings su MNG_Scenario.", this);
                return;
            }
            level = settings.For(Level);
            phrases = DebriefPhraseTable.Load(debriefPhrases);

            ignitionTime = Random.Range(settings.IgnitionTime.x, settings.IgnitionTime.y);
            explosionDelay = Random.Range(level.ExplosionDelay.x, level.ExplosionDelay.y);
            bystanderAttemptTime = Random.Range(settings.BystanderAttemptTime.x, settings.BystanderAttemptTime.y);
            ApplyReplayVariants();
            SubscribeEvents();

            generator.StartRunning();
            cable.SetPowered(true);
            if (crisisSmoke != null) { if (level.BackgroundNoise) crisisSmoke.Play(); else crisisSmoke.Stop(); }
            if (arrivingParamedic != null) arrivingParamedic.gameObject.SetActive(false);
            SetHighlights(level.HighlightObjects);
            IgnorePlayerCollisions();
            foreach (var b in AllBystanders()) b.GetComponent<NpcMover>().CanStep = barrierLine.AllowPass;
            if (arrivingParamedic != null) arrivingParamedic.CanStep = barrierLine.AllowPass;

            SetPhase(PHASE_SCAN);
            Subtitle("Collega", "Aiuto! Ha preso la scossa!");
            Alert("Folgorazione\nUn collega è a terra vicino al generatore");
            ElectricShockHud.Sound("[Crepitio elettrico, scintille e fumo dal cavo]");
            if (level.BackgroundNoise) ElectricShockHud.Sound("[Allarme di fondo]");
            Debug.Log($"[SI81 ElectricShock] Replay: T_ign {ignitionTime:0.0} s, T_exp {explosionDelay:0.0} s dopo l'accensione, colleghi a {bystanderAttemptTime:0.0} s, livello {Level}.");
        }

        /// <summary>Posizione della pozza, ordine del rack e terzo collega (variabili di replay).</summary>
        private void ApplyReplayVariants()
        {
            if (fuelPool != null && fuelPoolPositions != null && fuelPoolPositions.Length > 0)
            {
                var p = fuelPoolPositions[Random.Range(0, fuelPoolPositions.Length)];
                fuelPool.SetPositionAndRotation(p.position, p.rotation);
            }

            if (extinguisherSlots != null && extinguishers != null && extinguisherSlots.Length >= extinguishers.Length)
            {
                var order = Enumerable.Range(0, extinguishers.Length).ToList();
                if (level.RandomizeRack) order = order.OrderBy(_ => Random.value).ToList();
                for (int i = 0; i < extinguishers.Length; i++)
                    extinguishers[i].transform.SetPositionAndRotation(extinguisherSlots[order[i]].position, extinguisherSlots[order[i]].rotation);
            }

            if (extraBystander != null) extraBystander.gameObject.SetActive(level.Bystanders >= 3 && Random.value < 0.5f);
        }

        /// <summary>
        /// [TEMP rig] Il corpo del giocatore non urta persone, collega a terra, barriere (le attraversa: specifica §6),
        /// attrezzature e cavo: così nessun NPC lo spinge o lo blocca e può stare accanto alla vittima.
        /// </summary>
        private void IgnorePlayerCollisions()
        {
            var rig = TempRig.Instance;
            var body = rig != null ? rig.GetComponent<CharacterController>() : null;
            if (body == null) return;
            var roots = new List<Component> { victim, paramedicInShock, arrivingParamedic, cable, radio };
            roots.AddRange(bystanders);
            if (extraBystander != null) roots.Add(extraBystander);
            roots.AddRange(extinguishers);
            roots.AddRange(FindObjectsByType<BarrierPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            foreach (var r in roots)
            {
                if (r == null) continue;
                foreach (var c in r.GetComponentsInChildren<Collider>(true)) Physics.IgnoreCollision(body, c, true);
            }
        }

        /// <summary>Si iscrive agli eventi degli oggetti della scena.</summary>
        private void SubscribeEvents()
        {
            emergencyStop.Pressed += OnEmergencyStop;
            cable.TouchedWhilePowered += OnCableTouched;
            fire.Extinguished += OnFireExtinguished;
            fire.WaterApplied += OnWaterApplied;
            victim.TouchedWhileLive += OnVictimTouchedLive;
            victim.Checked += OnVictimChecked;
            victim.PositionedChanged += () => Feedback("Collega in posizione", FeedbackKind.Positive);
            radio.Called += OnRadioCall;
            barrierLine.ClosedChanged += OnBarrierChanged;
            paramedicInShock.EscortStarted += () => Feedback("PM-1 ti segue", FeedbackKind.Neutral);
            foreach (var e in extinguishers)
            {
                e.AutoReadLabel = level.AutoReadLabel;
                e.LabelInspected += OnLabelInspected;
                e.FirstUseOnFire += OnExtinguisherFirstUse;
            }
            foreach (var b in AllBystanders()) b.ReachedVictim += OnBystanderReachedVictim;
            TempRig.Shout += OnShout;
        }

        /// <summary>Si disiscrive dagli eventi statici.</summary>
        protected override void OnDestroy()
        {
            base.OnDestroy();
            TempRig.Shout -= OnShout;
        }

        // =====================================================================
        // Ciclo
        // =====================================================================

        /// <summary>Tempi, eventi dinamici, osservazione, fumo, suggerimenti, PM-2.</summary>
        private void Update()
        {
            if (!IsRunning || finished || level == null) return;
            float t = ElapsedSeconds;

            UpdateObservation(t);
            UpdateIgnition(t);
            UpdateExplosion(t);
            UpdateHarmfulAir();
            UpdateVictimCare();
            UpdateBystanders(t);
            UpdateParamedics(t);
            UpdateHints(t);
            UpdateDistractors(t);
        }

        /// <summary>Osservazione di cavo, pozza e persone (LG-01, LG-02) e odore di benzina.</summary>
        private void UpdateObservation(float t)
        {
            var head = PlayerLocator.Head;
            if (head == null) return;
            foreach (var o in observables)
            {
                if (o == null || !o.Tick(head, t)) continue;
                if (o.HazardId == "pool")
                {
                    log.RecordOnce(t, "LG-01", ChoiceOutcome.Correct, ScoreDimension.Awareness);
                    Feedback("Pozza di benzina individuata", FeedbackKind.Positive);
                }
            }

            if (!poolSmellAnnounced && fuelPool != null && Vector3.Distance(PlayerLocator.Feet, Flat(fuelPool.position)) < 9f)
            {
                poolSmellAnnounced = true;
                ElectricShockHud.Line("", "[Odore di benzina]");
            }

            if (t >= settings.ObservePoolDeadline && !IsObserved("pool"))
                log.RecordOnce(t, "LG-02", ChoiceOutcome.Missed, ScoreDimension.Awareness);
        }

        /// <summary>Scintilla sulla pozza: 2 s dopo l'arresto, oppure a T_ign con la corrente presente (EV-10-03).</summary>
        private void UpdateIgnition(float t)
        {
            if (ignitedAt >= 0f) return;
            if (pendingIgnitionAt >= 0f && t >= pendingIgnitionAt)
            {
                Ignite(t);
                return;
            }
            if (generator.IsRunning && t >= ignitionTime)
            {
                log.RecordOnce(t, "LG-10", ChoiceOutcome.Risky, ScoreDimension.Time);
                Ignite(t);
            }
        }

        /// <summary>Accende la pozza e fa partire l'orologio dell'esplosione.</summary>
        private void Ignite(float t)
        {
            ignitedAt = t;
            pendingIgnitionAt = -1f;
            fire.Ignite();
            SetPhase(PHASE_ACTION);
            Alert("Incendio\nLa benzina sotto il generatore ha preso fuoco");
            Subtitle("Collega", "Fuoco! La benzina!");
            ElectricShockHud.Sound("[Fiamma improvvisa, ruggito del fuoco]");
            ElectricShockHud.Pulse(0.9f, 0.4f);
            RefreshObjective();
        }

        /// <summary>Avviso a T_exp-12 s e vampata a T_exp se il fuoco non è spento (EV-10-05, LG-16, LG-17).</summary>
        private void UpdateExplosion(float t)
        {
            if (ignitedAt < 0f || exploded || !fire.IsBurning) return;
            float since = t - ignitedAt;

            if (!warningStarted && since >= explosionDelay - settings.ExplosionWarning)
            {
                warningStarted = true;
                warningAt = t;
                warnPlayerInside = PlayerInBlast();
                warnPm1Safe = paramedicInShock.IsSafe;
                warnVictimMoved = !InBlast(victim.transform.position);
                fuelTank.StartWarning(settings.ExplosionWarning, level.TankSwelling, level.BlastRing);
                Subtitle("", "[Il serbatoio sibila: allontanati]");
                Alert("Pericolo esplosione\nAllontanati oltre 7 m dal generatore");
                ElectricShockHud.Pulse(1f, 0.6f);
            }

            if (since >= explosionDelay) Explode(t);
        }

        /// <summary>Esplosione del serbatoio: ferisce chi è entro 7 m e chiude lo scenario.</summary>
        private void Explode(float t)
        {
            exploded = true;
            fuelTank.Explode();
            ElectricShockHud.Sound("[Esplosione]");
            ElectricShockHud.Pulse(1f, 1f);
            Alert("Esplosione\nIl serbatoio del generatore è esploso");
            RecordWarningReaction(t);

            if (PlayerInBlast())
            {
                playerInjured = true;
                log.Record(t, "LG-17", ChoiceOutcome.CriticalError, ScoreDimension.Safety);
            }
            if (InBlast(victim.transform.position))
            {
                victimInjured = true;
                victim.MarkWorsened();
                log.Record(t, "LG-25", ChoiceOutcome.CriticalError, ScoreDimension.Safety);
            }
            foreach (var b in AllBystanders())
            {
                if (b.Current == Bystander.State.Evacuated || !InBlast(b.transform.position)) continue;
                b.MarkInjured();
                otherInjured = true;
            }
            if (pm2 != ParamedicState.NotArrived && InBlast(arrivingParamedic.transform.position)) otherInjured = true;
            if (otherInjured) log.Record(t, "LG-26", ChoiceOutcome.CriticalError, ScoreDimension.Safety);
            // PM-1 nel raggio: solo ferita psicologica, non incide sull'esito (specifica §8).

            Finish("esplosione");
        }

        /// <summary>Registra la reazione all'avviso di esplosione (LG-16) alla fine della finestra di 12 s.</summary>
        private void RecordWarningReaction(float t)
        {
            if (!warningStarted || log.Has("LG-16")) return;
            string choice;
            bool good = true;
            if (fire.WasExtinguished) choice = "di spegnere il fuoco";
            else if (warnPlayerInside && !PlayerInBlast()) choice = "di ritirarti oltre 7 m";
            else if (!warnPm1Safe && paramedicInShock.IsSafe) choice = "di portare via PM-1";
            else if (!warnVictimMoved && !InBlast(victim.transform.position)) choice = "di spostare il collega";
            else { choice = "di restare dov'eri"; good = false; }

            var e = log.Record(warningAt, "LG-16", good ? ChoiceOutcome.Correct : ChoiceOutcome.Risky, ScoreDimension.Decision);
            e.Context["scelta"] = choice;
            e.Context["esito"] = good ? "nessuno è stato colpito per questa scelta" : "sei rimasto esposto alla vampata";
        }

        /// <summary>Fumo e aria nociva: dopo 20 s in ZN-11 o vicino al fuoco, penalità di Safety (EV-10-04).</summary>
        private void UpdateHarmfulAir()
        {
            var headT = PlayerLocator.Head;
            if (headT == null || log.Has("LG-22")) return;
            Vector3 head = headT.position;
            bool inZone = harmfulAirZone != null && harmfulAirZone.bounds.Contains(head);
            bool nearFire = fire.IsBurning && Vector3.Distance(Flat(head), Flat(fire.transform.position)) < 3f;
            if (!inZone && !nearFire) return;
            harmfulAirExposure += Time.deltaTime;
            if (harmfulAirExposure < settings.HarmfulAirSeconds) return;
            ElectricShockHud.Sound("[Tosse]");
            Alert("Aria nociva\nEsci dalla zona del fumo");
            log.RecordOnce(ElapsedSeconds, "LG-22", ChoiceOutcome.Risky, ScoreDimension.Safety);
        }

        /// <summary>Restare con il collega dopo il controllo della coscienza.</summary>
        private void UpdateVictimCare()
        {
            if (PlayerLocator.Head == null || stayedWithVictim || !victim.ConsciousnessChecked || victim.IsLive) return;
            if (Vector3.Distance(PlayerLocator.Feet, Flat(victim.transform.position)) > settings.StayDistance) return;
            stayTimer += Time.deltaTime;
            if (stayTimer < settings.StayWithVictimSeconds) return;
            stayedWithVictim = true;
            Feedback("Sei rimasto con il collega", FeedbackKind.Positive);
            RefreshObjective();
        }

        /// <summary>Colleghi che tentano di aiutare la vittima (EV-10-02).</summary>
        private void UpdateBystanders(float t)
        {
            if (bystandersAttempted || bystandersStopped || t < bystanderAttemptTime) return;
            bystandersAttempted = true;
            if (!generator.IsRunning) return; // contromisura: corrente già tolta, nessun tentativo pericoloso
            Subtitle("Collega", "Dobbiamo tirarlo via!");
            ElectricShockHud.Line("", "[I colleghi stanno per toccarlo]");
            foreach (var b in AllBystanders()) b.BeginAttempt(victimApproachPoint.position, settings.BystanderHesitation);
        }

        /// <summary>Un collega ha raggiunto la vittima: se la corrente è presente, nuova vittima (LG-15).</summary>
        private void OnBystanderReachedVictim(Bystander b)
        {
            if (finished || !generator.IsRunning) return;
            b.MarkInjured();
            secondVictim = true;
            log.RecordOnce(ElapsedSeconds, "LG-15", ChoiceOutcome.CriticalError, ScoreDimension.Safety);
            Alert("Nuova vittima\nUn collega ha toccato il folgorato");
            Finish("collega folgorato");
        }

        /// <summary>Arrivo e comportamento di PM-2 (EV-10-07).</summary>
        private void UpdateParamedics(float t)
        {
            if (safePoint != null && !paramedicInShock.IsSafe && Vector3.Distance(Flat(paramedicInShock.transform.position), Flat(safePoint.position)) <= safePointRadius)
            {
                bool byPm2 = arrivingParamedic != null && paramedicInShock.GetComponent<NpcMover>().FollowTarget == arrivingParamedic.transform;
                paramedicInShock.MarkSafe(byPm2 ? "pm2" : "player");
                var e = log.RecordOnce(t, "LG-20", ChoiceOutcome.Correct, ScoreDimension.Decision);
                if (e != null) e.Context["chi"] = byPm2 ? "PM-2 ha portato" : "hai portato";
                Feedback("PM-1 al punto sicuro", FeedbackKind.Positive);
                RefreshObjective();
                if (pm2 == ParamedicState.LeadingPm1) SendPm2ToVictim();
            }

            // PM-2 accanto al collega: chiude il livello quando il giocatore non sta più accompagnando PM-1
            // né assistendo il collega (al massimo dopo 30 s).
            if (pm2 == ParamedicState.AtVictim)
            {
                bool escorting = paramedicInShock.IsEscorted && !paramedicInShock.IsSafe;
                bool assisting = PlayerLocator.Head != null && victim.ConsciousnessChecked && !stayedWithVictim
                    && Vector3.Distance(PlayerLocator.Feet, Flat(victim.transform.position)) <= settings.StayDistance;
                if ((!escorting && !assisting) || t - pm2AtVictimSince >= PM2_MAX_WAIT)
                {
                    Subtitle("PM-2", "Ci penso io, grazie.");
                    Finish("assistenza conclusa da PM-2");
                    return;
                }
            }

            if (paramedicArrivalTime < 0f) paramedicArrivalTime = settings.ParamedicWithoutCall;
            if (pm2 == ParamedicState.NotArrived && t >= paramedicArrivalTime) Pm2Arrive(t);

            if (pm2 == ParamedicState.WaitingAtGate && ZoneSafe())
            {
                Subtitle("PM-2", "Zona sicura, entro.");
                Pm2Enter();
            }
        }

        /// <summary>PM-2 compare all'ingresso e cammina verso il varco.</summary>
        private void Pm2Arrive(float t)
        {
            pm2 = ParamedicState.Walking;
            if (callEntry != null) callEntry.Context["s"] = Mathf.RoundToInt(t - callTime).ToString();
            arrivingParamedic.gameObject.SetActive(true);
            arrivingParamedic.transform.position = paramedicPath[0].position;
            Subtitle("", "[Voci e passi dall'ingresso]");
            arrivingParamedic.WalkPath(paramedicPath.Skip(1).Select(p => p.position), 0f, OnPm2AtGate);
        }

        /// <summary>PM-2 davanti al varco DR-03: decide se fermarsi o entrare.</summary>
        private void OnPm2AtGate()
        {
            if (finished) return;
            float t = ElapsedSeconds;
            if (!barrierLine.IsClosed)
            {
                var e = log.RecordOnce(t, "LG-04", ChoiceOutcome.Risky, ScoreDimension.Safety);
                if (e != null) e.Context["esito"] = ZoneSafe() ? "la zona era sicura, quindi ha potuto entrare" : "la zona non era sicura";
            }

            if (ZoneSafe())
            {
                Pm2Enter();
                return;
            }

            if (barrierLine.IsClosed || pm2ToldToStop)
            {
                pm2 = ParamedicState.WaitingAtGate;
                Subtitle("PM-2", "Cosa è successo? Posso entrare?");
                return;
            }

            // Varco aperto e zona non sicura: PM-2 entra e diventa la seconda vittima.
            secondVictim = true;
            log.Record(t, "LG-23", ChoiceOutcome.CriticalError, ScoreDimension.Safety);
            Alert("Seconda vittima\nPM-2 è entrato in una zona non sicura");
            Finish("PM-2 in zona non sicura");
        }

        /// <summary>PM-2 entra: porta via PM-1 se serve, poi va dal collega.</summary>
        private void Pm2Enter()
        {
            pm2 = ParamedicState.Entering;
            if (!paramedicInShock.IsSafe && !paramedicInShock.IsEscorted)
            {
                pm2 = ParamedicState.LeadingPm1;
                var route = EntryRoute().Append(paramedicInShock.transform.position + Vector3.left);
                arrivingParamedic.WalkPath(route, 0f, () =>
                {
                    if (finished) return;
                    // Nel frattempo il giocatore può averlo già portato via: in quel caso PM-2 va dal collega.
                    if (paramedicInShock.IsSafe || paramedicInShock.IsEscorted) { SendPm2ToVictim(); return; }
                    Subtitle("PM-2", "Collega, vieni con me.");
                    paramedicInShock.FollowTarget(arrivingParamedic.transform);
                    arrivingParamedic.WalkPath(new[] { safePoint.position }, 0.8f, null);
                });
                return;
            }
            SendPm2ToVictim();
        }

        /// <summary>PM-2 raggiunge il collega: fine dell'assistenza e chiusura del livello.</summary>
        private void SendPm2ToVictim()
        {
            pm2 = ParamedicState.GoingToVictim;
            bool outside = arrivingParamedic.transform.position.x < barrierLine.transform.position.x + 0.5f;
            var route = (outside ? EntryRoute() : Enumerable.Empty<Vector3>()).Append(victimApproachPoint.position + Vector3.right * 0.7f);
            arrivingParamedic.WalkPath(route, 0f, () =>
            {
                if (finished) return;
                pm2 = ParamedicState.AtVictim;
                pm2AtVictimSince = ElapsedSeconds;
                Subtitle("PM-2", "Sono qui. Come sta?");
            });
        }

        /// <summary>Percorso dal varco all'interno della zona, attorno alla vittima.</summary>
        private IEnumerable<Vector3> EntryRoute() =>
            zoneEntryPath != null ? zoneEntryPath.Where(p => p != null).Select(p => p.position) : Enumerable.Empty<Vector3>();

        /// <summary>Suggerimenti: Guided dopo 20 s, Operational massimo 3 dopo 45 s, Crisis nessuno.</summary>
        private void UpdateHints(float t)
        {
            if (level.MaxHints == 0 || t < level.FirstHintDelay) return;
            if (level.MaxHints > 0 && hintsGiven >= level.MaxHints) return;
            string hint = null;
            if (generator.IsRunning) hint = "Prima togli la corrente.";
            else if (fire.IsBurning && CurrentlyHeldExtinguisher() == null) hint = "Prepara un estintore.";
            else if (fire.WasExtinguished && !barrierLine.IsClosed) hint = "Chiudi il varco con le barriere.";
            else if (fire.WasExtinguished && !victim.ConsciousnessChecked) hint = "Controlla coscienza e respiro del collega.";
            if (hint == null || !hintsShown.Add(hint)) return;
            hintsGiven++;
            SetHint(hint);
        }

        /// <summary>Distrattori di Operational e Crisis (colleghi che chiamano, allarme di fondo).</summary>
        private void UpdateDistractors(float t)
        {
            if (level.Distractors <= 0 || t < nextDistractorAt) return;
            nextDistractorAt = t + Random.Range(20f, 35f);
            string who = level.Distractors >= 2 && Random.value < 0.5f ? "Collega 2" : "Collega 1";
            Subtitle(who, "Ehi! Vieni qui un attimo!");
        }

        // =====================================================================
        // Eventi degli oggetti
        // =====================================================================

        /// <summary>PB-01 premuto: corrente tolta, scintilla sulla pozza dopo 2 s (LG-06/07/09/21).</summary>
        private void OnEmergencyStop()
        {
            if (finished || !generator.IsRunning) return;
            float t = ElapsedSeconds;
            stopTime = t;
            generator.Stop();
            cable.SetPowered(false);
            Feedback("Generatore fermato", FeedbackKind.Positive);
            ElectricShockHud.Sound("[Il motore si spegne]");

            bool observed = IsObserved("cable") && IsObserved("pool");
            if (t < settings.ImpulsiveStopTime && !observed)
                log.Record(t, "LG-21", ChoiceOutcome.Risky, ScoreDimension.Time);
            else if (observed && t - LastObservedAt() <= settings.TimelyStopAfterObserve)
                log.Record(t, "LG-09", ChoiceOutcome.Correct, ScoreDimension.Time);

            var held = CurrentlyHeldExtinguisher();
            if (held != null)
            {
                preparedEntry = log.Record(t, "LG-06", ChoiceOutcome.Correct, ScoreDimension.Decision);
                preparedEntry.Context["dettaglio"] = "";
            }
            else log.Record(t, "LG-07", ChoiceOutcome.Risky, ScoreDimension.Decision);

            if (ignitedAt < 0f)
            {
                pendingIgnitionAt = t + settings.IgnitionAfterStop;
                ElectricShockHud.Line("", "[Una scintilla cade verso la pozza di benzina]");
            }
            SetHighlights(false);
            RefreshObjective();
        }

        /// <summary>Cavo toccato con la corrente presente: giocatore folgorato.</summary>
        private void OnCableTouched()
        {
            if (finished) return;
            playerInjured = true;
            log.Record(ElapsedSeconds, "LG-24", ChoiceOutcome.CriticalError, ScoreDimension.Safety);
            Alert("Folgorazione\nHai toccato il cavo sotto tensione");
            Finish("giocatore folgorato dal cavo");
        }

        /// <summary>Collega toccato con la corrente presente (LG-08).</summary>
        private void OnVictimTouchedLive()
        {
            if (finished) return;
            playerInjured = true;
            log.Record(ElapsedSeconds, "LG-08", ChoiceOutcome.CriticalError, ScoreDimension.Safety);
            Alert("Folgorazione\nHai toccato il collega con la corrente presente");
            Finish("giocatore folgorato dal collega");
        }

        /// <summary>Coscienza e respiro controllati (LG-18).</summary>
        private void OnVictimChecked()
        {
            log.RecordOnce(ElapsedSeconds, "LG-18", ChoiceOutcome.Correct, ScoreDimension.Execution);
            Feedback("Cosciente, respira regolarmente", FeedbackKind.Positive);
            SetPhase(PHASE_RESOLUTION);
        }

        /// <summary>Etichetta letta (LG-12 se l'estintore è adatto e non ancora usato).</summary>
        private void OnLabelInspected(Extinguisher e)
        {
            if (!e.IsCorrectType || e.HasBeenUsed) return;
            log.RecordOnce(ElapsedSeconds, "LG-12", ChoiceOutcome.Correct, ScoreDimension.Awareness);
        }

        /// <summary>Primo spruzzo sul fuoco: con l'acqua l'errore è già gestito da OnWaterApplied.</summary>
        private void OnExtinguisherFirstUse(Extinguisher e)
        {
            if (e.IsCorrectType) Feedback($"Estintore a {e.AgentName}: corretto", FeedbackKind.Positive);
        }

        /// <summary>Acqua sul fuoco: la pozza si allarga e divampa (EV-10-06, LG-11).</summary>
        private void OnWaterApplied()
        {
            if (finished || waterUsed) return;
            waterUsed = true;
            playerInjured = true;
            log.Record(ElapsedSeconds, "LG-11", ChoiceOutcome.CriticalError, ScoreDimension.Execution);
            Alert("Vampata\nL'acqua ha allargato la benzina in fiamme");
            ElectricShockHud.Pulse(1f, 0.8f);
            Finish("estintore ad acqua");
        }

        /// <summary>Fuoco spento con polvere o CO2 (LG-13, completa LG-06).</summary>
        private void OnFireExtinguished(ExtinguisherAgent agent)
        {
            if (finished) return;
            float t = ElapsedSeconds;
            fuelTank.Calm();
            if (warningStarted) RecordWarningReaction(t);
            float margin = ignitedAt >= 0f ? explosionDelay - (t - ignitedAt) : 0f;
            var e = log.RecordOnce(t, "LG-13", ChoiceOutcome.Correct, ScoreDimension.Execution);
            if (e != null)
            {
                e.Context["tipo"] = agent == ExtinguisherAgent.Powder ? "la polvere" : "la CO2";
                e.Context["s"] = Mathf.Max(0, Mathf.RoundToInt(margin)).ToString();
            }
            if (preparedEntry != null && ignitedAt >= 0f) preparedEntry.Context["dettaglio"] = $" e hai spento il fuoco {Mathf.RoundToInt(t - ignitedAt)} s dopo l'accensione invece di 24 s";
            Alert("Fuoco spento\nControlla che l'area sia sicura");
            Subtitle("", "[Il fuoco è spento, il sibilo si ferma]");
            RefreshObjective();
        }

        /// <summary>Chiamata al 112 (LG-05) e calcolo dell'arrivo di PM-2.</summary>
        private void OnRadioCall()
        {
            if (finished) return;
            float t = ElapsedSeconds;
            callTime = t;
            callEntry = log.RecordOnce(t, "LG-05", ChoiceOutcome.Correct, ScoreDimension.Decision);
            Subtitle("112", "112, qual è l'emergenza?");
            Subtitle("Tu", "Un collega folgorato e un incendio al generatore.");
            Subtitle("112", "Mandiamo subito i soccorsi.");
            if (pm2 == ParamedicState.NotArrived)
                paramedicArrivalTime = t + Random.Range(settings.ParamedicAfterCall.x, settings.ParamedicAfterCall.y);
        }

        /// <summary>Linea di barriera chiusa o aperta (LG-03).</summary>
        private void OnBarrierChanged(bool closed)
        {
            if (!closed || finished) return;
            log.RecordOnce(ElapsedSeconds, "LG-03", ChoiceOutcome.Correct, ScoreDimension.Safety);
            Feedback("Varco chiuso", FeedbackKind.Positive);
            RefreshObjective();
        }

        /// <summary>Il giocatore parla: ferma i colleghi e li manda al punto di raccolta, ferma PM-2 (LG-14).</summary>
        private void OnShout(Vector3 headPosition, Vector3 forward)
        {
            if (!IsRunning || finished) return;
            Subtitle("Tu", "Fermi! Non toccatelo! Allontanatevi!");
            float t = ElapsedSeconds;
            bool anyone = false;
            var path = exitPath.Select(p => p.position).ToArray();
            foreach (var b in AllBystanders())
            {
                if (Vector3.Distance(Flat(headPosition), Flat(b.transform.position)) > settings.ShoutRange) continue;
                if (b.Current == Bystander.State.Evacuated || b.Current == Bystander.State.Evacuating || b.Current == Bystander.State.Injured) continue;
                b.Evacuate(path);
                anyone = true;
            }
            if (anyone)
            {
                bystandersStopped = true;
                log.RecordOnce(t, "LG-14", ChoiceOutcome.Correct, ScoreDimension.Safety);
                Subtitle("Collega", "Va bene, usciamo!");
                RefreshObjective();
            }
            if (pm2 == ParamedicState.Walking && Vector3.Distance(Flat(headPosition), Flat(arrivingParamedic.transform.position)) <= settings.ShoutRange)
            {
                pm2ToldToStop = true;
                Subtitle("PM-2", "D'accordo, aspetto qui.");
            }
        }

        // =====================================================================
        // Fine
        // =====================================================================

        /// <summary>Tempo limite del livello scaduto: chiude con l'esito della situazione attuale.</summary>
        protected override void OnTimerExpired()
        {
            Finish("tempo scaduto");
        }

        /// <summary>Calcola l'esito (specifica §8), costruisce il debrief e chiama EndMission una sola volta.</summary>
        /// <param name="reason">Motivo della chiusura (solo Console).</param>
        private void Finish(string reason)
        {
            if (finished) return;
            finished = true;
            float t = ElapsedSeconds;
            foreach (var b in AllBystanders()) b.GetComponent<NpcMover>().Stop();
            if (arrivingParamedic != null) arrivingParamedic.Stop();

            bool assisted = victim.ConsciousnessChecked && stayedWithVictim;
            if (!generator.IsRunning && !assisted && !log.HasCriticalError()) log.RecordOnce(t, "LG-19", ChoiceOutcome.Missed, ScoreDimension.Execution);

            bool injuries = playerInjured || victimInjured || otherInjured || secondVictim || waterUsed;
            bool isolated = barrierLine.IsClosed;
            bool evacuated = AllBystanders().All(b => b.Current == Bystander.State.Evacuated || b.Current == Bystander.State.Evacuating || !generatorZone.Contains(b.transform.position));
            bool fireOut = fire.WasExtinguished && !exploded;

            OutcomeType outcome;
            if (injuries || log.HasCriticalError()) outcome = OutcomeType.Negative;
            else if (generator.IsRunning && !isolated && exploded) outcome = OutcomeType.Negative;
            else if (!generator.IsRunning && evacuated && fireOut && isolated && assisted && paramedicInShock.IsSafe) outcome = OutcomeType.Positive;
            else outcome = OutcomeType.Partial;

            int[] dims = log.ComputeDimensions();
            var lines = phrases.BuildLines(log, Level == DifficultyLevel.Guided);
            if (reason == "tempo scaduto") lines.Add(DebriefLinePrefix.BAD + "Tempo scaduto prima di completare la missione.");

            Debug.Log($"[SI81 ElectricShock] Fine: {reason}. Generatore fermo {!generator.IsRunning}, zona isolata {isolated}, persone evacuate {evacuated}, fuoco spento {fireOut}, collega assistito {assisted}, PM-1 al sicuro {paramedicInShock.IsSafe}.");
            Debug.Log("[SI81 ElectricShock] Percorso corretto:\n" + string.Join("\n", phrases.correctPath));
            EndMission(outcome, -1, dims, lines);
        }

        // =====================================================================
        // Utilità
        // =====================================================================

        /// <summary>Aggiorna l'obiettivo corrente al primo non ancora raggiunto.</summary>
        private void RefreshObjective()
        {
            int next;
            if (generator.IsRunning) next = 0;
            else if (fire.IsBurning || ignitedAt < 0f) next = 1;
            else if (!barrierLine.IsClosed || !AllBystanders().All(b => b.Current == Bystander.State.Evacuated || b.Current == Bystander.State.Evacuating)) next = 2;
            else next = 3;
            if (next != CurrentObjectiveIndex) SetObjective(next);
        }

        /// <summary>Evidenzia PB-01 e gli estintori corretti (Guided).</summary>
        private void SetHighlights(bool on)
        {
            emergencyStop.SetHighlight(on && generator.IsRunning);
            foreach (var e in extinguishers) e.SetHighlight(on && e.IsCorrectType);
        }

        /// <summary>true se la zona è sicura: corrente tolta e nessun fuoco acceso o in arrivo.</summary>
        private bool ZoneSafe() => !generator.IsRunning && !fire.IsBurning && pendingIgnitionAt < 0f && (ignitedAt < 0f || fire.WasExtinguished) && !exploded;

        /// <summary>true se il giocatore è nel raggio della vampata.</summary>
        private bool PlayerInBlast() => PlayerLocator.Head != null && InBlast(PlayerLocator.Feet);

        /// <summary>true se il punto è entro il raggio della vampata dal fuoco.</summary>
        private bool InBlast(Vector3 p) => Vector3.Distance(Flat(p), Flat(fire.transform.position)) < settings.BlastRadius;

        /// <summary>true se l'elemento con quell'ID è stato osservato.</summary>
        private bool IsObserved(string id) => observables.Any(o => o != null && o.HazardId == id && o.IsObserved);

        /// <summary>Istante dell'ultima osservazione tra cavo e pozza.</summary>
        private float LastObservedAt() => observables.Where(o => o != null && (o.HazardId == "cable" || o.HazardId == "pool")).Max(o => o.ObservedAt);

        /// <summary>Estintore tenuto in mano dal giocatore (null se nessuno).</summary>
        private Extinguisher CurrentlyHeldExtinguisher() => extinguishers.FirstOrDefault(e => e.HeldBy != null);

        /// <summary>Colleghi attivi nella scena.</summary>
        private IEnumerable<Bystander> AllBystanders()
        {
            foreach (var b in bystanders) if (b != null && b.gameObject.activeInHierarchy) yield return b;
            if (extraBystander != null && extraBystander.gameObject.activeInHierarchy) yield return extraBystander;
        }

        /// <summary>Proietta un punto sul piano orizzontale.</summary>
        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
