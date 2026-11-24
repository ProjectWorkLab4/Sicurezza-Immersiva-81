using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SI81.UI;
using UnityEngine;

namespace G_Ludovica.ForkliftChaos
{
    /// <summary>
    /// Logica del scenario 07 Forklift Chaos. Estende il controller comune: non costruisce UI, ma usa HUD, decisioni,
    /// sottotitoli, timer, punteggio e debrief di SI81.UI. Gestisce EV-01..EV-08, le cinque decisioni, i rischi visibili
    /// e nascosti, gli esiti e le variabili di replay.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/Scenari/Forklift Chaos/Scenario Controller")]
    public sealed class ForkliftChaosScenario : ScenarioController
    {
        private enum ChoiceKind { Correct, Risky, Critical, Missed }

        private struct ChoiceRecord
        {
            public string Id;
            public float Time;
            public ChoiceKind Kind;
            public string Text;
            public string Consequence;
            public string Dimension;
        }

        [Header("Riferimenti scena")]
        [SerializeField] private Transform head;
        [SerializeField] private GameObject marco;
        [SerializeField] private GameObject forklift1;
        [SerializeField] private GameObject forklift2;
        [SerializeField] private GameObject radioPickup;
        [SerializeField] private GameObject barrierPickup;
        [SerializeField] private GameObject barrierVisual;
        [SerializeField] private GameObject conesVisual;
        [SerializeField] private GameObject colleagueVisual;
        [SerializeField] private GameObject m2WarningShadow;
        [SerializeField] private GameObject pallet1;
        [SerializeField] private GameObject pallet2;
        [SerializeField] private Transform zoneLzB;
        [SerializeField] private Transform zoneLzC;
        [SerializeField] private Transform zoneSlippery;
        [SerializeField] private Transform blindCorner;
        [SerializeField] private Transform accessLine;
        [SerializeField] private Transform[] m2Path = new Transform[0];

        [Header("Zone e soglie")]
        [SerializeField] private float lzBRadius = 2.2f;
        [SerializeField] private float lzCRadius = 1.4f;
        [SerializeField] private float slipperyRadius = 1.5f;
        [SerializeField] private float blindCornerRadius = 3f;
        [SerializeField] private float runningSpeed = 2.1f;

        [Header("Replay")]
        [SerializeField] private ForkliftChaosReplayConfig replay;

        // Stato della missione.
        private bool observed;
        private bool blindCornerObserved;
        private bool m1Stopped;
        private bool radioPicked;
        private bool called112;
        private bool calledForeman;
        private bool barrierPlaced;
        private bool conesPlaced;
        private bool spoken;
        private bool breathingChecked;
        private bool woundPressed;
        private bool covered;
        private bool cprStarted;
        private bool aedUsed;
        private bool firstAidAsked;
        private bool marcoMoved;
        private bool marcoMovedSafely;
        private bool colleaguesEntered;
        private bool colleaguesHeld;
        private bool corridorBlocked;
        private bool m2Started;
        private bool m2Horn;
        private bool m2Stopped;
        private bool m2Hit;
        private bool pallet1Fallen;
        private bool pallet2Fallen;
        private bool slipped;
        private bool rescueArrived;
        private bool rescueDelayed;
        private bool apnea;
        private bool timeout;
        private bool finished;

        private float callTime = -1f;
        private float hornTime = -1f;
        private float rescueAt = -1f;
        private float rescueExtraDelay;
        private float extraExecutionPenalty;
        private int colleagueCount = 2;
        private bool pa02High;

        // Rilevamento zone e velocità.
        private Vector3 lastHeadPosition;
        private float lastHeadTime;
        private float headSpeed;
        private bool insideLzb;
        private bool insideLzc;
        private float lzbEntered;
        private float lzcEntered;
        private bool lzbWarned;
        private bool lzcWarned;

        private readonly List<ChoiceRecord> records = new List<ChoiceRecord>();
        private readonly List<Coroutine> routines = new List<Coroutine>();

        /// <summary>true se il giocatore ha raccolto la radio portatile.</summary>
        public bool HasRadio => radioPicked;

        // =====================================================================
        // Avvio
        // =====================================================================

        protected override void OnScenarioStarted()
        {
            ResetState();
            ApplyReplay();
            ResolveHead();

            if (head != null)
            {
                lastHeadPosition = head.position;
                lastHeadTime = Time.time;
            }

            SetPhase(PHASE_SCAN);
            SetObjective(0);
            SetHint("Osserva: Marco, Muletto 1, pallet in bilico, angolo cieco.");
            HUDService.StartTimer(300);

            StartTracked(IntroRoutine());
            StartTracked(TimedEventsRoutine());
        }

        /// <summary>Azzera tutti i flag locali (la scena viene comunque ricaricata tra i replay).</summary>
        private void ResetState()
        {
            observed = blindCornerObserved = m1Stopped = radioPicked = called112 = calledForeman = false;
            barrierPlaced = conesPlaced = spoken = breathingChecked = woundPressed = covered = false;
            cprStarted = aedUsed = firstAidAsked = marcoMoved = marcoMovedSafely = colleaguesEntered = colleaguesHeld = false;
            corridorBlocked = m2Started = m2Horn = m2Stopped = m2Hit = pallet1Fallen = pallet2Fallen = false;
            slipped = rescueArrived = rescueDelayed = apnea = timeout = finished = false;
            callTime = hornTime = rescueAt = -1f;
            rescueExtraDelay = extraExecutionPenalty = 0f;
            insideLzb = insideLzc = lzbWarned = lzcWarned = false;
            lzbEntered = lzcEntered = 0f;
            records.Clear();
        }

