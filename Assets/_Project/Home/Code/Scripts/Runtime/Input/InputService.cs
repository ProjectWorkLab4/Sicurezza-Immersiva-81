using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.XR;

namespace SI81.Input
{
    /// <summary>
    /// Unico proprietario delle azioni VR condivise. Crea l'asset di azioni nel codice (nessun file .inputactions),
    /// espone polling, eventi C# per mano e un bridge UnityEvent opzionale. Una sola istanza runtime, creata prima della
    /// prima scena e marcata DontDestroyOnLoad; lo stato statico si azzera su SubsystemRegistration.
    /// I contesti <see cref="InputChannel.World"/> sospendono Select/Grab/Move/Turn senza toccare il canale UI.
    /// </summary>
    [DefaultExecutionOrder(EXECUTION_ORDER)]
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/Input/Input Service")]
    public sealed class InputService : MonoBehaviour
    {
        /// <summary>Ordine di esecuzione: prima dei consumatori, dopo l'aggiornamento dell'Input System.</summary>
        public const int EXECUTION_ORDER = -1000;

        private const int HAND_COUNT = 2;

        /// <summary>Istanza runtime unica (null se il servizio non è ancora stato creato).</summary>
        public static InputService Instance { get; private set; }

        // =====================================================================
        // Eventi
        // =====================================================================

        /// <summary>Grilletto premuto: riporta la mano che ha selezionato.</summary>
        public event Action<Hand> SelectPerformed;
        /// <summary>Grilletto rilasciato o selezione annullata (contesto/UI): riporta la mano.</summary>
        public event Action<Hand> SelectCanceled;
        /// <summary>Impugnatura premuta: riporta la mano.</summary>
        public event Action<Hand> GrabPerformed;
        /// <summary>Impugnatura rilasciata o presa annullata: riporta la mano.</summary>
        public event Action<Hand> GrabCanceled;
        /// <summary>Tasto menu premuto: riporta la mano (normalmente sinistra).</summary>
        public event Action<Hand> MenuPerformed;
        /// <summary>Tasto indietro premuto: riporta la mano.</summary>
        public event Action<Hand> BackPerformed;
        /// <summary>Stick di movimento cambiato: mano sinistra e valore grezzo.</summary>
        public event Action<Hand, Vector2> MoveChanged;
        /// <summary>Stick di rotazione cambiato: mano destra e valore grezzo.</summary>
        public event Action<Hand, Vector2> TurnChanged;

        /// <summary>Comodità AnyHand: select premuto su una qualsiasi mano.</summary>
        public event Action SelectPerformedAnyHand;
        /// <summary>Comodità AnyHand: select annullato su una qualsiasi mano.</summary>
        public event Action SelectCanceledAnyHand;
        /// <summary>Comodità AnyHand: grab premuto su una qualsiasi mano.</summary>
        public event Action GrabPerformedAnyHand;
        /// <summary>Comodità AnyHand: grab annullato su una qualsiasi mano.</summary>
        public event Action GrabCanceledAnyHand;

        // =====================================================================
        // Azioni
        // =====================================================================

        private InputActionAsset actions;
        private InputActionMap worldMap;
        private InputActionMap uiMap;
        private InputAction selectAction;
        private InputAction grabAction;
        private InputAction menuAction;
        private InputAction backAction;
        private InputAction moveAction;
        private InputAction turnAction;

        // =====================================================================
        // Stato per mano e contesti
        // =====================================================================

        private readonly bool[] selectHeld = new bool[HAND_COUNT];
        private readonly bool[] grabHeld = new bool[HAND_COUNT];
        private readonly int[] selectPressedFrame = { -1, -1 };
        private readonly bool[] uiPointing = new bool[HAND_COUNT];
        private readonly List<ContextEntry> contexts = new List<ContextEntry>(4);

        private Vector2 moveValue;
        private Vector2 turnValue;
        private int menuPressedFrame = -1;
        private int backPressedFrame = -1;
        private bool backEnabled;

