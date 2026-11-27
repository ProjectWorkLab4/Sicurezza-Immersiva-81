using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SI81.Ludovica.ElectricShock;
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
        [Tooltip("Animator del modello di Marco: il respiro si ferma durante l'apnea.")]
        [SerializeField] private Animator marcoAnimator;
        [Tooltip("Gruppo dei soccorritori, attivato quando arrivano i soccorsi.")]
        [SerializeField] private GameObject rescueTeam;
        [SerializeField] private GameObject forklift1;
        [SerializeField] private GameObject forklift2;
        [SerializeField] private GameObject radioPickup;
        [SerializeField] private GameObject barrierPickup;
        [SerializeField] private GameObject barrierVisual;
        [SerializeField] private GameObject conesVisual;
        [SerializeField] private GameObject colleagueVisual;
        [Tooltip("Dove i colleghi si fermano se il capo turno li ha avvisati (corridoio, prima delle corsie).")]
        [SerializeField] private Transform colleagueStopPoint;
        [Tooltip("Ingresso della corsia 1 e punto dentro la corsia: se nessuno li ferma i colleghi entrano (scenario fallito).")]
        [SerializeField] private Transform[] colleagueLanePath;
        [Tooltip("Uscita dei colleghi quando tornano indietro.")]
        [SerializeField] private Transform colleagueExitPoint;
        [Tooltip("Oltre questa z un collega e' dentro la corsia.")]
        [SerializeField] private float laneEntryZ = 9.6f;
        [SerializeField] private GameObject m2WarningShadow;
        [SerializeField] private GameObject pallet1;
        [SerializeField] private GameObject pallet2;
        [SerializeField] private Transform zoneLzB;
        [SerializeField] private Transform zoneLzC;
        [SerializeField] private Transform zoneSlippery;
        [SerializeField] private Transform blindCorner;
        [SerializeField] private Transform accessLine;
        [Tooltip("Cartello 'Posa barriera BL-01': se il giocatore rilascia la barriera qui vicino, la posa automaticamente.")]
        [SerializeField] private Transform barrierPlacement;
        [Tooltip("Pulsante 'Sposta Marco': puntabile solo quando esiste un pericolo immediato.")]
        [SerializeField] private GameObject emergencyMoveCue;
        [Tooltip("Pulsante RIANIMA: compare solo in caso di apnea (Operational/Crisis; mai in Guided).")]
        [SerializeField] private GameObject resuscitateCue;
        [SerializeField] private Transform[] m2Path = new Transform[0];

        [Header("Zone e soglie")]
        [SerializeField] private float lzBRadius = 2.2f;
        [SerializeField] private float lzCRadius = 1.4f;
        [SerializeField] private float slipperyRadius = 1.5f;
        [SerializeField] private float blindCornerRadius = 3f;
        [SerializeField] private float runningSpeed = 2.1f;
        [Tooltip("Quota a terra del pallet PA01: il pallet parte in bilico sul rack e cade fino a qui.")]
        [SerializeField] private float pallet1GroundY = -0.068722f;

        [Header("Replay")]
        [SerializeField] private ForkliftChaosReplayConfig replay;

        // Stato della missione.
        private bool observed;
        private bool blindCornerObserved;
        private bool m1Stopped;
        private bool radioPicked;
        private bool barrierPicked;
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

        // Posizione originale degli oggetti trasportabili: ripristinata a ogni reset/replay.
        private Transform radioSavedParent;
        private Vector3 radioSavedPosition;
        private Quaternion radioSavedRotation;
        private bool radioSaved;
        private Transform barrierSavedParent;
        private Vector3 barrierSavedPosition;
        private Quaternion barrierSavedRotation;
        private bool barrierSaved;

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

        // Caduta del pallet: annullabile se il giocatore lascia la zona di pericolo in tempo.
        private Coroutine pallet1FallRoutine;
        private Coroutine pallet2FallRoutine;

        // Posizione iniziale del pallet sul rack, ripristinata a ogni replay.
        private Vector3 pallet1SavedPosition;
        private Quaternion pallet1SavedRotation;
        private bool pallet1Saved;

        // Suggerimenti guidati in sequenza: uno alla volta, max ~60 caratteri.
        private int lastHintStep = -1;
        private int lastRescueCountdown = -1;
        private const float RESCUE_MIN_SECONDS = 60f;
        private const float RESCUE_MAX_SECONDS = 75f;

        private readonly List<ChoiceRecord> records = new List<ChoiceRecord>();
        private readonly List<Coroutine> routines = new List<Coroutine>();

        /// <summary>true se il giocatore ha raccolto la radio portatile.</summary>
        public bool HasRadio => radioPicked;

        // =====================================================================
        // Avvio
        // =====================================================================

        protected override void OnScenarioStarted()
        {
            if (!pallet1Saved && pallet1 != null)
            {
                pallet1SavedPosition = pallet1.transform.position;
                pallet1SavedRotation = pallet1.transform.rotation;
                pallet1Saved = true;
            }
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
            UpdateHints();
            HUDService.StartTimer(300);

            StartTracked(IntroRoutine());
            StartTracked(TimedEventsRoutine());
            StartTracked(IdleWobbleRoutine());
            StartTracked(BreathingRoutine());
        }

        /// <summary>Azzera tutti i flag locali (la scena viene comunque ricaricata tra i replay).</summary>
        private void ResetState()
        {
            observed = blindCornerObserved = m1Stopped = radioPicked = barrierPicked = called112 = calledForeman = false;
            barrierPlaced = conesPlaced = spoken = breathingChecked = woundPressed = covered = false;
            cprStarted = aedUsed = marcoMoved = marcoMovedSafely = colleaguesEntered = colleaguesHeld = false;
            corridorBlocked = m2Started = m2Horn = m2Stopped = m2Hit = pallet1Fallen = pallet2Fallen = false;
            slipped = rescueArrived = rescueDelayed = apnea = timeout = finished = false;
            callTime = hornTime = rescueAt = -1f;
            rescueExtraDelay = extraExecutionPenalty = 0f;
            insideLzb = insideLzc = lzbWarned = lzcWarned = false;
            lzbEntered = lzcEntered = 0f;
            pallet1FallRoutine = pallet2FallRoutine = null;
            lastHintStep = -1;
            lastRescueCountdown = -1;
            if (pallet1Saved && pallet1 != null)
            {
                pallet1.transform.SetPositionAndRotation(pallet1SavedPosition, pallet1SavedRotation);
            }
            if (emergencyMoveCue != null) emergencyMoveCue.SetActive(false);
            if (resuscitateCue != null) resuscitateCue.SetActive(false);
            if (marcoAnimator != null) marcoAnimator.speed = 1f;
            if (rescueTeam != null) rescueTeam.SetActive(false);
            RestoreCarriedItem(radioPickup, ref radioSaved, ref radioSavedParent, ref radioSavedPosition, ref radioSavedRotation);
            RestoreCarriedItem(barrierPickup, ref barrierSaved, ref barrierSavedParent, ref barrierSavedPosition, ref barrierSavedRotation);
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

            // Varianti di posizione disattivate dopo la review: il secondo pallet sta sul suo rack in corsia 2 (con la sua zona di
            // caduta) e la radio sullo scaffale nel corridoio; spostarli li lascerebbe a mezz'aria.

            // Un solo collega: il secondo resta spento.
            if (colleagueVisual != null && colleagueCount <= 1 && colleagueVisual.transform.childCount > 1)
                colleagueVisual.transform.GetChild(1).gameObject.SetActive(false);
        }

        private void ResolveHead()
        {
            if (head != null) return;
            head = UIScreenManager.Instance != null ? UIScreenManager.Instance.Head : null;
            if (head == null && Camera.main != null) head = Camera.main.transform;
        }

        // =====================================================================
        // Audio di scena (clip CC0 / generati localmente, in Resources/Audio)
        // =====================================================================

        private AudioSource sfxSource;
        private readonly HashSet<string> sfxClipLogged = new HashSet<string>();

        private void EnsureSfx()
        {
            if (sfxSource != null) return;

            // SCN_Core non contiene un AudioListener serializzato: senza di esso Unity non riproduce
            // alcun suono. Lo creiamo solo se manca, così non se ne ha mai più di uno attivo.
            if (FindFirstObjectByType<AudioListener>() == null)
            {
                Transform listenerParent = head != null ? head : (Camera.main != null ? Camera.main.transform : transform);
                listenerParent.gameObject.AddComponent<AudioListener>();
                Debug.Log("[FKC] AudioListener creato su " + listenerParent.name);
            }

            sfxSource = gameObject.GetComponent<AudioSource>();
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;
            sfxSource.volume = 0.85f;
        }

        private void PlaySfx(string clipName, float volume = 1f)
        {
            EnsureSfx();
            AudioClip clip = Resources.Load<AudioClip>("Audio/" + clipName);
            if (clip == null)
            {
                Debug.LogWarning("[FKC] AudioClip mancante in Resources/Audio: " + clipName);
                return;
            }
            if (!sfxClipLogged.Add(clipName))
                Debug.Log("[FKC] SFX caricato: " + clipName + " (" + clip.length.ToString("0.0") + " s)");
            sfxSource.PlayOneShot(clip, volume);
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
            // Radio o barriera agganciate alla testa sono figlie del rig (scena core): vanno via con lo scenario, altrimenti
            // restano attaccate al giocatore dopo Ricomincia / Esci.
            foreach (var carried in new[] { radioPickup, barrierPickup })
                if (carried != null && carried.scene != gameObject.scene) Destroy(carried);
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
            PlaySfx("SFX_FKC_Scream", 0.9f);
            HUDService.Pulse(0.6f, 0.25f);
            Record("EV-01", "Urlo udito, timer avviato.", ChoiceKind.Correct, "Awareness");

            yield return new WaitForSeconds(2.5f);
            SetHint("Osserva Marco e il muletto.");
        }

        // =====================================================================
        // Osservazione e oggetti fisici (nessun pannello a scelta multipla)
        // =====================================================================

        /// <summary>Segna l'osservazione richiesta: il giocatore deve guardare l'oggetto, non scegliere da un menu.</summary>
        private void Observe(ForkliftChaosAction action)
        {
            if (finished || !IsRunning) return;
            switch (action)
            {
                case ForkliftChaosAction.ObserveMarco:
                    if (!observed)
                    {
                        observed = true;
                        Record("LG-01", "Marco a terra: visto sanguinamento e stato di coscienza.", ChoiceKind.Correct, "Awareness");
                        Feedback("Marco è a terra e sanguina. Non spostarlo.", FeedbackKind.Positive);
                        SetHint("Rack RK-01: grip su radio e barriera.");
                        UpdateObjectives();
                    }
                    break;
                case ForkliftChaosAction.ObserveForklift1:
                    if (!observed)
                    {
                        observed = true;
                        Record("LG-01", "Muletto 1 acceso con carico instabile individuato.", ChoiceKind.Correct, "Awareness");
                        Feedback("Muletto 1 è acceso: spegnilo dal comando.", FeedbackKind.Positive);
                        UpdateObjectives();
                    }
                    break;
                case ForkliftChaosAction.ObservePallet:
                    if (!observed)
                    {
                        observed = true;
                        Record("LG-01", "Pallet in bilico individuato prima dell'urto.", ChoiceKind.Correct, "Awareness");
                        Feedback("Pallet instabile: non sostarci sotto.", FeedbackKind.Positive);
                        UpdateObjectives();
                    }
                    break;
                case ForkliftChaosAction.ObserveBlindCorner:
                    blindCornerObserved = true;
                    if (!observed)
                    {
                        observed = true;
                        Record("LG-01", "Angolo cieco controllato prima di entrare in corsia.", ChoiceKind.Correct, "Awareness");
                    }
                    Record("LG-02", "Angolo cieco verificato: nessun mezzo arriva alle spalle.", ChoiceKind.Correct, "Awareness");
                    Feedback("Hai controllato l'angolo cieco.", FeedbackKind.Positive);
                    UpdateObjectives();
                    break;
            }
        }

        /// <summary>Salva la posa di scena di un oggetto trasportabile, una sola volta, prima del primo spostamento.</summary>
        private static void SaveOriginal(GameObject item, ref bool saved, ref Transform parent,
            ref Vector3 position, ref Quaternion rotation)
        {
            if (item == null || saved) return;
            saved = true;
            parent = item.transform.parent;
            position = item.transform.localPosition;
            rotation = item.transform.localRotation;
        }

        /// <summary>Riporta un oggetto trasportabile alla posa di scena (reset e replay).</summary>
        private static void RestoreCarriedItem(GameObject item, ref bool saved, ref Transform parent,
            ref Vector3 position, ref Quaternion rotation)
        {
            if (item == null || !saved) return;
            item.SetActive(true);
            item.transform.SetParent(parent, false);
            item.transform.localPosition = position;
            item.transform.localRotation = rotation;
            saved = false;
            parent = null;
        }

        /// <summary>La radio resta in mano: i due pulsanti (112 / capo turno) sono oggetti fisici figli della radio.</summary>
        private void AttachRadioToHead()
        {
            if (radioPickup == null) return;
            SaveOriginal(radioPickup, ref radioSaved, ref radioSavedParent, ref radioSavedPosition, ref radioSavedRotation);
            radioPickup.SetActive(true);
            Transform h = head != null ? head : (Camera.main != null ? Camera.main.transform : null);
            if (h == null) return;
            radioPickup.transform.SetParent(h, true);
            radioPickup.transform.localPosition = new Vector3(0.32f, -0.3f, 0.62f);
            radioPickup.transform.localRotation = Quaternion.Euler(12f, -18f, 0f);
        }

        private void AttachBarrierToHead()
        {
            if (barrierPickup == null) return;
            SaveOriginal(barrierPickup, ref barrierSaved, ref barrierSavedParent, ref barrierSavedPosition, ref barrierSavedRotation);
            barrierPickup.SetActive(true);
            Transform h = head != null ? head : (Camera.main != null ? Camera.main.transform : null);
            if (h == null) return;
            barrierPickup.transform.SetParent(h, true);
            barrierPickup.transform.localPosition = new Vector3(-0.38f, -0.38f, 0.75f);
            barrierPickup.transform.localRotation = Quaternion.Euler(0f, 0f, 18f);
        }

        /// <summary>
        /// Il giocatore ha rilasciato il grip: l'oggetto resta dove è stato lasciato (non più agganciato alla testa).
        /// Se la barriera è stata lasciata vicino alla linea BL-01, la posa automaticamente; altrimenti spiega come fare.
        /// </summary>
        public void HandleCarriedReleased(ForkliftChaosAction action)
        {
            if (!IsRunning || finished) return;
            if (action == ForkliftChaosAction.PickupRadio && radioPicked)
            {
                Feedback("Radio posata: punta i pulsanti 112 e CAPO con l'altro controller.", FeedbackKind.Neutral);
            }
            else if (action == ForkliftChaosAction.PickupBarrier && barrierPicked && !barrierPlaced)
            {
                // Grab-and-put: la barriera si posa lasciandola sulla zona blu davanti alle corsie 2 e 3 (larga ~5 m).
                if (barrierPlacement != null && barrierPickup != null && OnBarrierDropZone(barrierPickup.transform.position))
                    HandleInteraction(ForkliftChaosAction.PlaceBarrier);
                else
                    Feedback("Porta la barriera sulla zona blu davanti alle corsie 2 e 3 e lascia il GRIP.", FeedbackKind.Neutral);
            }
        }

        /// <summary>true se il punto è sulla zona di posa della barriera: entro 2,7 m lungo la linea e 1,3 m di profondità.</summary>
        private bool OnBarrierDropZone(Vector3 p)
        {
            Vector3 d = p - barrierPlacement.position;
            return Mathf.Abs(d.x) < 2.7f && Mathf.Abs(d.z) < 1.3f;
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
                case ForkliftChaosAction.ObserveMarco:
                case ForkliftChaosAction.ObserveForklift1:
                case ForkliftChaosAction.ObservePallet:
                case ForkliftChaosAction.ObserveBlindCorner:
                    Observe(action);
                    break;

                case ForkliftChaosAction.KnockPallet:
                    TriggerPallet1Fall(true);
                    break;

                case ForkliftChaosAction.PickupBarrier:
                    if (!barrierPicked)
                    {
                        barrierPicked = true;
                        if (source == null || !source.IsHeld) AttachBarrierToHead();
                        Feedback("Barriera in mano: portala sulla zona blu e lascia il GRIP.", FeedbackKind.Positive);
                    }
                    break;

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
                        if (source == null || !source.IsHeld) AttachRadioToHead();
                        Feedback("Radio in mano: usa i tasti 112 e capo turno.", FeedbackKind.Positive);
                    }
                    break;

                case ForkliftChaosAction.Call112:
                    if (!radioPicked) Feedback("Prendi prima la radio dal rack RK-01.", FeedbackKind.Negative);
                    else Call112();
                    break;

                case ForkliftChaosAction.CallForeman:
                    if (!radioPicked) Feedback("Prendi prima la radio dal rack RK-01.", FeedbackKind.Negative);
                    else CallForeman();
                    break;

                case ForkliftChaosAction.PlaceBarrier:
                    if (!barrierPicked)
                    {
                        Feedback("GRIP sulla barriera nel corridoio per prenderla.", FeedbackKind.Negative);
                        break;
                    }
                    if (!barrierPlaced)
                    {
                        barrierPlaced = true;
                        SetBarrierActive(true);
                        if (barrierPickup != null)
                        {
                            barrierPickup.transform.SetParent(null, true);
                            barrierPickup.SetActive(false);
                        }
                        // Barriera posata: la zona blu di posa (con la sua etichetta) non serve piu'.
                        if (barrierPlacement != null) barrierPlacement.gameObject.SetActive(false);
                        Record("LG-06", "Corsia delimitata: M2 e colleghi non entrano.", ChoiceKind.Correct, "Safety");
                        Feedback("Barriera BL-01 posata.", FeedbackKind.Positive);
                        UpdateObjectives();
                    }
                    break;

                case ForkliftChaosAction.PlaceCones:
                    if (!conesPlaced)
                    {
                        conesPlaced = true;
                        if (conesVisual != null) { conesVisual.SetActive(true); LetPlayerPass(conesVisual); }
                        if (source != null) source.gameObject.SetActive(false);
                        Feedback("Coni posizionati lungo BL-01.", FeedbackKind.Positive);
                        UpdateObjectives();
                    }
                    break;

                case ForkliftChaosAction.AssessMarco:
                    if (!spoken || !breathingChecked)
                    {
                        spoken = true;
                        breathingChecked = true;
                        Record("LG-13", "Marco valutato: coscienza e respiro controllati.", ChoiceKind.Correct, "Execution");
                        Subtitle("Marco", "Ho male qui... non mi muovere.", 4f);
                        Feedback("Marco valutato: non spostarlo.", FeedbackKind.Positive);
                        UpdateObjectives();
                    }
                    break;

                case ForkliftChaosAction.TreatWound:
                    if (!woundPressed || !covered)
                    {
                        woundPressed = true;
                        covered = true;
                        Feedback("Ferita compressa e Marco coperto.", FeedbackKind.Positive);
                        Alert("Ferita trattata\nMarco coperto", "ICN_CheckCircle");
                        Subtitle("Ambiente", "[Compressione e copertura completate]", 4f);
                        HUDService.Pulse(0.5f, 0.25f);
                        CheckWoundAndCover();
                        UpdateObjectives();
                    }
                    break;

                case ForkliftChaosAction.Resuscitate:
                    if (!cprStarted || !aedUsed)
                    {
                        cprStarted = true;
                        aedUsed = true;
                        Feedback("Massaggio cardiaco e DAE avviati.", FeedbackKind.Positive);
                        CheckCPR();
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
                        SetHint("Ora usa VALUTA e TRATTA su Marco.");
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
            if (barrierVisual == null) return;
            barrierVisual.SetActive(active);
            if (active) LetPlayerPass(barrierVisual);
        }

        /// <summary>
        /// Barriera e coni delimitano l'area ma non devono bloccare il giocatore: i collider restano (raggio e logica),
        /// solo il corpo del rig (CharacterController) li attraversa. Va richiamato dopo ogni SetActive(true):
        /// Unity dimentica IgnoreCollision quando un collider viene disattivato.
        /// </summary>
        private void LetPlayerPass(GameObject go)
        {
            var body = head != null ? head.GetComponentInParent<CharacterController>() : null;
            if (body == null) body = FindFirstObjectByType<CharacterController>();
            if (body == null || go == null) return;
            foreach (var c in go.GetComponentsInChildren<Collider>(true))
                if (c != body) Physics.IgnoreCollision(body, c, true);
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
                // Spostamento verso ovest: a sud c'è il muletto 1, a nord il rack del pallet.
                if (marco != null) marco.transform.position += new Vector3(-2.2f, 0f, 0f);
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
                Record("LG-15", "Massaggio cardiaco e DAE sostenuti fino all'arrivo dei soccorsi.", ChoiceKind.Correct, "Execution");
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
            // Arrivo dei soccorsi 60-75 s dopo la chiamata (prima 110-130 s: dopo aver finito i compiti l'attesa era troppo lunga).
            float rescueIn = Random.Range(RESCUE_MIN_SECONDS, RESCUE_MAX_SECONDS);
            rescueAt = ElapsedSeconds + rescueIn;
            // Il muletto 2 arriva sempre prima dei soccorsi (almeno 15 s prima), anche con l'attesa piu' breve.
            StartTracked(M2Routine(Mathf.Min(RandomM2Delay(), rescueIn - 15f)));
            StartTracked(RescueRoutine());
            UpdateObjectives();
        }

        private void CallForeman()
        {
            if (!radioPicked)
            {
                Feedback("Prendi prima la radio dal rack RK-01.", FeedbackKind.Negative);
                return;
            }
            if (calledForeman) return;
            calledForeman = true;
            colleaguesHeld = true;
            Record("LG-17", "Capo turno avvisato: colleghi tenuti fuori.", ChoiceKind.Correct, "Safety");
            Feedback("Colleghi fermati dal capo turno.", FeedbackKind.Positive);
        }

        // =====================================================================
        // Eventi dinamici
        // =====================================================================

        /// <summary>Movimento minimo del carico instabile: il mezzo "vive" prima ancora degli eventi.</summary>
        private IEnumerator IdleWobbleRoutine()
        {
            while (!m1Stopped && !finished && forklift1 != null)
            {
                forklift1.transform.Rotate(0f, 0f, 1.2f);
                yield return new WaitForSeconds(0.4f);
                if (m1Stopped || finished || forklift1 == null) yield break;
                forklift1.transform.Rotate(0f, 0f, -1.2f);
                yield return new WaitForSeconds(0.4f);
            }
        }

        /// <summary>Respiro di Marco: l'Animator va in pausa durante l'apnea, così il torace si ferma davvero.</summary>
        private IEnumerator BreathingRoutine()
        {
            if (marco == null && marcoAnimator == null) yield break;
            while (!finished && !rescueArrived)
            {
                if (marcoAnimator != null) marcoAnimator.speed = apnea ? 0f : 1f;
                yield return null;
            }
            if (marcoAnimator != null) marcoAnimator.speed = 1f;
        }

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
                    SetHint("Vicino a Marco: usa VALUTA e TRATTA.");
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
            PlaySfx("SFX_FKC_Horn", 0.95f);
            HUDService.Pulse(0.8f, 0.4f, false);
            if (Level != DifficultyLevel.Crisis) SetM2Warning(true);
            if (Level == DifficultyLevel.Guided)
                Feedback("Preavviso 6 s: barriera F5 o radio F11.", FeedbackKind.Neutral);
            Record("EV-02", "Clacson e avvisi percepiti.", ChoiceKind.Correct, "Awareness");

            SetHint("Clacson: ferma M2 col grilletto o col tasto CAPO.");
            if (calledForeman && !barrierPlaced)
            {
                m2Stopped = true;
                SetM2Warning(false);
                Record("LG-09", "Muletto 2 fermato via radio dal capo turno.", ChoiceKind.Correct, "Decision");
                Feedback("Mezzo fermo prima dell'accesso.", FeedbackKind.Positive);
                yield break;
            }
            yield return new WaitForSeconds(lead);
            yield return MoveM2Routine();
        }

        private IEnumerator MoveM2Routine()
        {
            if (forklift2 == null) yield break;
            Vector3 start = forklift2.transform.position;
            Transform[] path = m2Path != null && m2Path.Length > 1 ? m2Path : new Transform[] { null, null };

            // Punto di arresto: il penultimo nodo del percorso, spostato prima della linea BL-01.
            // Viene ricalcolato a ogni frame: se il giocatore posa la barriera mentre il muletto
            // è già in corsia, il mezzo si ferma comunque davanti alla barriera.
            int stopSegment = Mathf.Min(1, path.Length - 2);
            Vector3 barrierStop = path.Length > 2 && path[2] != null ? path[2].position : start;
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
                    Vector3 nextPos = Vector3.Lerp(from, to, k);

                    if (m2Stopped)
                    {
                        SetM2Warning(false);
                        yield break;
                    }
                    if (barrierPlaced && i >= stopSegment &&
                        (Vector3.Distance(nextPos, barrierStop) < 0.35f || k >= 1f))
                    {
                        StopM2WithBarrier(barrierStop);
                        yield break;
                    }

                    forklift2.transform.position = nextPos;
                    Vector3 dir = to - from;
                    if (dir.sqrMagnitude > 0.01f)
                        forklift2.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
                    yield return null;
                }
                if (barrierPlaced && i >= stopSegment)
                {
                    StopM2WithBarrier(barrierStop);
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

        /// <summary>Il muletto si arresta davanti alla linea BL-01, senza passarci sopra.</summary>
        private void StopM2WithBarrier(Vector3 stopPoint)
        {
            if (m2Stopped) return;
            m2Stopped = true;
            if (forklift2 != null) forklift2.transform.position = stopPoint;
            SetM2Warning(false);
            Record("LG-09", "Muletto 2 fermato dalla barriera BL-01 prima dell'accesso.", ChoiceKind.Correct, "Decision");
            Feedback("Muletto fermato davanti alla barriera.", FeedbackKind.Positive);
            Alert("Muletto fermato\nDavanti alla barriera", "ICN_CheckCircle");
        }

        /// <summary>
        /// I colleghi arrivano dal corridoio e vanno verso la corsia. Se il capo turno e' stato avvisato (radio: CAPO) si fermano
        /// e tornano indietro; altrimenti entrano nella corsia e lo scenario fallisce (LG-18).
        /// </summary>
        private IEnumerator ColleaguesRoutine()
        {
            float delay = replay != null ? Random.Range(replay.colleagueArrivalAfterShout.x, replay.colleagueArrivalAfterShout.y) : Random.Range(60f, 90f);
            yield return new WaitForSeconds(delay);
            if (finished || colleagueVisual == null) yield break;

            colleagueVisual.SetActive(true);
            var movers = colleagueVisual.GetComponentsInChildren<NpcMover>(false);
            if (movers.Length == 0) yield break;
            Subtitle("Ambiente", "[Voci e passi dal corridoio]", 4f);
            if (!colleaguesHeld) Alert("Colleghi in arrivo\nStanno andando verso la corsia", "ICN_Warning");

            // Verso il punto di sosta nel corridoio, uno accanto all'altro.
            for (int i = 0; i < movers.Length; i++)
                if (colleagueStopPoint != null) movers[i].WalkPath(new[] { colleagueStopPoint.position + Side(i, movers.Length) }, 0f, null);
            // Arrivano comunque fino al punto di sosta: li' si vede se il capo turno li ha fermati.
            while (!finished && movers.Any(m => m.IsMoving)) yield return null;
            if (finished) yield break;

            if (!colleaguesHeld && colleagueLanePath != null && colleagueLanePath.Length > 0)
            {
                // Nessuno li ha fermati: proseguono dritti nella corsia.
                for (int i = 0; i < movers.Length; i++)
                    movers[i].WalkPath(colleagueLanePath.Where(t => t != null).Select(t => t.position + Side(i, movers.Length) * 0.6f), 0f, null);
                while (!finished && !colleaguesHeld)
                {
                    if (movers.Any(m => m.transform.position.z > laneEntryZ))
                    {
                        colleaguesEntered = true;
                        Record("LG-18", "Colleghi entrati in corsia senza che il capo turno li fermasse: livello fallito.", ChoiceKind.Critical, "Safety");
                        Alert("Colleghi in corsia\nNessuno li ha fermati", "ICN_Warning_Filled");
                        Finish();
                        yield break;
                    }
                    if (!movers.Any(m => m.IsMoving)) break;
                    yield return null;
                }
                if (finished || !colleaguesHeld) yield break;
            }

            // Capo turno avvisato: si fermano, poi tornano indietro ed escono.
            foreach (var m in movers) m.Stop();
            Subtitle("Capo turno", "Fermi! Restate fuori dalla corsia, tornate indietro.", 4f);
            Feedback("Colleghi fermati dal capo turno.", FeedbackKind.Positive);
            yield return new WaitForSeconds(2.5f);
            if (finished) yield break;
            for (int i = 0; i < movers.Length; i++)
                if (colleagueExitPoint != null) movers[i].WalkPath(new[] { colleagueExitPoint.position + Side(i, movers.Length) }, 0f, null);
            while (!finished && movers.Any(m => m.IsMoving)) yield return null;
            if (!finished) colleagueVisual.SetActive(false);
        }

        /// <summary>Scostamento laterale (asse z) per non sovrapporre i colleghi che camminano insieme.</summary>
        private static Vector3 Side(int i, int count) => new Vector3(0f, 0f, (i - (count - 1) * 0.5f) * 0.8f);

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
            if (resuscitateCue != null) resuscitateCue.SetActive(true);
            Alert("Marco non respira\nPremi RIANIMA: massaggio e DAE", "ICN_Warning_Filled");
            Subtitle("Capo turno", "Se il respiro è dubbio, agisci come se fosse assente.", 6f);
            SetHint("Marco non respira: premi RIANIMA.");
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
            if (rescueTeam != null) rescueTeam.SetActive(true);
            Alert("Soccorsi arrivati\nFine scenario", "ICN_CheckCircle");
            HUDService.ShowSoundEvent("Sirena dei soccorsi", true);
            PlaySfx("SFX_FKC_Siren", 0.85f);
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
            UpdateEmergencyCue();
            UpdateHints();
            UpdateRescueCountdown();
        }

        /// <summary>Il pulsante di spostamento di Marco compare solo se c'è un pericolo immediato.</summary>
        private void UpdateEmergencyCue()
        {
            if (emergencyMoveCue == null) return;
            bool danger = (lzbWarned && insideLzb) || (m2Horn && !m2Stopped && m2Started) || pallet1Fallen;
            bool show = danger && !marcoMoved && !finished;
            if (emergencyMoveCue.activeSelf != show) emergencyMoveCue.SetActive(show);
        }

        // =====================================================================
        // Suggerimenti guidati: un passo alla volta, in ordine
        // =====================================================================

        private static readonly string[] HintTexts =
        {
            "Osserva Marco (corsia 3) e il muletto",
            "GRILLETTO sul muletto 1: spegnilo",
            "GRIP: prendi radio e barriera (blu)",
            "GRILLETTO sui tasti radio: 112 e CAPO",
            "GRIP: barriera sulla zona blu, poi lascia",
            "GRILLETTO sul blocco verde: coni",
            "GRILLETTO: VALUTA Marco",
            "GRILLETTO: TRATTA la ferita",
            "GRILLETTO: RIANIMA (massaggio e DAE)",
            "GRILLETTO sul muletto 2 (corsia 1)",
            "Attendi i soccorsi vicino a Marco"
        };

        private int HintStep()
        {
            if (!observed) return 0;
            if (!m1Stopped) return 1;
            if (!radioPicked) return 2;
            if (!called112) return 3;
            if (!barrierPlaced) return 4;
            if (!conesPlaced) return 5;
            if (!spoken || !breathingChecked) return 6;
            if (!woundPressed || !covered) return 7;
            if (apnea && (!cprStarted || !aedUsed)) return 8;
            if (m2Horn && !m2Stopped) return 9;
            return 10;
        }

        private void UpdateHints()
        {
            if (!IsRunning || finished) return;
            int step = HintStep();
            if (step == lastHintStep) return;
            lastHintStep = step;
            SetHint(HintTexts[step]);
        }

        /// <summary>Conto alla rovescia dei soccorsi sul polso, aggiornato una volta al secondo.</summary>
        /// <remarks>
        /// Riga dedicata del polso (HUDService.SetCountdown): visibile in tutti i livelli e non sovrascritta dai suggerimenti.
        /// Non usa il timer missione, che alla scadenza farebbe fallire la missione.
        /// </remarks>
        private void UpdateRescueCountdown()
        {
            if (rescueAt <= 0f) return;
            if (rescueArrived || finished)
            {
                if (lastRescueCountdown != -2) { HUDService.SetCountdown(string.Empty); lastRescueCountdown = -2; }
                return;
            }
            float remaining = Mathf.Max(0f, rescueAt + rescueExtraDelay - ElapsedSeconds);
            int seconds = Mathf.CeilToInt(remaining);
            if (seconds == lastRescueCountdown) return;
            lastRescueCountdown = seconds;
            HUDService.SetCountdown("Soccorsi " + (seconds / 60) + ":" + (seconds % 60).ToString("00"));
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
                if (warned)
                {
                    // Uscire in tempo annulla la caduta: l'avviso "Allontanati" deve avere un effetto reale.
                    Coroutine pending = isPallet1 ? pallet1FallRoutine : pallet2FallRoutine;
                    if (pending != null) StopCoroutine(pending);
                    if (isPallet1) pallet1FallRoutine = null;
                    else pallet2FallRoutine = null;
                    warned = false;
                    entered = 0f;
                    if (isPallet1 || Level == DifficultyLevel.Guided)
                        Feedback("Pallet rimasto in bilico: sei uscito in tempo.", FeedbackKind.Positive);
                }
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
            Coroutine fall = StartTracked(PalletFallRoutine(isPallet1));
            if (isPallet1) pallet1FallRoutine = fall;
            else pallet2FallRoutine = fall;
        }

        private IEnumerator PalletFallRoutine(bool isPallet1)
        {
            yield return new WaitForSeconds(4f);
            if (finished) yield break;
            GameObject target = isPallet1 ? pallet1 : pallet2;
            if (target != null)
                target.transform.Rotate(isPallet1 ? 24f : -20f, 0f, 0f);
            if (isPallet1)
            {
                pallet1FallRoutine = null;
                TriggerPallet1Fall(false);
            }
            else
            {
                pallet2FallRoutine = null;
                TriggerPallet2Fall(false);
            }
        }

        private void TriggerPallet1Fall(bool ontoMarco)
        {
            if (pallet1Fallen) return;
            pallet1Fallen = true;

            if (pallet1 == null) return;

            // Il pallet è già in bilico sul rack MDL_Rack_PA01: non viene teletrasportato in aria.
            Vector3 start = pallet1.transform.position;
            Vector3 landing = new Vector3(start.x, pallet1GroundY, start.z);
            bool hitsMarco = ontoMarco || (marco != null && !marcoMoved &&
                                           Vector3.Distance(landing, marco.transform.position) < 3f);
            StartTracked(PalletDropRoutine(pallet1, start, landing, hitsMarco));
        }

        /// <summary>Animazione di caduta del carico: 0,7 s di discesa e ribaltamento, poi esito e debrief.</summary>
        private IEnumerator PalletDropRoutine(GameObject pallet, Vector3 start, Vector3 landing, bool hitsMarco)
        {
            Quaternion startRotation = pallet.transform.rotation;
            Quaternion endRotation = startRotation * Quaternion.Euler(38f, 0f, 0f);
            float t = 0f;
            while (t < 0.7f && !finished)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / 0.7f);
                pallet.transform.position = Vector3.Lerp(start, landing, k * k);
                pallet.transform.rotation = Quaternion.Slerp(startRotation, endRotation, k);
                yield return null;
            }
            if (finished) yield break;
            pallet.transform.position = landing;
            pallet.transform.rotation = endRotation;
            PlaySfx("SFX_FKC_PalletImpact", 1f);
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

        /// <summary>
        /// Secondo pallet (sul rack in corsia 2): cade a terra ma non fa fallire lo scenario e non blocca la corsia di passaggio;
        /// resta una penalita' di sicurezza nel debrief.
        /// </summary>
        private void TriggerPallet2Fall(bool underPlayer)
        {
            if (pallet2Fallen) return;
            pallet2Fallen = true;
            if (pallet2 != null)
            {
                Vector3 start = pallet2.transform.position;
                StartTracked(Pallet2DropRoutine(start, new Vector3(start.x, pallet1GroundY, start.z)));
            }
            Record("LG-20", "Pallet caduto in corsia 2: nessun ferito, corsia di passaggio libera.", ChoiceKind.Risky, "Safety");
            Alert("Pallet caduto\nCorsia 2, nessun ferito", "ICN_Warning_Filled");
        }

        private IEnumerator Pallet2DropRoutine(Vector3 start, Vector3 landing)
        {
            Quaternion from = pallet2.transform.rotation;
            Quaternion to = from * Quaternion.Euler(-38f, 0f, 0f);
            float t = 0f;
            while (t < 0.7f && pallet2 != null)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / 0.7f);
                pallet2.transform.SetPositionAndRotation(Vector3.Lerp(start, landing, k * k), Quaternion.Slerp(from, to, k));
                yield return null;
            }
            if (pallet2 == null) yield break;
            pallet2.transform.SetPositionAndRotation(landing, to);
            PlaySfx("SFX_FKC_PalletImpact", 1f);
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
            if (barrierPlaced && conesPlaced && CurrentObjectiveIndex == 3)
            {
                CompleteObjective();
                SetHint("Resta vicino a Marco: arriva la sirena.");
            }
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
            HUDService.SetCountdown(string.Empty);

            bool critical = records.Any(r => r.Kind == ChoiceKind.Critical);
            bool assisted = spoken && breathingChecked && ((woundPressed && covered) || (cprStarted && aedUsed));
            bool accessFree = !colleaguesEntered;
            bool success = !critical && !timeout && rescueArrived && barrierPlaced && m1Stopped &&
                           accessFree && assisted && !marcoMoved && !rescueDelayed && !m2Hit &&
                           (m2Stopped || !m2Horn);
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
            if (!m2Stopped && m2Horn) decision -= 20;
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