        /// <summary>Applica le variabili di replay e inizializza il generatore casuale.</summary>
        private void ApplyReplay()
        {
            if (replay == null) return;
            int attempt = PlayerPrefs.GetInt("FKC_Attempt", 0) + 1;
            PlayerPrefs.SetInt("FKC_Attempt", attempt);
            PlayerPrefs.Save();
            Random.InitState(replay.seed + attempt * 17);

            colleagueCount = replay.fixedReplay
                ? replay.colleagueCountMin
                : Random.Range(replay.colleagueCountMin, replay.colleagueCountMax + 1);
            pa02High = replay.pa02High;

            if (pallet2 != null && pa02High)
                pallet2.transform.position += new Vector3(0f, 0f, 3.5f);

            if (radioPickup != null && replay.rackEast)
                radioPickup.transform.position += new Vector3(3f, 0f, 0f);

            if (colleagueVisual != null && colleagueCount <= 1)
                colleagueVisual.transform.localScale = new Vector3(0.6f, 1f, 0.6f);
        }

        private void ResolveHead()
        {
            if (head != null) return;
            head = UIScreenManager.Instance != null ? UIScreenManager.Instance.Head : null;
            if (head == null && Camera.main != null) head = Camera.main.transform;
        }

        private Coroutine StartTracked(IEnumerator routine)
        {
            Coroutine c = StartCoroutine(routine);
            routines.Add(c);
            return c;
        }

        protected override void OnDestroy()
        {
            StopAllCoroutines();
            base.OnDestroy();
        }

        // =====================================================================
        // Introduzione e decisioni
        // =====================================================================

        private IEnumerator IntroRoutine()
        {
            yield return new WaitForSeconds(0.6f);
            Alert("Aiuto!\nMarco è a terra nella corsia 3", "ICN_Warning_Filled");
            Subtitle("Marco", "Aiuto! Il carrello... non spostarmi.", 5f);
            HUDService.ShowSoundEvent("Urlo da terra", true);
            HUDService.Pulse(0.6f, 0.25f);
            Record("EV-01", "Urlo udito, timer avviato.", ChoiceKind.Correct, "Awareness");

            yield return new WaitForSeconds(2.5f);
            AskObservationDecision();
        }

        private void AskObservationDecision()
        {
            SetPhase(PHASE_DECISION);
            float timeout = Level == DifficultyLevel.Guided ? 14f : Level == DifficultyLevel.Operational ? 9f : 6f;
            AskDecision("Prima di intervenire, cosa fai?",
                new[] { "Osserva dalla soglia", "Corri verso Marco", "Afferra il pallet" },
                index =>
                {
                    SetPhase(PHASE_ACTION);
                    if (index == 0)
                    {
                        observed = true;
                        blindCornerObserved = head != null && blindCorner != null &&
                                              Vector3.Distance(head.position, blindCorner.position) <= blindCornerRadius;
                        Record("LG-01", "Individuati M1 acceso, pallet instabile e angolo cieco.", ChoiceKind.Correct, "Awareness");
                        Feedback("Osservazione corretta.", FeedbackKind.Positive);
                        UpdateObjectives();
                    }
                    else if (index == 1)
                    {
                        Record("LG-03", "Esposto a Muletto 2 e pallet senza osservare.", ChoiceKind.Risky, "Time");
                        Feedback("Entrare di corsa non dà punti Time.", FeedbackKind.Negative);
                    }
                    else if (index == 2)
                    {
                        Record("LG-19", "Pallet urtato: rischio di caduta su Marco.", ChoiceKind.Critical, "Safety");
                        TriggerPallet1Fall(true);
                    }
                    else
                    {
                        Record("LG-02", "Angolo cieco non controllato.", ChoiceKind.Missed, "Awareness");
                    }
                    StartTracked(SecondDecisionRoutine());
                }, timeout);
        }

        private IEnumerator SecondDecisionRoutine()
        {
            yield return new WaitForSeconds(5f);
            if (finished) yield break;
            SetPhase(PHASE_DECISION);
            float timeout = Level == DifficultyLevel.Guided ? 14f : 9f;
            AskDecision("Come metti in sicurezza la corsia?",
                new[] { "Spegni M1 e posa BL-01", "Solo radio al capo turno", "Sposta Marco subito" },
                index =>
                {
                    SetPhase(PHASE_ACTION);
                    if (index == 0)
                    {
                        Feedback("Spegni M1 dal comando e posa la barriera sul rack.", FeedbackKind.Positive);
                        SetHint("Comando M1: F1. Barriera: F5, coni: F6.");
                    }
                    else if (index == 1)
                    {
                        Record("LG-05", "Muletto 1 lasciato acceso: niente Successo.", ChoiceKind.Missed, "Safety");
                        SetHint("La radio non sostituisce lo spegnimento di M1.");
                    }
                    else if (index == 2)
                    {
                        MoveMarco(false);
                    }
                    else
                    {
                        Record("LG-05", "Muletto 1 non spento.", ChoiceKind.Missed, "Safety");
                    }
                }, timeout);
        }