        // =====================================================================
        // Posa aim (cache dei controlli, nessuna ricerca per frame)
        // =====================================================================

        private XRController leftController;
        private XRController rightController;
        private Vector3Control leftAimPosition;
        private QuaternionControl leftAimRotation;
        private Vector3Control rightAimPosition;
        private QuaternionControl rightAimRotation;
        private bool deviceEventsSubscribed;

        // Evita di ripetere l'avviso per ogni istanza duplicata nella stessa sessione.
        private static bool duplicateWarned;

        /// <summary>Asset delle azioni create a runtime (null prima di Awake).</summary>
        public InputActionAsset Actions => actions;
        /// <summary>Mappa delle azioni del mondo (Select, Grab, Menu, Move, Turn).</summary>
        public InputActionMap WorldMap => worldMap;
        /// <summary>Mappa delle azioni UI, da assegnare all'unico InputSystemUIInputModule.</summary>
        public InputActionMap UIMap => uiMap;

        /// <summary>
        /// Crea il servizio se non esiste e lo restituisce. Usato dal bootstrap e dai test.
        /// </summary>
        /// <returns>L'istanza unica.</returns>
        public static InputService EnsureInstance()
        {
            if (Instance != null) return Instance;
            GameObject host = new GameObject("[SI81 InputService]");
            return host.AddComponent<InputService>();
        }

        /// <summary>
        /// Distrugge l'istanza corrente. Serve ai test e al bootstrap con Domain Reload disattivato.
        /// </summary>
        public static void ResetInstance()
        {
            InputService instance = Instance;
            Instance = null;
            if (instance == null) return;
            instance.enabled = false;
            if (Application.isPlaying) Destroy(instance.gameObject);
            else DestroyImmediate(instance.gameObject);
        }

        /// <summary>
        /// Crea il servizio prima della prima scena.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            EnsureInstance();
        }

