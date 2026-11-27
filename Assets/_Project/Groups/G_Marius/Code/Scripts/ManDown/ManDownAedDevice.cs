using System;
using SI81.Input;
using SI81.Interaction;
using SI81.Rig;
using SI81.UI;
using UnityEngine;

namespace SI81.ManDown
{
    /// <summary>Stati operativi del DAE, distinti dallo stato fisico di recupero/accensione.</summary>
    public enum ManDownAedState
    {
        Off,
        PoweredAwaitingElectrodes,
        AwaitingHandsClear,
        Analyzing,
        ShockNotAdvised,
        ShockAuthorized,
        ShockDelivered
    }

    /// <summary>
    /// Controller del dispositivo DAE: recupero con grab condiviso, accensione automatica, elettrodi,
    /// analisi cardiaca semplificata, autorizzazione della scarica, pulsante con Select condiviso e
    /// valutazione temporale della risposta. Tutto lo stato storico è esposto come snapshot grezzo:
    /// nessun punteggio, penalità nuova, progressione RCP o UI personalizzata.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/Man Down/AED Device")]
    public sealed class ManDownAedDevice : MonoBehaviour
    {
        [Header("Riferimenti di scena")]
        [Tooltip("Controller della missione: fonte autorevole di disponibilità, tempo e registrazione azioni.")]
        [SerializeField] private ManDownScenario scenario;
        [Tooltip("Interactable sulla radice del dispositivo (FollowsHand attivo come per i telefoni).")]
        [SerializeField] private Interactable interaction;
        [Tooltip("Interactable del pulsante di scarica: Select con grilletto, FollowsHand disattivato.")]
        [SerializeField] private Interactable shockButton;
        [Tooltip("Mesh separata del pulsante: si premere leggermente quando viene rilevata la pressione.")]
        [SerializeField] private Transform shockButtonVisual;
        [Tooltip("Offset locale della mesh alla pressione (verso l'interno del dispositivo).")]
        [SerializeField] private Vector3 buttonPressOffset = new Vector3(-0.008f, 0f, 0f);
        [Tooltip("Durata del movimento visivo di pressione, in secondi.")]
        [Min(0.01f)]
        [SerializeField] private float buttonPressDuration = 0.12f;
        [Tooltip("Ampiezza dell'impulso aptico sul controller che preme il pulsante (0 = disattivato).")]
        [Range(0f, 1f)]
        [SerializeField] private float buttonHapticAmplitude = 0.35f;
        [Tooltip("Durata dell'impulso aptico di pressione del pulsante.")]
        [Min(0f)]
        [SerializeField] private float buttonHapticDuration = 0.08f;
        [Tooltip("Etichetta Interaction comune del dispositivo.")]
        [SerializeField] private LabelAnchor label;
        [Tooltip("Renderer della primitive dell'indicatore: solo MaterialPropertyBlock, mai il materiale condiviso.")]
        [SerializeField] private Renderer indicator;

        [Header("Elettrodi e zone")]
        [SerializeField] private Transform electrodeSlotA;
        [SerializeField] private Transform electrodeSlotB;
        [SerializeField] private ManDownAedElectrode electrodeA;
        [SerializeField] private ManDownAedElectrode electrodeB;
        [SerializeField] private ManDownAedAttachments attachments;
        [SerializeField] private ManDownAedLeads leads;

        [Header("Contatto con l'NPC")]
        [Tooltip("Zone di sicurezza locali sull'NPC (testa, torace, bacino, braccia, gambe).")]
        [SerializeField] private ManDownAedContactZones contactZones;

        [Header("RCP (solo interruzione della sequenza)")]
        [SerializeField] private ManDownCpr cprDetector;
        [SerializeField] private ManDownCprRhythm cprRhythm;

        [Header("Analisi e scarica")]
        [Tooltip("Durata dell'analisi cardiaca simulata in secondi.")]
        [Min(0.1f)]
        [SerializeField] private float analysisDuration = 3f;
        [Tooltip("Piccolo ritardo anti-flicker prima dell'avvio dell'analisi, in secondi.")]
        [Min(0f)]
        [SerializeField] private float handsClearDelay = 0.15f;
        [Tooltip("Grace period del QTE: entro questo tempo la qualità temporale è 100%.")]
        [Min(0f)]
        [SerializeField] private float gracePeriod = 1.5f;
        [Tooltip("Tempo di risposta che corrisponde a qualità zero.")]
        [Min(0.01f)]
        [SerializeField] private float zeroQualityTime = 8f;

        [Header("Audio (opzionale)")]
        [SerializeField] private AudioSource audioSource;
        [Tooltip("Beep di autorizzazione: segnale d'inizio del QTE.")]
        [SerializeField] private AudioClip authorizationBeep;
        [SerializeField] private AudioClip powerOnClip;
        [SerializeField] private AudioClip handsRequestClip;
        [SerializeField] private AudioClip analysisStartClip;
        [SerializeField] private AudioClip notAdvisedClip;
        [SerializeField] private AudioClip shockCompleteClip;
        [Tooltip("Volume dell'AudioSource 3D del dispositivo.")]
        [Range(0f, 1f)]
        [SerializeField] private float audioVolume = 0.75f;
        [SerializeField] private float audioMinDistance = 0.5f;
        [SerializeField] private float audioMaxDistance = 8f;