        private void AskRadioDecision()
        {
            if (called112 || finished) return;
            SetPhase(PHASE_DECISION);
            AskDecision("Hai la radio. Chi chiami?",
                new[] { "112 e capo turno", "Solo capo turno", "Nessuno" },
                index =>
                {
                    SetPhase(PHASE_ACTION);
                    if (index == 0)
                    {
                        calledForeman = true;
                        Call112();
                    }
                    else if (index == 1)
                    {
                        calledForeman = true;
                        Record("LG-07", "112 non chiamato: soccorsi non programmati.", ChoiceKind.Missed, "Decision");
                    }
                    else
                    {
                        Record("LG-07", "Nessuna chiamata: soccorsi non programmati.", ChoiceKind.Missed, "Decision");
                    }
                }, Level == DifficultyLevel.Guided ? 12f : 7f);
        }

        private void AskRescueDecision()
        {
            SetPhase(PHASE_DECISION);
            AskDecision("Marco è confuso. Cosa fai?",
                new[] { "Parla, controlla il respiro, comprimi e copri", "Lo sposto subito", "Aspetto i soccorsi" },
                index =>
                {
                    SetPhase(PHASE_ACTION);
                    if (index == 0)
                    {
                        spoken = true;
                        breathingChecked = true;
                        Record("LG-13", "Stato di Marco verificato.", ChoiceKind.Correct, "Execution");
                        Feedback("Ora premi sulla ferita e copri Marco.", FeedbackKind.Positive);
                        UpdateObjectives();
                        AskFirstAidDecision();
                    }
                    else if (index == 1)
                    {
                        MoveMarco(false);
                    }
                    else
                    {
                        Record("LG-14", "Nessun intervento su Marco.", ChoiceKind.Missed, "Execution");
                    }
                }, Level == DifficultyLevel.Guided ? 14f : 8f);
        }

        /// <summary>Passi di primo soccorso selezionabili col raggio sul pannello (oltre ai tasti F9/F10 in Editor).</summary>
        private void AskFirstAidDecision()
        {
            if (firstAidAsked || finished || !IsRunning) return;
            firstAidAsked = true;
            SetPhase(PHASE_DECISION);
            AskDecision("Come assisti Marco?",
                new[] { "Comprimi e copri", "Sposta Marco", "Attendi soccorsi" },
                index =>
                {
                    SetPhase(PHASE_ACTION);
                    if (index == 0)
                    {
                        woundPressed = true;
                        covered = true;
                        Feedback("Compressione e copertura eseguite.", FeedbackKind.Positive);
                        CheckWoundAndCover();
                    }
                    else if (index == 1)
                    {
                        MoveMarco(false);
                    }
                    else
                    {
                        Record("LG-14", "Nessun intervento su Marco.", ChoiceKind.Missed, "Execution");
                    }
                    UpdateObjectives();
                }, Level == DifficultyLevel.Guided ? 16f : 9f);
        }

        /// <summary>Variante apnea: RCP 30:2 e DAE scelti dal pannello, con scadenza gestita da ApneaRoutine.</summary>
        private void AskApneaDecision()
        {
            if (finished || !IsRunning) return;
            SetPhase(PHASE_DECISION);
            AskDecision("Marco non respira. Cosa fai?",
                new[] { "RCP 30:2 e DAE", "Controlla ancora", "Sposta Marco" },
                index =>
                {
                    SetPhase(PHASE_ACTION);
                    if (index == 0)
                    {
                        cprStarted = true;
                        aedUsed = true;
                        Record("LG-15", "RCP 30:2 e DAE avviati dopo il respiro assente.", ChoiceKind.Correct, "Execution");
                        Feedback("RCP e DAE avviati: continua fino ai soccorsi.", FeedbackKind.Positive);
                    }
                    else if (index == 1)
                    {
                        Record("LG-16", "Respiro dubbio: agisci come se fosse assente.", ChoiceKind.Missed, "Execution");
                    }
                    else
                    {
                        MoveMarco(false);
                    }
                }, Level == DifficultyLevel.Guided ? 14f : 8f);
        }

        private void AskM2Decision()
        {
            SetPhase(PHASE_DECISION);
            AskDecision("Muletto 2 arriva dall'angolo cieco. Come lo fermi?",
                new[] { "Barriera BL-01", "Radio al capo turno", "Corri nella corsia" },
                index =>
                {
                    SetPhase(PHASE_ACTION);
                    if (index == 0)
                    {
                        if (!barrierPlaced)
                        {
                            barrierPlaced = true;
                            SetBarrierActive(true);
                            Record("LG-06", "Barriera chiusa prima dell'arrivo di M2.", ChoiceKind.Correct, "Safety");
                        }
                        Record("LG-09", "Muletto fermato con la barriera.", ChoiceKind.Correct, "Decision");
                    }
                    else if (index == 1)
                    {
                        if (!calledForeman) calledForeman = true;
                        Record("LG-09", "Muletto fermato via radio.", ChoiceKind.Correct, "Decision");
                    }
                    else
                    {
                        Record("LG-10", "Sei corso nella corsia davanti al mezzo.", ChoiceKind.Critical, "Safety");
                    }
                }, 7f);
        }

        // =====================================================================
        // Interazioni
        // =====================================================================