        /// <summary>
        /// Azzera lo stato statico quando parte il Play Mode con Domain Reload disattivato.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
            duplicateWarned = false;
        }

        /// <summary>
        /// Registra l'istanza, rifiuta i duplicati e crea le azioni.
        /// </summary>
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                if (!duplicateWarned)
                {
                    duplicateWarned = true;
                    Debug.LogWarning("[SI81 Input] Istanza duplicata rifiutata: vince la prima. " +
                                     "Togli l'InputService dalla scena o dal prefab.", this);
                }
                enabled = false;
                Destroy(this);
                return;
            }

            Instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
            BuildActions();
            RefreshBackAction();
        }

        /// <summary>
        /// Abilita le mappe, la cache della posa aim e il filtro Back.
        /// </summary>
        private void OnEnable()
        {
            if (actions == null) return;
            worldMap.Enable();
            uiMap.Enable();
            RefreshBackAction();
            if (!deviceEventsSubscribed)
            {
                InputSystem.onDeviceChange += HandleDeviceChange;
                deviceEventsSubscribed = true;
            }
            CacheControllers();
        }

        /// <summary>
        /// Disabilita le mappe e scollega i cambi dispositivo.
        /// </summary>
        private void OnDisable()
        {
            if (deviceEventsSubscribed)
            {
                InputSystem.onDeviceChange -= HandleDeviceChange;
                deviceEventsSubscribed = false;
            }
            if (worldMap != null) worldMap.Disable();
            if (uiMap != null) uiMap.Disable();
            ClearState();
        }

        /// <summary>
        /// Azzera lo stato per mano senza notificare (componente disattivato).
        /// </summary>
        private void ClearState()
        {
            for (int i = 0; i < HAND_COUNT; i++)
            {
                selectHeld[i] = false;
                grabHeld[i] = false;
                selectPressedFrame[i] = -1;
                uiPointing[i] = false;
            }
            menuPressedFrame = -1;
            backPressedFrame = -1;
            moveValue = default;
            turnValue = default;
        }

        /// <summary>
        /// Azzera l'istanza e distrugge l'asset creato in memoria.
        /// </summary>
        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (actions == null) return;
            if (Application.isPlaying) Destroy(actions);
            else DestroyImmediate(actions);
            actions = null;
        }

        /// <summary>
        /// Aggiorna i valori di Move/Turn una volta per frame; nessuna allocazione.
        /// Con World sospeso i valori vengono azzerati e non arrivano input di movimento.
        /// </summary>
        private void Update()
        {
            if (moveAction == null || turnAction == null) return;
            bool suppressed = IsSuppressed(InputChannel.World);
            Vector2 nextMove = suppressed ? Vector2.zero : moveAction.ReadValue<Vector2>();
            Vector2 nextTurn = suppressed ? Vector2.zero : turnAction.ReadValue<Vector2>();
            if (nextMove != moveValue)
            {
                moveValue = nextMove;
                MoveChanged?.Invoke(Hand.Left, nextMove);
            }
            if (nextTurn != turnValue)
            {
                turnValue = nextTurn;
                TurnChanged?.Invoke(Hand.Right, nextTurn);
            }
        }

        // =====================================================================
        // Costruzione delle azioni
        // =====================================================================

        /// <summary>
        /// Crea mappe, azioni, schemi e binding in memoria, con i percorsi presi da <see cref="InputBindings"/>.
        /// </summary>
        private void BuildActions()
        {
            actions = ScriptableObject.CreateInstance<InputActionAsset>();
            actions.name = "SI81Input";
            actions.AddControlScheme(InputBindings.MetaQuestScheme).WithRequiredDevice("<XRController>");
            actions.AddControlScheme(InputBindings.EditorScheme).WithRequiredDevice("<Keyboard>").WithOptionalDevice("<Mouse>");

            worldMap = actions.AddActionMap(InputBindings.WorldMap);
            selectAction = worldMap.AddAction(InputBindings.Select, InputActionType.PassThrough, expectedControlLayout: "Button");
            selectAction.AddBinding(InputBindings.LeftTrigger, groups: InputBindings.MetaQuestScheme);
            selectAction.AddBinding(InputBindings.RightTrigger, groups: InputBindings.MetaQuestScheme);
            selectAction.AddBinding(InputBindings.MouseLeft, groups: InputBindings.EditorScheme);

            grabAction = worldMap.AddAction(InputBindings.Grab, InputActionType.PassThrough, expectedControlLayout: "Button");
            grabAction.AddBinding(InputBindings.LeftGrip, groups: InputBindings.MetaQuestScheme);
            grabAction.AddBinding(InputBindings.RightGrip, groups: InputBindings.MetaQuestScheme);
            grabAction.AddBinding(InputBindings.MouseRight, groups: InputBindings.EditorScheme);

            menuAction = worldMap.AddAction(InputBindings.Menu, InputActionType.Button);
            menuAction.AddBinding(InputBindings.LeftMenu, groups: InputBindings.MetaQuestScheme);
            menuAction.AddBinding(InputBindings.KeyboardTab, groups: InputBindings.EditorScheme);

            moveAction = worldMap.AddAction(InputBindings.Move, InputActionType.Value, expectedControlLayout: "Vector2");
            moveAction.AddBinding(InputBindings.LeftThumbstick, groups: InputBindings.MetaQuestScheme);
            AddKeyboardMove(moveAction);

            turnAction = worldMap.AddAction(InputBindings.Turn, InputActionType.Value, expectedControlLayout: "Vector2");
            turnAction.AddBinding(InputBindings.RightThumbstick, groups: InputBindings.MetaQuestScheme);
            InputActionSetupExtensions.CompositeSyntax turnComposite = turnAction.AddCompositeBinding("2DVector");
            turnComposite.With("Left", InputBindings.KeyboardTurnLeft, InputBindings.EditorScheme);
            turnComposite.With("Right", InputBindings.KeyboardTurnRight, InputBindings.EditorScheme);

            uiMap = actions.AddActionMap(InputBindings.UIMap);
            InputAction point = uiMap.AddAction(InputBindings.Point, InputActionType.PassThrough, expectedControlLayout: "Vector2");
            point.AddBinding(InputBindings.MousePosition, groups: InputBindings.EditorScheme);

            InputAction click = uiMap.AddAction(InputBindings.Click, InputActionType.PassThrough, expectedControlLayout: "Button");
            click.AddBinding(InputBindings.MouseLeft, groups: InputBindings.EditorScheme);
            click.AddBinding(InputBindings.LeftTrigger, groups: InputBindings.MetaQuestScheme);
            click.AddBinding(InputBindings.RightTrigger, groups: InputBindings.MetaQuestScheme);

            InputAction scroll = uiMap.AddAction(InputBindings.Scroll, InputActionType.PassThrough, expectedControlLayout: "Vector2");
            scroll.AddBinding(InputBindings.MouseScroll, groups: InputBindings.EditorScheme);

            InputAction trackedPosition = uiMap.AddAction(InputBindings.TrackedDevicePosition, InputActionType.PassThrough, expectedControlLayout: "Vector3");
            trackedPosition.AddBinding(InputBindings.LeftPointerPosition, groups: InputBindings.MetaQuestScheme);
            trackedPosition.AddBinding(InputBindings.RightPointerPosition, groups: InputBindings.MetaQuestScheme);

            InputAction trackedOrientation = uiMap.AddAction(InputBindings.TrackedDeviceOrientation, InputActionType.PassThrough, expectedControlLayout: "Quaternion");
            trackedOrientation.AddBinding(InputBindings.LeftPointerRotation, groups: InputBindings.MetaQuestScheme);
            trackedOrientation.AddBinding(InputBindings.RightPointerRotation, groups: InputBindings.MetaQuestScheme);

            InputAction navigate = uiMap.AddAction(InputBindings.Navigate, InputActionType.Value, expectedControlLayout: "Vector2");
            navigate.AddBinding(InputBindings.LeftThumbstick, groups: InputBindings.MetaQuestScheme);
            navigate.AddBinding(InputBindings.RightThumbstick, groups: InputBindings.MetaQuestScheme);
            AddKeyboardMove(navigate);

            backAction = uiMap.AddAction(InputBindings.Back, InputActionType.Button);
            backAction.AddBinding(InputBindings.LeftBack, groups: InputBindings.MetaQuestScheme);
            backAction.AddBinding(InputBindings.RightBack, groups: InputBindings.MetaQuestScheme);
            backAction.AddBinding(InputBindings.KeyboardEscape, groups: InputBindings.EditorScheme);
            backAction.AddBinding(InputBindings.KeyboardBackspace, groups: InputBindings.EditorScheme);

            selectAction.performed += HandleSelectPerformed;
            selectAction.canceled += HandleSelectCanceled;
            grabAction.performed += HandleGrabPerformed;
            grabAction.canceled += HandleGrabCanceled;
            menuAction.performed += HandleMenuPerformed;
            backAction.performed += HandleBackPerformed;
        }

        /// <summary>
        /// Aggiunge lo schema Editor (WASD + frecce) a un'azione Vector2 di movimento/navigazione.
        /// </summary>
        /// <param name="action">Azione a cui aggiungere il composito 2DVector.</param>
        private static void AddKeyboardMove(InputAction action)
        {
            InputActionSetupExtensions.CompositeSyntax composite = action.AddCompositeBinding("2DVector");
            composite.With("Up", InputBindings.KeyboardW, InputBindings.EditorScheme);
            composite.With("Down", InputBindings.KeyboardS, InputBindings.EditorScheme);
            composite.With("Left", InputBindings.KeyboardA, InputBindings.EditorScheme);
            composite.With("Right", InputBindings.KeyboardD, InputBindings.EditorScheme);
            composite.With("Up", InputBindings.KeyboardUp, InputBindings.EditorScheme);
            composite.With("Down", InputBindings.KeyboardDown, InputBindings.EditorScheme);
            composite.With("Left", InputBindings.KeyboardLeft, InputBindings.EditorScheme);
            composite.With("Right", InputBindings.KeyboardRight, InputBindings.EditorScheme);
        }

        // =====================================================================
        // Callback delle azioni
        // =====================================================================

        /// <summary>
        /// Grilletto cambiato: con l'azione PassThrough ogni mano è indipendente (anche con l'altra già premuta).
        /// </summary>
        /// <param name="context">Contesto dell'azione Input System.</param>
        private void HandleSelectPerformed(InputAction.CallbackContext context)
        {
            Hand hand = HandOf(context.control);
            if (ReadButtonValue(context.control) >= 0.5f) HandleSelectPressed(hand);
            else HandleSelectReleased(hand);
        }

        /// <summary>
        /// Grilletto rilasciato/annullato: chiude la selezione della mano.
        /// </summary>
        /// <param name="context">Contesto dell'azione Input System.</param>
        private void HandleSelectCanceled(InputAction.CallbackContext context)
        {
            HandleSelectReleased(HandOf(context.control));
        }

        /// <summary>
        /// Consegna Select alla mano, salvo UI su quella mano o World sospeso.
        /// </summary>
        /// <param name="hand">Mano che ha premuto.</param>
        private void HandleSelectPressed(Hand hand)
        {
            if (IsSuppressed(InputChannel.World)) return;
            int index = hand.Index();
            if (uiPointing[index] || selectHeld[index]) return;
            selectHeld[index] = true;
            selectPressedFrame[index] = Time.frameCount;
            SelectPerformed?.Invoke(hand);
            SelectPerformedAnyHand?.Invoke();
        }

        /// <summary>
        /// Chiude Select per la mano, se era tenuto.
        /// </summary>
        /// <param name="hand">Mano che ha rilasciato.</param>
        private void HandleSelectReleased(Hand hand)
        {
            int index = hand.Index();
            if (!selectHeld[index]) return;
            selectHeld[index] = false;
            SelectCanceled?.Invoke(hand);
            SelectCanceledAnyHand?.Invoke();
        }

        /// <summary>
        /// Impugnatura cambiata: con l'azione PassThrough ogni mano è indipendente.
        /// </summary>
        /// <param name="context">Contesto dell'azione Input System.</param>
        private void HandleGrabPerformed(InputAction.CallbackContext context)
        {
            Hand hand = HandOf(context.control);
            if (ReadButtonValue(context.control) >= 0.5f) HandleGrabPressed(hand);
            else HandleGrabReleased(hand);
        }

        /// <summary>
        /// Impugnatura rilasciata/annullata: chiude la presa della mano.
        /// </summary>
        /// <param name="context">Contesto dell'azione Input System.</param>
        private void HandleGrabCanceled(InputAction.CallbackContext context)
        {
            HandleGrabReleased(HandOf(context.control));
        }

        /// <summary>
        /// Consegna Grab alla mano, salvo World sospeso.
        /// </summary>
        /// <param name="hand">Mano che ha premuto.</param>
        private void HandleGrabPressed(Hand hand)
        {
            if (IsSuppressed(InputChannel.World)) return;
            int index = hand.Index();
            if (grabHeld[index]) return;
            grabHeld[index] = true;
            GrabPerformed?.Invoke(hand);
            GrabPerformedAnyHand?.Invoke();
        }

        /// <summary>
        /// Chiude Grab per la mano, se era tenuto.
        /// </summary>
        /// <param name="hand">Mano che ha rilasciato.</param>
        private void HandleGrabReleased(Hand hand)
        {
            int index = hand.Index();
            if (!grabHeld[index]) return;
            grabHeld[index] = false;
            GrabCanceled?.Invoke(hand);
            GrabCanceledAnyHand?.Invoke();
        }

        /// <summary>
        /// Tasto menu premuto: riporta la mano all'owner della pausa.
        /// </summary>
        /// <param name="context">Contesto dell'azione Input System.</param>
        private void HandleMenuPerformed(InputAction.CallbackContext context)
        {
            Hand hand = HandOf(context.control);
            menuPressedFrame = Time.frameCount;
            MenuPerformed?.Invoke(hand);
        }

        /// <summary>
        /// Tasto indietro premuto (solo con menu aperto, vedi SetBackEnabled).
        /// </summary>
        /// <param name="context">Contesto dell'azione Input System.</param>
        private void HandleBackPerformed(InputAction.CallbackContext context)
        {
            Hand hand = HandOf(context.control);
            backPressedFrame = Time.frameCount;
            BackPerformed?.Invoke(hand);
        }

        // =====================================================================
        // Contesti
        // =====================================================================

        /// <summary>
        /// Sospende un canale per conto di un owner. Il primo contesto World annulla Select/Grab tenuti.
        /// </summary>
        /// <param name="channel">Canale da sospendere.</param>
        /// <param name="owner">Owner del contesto (non null).</param>
        /// <returns>Handle da rilasciare con Dispose.</returns>
        public InputContextHandle PushContext(InputChannel channel, object owner)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            bool wasWorldSuppressed = IsSuppressed(InputChannel.World);
            contexts.Add(new ContextEntry(channel, owner));
            if (channel == InputChannel.World && !wasWorldSuppressed) CancelWorldActions();
            return new InputContextHandle(this, channel, owner);
        }

        /// <summary>
        /// Toglie il contesto dell'owner indicato. Idempotente e non tocca gli altri owner.
        /// </summary>
        /// <param name="channel">Canale del contesto.</param>
        /// <param name="owner">Owner del contesto.</param>
        public void PopContext(InputChannel channel, object owner)
        {
            if (owner == null) return;
            for (int i = contexts.Count - 1; i >= 0; i--)
            {
                if (contexts[i].Channel == channel && ReferenceEquals(contexts[i].Owner, owner))
                {
                    contexts.RemoveAt(i);
                    return;
                }
            }
        }

        /// <summary>
        /// true se il canale è sospeso da almeno un contesto.
        /// </summary>
        /// <param name="channel">Canale da controllare.</param>
        public bool IsSuppressed(InputChannel channel)
        {
            for (int i = 0; i < contexts.Count; i++)
                if (contexts[i].Channel == channel) return true;
            return false;
        }

        /// <summary>
        /// true se World è sospeso (menu/pausa aperti).
        /// </summary>
        public bool IsWorldSuppressed => IsSuppressed(InputChannel.World);

        /// <summary>
        /// Annulla Select e Grab tenuti e notifica i consumatori.
        /// </summary>
        private void CancelWorldActions()
        {
            for (int i = 0; i < HAND_COUNT; i++)
            {
                if (selectHeld[i])
                {
                    selectHeld[i] = false;
                    selectPressedFrame[i] = -1;
                    SelectCanceled?.Invoke((Hand)i);
                    SelectCanceledAnyHand?.Invoke();
                }
                if (grabHeld[i])
                {
                    grabHeld[i] = false;
                    GrabCanceled?.Invoke((Hand)i);
                    GrabCanceledAnyHand?.Invoke();
                }
            }
        }

        // =====================================================================
        // Gate UI e Back
        // =====================================================================

        /// <summary>
        /// Aggiorna lo stato del raggio UI per una mano. Se la mano punta la UI, il suo Select non raggiunge il mondo.
        /// </summary>
        /// <param name="hand">Mano (Any per entrambe).</param>
        /// <param name="pointing">true se il raggio è su un elemento UI.</param>
        public void SetUIPointerOverUI(Hand hand, bool pointing)
        {
            if (hand == Hand.Any)
            {
                SetUIPointerOverUI(Hand.Left, pointing);
                SetUIPointerOverUI(Hand.Right, pointing);
                return;
            }
            if (!hand.IsSingle()) return;
            int index = hand.Index();
            if (uiPointing[index] == pointing) return;
            uiPointing[index] = pointing;
            if (!pointing || !selectHeld[index]) return;
            selectHeld[index] = false;
            selectPressedFrame[index] = -1;
            SelectCanceled?.Invoke(hand);
            SelectCanceledAnyHand?.Invoke();
        }

        /// <summary>
        /// true se la mano sta puntando la UI.
        /// </summary>
        /// <param name="hand">Mano singola.</param>
        public bool IsPointingAtUI(Hand hand)
        {
            return hand.IsSingle() && uiPointing[hand.Index()];
        }

        /// <summary>
        /// Abilita/disabilita Back. La UI lo abilita solo mentre il menu è visibile.
        /// </summary>
        /// <param name="value">true per abilitarlo.</param>
        public void SetBackEnabled(bool value)
        {
            backEnabled = value;
            RefreshBackAction();
        }

        /// <summary>
        /// Allinea l'azione Back allo stato richiesto e all'attivazione del componente.
        /// </summary>
        private void RefreshBackAction()
        {
            if (backAction == null) return;
            if (backEnabled && isActiveAndEnabled) backAction.Enable();
            else backAction.Disable();
        }

        // =====================================================================
        // Polling
        // =====================================================================

        /// <summary>
        /// true se Select è stato premuto in questo frame su una qualsiasi mano.
        /// </summary>
        public bool WasSelectPressed()
        {
            return selectPressedFrame[0] == Time.frameCount || selectPressedFrame[1] == Time.frameCount;
        }

        /// <summary>
        /// true se Select è stato premuto in questo frame sulla mano indicata (Any = entrambe).
        /// </summary>
        /// <param name="hand">Mano da controllare.</param>
        public bool WasSelectPressed(Hand hand)
        {
            if (hand == Hand.Any) return WasSelectPressed();
            return hand.IsSingle() && selectPressedFrame[hand.Index()] == Time.frameCount;
        }

        /// <summary>
        /// true se Grab è tenuto su una qualsiasi mano.
        /// </summary>
        public bool IsGrabHeld()
        {
            return grabHeld[0] || grabHeld[1];
        }

        /// <summary>
        /// true se Grab è tenuto sulla mano indicata (Any = entrambe).
        /// </summary>
        /// <param name="hand">Mano da controllare.</param>
        public bool IsGrabHeld(Hand hand)
        {
            if (hand == Hand.Any) return IsGrabHeld();
            return hand.IsSingle() && grabHeld[hand.Index()];
        }

        /// <summary>
        /// true se il tasto menu è stato premuto in questo frame.
        /// </summary>
        public bool WasMenuPressed()
        {
            return menuPressedFrame == Time.frameCount;
        }

        /// <summary>
        /// true se Back è stato premuto in questo frame.
        /// </summary>
        public bool WasBackPressed()
        {
            return backPressedFrame == Time.frameCount;
        }

        /// <summary>
        /// Valore corrente dello stick sinistro (zero se World è sospeso).
        /// </summary>
        public Vector2 ReadMove()
        {
            return moveValue;
        }

        /// <summary>
        /// Valore corrente dello stick destro (zero se World è sospeso).
        /// </summary>
        public Vector2 ReadTurn()
        {
            return turnValue;
        }

        // =====================================================================
        // Posa aim in spazio di tracciamento
        // =====================================================================

        /// <summary>
        /// Restituisce la posa "aim" del controller nello spazio di tracciamento (non mondo).
        /// Il rig la converte con la sua tracking origin. false se il controller non è tracciato/collegato.
        /// </summary>
        /// <param name="hand">Mano singola.</param>
        /// <param name="position">Posizione locale nello spazio di tracciamento.</param>
        /// <param name="rotation">Rotazione locale nello spazio di tracciamento.</param>
        public bool TryGetAimPose(Hand hand, out Vector3 position, out Quaternion rotation)
        {
            position = default;
            rotation = default;
            if (!hand.IsSingle()) return false;

            XRController controller = hand == Hand.Left ? leftController : rightController;
            if (controller == null)
            {
                CacheControllers();
                controller = hand == Hand.Left ? leftController : rightController;
            }
            if (controller == null) return false;

            Vector3Control aimPosition = hand == Hand.Left ? leftAimPosition : rightAimPosition;
            QuaternionControl aimRotation = hand == Hand.Left ? leftAimRotation : rightAimRotation;
            if (aimPosition != null && aimRotation != null)
            {
                position = aimPosition.ReadValue();
                rotation = aimRotation.ReadValue();
            }
            else
            {
                position = controller.devicePosition.ReadValue();
                rotation = controller.deviceRotation.ReadValue();
            }
            return controller.isTracked.isPressed;
        }

        /// <summary>
        /// Aggiorna la cache dei controller e dei controlli "pointer" (aim) quando cambiano i dispositivi.
        /// </summary>
        /// <param name="device">Dispositivo cambiato (non usato).</param>
        /// <param name="change">Tipo di cambio (non usato).</param>
        private void HandleDeviceChange(InputDevice device, InputDeviceChange change)
        {
            CacheControllers();
        }

        /// <summary>
        /// Legge i controller XR correnti e i loro controlli aim, senza ricerche per frame.
        /// </summary>
        private void CacheControllers()
        {
            leftController = XRController.leftHand;
            rightController = XRController.rightHand;
            leftAimPosition = leftController != null ? leftController.TryGetChildControl<Vector3Control>("pointerPosition") : null;
            leftAimRotation = leftController != null ? leftController.TryGetChildControl<QuaternionControl>("pointerRotation") : null;
            rightAimPosition = rightController != null ? rightController.TryGetChildControl<Vector3Control>("pointerPosition") : null;
            rightAimRotation = rightController != null ? rightController.TryGetChildControl<QuaternionControl>("pointerRotation") : null;
        }

        /// <summary>
        /// Legge il valore del controllo che ha attivato il callback, non il valore complessivo dell'azione:
        /// con più binding PassThrough (due grilletti) il valore dell'azione sarebbe ambiguo.
        /// </summary>
        /// <param name="control">Controllo del binding.</param>
        /// <returns>1 se premuto, 0 se rilasciato.</returns>
        private static float ReadButtonValue(InputControl control)
        {
            if (control is InputControl<float> floatControl) return floatControl.ReadValue();
            return 0f;
        }

        /// <summary>
        /// Determina la mano dal dispositivo che ha prodotto l'azione. Mouse/tastiera ricadono sulla destra.
        /// </summary>
        /// <param name="control">Controllo che ha attivato l'azione.</param>
        /// <returns>Mano sinistra o destra.</returns>
        private static Hand HandOf(InputControl control)
        {
            InputDevice device = control != null ? control.device : null;
            if (device != null)
            {
                var usages = device.usages;
                for (int i = 0; i < usages.Count; i++)
                {
                    if (usages[i] == CommonUsages.LeftHand) return Hand.Left;
                    if (usages[i] == CommonUsages.RightHand) return Hand.Right;
                }
            }
            return Hand.Right;
        }

        /// <summary>
        /// Contesto inserito nello stack, con canale e owner.
        /// </summary>
        private readonly struct ContextEntry
        {
            /// <summary>Canale sospeso.</summary>
            public readonly InputChannel Channel;
            /// <summary>Owner che ha chiesto la sospensione.</summary>
            public readonly object Owner;

            /// <summary>
            /// Crea una voce di contesto.
            /// </summary>
            /// <param name="channel">Canale.</param>
            /// <param name="owner">Owner.</param>
            public ContextEntry(InputChannel channel, object owner)
            {
                Channel = channel;
                Owner = owner;
            }
        }
    }
}
