namespace SI81.Input
{
    /// <summary>
    /// Nomi delle mappe/azioni, schemi di controllo e percorsi di binding dell'Input Service.
    /// Per cambiare un tasto si modifica SOLO una costante di questa classe: il servizio non scrive mai un percorso
    /// direttamente in un punto di chiamata.
    /// I percorsi dei controller corrispondono ai controlli OpenXR usati dalla UI comune (posa "aim" = pointer).
    /// </summary>
    public static class InputBindings
    {
        // =====================================================================
        // Mappe, azioni e schemi
        // =====================================================================

        /// <summary>Mappa delle azioni del mondo 3D.</summary>
        public const string WorldMap = "World";
        /// <summary>Mappa delle azioni della UI (assegnata all'unico InputSystemUIInputModule).</summary>
        public const string UIMap = "UI";
        /// <summary>Schema di controllo dei controller Meta Quest.</summary>
        public const string MetaQuestScheme = "MetaQuest";
        /// <summary>Schema di controllo di tastiera e mouse (solo Editor).</summary>
        public const string EditorScheme = "Editor";

        /// <summary>Azione: grilletto, seleziona.</summary>
        public const string Select = "Select";
        /// <summary>Azione: impugnatura, presa.</summary>
        public const string Grab = "Grab";
        /// <summary>Azione: indietro (B/Y), attiva solo con il menu aperto.</summary>
        public const string Back = "Back";
        /// <summary>Azione: tasto menu sinistro, alterna la pausa.</summary>
        public const string Menu = "Menu";
        /// <summary>Azione: stick sinistro, movimento.</summary>
        public const string Move = "Move";
        /// <summary>Azione: stick destro, rotazione a scatti.</summary>
        public const string Turn = "Turn";
        /// <summary>Azione UI: posizione del puntatore mouse.</summary>
        public const string Point = "Point";
        /// <summary>Azione UI: click/selezione.</summary>
        public const string Click = "Click";
        /// <summary>Azione UI: rotellina.</summary>
        public const string Scroll = "Scroll";
        /// <summary>Azione UI: posizione del controller tracciato.</summary>
        public const string TrackedDevicePosition = "TrackedDevicePosition";
        /// <summary>Azione UI: rotazione del controller tracciato.</summary>
        public const string TrackedDeviceOrientation = "TrackedDeviceOrientation";
        /// <summary>Azione UI: navigazione da stick/tastiera.</summary>
        public const string Navigate = "Navigate";

        // =====================================================================
        // Percorsi dei controller XR (schema MetaQuest)
        // =====================================================================

        /// <summary>Grilletto sinistro.</summary>
        public const string LeftTrigger = "<XRController>{LeftHand}/triggerPressed";
        /// <summary>Grilletto destro.</summary>
        public const string RightTrigger = "<XRController>{RightHand}/triggerPressed";
        /// <summary>Impugnatura sinistra.</summary>
        public const string LeftGrip = "<XRController>{LeftHand}/gripPressed";
        /// <summary>Impugnatura destra.</summary>
        public const string RightGrip = "<XRController>{RightHand}/gripPressed";
        /// <summary>Tasto secondario (Y) del controller sinistro: indietro.</summary>
        public const string LeftBack = "<XRController>{LeftHand}/secondaryButton";
        /// <summary>Tasto secondario (B) del controller destro: indietro.</summary>
        public const string RightBack = "<XRController>{RightHand}/secondaryButton";
        /// <summary>Tasto menu del controller sinistro: pausa.</summary>
        public const string LeftMenu = "<XRController>{LeftHand}/menuButton";
        /// <summary>Stick sinistro: movimento.</summary>
        public const string LeftThumbstick = "<XRController>{LeftHand}/primary2DAxis";
        /// <summary>Stick destro: rotazione a scatti.</summary>
        public const string RightThumbstick = "<XRController>{RightHand}/primary2DAxis";
        /// <summary>Posa "aim" del controller sinistro (posizione).</summary>
        public const string LeftPointerPosition = "<XRController>{LeftHand}/pointerPosition";
        /// <summary>Posa "aim" del controller destro (posizione).</summary>
        public const string RightPointerPosition = "<XRController>{RightHand}/pointerPosition";
        /// <summary>Posa "aim" del controller sinistro (rotazione).</summary>
        public const string LeftPointerRotation = "<XRController>{LeftHand}/pointerRotation";
        /// <summary>Posa "aim" del controller destro (rotazione).</summary>
        public const string RightPointerRotation = "<XRController>{RightHand}/pointerRotation";

        // =====================================================================
        // Percorsi Editor (schema Editor)
        // =====================================================================

        /// <summary>Mouse: posizione puntatore UI.</summary>
        public const string MousePosition = "<Mouse>/position";
        /// <summary>Mouse: tasto sinistro, seleziona.</summary>
        public const string MouseLeft = "<Mouse>/leftButton";
        /// <summary>Mouse: tasto destro, presa.</summary>
        public const string MouseRight = "<Mouse>/rightButton";
        /// <summary>Mouse: rotellina.</summary>
        public const string MouseScroll = "<Mouse>/scroll";
        /// <summary>Tastiera: W, avanti.</summary>
        public const string KeyboardW = "<Keyboard>/w";
        /// <summary>Tastiera: S, indietro.</summary>
        public const string KeyboardS = "<Keyboard>/s";
        /// <summary>Tastiera: A, sinistra.</summary>
        public const string KeyboardA = "<Keyboard>/a";
        /// <summary>Tastiera: D, destra.</summary>
        public const string KeyboardD = "<Keyboard>/d";
        /// <summary>Tastiera: freccia su.</summary>
        public const string KeyboardUp = "<Keyboard>/upArrow";
        /// <summary>Tastiera: freccia giù.</summary>
        public const string KeyboardDown = "<Keyboard>/downArrow";
        /// <summary>Tastiera: freccia sinistra.</summary>
        public const string KeyboardLeft = "<Keyboard>/leftArrow";
        /// <summary>Tastiera: freccia destra.</summary>
        public const string KeyboardRight = "<Keyboard>/rightArrow";
        /// <summary>Tastiera: Q, rotazione a sinistra.</summary>
        public const string KeyboardTurnLeft = "<Keyboard>/q";
        /// <summary>Tastiera: E, rotazione a destra.</summary>
        public const string KeyboardTurnRight = "<Keyboard>/e";
        /// <summary>Tastiera: Esc, indietro.</summary>
        public const string KeyboardEscape = "<Keyboard>/escape";
        /// <summary>Tastiera: Backspace, indietro.</summary>
        public const string KeyboardBackspace = "<Keyboard>/backspace";
        /// <summary>Tastiera: Tab, menu/pausa.</summary>
        public const string KeyboardTab = "<Keyboard>/tab";
    }
}