        /// <summary>Riceve un'interazione da ForkliftChaosInteractable (o da un pulsante di debug).</summary>
        public void HandleInteraction(ForkliftChaosAction action, ForkliftChaosInteractable source = null)
        {
            if (!IsRunning || finished) return;
            switch (action)
            {
                case ForkliftChaosAction.StopForklift1:
                    if (!m1Stopped)
                    {
                        m1Stopped = true;
                        if (forklift1 != null) forklift1.transform.Rotate(0f, 0f, 8f);
                        Record("LG-04", "Pericolo vicino a Marco eliminato.", ChoiceKind.Correct, "Safety");
                        Feedback("Muletto 1 spento.", FeedbackKind.Positive);
                        UpdateObjectives();
                    }
                    break;

                case ForkliftChaosAction.PickupRadio:
                    if (!radioPicked)
                    {
                        radioPicked = true;
                        if (radioPickup != null) radioPickup.SetActive(false);
                        Feedback("Radio portatile raccolta.", FeedbackKind.Positive);
                        AskRadioDecision();
                    }
                    break;

                case ForkliftChaosAction.Call112:
                    Call112();
                    break;

                case ForkliftChaosAction.CallForeman:
                    CallForeman();
                    break;

                case ForkliftChaosAction.PlaceBarrier:
                    if (!barrierPlaced)
                    {
                        barrierPlaced = true;
                        SetBarrierActive(true);
                        if (barrierPickup != null) barrierPickup.SetActive(false);
                        Record("LG-06", "Corsia delimitata: M2 e colleghi non entrano.", ChoiceKind.Correct, "Safety");
                        Feedback("Barriera BL-01 posata.", FeedbackKind.Positive);
                        UpdateObjectives();
                    }
                    break;

                case ForkliftChaosAction.PlaceCones:
                    if (!conesPlaced)
                    {
                        conesPlaced = true;
                        if (conesVisual != null) conesVisual.SetActive(true);
                        Feedback("Coni posizionati lungo BL-01.", FeedbackKind.Positive);
                        UpdateObjectives();
                    }
                    break;

                case ForkliftChaosAction.TalkToMarco:
                    if (!spoken)
                    {
                        spoken = true;
                        Record("LG-13", "Marco ha risposto: non spostarlo.", ChoiceKind.Correct, "Execution");
                        Subtitle("Marco", "Ho male qui... non mi muovere.", 4f);
                    }
                    break;

                case ForkliftChaosAction.CheckBreathing:
                    if (!breathingChecked)
                    {
                        breathingChecked = true;
                        Record("LG-13", "Respiro controllato: scegli il soccorso corretto.", ChoiceKind.Correct, "Execution");
                        Feedback("Respiro verificato.", FeedbackKind.Positive);
                        if (!spoken) AskRescueDecision();
                        else AskFirstAidDecision();
                    }
                    break;

                case ForkliftChaosAction.PressWound:
                    if (!woundPressed)
                    {
                        woundPressed = true;
                        Feedback("Compressione sulla ferita.", FeedbackKind.Positive);
                        CheckWoundAndCover();
                    }
                    break;

                case ForkliftChaosAction.CoverMarco:
                    if (!covered)
                    {
                        covered = true;
                        Feedback("Marco coperto.", FeedbackKind.Positive);
                        CheckWoundAndCover();
                    }
                    break;

                case ForkliftChaosAction.StartCPR:
                    if (!cprStarted)
                    {
                        cprStarted = true;
                        Record("LG-15", "RCP 30:2 avviata.", ChoiceKind.Correct, "Execution");
                        Feedback("RCP 30:2 in corso: usa il DAE.", FeedbackKind.Positive);
                        CheckCPR();
                    }
                    break;

                case ForkliftChaosAction.UseAED:
                    if (!aedUsed)
                    {
                        aedUsed = true;
                        Feedback("DAE acceso, segui le istruzioni vocali.", FeedbackKind.Positive);
                        CheckCPR();
                    }
                    break;

                case ForkliftChaosAction.MoveMarco:
                    MoveMarco(true);
                    break;

                case ForkliftChaosAction.KeepColleaguesOut:
                    CallForeman();
                    break;

                case ForkliftChaosAction.StopForklift2:
                    if (m2Horn && !m2Stopped)
                    {
                        m2Stopped = true;
                        float reaction = Time.time - hornTime;
                        Record("LG-09", "Muletto 2 fermato con la radio in " + reaction.ToString("0") + " s.",
                            ChoiceKind.Correct, "Decision");
                        Feedback("Mezzo fermo prima dell'accesso.", FeedbackKind.Positive);
                        SetM2Warning(false);
                    }
                    else
                    {
                        Feedback("Usa la radio quando senti il clacson.", FeedbackKind.Neutral);
                    }
                    break;
            }
        }

        private void SetBarrierActive(bool active)
        {
            if (barrierVisual != null) barrierVisual.SetActive(active);
        }

        private void SetM2Warning(bool active)
        {
            if (m2WarningShadow != null) m2WarningShadow.SetActive(active);
        }

        private void MoveMarco(bool fromInteraction)
        {
            if (marcoMoved) return;
            bool immediateDanger = (lzbWarned && insideLzb) || (m2Horn && !m2Stopped && m2Started) || pallet1Fallen;
            marcoMoved = true;
            if (immediateDanger)
            {
                marcoMovedSafely = true;
                if (marco != null) marco.transform.position += new Vector3(0f, 0f, -2f);
                Record("LG-12", "Spostato il minimo indispensabile per pericolo immediato (eccezione).",
                    ChoiceKind.Correct, "Decision");
                Feedback("Spostamento di emergenza: testa sostenuta, max 3 m.", FeedbackKind.Positive);
            }
            else
            {
                Record("LG-11", "Marco spostato senza pericolo immediato: peggioramento, livello fallito.",
                    ChoiceKind.Critical, "Safety");
                Feedback("Non si sposta Marco senza pericolo immediato.", FeedbackKind.Negative);
                Finish();
            }
        }

        private void CheckWoundAndCover()
        {
            if (!woundPressed || !covered || !breathingChecked) return;
            Record("LG-14", "Sanguinamento e calore sotto controllo.", ChoiceKind.Correct, "Execution");
            UpdateObjectives();
        }