        [Header("Indicatore")]
        [SerializeField] private Color offColor = new Color(0.45f, 0.45f, 0.45f, 1f);
        [SerializeField] private Color onColor = new Color(0.10f, 0.90f, 0.25f, 1f);
        [SerializeField] private Color awaitingClearColor = new Color(0.95f, 0.85f, 0.10f, 1f);
        [SerializeField] private Color analyzingColor = new Color(0.15f, 0.45f, 0.95f, 1f);
        [SerializeField] private Color authorizedColor = new Color(1f, 0.55f, 0.10f, 1f);
        [SerializeField] private Color blockedColor = new Color(0.85f, 0.15f, 0.15f, 1f);
        [SerializeField] private Color completedColor = new Color(0.10f, 0.90f, 0.25f, 1f);
        [Tooltip("Frequenza di lampeggiamento dell'indicatore quando la scarica è autorizzata.")]
        [Min(0f)]
        [SerializeField] private float blinkHz = 2f;
        [Tooltip("Log solo sugli eventi (pressione pulsante, contatto, autorizzazione, rifiuti): non ogni frame.")]
        [SerializeField] private bool debugLog = true;

        /// <summary>Primo recupero valido registrato.</summary>
        public event Action Retrieved;
        /// <summary>Prima accensione automatica registrata.</summary>
        public event Action PoweredOn;
        /// <summary>Cambiato stato di applicazione degli elettrodi.</summary>
        public event Action ElectrodeStateChanged;
        /// <summary>Cambiata la posa visiva dell'NPC mentre gli elettrodi sono agganciati.</summary>
        public event Action<ManDownPose> ElectrodePoseChanged;
        /// <summary>Analisi avviata (timestamp di missione).</summary>
        public event Action<float> AnalysisStarted;
        /// <summary>Analisi interrotta (motivo tecnico).</summary>
        public event Action<string> AnalysisInterrupted;
        /// <summary>Scarica autorizzata (timestamp del segnale).</summary>
        public event Action<float> ShockAuthorized;
        /// <summary>Scarica completata (timestamp e qualità temporale 0..1).</summary>
        public event Action<float, float> ShockCompleted;
        /// <summary>È necessario mantenere le mani lontane dall'NPC (per il Task 08.5).</summary>
        public event Action<bool> HandsClearRequiredChanged;
        /// <summary>Cambiato stato operativo.</summary>
        public event Action<ManDownAedState> StateChanged;

        private readonly bool[] zoneActionRecorded = new bool[2];
        private Rigidbody body;
        private Vector3 initialLocalPosition;
        private Quaternion initialLocalRotation;
        private Vector3 initialLocalScale;
        private bool poseCaptured;
        private MaterialPropertyBlock indicatorBlock;

        private bool recovered;
        private bool poweredOn;
        private bool deviceGrabbed;
        private bool grabAllowed;
        private bool interactionsDisabled;

        private ManDownAedState state = ManDownAedState.Off;
        private float analysisProgress;
        private float clearTimer;
        private bool shockAdvised;
        private bool safetyBlocked;
        private bool handsClearNotified;
        private bool inappropriateRecorded;
        private bool notAdvisedNotified;
        private bool authorizedNotified;

        private int analysisStartedCount;
        private int analysisInterruptedCount;
        private int contactInterruptCount;
        private int authorizationCount;
        private int invalidEarlyPressCount;
        private int invalidContactCount;
        private int invalidMissingElectrodesCount;
        private int invalidNotAdvisedCount;
        private int inappropriateUseCount;
        private float firstAnalysisAt = -1f;
        private float firstAuthorizationAt = -1f;
        private float lastAuthorizationAt = -1f;
        private float signalAt = -1f;
        private float shockAt = -1f;
        private float responseTime = -1f;
        private float responseQuality = -1f;
        private bool shockCompleted;
        private string lastContactZone = string.Empty;
        private string lastContactHand = string.Empty;
        private Vector3 buttonRestLocal;
        private bool buttonPoseCaptured;
        private float buttonPressTimer;

        /// <summary>true dopo il primo GRIP valido.</summary>
        public bool IsRetrieved => recovered;
        /// <summary>true dopo la prima accensione automatica.</summary>
        public bool IsPoweredOn => poweredOn;
        /// <summary>true se il dispositivo è disponibile e la missione è ancora attiva.</summary>
        public bool IsAvailable => scenario != null && scenario.IsAedAvailable && !scenario.IsMissionConcluded;
        public ManDownAedElectrode ElectrodeA => electrodeA;
        public ManDownAedElectrode ElectrodeB => electrodeB;