        private void CheckCPR()
        {
            if (!cprStarted) return;
            if (aedUsed || Time.time - hornTime > 2f)
                Record("LG-15", "RCP e DAE sostenuti fino all'arrivo dei soccorsi.", ChoiceKind.Correct, "Execution");
        }

        private void Call112()
        {
            if (called112 || finished) return;
            called112 = true;
            callTime = ElapsedSeconds;
            if (Time.time - lastHeadTime > 60f)
                Record("LG-08", "Chiamata dopo 60 s: soccorsi più lenti.", ChoiceKind.Risky, "Time");
            else
                Record("LG-07", "112 e capo turno chiamati: soccorsi in arrivo.", ChoiceKind.Correct, "Decision");

            Alert("112 chiamato\nResta vicino a Marco", "ICN_CheckCircle");
            Subtitle("Capo turno", "Ho avvisato il 112. Tieni libera la corsia.", 5f);
            rescueAt = ElapsedSeconds + Random.Range(120f, 210f);
            StartTracked(M2Routine(RandomM2Delay()));
            StartTracked(RescueRoutine());
            UpdateObjectives();
        }

        private void CallForeman()
        {
            if (calledForeman) return;
            calledForeman = true;
            colleaguesHeld = true;
            Record("LG-17", "Capo turno avvisato: colleghi tenuti fuori.", ChoiceKind.Correct, "Safety");
            Feedback("Colleghi fermati dal capo turno.", FeedbackKind.Positive);
        }

        // =====================================================================
        // Eventi dinamici
        // =====================================================================

        private IEnumerator TimedEventsRoutine()
        {
            StartTracked(NoCallM2Routine());
            StartTracked(ColleaguesRoutine());
            StartTracked(BlindCornerCheckRoutine());
            StartTracked(MarcoWorsenRoutine());
            StartTracked(ApneaRoutine());

            while (!finished)
            {
                float t = ElapsedSeconds;
                if (t > 30f && !blindCornerObserved && !observed)
                {
                    Record("LG-02", "Angolo cieco non controllato entro 30 s.", ChoiceKind.Missed, "Awareness");
                    blindCornerObserved = true;
                }
                if (t > 20f && head != null && marco != null &&
                    Vector3.Distance(head.position, marco.transform.position) < 3.2f && !spoken && !breathingChecked)
                {
                    AskRescueDecision();
                    break;
                }
                yield return new WaitForSeconds(1f);
            }
        }

        private IEnumerator NoCallM2Routine()
        {
            yield return new WaitForSeconds((replay != null ? replay.m2DelayWithoutCall : 150f));
            if (!called112 && !m2Started) StartTracked(M2Routine(0f));
        }

        private float RandomM2Delay()
        {
            if (replay == null) return Random.Range(30f, 90f);
            Vector2 range = Level == DifficultyLevel.Guided ? replay.m2DelayAfterCallGuided : replay.m2DelayAfterCallOperational;
            return replay.fixedReplay ? range.x : Random.Range(range.x, range.y);
        }

        private IEnumerator M2Routine(float delay)
        {
            if (m2Started) yield break;
            m2Started = true;
            yield return new WaitForSeconds(Mathf.Max(0f, delay));

            float lead = replay != null ? replay.hornLeadSeconds : 6f;
            m2Horn = true;
            hornTime = Time.time;
            Alert("Clacson!\nMuletto in arrivo dall'angolo cieco", "ICN_Warning_Filled");
            Subtitle("Ambiente", "[Clacson: muletto in arrivo]", 4f);
            HUDService.ShowSoundEvent("Clacson: muletto in arrivo", true);
            HUDService.Pulse(0.8f, 0.4f, false);
            if (Level != DifficultyLevel.Crisis) SetM2Warning(true);
            if (Level == DifficultyLevel.Guided)
                Feedback("Preavviso 6 s: barriera F5 o radio F11.", FeedbackKind.Neutral);
            Record("EV-02", "Clacson e avvisi percepiti.", ChoiceKind.Correct, "Awareness");

            if (Time.time - hornTime < 0.1f) AskM2Decision();
            yield return new WaitForSeconds(lead);
            yield return MoveM2Routine();
        }

        private IEnumerator MoveM2Routine()
        {
            if (forklift2 == null) yield break;
            Vector3 start = forklift2.transform.position;
            Transform[] path = m2Path != null && m2Path.Length > 1 ? m2Path : new Transform[] { null, null };
            int stopIndex = barrierPlaced ? Mathf.Min(2, path.Length - 1) : -1;
            float segmentDuration = 8f / Mathf.Max(1, path.Length - 1);

            for (int i = 0; i < path.Length - 1; i++)
            {
                if (finished) yield break;
                Vector3 from = i == 0 ? start : (path[i] != null ? path[i].position : start);
                Vector3 to = path[i + 1] != null ? path[i + 1].position :
                             (marco != null ? marco.transform.position : start);
                float elapsed = 0f;
                while (elapsed < segmentDuration)
                {
                    elapsed += Time.deltaTime;
                    float k = Mathf.Clamp01(elapsed / segmentDuration);
                    forklift2.transform.position = Vector3.Lerp(from, to, k);
                    Vector3 dir = to - from;
                    if (dir.sqrMagnitude > 0.01f)
                        forklift2.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
                    yield return null;
                }
                if (m2Stopped || (stopIndex >= 0 && i >= stopIndex - 1))
                {
                    if (!m2Stopped)
                    {
                        m2Stopped = true;
                        Record("LG-09", "Muletto fermato dalla barriera prima dell'accesso.", ChoiceKind.Correct, "Decision");
                    }
                    SetM2Warning(false);
                    yield break;
                }
            }

            if (barrierPlaced || m2Stopped)
            {
                SetM2Warning(false);
                yield break;
            }

            m2Hit = true;
            Record("LG-10", "Muletto 2 non fermato: investimento.", ChoiceKind.Critical, "Safety");
            Alert("Investimento!\nMuletto non fermato", "ICN_Warning_Filled");
            Finish();
        }

        private IEnumerator ColleaguesRoutine()
        {
            float delay = replay != null ? Random.Range(replay.colleagueArrivalAfterShout.x, replay.colleagueArrivalAfterShout.y) : Random.Range(60f, 90f);
            yield return new WaitForSeconds(delay);
            if (finished) yield break;

            if (barrierPlaced || colleaguesHeld)
            {
                if (!colleaguesHeld) Record("LG-17", "Colleghi fermati dalla barriera.", ChoiceKind.Correct, "Safety");
                Feedback("Colleghi tenuti fuori dalla corsia.", FeedbackKind.Positive);
                yield break;
            }

            colleaguesEntered = true;
            corridorBlocked = true;
            rescueExtraDelay = 60f;
            if (colleagueVisual != null) colleagueVisual.SetActive(true);
            Record("LG-18", "Colleghi entrati: soccorsi ritardati, niente Successo.", ChoiceKind.Risky, "Decision");
            Alert("Colleghi in corsia\n[Voci e passi dal corridoio]", "ICN_Warning");
            Subtitle("Ambiente", "[Voci e passi dal corridoio]", 4f);
        }

        private IEnumerator BlindCornerCheckRoutine()
        {
            while (!finished && ElapsedSeconds < 30f)
            {
                if (head != null && blindCorner != null &&
                    Vector3.Distance(head.position, blindCorner.position) <= blindCornerRadius)
                {
                    if (!blindCornerObserved)
                    {
                        blindCornerObserved = true;
                        Feedback("Hai controllato l'angolo cieco.", FeedbackKind.Positive);
                    }
                    yield break;
                }
                yield return new WaitForSeconds(0.5f);
            }
        }

        private IEnumerator MarcoWorsenRoutine()
        {
            yield return new WaitForSeconds(180f);
            if (finished || covered || (cprStarted && aedUsed)) yield break;
            extraExecutionPenalty += 10f;
            Record("LG-14", "Marco non coperto entro 3 minuti.", ChoiceKind.Missed, "Execution");
            Alert("Marco peggiora\nRespiro e sanguinamento", "ICN_Warning_Filled");
        }

        private IEnumerator ApneaRoutine()
        {
            float at = replay != null ? replay.apneaAtSeconds : 150f;
            yield return new WaitForSeconds(at);
            if (finished) yield break;

            bool should = replay != null && replay.forceApnea;
            if (replay != null && !should)
            {
                float chance = Level == DifficultyLevel.Guided ? replay.apneaChanceGuided :
                               Level == DifficultyLevel.Operational ? replay.apneaChanceOperational : replay.apneaChanceCrisis;
                should = Random.value < chance;
            }
            if (!should) yield break;

            apnea = true;
            Alert("Marco non respira\nInizia RCP 30:2 e usa il DAE", "ICN_Warning_Filled");
            Subtitle("Capo turno", "Se il respiro è dubbio, agisci come se fosse assente.", 6f);
            AskApneaDecision();
            yield return new WaitForSeconds(replay != null ? replay.cprDeadlineSeconds : 120f);
            if (finished || cprStarted) yield break;
            Record("LG-16", "Apnea senza RCP entro 120 s: livello fallito.", ChoiceKind.Critical, "Execution");
            Finish();
        }

        private IEnumerator RescueRoutine()
        {
            while (!finished && rescueAt > 0f && ElapsedSeconds < rescueAt)
                yield return new WaitForSeconds(1f);
            if (finished) yield break;

            if (rescueExtraDelay > 0f)
            {
                rescueDelayed = true;
                yield return new WaitForSeconds(rescueExtraDelay);
            }
            if (finished) yield break;

            rescueArrived = true;
            Alert("Soccorsi arrivati\nFine scenario", "ICN_CheckCircle");
            HUDService.ShowSoundEvent("Sirena dei soccorsi", true);
            Finish();
        }

        // =====================================================================
        // Rischi, zone e obiettivi
        // =====================================================================

        private void Update()
        {
            if (!IsRunning || finished) return;
            UpdateHeadSpeed();
            CheckZone(zoneLzB, lzBRadius, ref insideLzb, ref lzbEntered, ref lzbWarned, true);
            CheckZone(zoneLzC, lzCRadius, ref insideLzc, ref lzcEntered, ref lzcWarned, false);
            CheckSlippery();
        }

        private void UpdateHeadSpeed()
        {
            if (head == null) return;
            float now = Time.time;
            float dt = Mathf.Max(0.001f, now - lastHeadTime);
            headSpeed = Vector3.Distance(head.position, lastHeadPosition) / dt;
            lastHeadPosition = head.position;
            lastHeadTime = now;
        }