        public ManDownAedState State => state;
        public bool IsAnalysisInProgress => state == ManDownAedState.Analyzing;
        public bool IsShockAuthorized => state == ManDownAedState.ShockAuthorized && !safetyBlocked;
        public bool NeedsHandsClear => state == ManDownAedState.AwaitingHandsClear ||
                                      state == ManDownAedState.Analyzing ||
                                      state == ManDownAedState.ShockAuthorized;
        public bool IsShockCompleted => shockCompleted;
        public bool IsShockAdvised => shockAdvised;
        public bool LeftHandContacting { get; private set; }
        public bool RightHandContacting { get; private set; }
        public int AnalysisStartedCount => analysisStartedCount;
        public int AnalysisInterruptedCount => analysisInterruptedCount;
        public int ContactInterruptCount => contactInterruptCount;
        public int AuthorizationCount => authorizationCount;
        public int InvalidEarlyPressCount => invalidEarlyPressCount;
        public int InvalidContactCount => invalidContactCount;
        public int InvalidMissingElectrodesCount => invalidMissingElectrodesCount;
        public int InvalidNotAdvisedCount => invalidNotAdvisedCount;
        public int InappropriateUseCount => inappropriateUseCount;
        public float FirstAnalysisAt => firstAnalysisAt;
        public float FirstAuthorizationAt => firstAuthorizationAt;
        public float LastAuthorizationAt => lastAuthorizationAt;
        public float SignalAt => signalAt;
        public float ShockAt => shockAt;
        public float ResponseTime => responseTime;
        public float ResponseQuality => responseQuality;
        /// <summary>Snapshot grezzo corrente, leggibile dal controller e dai task successivi.</summary>
        public ManDownAedSnapshot Snapshot => BuildSnapshot();

        private void Awake()
        {
            CapturePose();
            body = GetComponent<Rigidbody>();
        }

        private void OnEnable()
        {
            CapturePose();
            if (shockButtonVisual != null && !buttonPoseCaptured)
            {
                buttonRestLocal = shockButtonVisual.localPosition;
                buttonPoseCaptured = true;
            }
            if (body == null) body = GetComponent<Rigidbody>();
            if (interaction != null)
            {
                interaction.GrabEntered += OnGrabEntered;
                interaction.GrabExited += OnGrabExited;
            }
            if (shockButton != null) shockButton.SelectEntered += OnShockButtonPressed;
            if (scenario != null)
            {
                scenario.AedRetrieved += OnScenarioRetrieved;
                scenario.AedPoweredOn += OnScenarioPoweredOn;
                scenario.MissionConcluded += OnMissionConcluded;
            }
            if (electrodeA != null) electrodeA.StateChanged += OnElectrodeStateChanged;
            if (electrodeB != null) electrodeB.StateChanged += OnElectrodeStateChanged;
            if (attachments != null) attachments.PoseChanged += OnPoseChanged;
            MissionSession.Started += OnMissionStarted;
            RigRegistration.Registered += OnRigRegistered;
            IgnorePlayerBodyCollisions();
        }

        private void OnDisable()
        {
            if (interaction != null)
            {
                interaction.GrabEntered -= OnGrabEntered;
                interaction.GrabExited -= OnGrabExited;
            }
            if (shockButton != null) shockButton.SelectEntered -= OnShockButtonPressed;
            if (scenario != null)
            {
                scenario.AedRetrieved -= OnScenarioRetrieved;
                scenario.AedPoweredOn -= OnScenarioPoweredOn;
                scenario.MissionConcluded -= OnMissionConcluded;
            }
            MissionSession.Started -= OnMissionStarted;
            RigRegistration.Registered -= OnRigRegistered;
            if (electrodeA != null) electrodeA.StateChanged -= OnElectrodeStateChanged;
            if (electrodeB != null) electrodeB.StateChanged -= OnElectrodeStateChanged;
            if (attachments != null) attachments.PoseChanged -= OnPoseChanged;
            StopAudio();
        }

        private void OnDestroy()
        {
            Retrieved = null;
            PoweredOn = null;
            ElectrodeStateChanged = null;
            ElectrodePoseChanged = null;
            AnalysisStarted = null;
            AnalysisInterrupted = null;
            ShockAuthorized = null;
            ShockCompleted = null;
            HandsClearRequiredChanged = null;
            StateChanged = null;
        }

        private void CapturePose()
        {
            if (poseCaptured) return;
            initialLocalPosition = transform.localPosition;
            initialLocalRotation = transform.localRotation;
            initialLocalScale = transform.localScale;
            poseCaptured = true;
        }

        /// <summary>Macchina a stati operativa. Nessun log per frame.</summary>
        private void Update()
        {
            AnimateButtonPress();
            if (scenario == null || !scenario.AcceptsActions || interactionsDisabled) return;

            bool powered = poweredOn;
            bool applied = attachments != null && attachments.BothOccupied;
            bool supine = attachments != null && attachments.IsSupine;
            bool contact = ReadHandContact();

            switch (state)
            {
                case ManDownAedState.Off:
                    if (powered) SetState(ManDownAedState.PoweredAwaitingElectrodes);
                    break;

                case ManDownAedState.PoweredAwaitingElectrodes:
                    if (!powered) SetState(ManDownAedState.Off);
                    else if (applied && supine)
                    {
                        clearTimer = 0f;
                        SetState(ManDownAedState.AwaitingHandsClear);
                    }
                    break;

                case ManDownAedState.AwaitingHandsClear:
                    if (!powered) SetState(ManDownAedState.Off);
                    else if (!applied || !supine) SetState(ManDownAedState.PoweredAwaitingElectrodes);
                    else if (contact)
                    {
                        clearTimer = 0f;
                        if (!handsClearNotified)
                        {
                            handsClearNotified = true;
                            scenario.Alert("Allontana le mani dalla persona.");
                        }
                    }
                    else
                    {
                        clearTimer += Time.deltaTime;
                        if (clearTimer >= handsClearDelay) StartAnalysis();
                    }
                    break;

                case ManDownAedState.Analyzing:
                    if (!powered || !applied || !supine || contact)
                        InterruptAnalysis(contact ? "contact" : "conditions");
                    else
                    {
                        analysisProgress += Time.deltaTime;
                        if (analysisProgress >= Mathf.Max(0.1f, analysisDuration)) FinishAnalysis();
                    }
                    break;

                case ManDownAedState.ShockNotAdvised:
                    if (!powered || !applied || !supine)
                    {
                        shockAdvised = false;
                        notAdvisedNotified = false;
                        SetState(ManDownAedState.PoweredAwaitingElectrodes);
                    }
                    break;

                case ManDownAedState.ShockAuthorized:
                    if (!powered || !applied || !supine || shockCompleted) RevokeAuthorization("conditions");
                    else if (contact && !safetyBlocked)
                    {
                        safetyBlocked = true;
                        RecordInvalid("contact_after_authorization", ref invalidContactCount);
                    }
                    else if (!contact && safetyBlocked) safetyBlocked = false; // nessun nuovo beep
                    break;

                case ManDownAedState.ShockDelivered:
                    break;
            }

            UpdateIndicatorColor();
        }