        private void CheckZone(Transform zone, float radius, ref bool inside, ref float entered, ref bool warned, bool isPallet1)
        {
            if (zone == null || head == null) return;
            bool now = Vector3.Distance(head.position, zone.position) <= radius;
            if (now && !inside)
            {
                inside = true;
                entered = ElapsedSeconds;
                warned = false;
            }
            else if (!now && inside)
            {
                inside = false;
            }

            if (!inside || warned) return;
            float stay = ElapsedSeconds - entered;
            if (stay < 1f) return;

            warned = true;
            if (isPallet1)
            {
                Subtitle("Ambiente", "[Pallet instabile: allontanati]", 4f);
                Alert("Pallet instabile\nAllontanati dalla pila", "ICN_Warning_Filled");
                HUDService.Pulse(0.7f, 0.3f, false);
            }
            else if (Level == DifficultyLevel.Guided)
            {
                Subtitle("Ambiente", "[Pallet instabile: allontanati]", 4f);
                Alert("Secondo pallet instabile\nEsci da LZ-C", "ICN_Warning_Filled");
            }
            StartTracked(PalletFallRoutine(isPallet1));
        }

        private IEnumerator PalletFallRoutine(bool isPallet1)
        {
            yield return new WaitForSeconds(4f);
            if (finished) yield break;
            GameObject target = isPallet1 ? pallet1 : pallet2;
            if (target != null)
                target.transform.Rotate(isPallet1 ? 24f : -20f, 0f, 0f);
            if (isPallet1) TriggerPallet1Fall(false);
            else TriggerPallet2Fall(false);
        }

        private void TriggerPallet1Fall(bool ontoMarco)
        {
            if (pallet1Fallen) return;
            pallet1Fallen = true;
            if (pallet1 != null) pallet1.transform.Rotate(30f, 0f, 0f);
            bool hitsMarco = ontoMarco || (marco != null && !marcoMoved &&
                                           Vector3.Distance(pallet1.transform.position, marco.transform.position) < 3f);
            if (hitsMarco)
            {
                Record("LG-19", "Pallet caduto su Marco: nuovo ferimento, livello fallito.", ChoiceKind.Critical, "Safety");
                Finish();
            }
            else
            {
                Record("LG-19", "Pallet caduto di lato: avviso, nessun ferimento.", ChoiceKind.Risky, "Safety");
                Feedback("Pallet caduto di lato: area da delimitare.", FeedbackKind.Negative);
            }
        }

        private void TriggerPallet2Fall(bool underPlayer)
        {
            if (pallet2Fallen) return;
            pallet2Fallen = true;
            corridorBlocked = true;
            rescueExtraDelay = Mathf.Max(rescueExtraDelay, 60f);
            if (pallet2 != null) pallet2.transform.Rotate(-24f, 0f, 0f);
            Record("LG-20", "Corsia ostrutta: soccorsi ritardati di 60 s.", ChoiceKind.Risky, "Safety");
            Alert("Corsia ostruita\nSoccorsi ritardati", "ICN_Warning_Filled");
            if (underPlayer)
            {
                Record("EV-07", "Giocatore colpito dal secondo pallet.", ChoiceKind.Critical, "Safety");
                Finish();
            }
        }

        private void CheckSlippery()
        {
            if (slipped || zoneSlippery == null || head == null || marco == null) return;
            if (headSpeed < runningSpeed) return;
            if (Vector3.Distance(head.position, zoneSlippery.position) > slipperyRadius) return;
            if (Vector3.Distance(head.position, marco.transform.position) > slipperyRadius + 0.5f) return;

            slipped = true;
            Record("LG-21", "Scivolato vicino a Marco: rallentamento e penalità.", ChoiceKind.Risky, "Safety");
            Alert("Pavimento scivoloso\nCammina, non correre", "ICN_Warning");
            Subtitle("Ambiente", "[Pavimento scivoloso]", 3f);
            Feedback("Nessuno spostamento forzato: solo penalità.", FeedbackKind.Negative);
        }

        private void UpdateObjectives()
        {
            if (observed && CurrentObjectiveIndex == 0) CompleteObjective();
            if ((called112 || calledForeman) && m1Stopped && CurrentObjectiveIndex == 1) CompleteObjective();
            if ((m1Stopped || m2Stopped) && CurrentObjectiveIndex == 2) CompleteObjective();
            if (barrierPlaced && conesPlaced && CurrentObjectiveIndex == 3) CompleteObjective();
        }

        // =====================================================================
        // Chiusura, punteggio e debrief
        // =====================================================================

        protected override void OnTimerExpired()
        {
            if (finished) return;
            timeout = true;
            Record("TIMEOUT", "Tempo scaduto prima di mettere in sicurezza l'area.", ChoiceKind.Critical, "Time");
            Finish();
        }

        private void Finish()
        {
            if (finished) return;
            finished = true;
            SetPhase(PHASE_RESOLUTION);
            SetM2Warning(false);

            bool critical = records.Any(r => r.Kind == ChoiceKind.Critical);
            bool assisted = spoken && breathingChecked && ((woundPressed && covered) || (cprStarted && aedUsed));
            bool accessFree = !colleaguesEntered && !pallet2Fallen;
            bool success = !critical && !timeout && rescueArrived && barrierPlaced && m1Stopped &&
                           accessFree && assisted && !marcoMoved && !rescueDelayed && !m2Hit &&
                           (m2Stopped || !m2Started);
            OutcomeType outcome = critical || timeout || m2Hit ? OutcomeType.Negative :
                                  success ? OutcomeType.Positive : OutcomeType.Partial;

            int[] dims = ComputeDimensions(success);
            int score = Mathf.Clamp(Mathf.RoundToInt((float)dims.Average()), 0, 100);
            List<string> lines = BuildDebrief(outcome, success, dims);

            EndMission(outcome, score, dims, lines);
            StopAllCoroutines();
            routines.Clear();
        }