        /// <summary>Dopo il recupero rende il corpo dinamico una volta gestito il rilascio del grab.</summary>
        private void LateUpdate()
        {
            if (!recovered || interactionsDisabled || body == null || interaction == null) return;
            if (interaction.IsGrabbed) return;
            if (!body.isKinematic) return;

            body.isKinematic = false;
            body.useGravity = true;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        /// <summary>Movimento visivo breve della mesh del pulsante quando la pressione viene rilevata.</summary>
        private void AnimateButtonPress()
        {
            if (!buttonPoseCaptured || shockButtonVisual == null || buttonPressTimer <= 0f) return;
            buttonPressTimer -= Time.deltaTime;
            float t = Mathf.Clamp01(buttonPressTimer / Mathf.Max(0.01f, buttonPressDuration));
            shockButtonVisual.localPosition = buttonRestLocal + buttonPressOffset * Mathf.Sin(t * Mathf.PI);
            if (buttonPressTimer <= 0f) shockButtonVisual.localPosition = buttonRestLocal;
        }

        /// <summary>
        /// Contatto delle sole mani del rig con il volume configurato sull'NPC: nessun collider sul rig,
        /// nessun falso contatto da elettrodi/cavi/DAE e gestione distinta sinistra/destra.
        /// </summary>
        private bool ReadHandContact()
        {
            bool wasLeft = LeftHandContacting;
            bool wasRight = RightHandContacting;
            string previousZone = lastContactZone;

            if (contactZones == null)
            {
                LeftHandContacting = false;
                RightHandContacting = false;
                lastContactZone = string.Empty;
                lastContactHand = string.Empty;
                return false;
            }

            contactZones.Refresh();
            LeftHandContacting = contactZones.LeftContacting;
            RightHandContacting = contactZones.RightContacting;
            lastContactZone = contactZones.LastZoneName;
            lastContactHand = contactZones.LastHandName;

            // Log solo al variare dello stato di contatto, mai per frame.
            if (debugLog && (wasLeft != LeftHandContacting || wasRight != RightHandContacting ||
                             previousZone != lastContactZone))
            {
                string detail = LeftHandContacting || RightHandContacting
                    ? "mano=" + lastContactHand + ", zona=" + lastContactZone +
                      ", sinistra=" + LeftHandContacting + ", destra=" + RightHandContacting
                    : "nessuna mano nelle zone di sicurezza";
                Debug.Log("[SI81 ManDown][AED] Contatto NPC: " + detail + ".", this);
            }
            return LeftHandContacting || RightHandContacting;
        }

        private void StartAnalysis()
        {
            analysisStartedCount++;
            if (firstAnalysisAt < 0f) firstAnalysisAt = MissionTime();
            analysisProgress = 0f;
            shockAdvised = false;
            handsClearNotified = false;
            authorizedNotified = false;
            notAdvisedNotified = false;
            inappropriateRecorded = false;
            // Interrompe la sequenza ritmica senza cancellare le statistiche RCP.
            if (cprRhythm != null) cprRhythm.InterruptSequence();
            if (cprDetector != null) cprDetector.InterruptForAed();
            SetState(ManDownAedState.Analyzing);
            AnalysisStarted?.Invoke(firstAnalysisAt);
            ReportSnapshot();
            if (debugLog) Debug.Log($"[SI81 ManDown][AED] Analisi avviata #{analysisStartedCount} @ {MissionTime():F2}s.", this);
        }

        private void InterruptAnalysis(string reason)
        {
            analysisInterruptedCount++;
            if (reason == "contact")
            {
                contactInterruptCount++;
                RecordInvalid("analysis_contact", ref invalidContactCount);
            }
            analysisProgress = 0f;
            shockAdvised = false;
            SetState(ManDownAedState.AwaitingHandsClear);
            AnalysisInterrupted?.Invoke(reason);
            ReportSnapshot();
            if (debugLog) Debug.Log($"[SI81 ManDown][AED] Analisi interrotta ({reason}) @ {MissionTime():F2}s.", this);
        }

        private void FinishAnalysis()
        {
            // Semplificazione concordata: esito dallo stato respiratorio effettivo, non dalla verifica del giocatore.
            shockAdvised = scenario != null && scenario.IsBreathingAbnormal;
            if (!shockAdvised)
            {
                SetState(ManDownAedState.ShockNotAdvised);
                if (!inappropriateRecorded)
                {
                    inappropriateRecorded = true;
                    inappropriateUseCount++;
                    invalidNotAdvisedCount++;
                    if (scenario != null)
                    {
                        scenario.ReportAedIncorrectUse();
                        scenario.ReportAedInvalidAttempt("not_advised");
                    }
                    if (!notAdvisedNotified)
                    {
                        notAdvisedNotified = true;
                        scenario.Alert("Scarica non consigliata.");
                    }
                }
                ReportSnapshot();
                return;
            }
            AuthorizeShock();
        }

        private void AuthorizeShock()
        {
            if (shockCompleted || state == ManDownAedState.ShockAuthorized) return;
            float now = MissionTime();
            authorizationCount++;
            lastAuthorizationAt = now;
            if (firstAuthorizationAt < 0f) firstAuthorizationAt = now;
            // Segnale sonoro, indicatore e QTE partono dallo stesso istante logico.
            signalAt = now;
            safetyBlocked = ReadHandContact();
            PlayAuthorizationBeep();
            SetState(ManDownAedState.ShockAuthorized);
            if (scenario != null) scenario.ActivateAed();
            if (!authorizedNotified)
            {
                authorizedNotified = true;
                scenario.Alert("Scarica autorizzata. Premi il pulsante sul DAE.");
            }
            ShockAuthorized?.Invoke(now);
            ReportSnapshot();
            if (debugLog) Debug.Log($"[SI81 ManDown][AED] Scarica autorizzata @ {now:F2}s.", this);
        }

        private void RevokeAuthorization(string reason)
        {
            if (state != ManDownAedState.ShockAuthorized) return;
            StopAudio();
            shockAdvised = false;
            safetyBlocked = false;
            authorizedNotified = false;
            SetState(ManDownAedState.PoweredAwaitingElectrodes);
            AnalysisInterrupted?.Invoke(reason);
            ReportSnapshot();
        }

        /// <summary>Pressione del pulsante con Select condiviso: feedback e log solo sull'evento.</summary>
        private void OnShockButtonPressed(Hand hand)
        {
            buttonPressTimer = buttonPressDuration;
            RigRegistration rig = RigRegistration.Instance;
            if (rig != null) rig.Pulse(hand, buttonHapticAmplitude, buttonHapticDuration);
            if (debugLog) Debug.Log($"[SI81 ManDown][AED] Pressione pulsante rilevata da {hand}.", this);

            if (interactionsDisabled || scenario == null || !scenario.AcceptsActions)
            {
                RefuseWithoutCount("missione non attiva");
                return;
            }
            if (shockCompleted)
            {
                RefuseWithoutCount("scarica già completata");
                return;
            }
            if (!poweredOn)
            {
                Refuse("DAE spento", "early_press", ref invalidEarlyPressCount);
                return;
            }
            if (state != ManDownAedState.ShockAuthorized)
            {
                if (state == ManDownAedState.ShockNotAdvised)
                    Refuse("scarica non consigliata", "not_advised", ref invalidNotAdvisedCount);
                else
                    Refuse("analisi non completata o autorizzazione non valida", "early_press", ref invalidEarlyPressCount);
                return;
            }
            if (safetyBlocked || ReadHandContact())
            {
                safetyBlocked = true;
                Refuse("contatto con l'NPC", "shock_with_contact", ref invalidContactCount);
                return;
            }
            if (attachments == null || !attachments.BothOccupied)
            {
                Refuse("elettrodi mancanti", "missing_electrodes", ref invalidMissingElectrodesCount);
                return;
            }
            if (!attachments.IsSupine)
            {
                Refuse("NPC non supino", "missing_electrodes", ref invalidMissingElectrodesCount);
                return;
            }

            if (debugLog) Debug.Log("[SI81 ManDown][AED] Richiesta di scarica accettata: tutte le condizioni soddisfatte.", this);
            DeliverShock();
        }

        private void Refuse(string reason, string note, ref int counter)
        {
            RecordInvalid(note, ref counter);
            if (debugLog) Debug.Log($"[SI81 ManDown][AED] Scarica rifiutata: {reason} ({note}).", this);
        }

        private void RefuseWithoutCount(string reason)
        {
            if (debugLog) Debug.Log($"[SI81 ManDown][AED] Scarica ignorata: {reason}.", this);
        }

        private void DeliverShock()
        {
            if (shockCompleted) return;
            float now = MissionTime();
            responseTime = Mathf.Max(0f, now - signalAt);
            responseQuality = ComputeResponseQuality(responseTime, gracePeriod, zeroQualityTime);
            shockAt = now;
            shockCompleted = true;
            StopAudio();
            SetState(ManDownAedState.ShockDelivered);
            if (scenario != null) scenario.ConfirmAedInstruction();
            ShockCompleted?.Invoke(now, responseQuality);
            ReportSnapshot();
            if (debugLog) Debug.Log($"[SI81 ManDown][AED] Scarica @ {now:F2}s, risposta {responseTime:F2}s, qualità {responseQuality:P1}.", this);
        }

        /// <summary>Qualità temporale pura della risposta; testabile senza visore e senza audio.</summary>
        public static float ComputeResponseQuality(float responseTime, float gracePeriod, float zeroQualityTime)
        {
            float span = Mathf.Max(0.01f, zeroQualityTime - gracePeriod);
            return Mathf.Clamp01(1f - (responseTime - gracePeriod) / span);
        }

        private void RecordInvalid(string note, ref int counter)
        {
            counter++;
            if (scenario != null) scenario.ReportAedInvalidAttempt(note);
            if (debugLog) Debug.Log($"[SI81 ManDown][AED] Tentativo non valido ({note}) @ {MissionTime():F2}s.", this);
        }

        private float MissionTime()
        {
            return scenario != null ? scenario.MissionElapsedSeconds : -1f;
        }

        private void SetState(ManDownAedState next)
        {
            if (state == next) return;
            bool oldNeedsClear = state == ManDownAedState.AwaitingHandsClear ||
                                 state == ManDownAedState.Analyzing ||
                                 state == ManDownAedState.ShockAuthorized;
            state = next;
            if (next != ManDownAedState.AwaitingHandsClear) handsClearNotified = false;
            if (next != ManDownAedState.ShockAuthorized) safetyBlocked = false;
            UpdateIndicatorColor();
            StateChanged?.Invoke(next);
            bool newNeedsClear = next == ManDownAedState.AwaitingHandsClear ||
                                 next == ManDownAedState.Analyzing ||
                                 next == ManDownAedState.ShockAuthorized;
            if (oldNeedsClear != newNeedsClear) HandsClearRequiredChanged?.Invoke(newNeedsClear);
        }

        /// <summary>Abilita o disabilita la presa in base all'apertura decisa dalla scatola.</summary>
        public void SetInteractionAllowed(bool value)
        {
            grabAllowed = value;
            RefreshInteraction();
            RefreshLabel();
        }

        /// <summary>Disattiva presa, pulsante ed etichetta a missione conclusa.</summary>
        public void SetInteractionsDisabled()
        {
            interactionsDisabled = true;
            StopAudio();
            RefreshInteraction();
            RefreshLabel();
        }

        /// <summary>Ripristina stato fisico e operativo iniziale, senza ricalcolare la posa di reset.</summary>
        [ContextMenu("SI81/AED Device/Reset")]
        public void ResetDevice()
        {
            recovered = false;
            poweredOn = false;
            deviceGrabbed = false;
            grabAllowed = false;
            interactionsDisabled = false;
            ResetOperational();
            if (!poseCaptured) CapturePose();

            transform.localPosition = initialLocalPosition;
            transform.localRotation = initialLocalRotation;
            transform.localScale = initialLocalScale;

            if (body == null) body = GetComponent<Rigidbody>();
            if (body != null)
            {
                if (!body.isKinematic)
                {
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }
                body.isKinematic = true;
                body.useGravity = false;
            }

            zoneActionRecorded[0] = false;
            zoneActionRecorded[1] = false;
            if (attachments != null) attachments.ResetZones();
            if (electrodeA != null) electrodeA.ResetElectrode();
            if (electrodeB != null) electrodeB.ResetElectrode();
            if (leads != null) leads.Refresh();
            RefreshInteraction();
            RefreshLabel();
        }

        private void ResetOperational()
        {
            StopAudio();
            state = ManDownAedState.Off;
            analysisProgress = 0f;
            clearTimer = 0f;
            shockAdvised = false;
            safetyBlocked = false;
            handsClearNotified = false;
            inappropriateRecorded = false;
            notAdvisedNotified = false;
            authorizedNotified = false;
            analysisStartedCount = 0;
            analysisInterruptedCount = 0;
            contactInterruptCount = 0;
            authorizationCount = 0;
            invalidEarlyPressCount = 0;
            invalidContactCount = 0;
            invalidMissingElectrodesCount = 0;
            invalidNotAdvisedCount = 0;
            inappropriateUseCount = 0;
            firstAnalysisAt = -1f;
            firstAuthorizationAt = -1f;
            lastAuthorizationAt = -1f;
            signalAt = -1f;
            shockAt = -1f;
            responseTime = -1f;
            responseQuality = -1f;
            shockCompleted = false;
            LeftHandContacting = false;
            RightHandContacting = false;
            lastContactZone = string.Empty;
            lastContactHand = string.Empty;
            buttonPressTimer = 0f;
            if (buttonPoseCaptured && shockButtonVisual != null) shockButtonVisual.localPosition = buttonRestLocal;
            if (contactZones != null) contactZones.ResetContact();
            ApplyIndicatorColor();
        }

        private void OnMissionStarted()
        {
            ResetDevice();
        }

        private void OnMissionConcluded(ManDownScenarioResult result)
        {
            StopAudio();
            StopOperationalActions();
            SetInteractionsDisabled();
            if (electrodeA != null) electrodeA.SetInteractionsDisabled();
            if (electrodeB != null) electrodeB.SetInteractionsDisabled();
        }

        private void StopOperationalActions()
        {
            analysisProgress = 0f;
            clearTimer = 0f;
            if (state == ManDownAedState.Analyzing) AnalysisInterrupted?.Invoke("mission_concluded");
            if (state == ManDownAedState.ShockAuthorized) RevokeAuthorization("mission_concluded");
        }

        private void OnScenarioRetrieved()
        {
            recovered = true;
            ApplyIndicatorColor();
            RefreshInteraction();
            Retrieved?.Invoke();
        }

        private void OnScenarioPoweredOn()
        {
            poweredOn = true;
            ApplyIndicatorColor();
            // Il pulsante diventa selezionabile solo ora: senza questo refresh restava disabilitato.
            RefreshInteraction();
            if (electrodeA != null) electrodeA.RefreshAvailability();
            if (electrodeB != null) electrodeB.RefreshAvailability();
            PlayOptionalClip(powerOnClip);
            PoweredOn?.Invoke();
        }

        private void OnElectrodeStateChanged()
        {
            NotifyElectrodeStateChanged();
        }

        private void OnPoseChanged(ManDownPose pose)
        {
            ElectrodePoseChanged?.Invoke(pose);
        }

        /// <summary>Aggiorna il conteggio centrale degli elettrodi applicati senza duplicare azioni.</summary>
        public void NotifyElectrodeStateChanged()
        {
            int count = 0;
            for (int i = 0; i < 2; i++)
            {
                ManDownAedElectrode electrode = i == 0 ? electrodeA : electrodeB;
                if (electrode == null || !electrode.IsAttached) continue;
                count++;
                int zone = electrode.AttachedZone;
                if (zone < 0 || zone > 1 || zoneActionRecorded[zone] || scenario == null) continue;
                zoneActionRecorded[zone] = true;
                scenario.PlaceElectrode(zone);
            }

            bool complete = count == 2 && attachments != null && attachments.BothOccupied;
            if (scenario != null) scenario.ReportAedApplicationState(count, complete);
            ElectrodeStateChanged?.Invoke();
        }

        private void OnGrabEntered(Hand hand)
        {
            if (interactionsDisabled || recovered) return;
            if (scenario == null || !scenario.IsAedAvailable || !grabAllowed) return;

            deviceGrabbed = true;
            scenario.ReportAedRetrieved();
            if (scenario.IsAedRetrieved) scenario.ReportAedPoweredOn();
            RefreshLabel();
        }

        private void OnGrabExited(Hand hand)
        {
            deviceGrabbed = false;
            RefreshLabel();
        }

        private void RefreshInteraction()
        {
            if (interaction != null) interaction.Grabbable = !interactionsDisabled && (grabAllowed || recovered);
            if (shockButton != null) shockButton.Selectable = !interactionsDisabled && poweredOn && IsAvailable;
        }

        private void RefreshLabel()
        {
            if (label == null) return;
            bool available = IsAvailable;
            label.Suppressed = !available || interactionsDisabled || deviceGrabbed || (!grabAllowed && !recovered);
        }

        private void ApplyIndicatorColor()
        {
            Color color = offColor;
            switch (state)
            {
                case ManDownAedState.PoweredAwaitingElectrodes:
                case ManDownAedState.ShockNotAdvised:
                case ManDownAedState.ShockDelivered:
                    color = onColor; break;
                case ManDownAedState.AwaitingHandsClear: color = awaitingClearColor; break;
                case ManDownAedState.Analyzing: color = analyzingColor; break;
                case ManDownAedState.ShockAuthorized:
                    color = safetyBlocked ? blockedColor : authorizedColor; break;
            }
            if (indicator == null) return;
            if (indicatorBlock == null) indicatorBlock = new MaterialPropertyBlock();
            indicator.GetPropertyBlock(indicatorBlock);
            indicatorBlock.SetColor("_BaseColor", color);
            indicator.SetPropertyBlock(indicatorBlock);
        }

        private void UpdateIndicatorColor()
        {
            if (state == ManDownAedState.ShockAuthorized && !safetyBlocked && blinkHz > 0f)
            {
                float blink = Mathf.Sin(Time.time * blinkHz * Mathf.PI * 2f) >= 0f ? 1f : 0.35f;
                if (indicator == null) return;
                if (indicatorBlock == null) indicatorBlock = new MaterialPropertyBlock();
                indicator.GetPropertyBlock(indicatorBlock);
                indicatorBlock.SetColor("_BaseColor", authorizedColor * blink);
                indicator.SetPropertyBlock(indicatorBlock);
                return;
            }
            ApplyIndicatorColor();
        }

        private void PlayAuthorizationBeep()
        {
            if (audioSource == null) return;
            audioSource.Stop();
            audioSource.clip = authorizationBeep;
            if (authorizationBeep == null) return;
            audioSource.volume = audioVolume;
            audioSource.spatialBlend = 1f;
            audioSource.loop = false;
            audioSource.minDistance = audioMinDistance;
            audioSource.maxDistance = audioMaxDistance;
            audioSource.Play();
        }

        private void PlayOptionalClip(AudioClip clip)
        {
            if (audioSource == null || clip == null) return;
            audioSource.PlayOneShot(clip, audioVolume);
        }

        private void StopAudio()
        {
            if (audioSource != null) audioSource.Stop();
        }

        private ManDownAedSnapshot BuildSnapshot()
        {
            return new ManDownAedSnapshot
            {
                StateLabel = state.ToString(),
                PoweredOn = poweredOn,
                ElectrodesApplied = attachments != null && attachments.BothOccupied,
                AnalysisStartedCount = analysisStartedCount,
                AnalysisInterruptedCount = analysisInterruptedCount,
                ContactInterruptCount = contactInterruptCount,
                FirstAnalysisAt = firstAnalysisAt,
                ShockAdvised = shockAdvised,
                AuthorizationCount = authorizationCount,
                FirstAuthorizationAt = firstAuthorizationAt,
                LastAuthorizationAt = lastAuthorizationAt,
                SignalAt = signalAt,
                ShockAt = shockAt,
                ResponseTime = responseTime,
                ResponseQuality = responseQuality,
                ShockCompleted = shockCompleted,
                InvalidEarlyPressCount = invalidEarlyPressCount,
                InvalidContactCount = invalidContactCount,
                InvalidMissingElectrodesCount = invalidMissingElectrodesCount,
                InvalidNotAdvisedCount = invalidNotAdvisedCount,
                InappropriateUseCount = inappropriateUseCount
            };
        }

        private void ReportSnapshot()
        {
            if (scenario != null) scenario.ReportAedSnapshot(BuildSnapshot());
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (shockButton == null) return;
            Collider buttonCollider = shockButton.GetComponent<Collider>();
            if (buttonCollider == null) return;
            Gizmos.color = new Color(0.15f, 0.95f, 0.95f);
            Gizmos.DrawWireCube(buttonCollider.bounds.center, buttonCollider.bounds.size);
        }
#endif

        private void IgnorePlayerBodyCollisions()
        {
            IgnorePlayerBodyCollisions(RigRegistration.Instance);
        }

        private void OnRigRegistered(RigRegistration rig)
        {
            if (rig != null) IgnorePlayerBodyCollisions(rig);
        }

        private void IgnorePlayerBodyCollisions(RigRegistration rig)
        {
            if (rig == null) return;
            Collider[] playerColliders = rig.GetComponentsInChildren<Collider>(true);
            if (playerColliders.Length == 0) return;

            // Come per i telefoni, ma esteso ai figli: il pulsante di scarica ha un BoxCollider
            // proprio che si muove insieme al DAE quando viene afferrato.
            Collider[] deviceColliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < deviceColliders.Length; i++)
            {
                if (deviceColliders[i] == null) continue;
                for (int j = 0; j < playerColliders.Length; j++)
                {
                    if (playerColliders[j] != null)
                        Physics.IgnoreCollision(deviceColliders[i], playerColliders[j], true);
                }
            }
        }

    }
}