        private int[] ComputeDimensions(bool success)
        {
            int safety = 100;
            int awareness = 100;
            int decision = 100;
            int execution = 100;
            int time = 100;

            if (!observed) awareness -= 20;
            if (!blindCornerObserved) awareness -= 12;
            if (!m1Stopped) safety -= 18;
            if (!barrierPlaced || !conesPlaced) safety -= 18;
            if (!radioPicked) decision -= 10;
            if (!called112) decision -= 25;
            if (!calledForeman) decision -= 8;
            if (colleaguesEntered) decision -= 12;
            if (!m2Stopped && m2Started) decision -= 20;
            if (!spoken || !breathingChecked) execution -= 15;
            if (!woundPressed || !covered) execution -= 12;
            if (apnea && !cprStarted) execution -= 35;
            if (aedUsed && cprStarted) execution += 4;
            if (marcoMoved && !marcoMovedSafely) safety -= 100;
            if (pallet1Fallen) safety -= 25;
            if (pallet2Fallen) safety -= 18;
            if (slipped) safety -= 6;
            if (rescueDelayed) time -= 15;
            if (callTime > 60f) time -= 10;
            if (callTime < 0f) time -= 20;
            if (m2Horn && hornTime > 0f && Time.time - hornTime > 6f && !m2Stopped) time -= 10;
            if (timeout) time -= 40;
            if (success) execution += 5;

            return new[]
            {
                Mathf.Clamp(safety, 0, 100),
                Mathf.Clamp(awareness, 0, 100),
                Mathf.Clamp(decision, 0, 100),
                Mathf.Clamp(execution, 0, 100),
                Mathf.Clamp(time, 0, 100)
            };
        }

        private List<string> BuildDebrief(OutcomeType outcome, bool success, int[] dims)
        {
            var lines = new List<string>();
            string title = outcome == OutcomeType.Positive ? "SUCCESSO" : outcome == OutcomeType.Partial ? "PARZIALE" : "FALLIMENTO";
            lines.Add("= " + title + " · SPI " + dims.Average().ToString("0") + "/100 · Safety " + dims[0] +
                      ", Awareness " + dims[1] + ", Decision " + dims[2] + ", Execution " + dims[3] + ", Time " + dims[4] + ".");

            var ordered = records
                .OrderByDescending(r => r.Kind == ChoiceKind.Critical ? 3 : r.Kind == ChoiceKind.Missed ? 2 : r.Kind == ChoiceKind.Risky ? 1 : 0)
                .ThenBy(r => r.Time)
                .ToList();

            foreach (ChoiceRecord r in ordered)
            {
                string prefix = r.Kind == ChoiceKind.Correct ? DebriefLinePrefix.GOOD :
                                r.Kind == ChoiceKind.Risky ? DebriefLinePrefix.BAD :
                                r.Kind == ChoiceKind.Critical ? DebriefLinePrefix.CRITICAL : DebriefLinePrefix.BAD;
                string verdict = r.Kind == ChoiceKind.Correct ? "Corretto" : r.Kind == ChoiceKind.Risky ? "Rischioso" :
                                 r.Kind == ChoiceKind.Critical ? "Errore critico" : "Mancato";
                lines.Add(prefix + "Al minuto " + FormatTime(r.Time) + " [" + verdict + "] " + r.Id + ": " + r.Text +
                          " Conseguenza: " + r.Consequence + ".");
            }

            if (!m1Stopped) lines.Add(DebriefLinePrefix.BAD + "Mancato: Muletto 1 non spento. Niente Successo.");
            if (!barrierPlaced) lines.Add(DebriefLinePrefix.BAD + "Mancato: barriera BL-01 non posata. Niente Successo.");
            if (colleaguesEntered) lines.Add(DebriefLinePrefix.BAD + "I colleghi sono entrati in corsia: soccorsi ritardati.");
            if (rescueDelayed) lines.Add(DebriefLinePrefix.BAD + "Soccorsi arrivati con 60 s di ritardo.");
            if (marcoMoved && !marcoMovedSafely)
                lines.Add(DebriefLinePrefix.CRITICAL + "Marco spostato senza pericolo immediato: eccezione non applicabile.");
            else if (marcoMovedSafely)
                lines.Add(DebriefLinePrefix.GOOD + "Spostamento di Marco solo per pericolo immediato: eccezione prevista.");

            lines.Add(DebriefLinePrefix.GOOD + "Percorso corretto: 1) osserva; 2) spegni M1 e posa BL-01 con i coni; " +
                      "3) prendi la radio e chiama 112 e capo turno; 4) parla con Marco, controlla il respiro, comprimi e copri " +
                      "(RCP 30:2 e DAE se non respira); 5) non spostare Marco salvo pericolo immediato (max 3 m, testa sostenuta); " +
                      "6) tieni fuori i colleghi; 7) ferma M2 con barriera o radio; 8) mantieni la corsia libera fino ai soccorsi.");
            lines.Add(DebriefLinePrefix.GOOD + "Nota: in Italia il numero unico di emergenza è il 112; l'organizzazione regionale può variare.");
            return lines;
        }

        private void Record(string id, string consequence, ChoiceKind kind, string dimension)
        {
            records.Add(new ChoiceRecord
            {
                Id = id,
                Time = ElapsedSeconds,
                Kind = kind,
                Text = consequence,
                Consequence = consequence,
                Dimension = dimension
            });
        }

        private static string FormatTime(float seconds)
        {
            int total = Mathf.Max(0, Mathf.RoundToInt(seconds));
            return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
        }
    }
}
