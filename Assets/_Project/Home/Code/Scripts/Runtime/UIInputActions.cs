using UnityEngine;
using UnityEngine.InputSystem;

namespace SI81.UI
{
    /// <summary>
    /// Azioni di input della UI comune, create nel codice (nessun file .inputactions da nominare o mantenere).
    /// I percorsi dei binding sono costanti documentate qui sotto: per cambiare un tasto modificare SOLO queste costanti.
    /// Le azioni dei puntatori sono PassThrough, come richiesto da InputSystemUIInputModule per distinguere più
    /// puntatori (mouse + due controller) nello stesso frame.
    /// </summary>
    public static class UIInputActions
    {
        // =====================================================================
        // Nomi delle mappe e delle azioni
        // =====================================================================

        /// <summary>Nome della mappa con le azioni dei puntatori (InputSystemUIInputModule).</summary>
        public const string POINTER_MAP = "UIPointer";
        /// <summary>Nome della mappa con le azioni di navigazione della UI comune.</summary>
        public const string NAVIGATION_MAP = "UINavigation";
        /// <summary>Azione posizione del mouse (Vector2).</summary>
        public const string ACTION_POINT = "Point";
        /// <summary>Azione click/selezione (mouse sinistro, grilletto dei controller).</summary>
        public const string ACTION_CLICK = "Click";
        /// <summary>Azione rotellina (Vector2).</summary>
        public const string ACTION_SCROLL = "ScrollWheel";
        /// <summary>Azione posizione del controller tracciato (Vector3).</summary>
        public const string ACTION_TRACKED_POSITION = "TrackedDevicePosition";
        /// <summary>Azione rotazione del controller tracciato (Quaternion).</summary>
        public const string ACTION_TRACKED_ORIENTATION = "TrackedDeviceOrientation";
        /// <summary>Azione "indietro" (BTN_Back, tasti B/Y, Esc, Backspace).</summary>
        public const string ACTION_BACK = "Back";

        // =====================================================================
        // Percorsi dei binding (Input System)
        // =====================================================================

        /// <summary>Posizione del controller sinistro: posa "aim" (pointer) di OpenXR, la direzione naturale del raggio, non la posa "grip".</summary>
        public const string LEFT_POSITION = "<XRController>{LeftHand}/pointerPosition";
        /// <summary>Posizione del controller destro.</summary>
        public const string RIGHT_POSITION = "<XRController>{RightHand}/pointerPosition";
        /// <summary>Rotazione del controller sinistro (posa "aim").</summary>
        public const string LEFT_ROTATION = "<XRController>{LeftHand}/pointerRotation";
        /// <summary>Rotazione del controller destro.</summary>
        public const string RIGHT_ROTATION = "<XRController>{RightHand}/pointerRotation";
        /// <summary>Grilletto premuto, controller sinistro (seleziona).</summary>
        public const string LEFT_TRIGGER = "<XRController>{LeftHand}/triggerPressed";
        /// <summary>Grilletto premuto, controller destro (seleziona).</summary>
        public const string RIGHT_TRIGGER = "<XRController>{RightHand}/triggerPressed";
        /// <summary>Tasto Y (secondario) del controller sinistro: indietro.</summary>
        public const string LEFT_BACK = "<XRController>{LeftHand}/secondaryButton";
        /// <summary>Tasto B (secondario) del controller destro: indietro.</summary>
        public const string RIGHT_BACK = "<XRController>{RightHand}/secondaryButton";
        /// <summary>Esc da tastiera: indietro (test in Editor).</summary>
        public const string KEYBOARD_BACK = "<Keyboard>/escape";
        /// <summary>Backspace da tastiera: indietro (test in Editor).</summary>
        public const string KEYBOARD_BACK_ALT = "<Keyboard>/backspace";
        /// <summary>Posizione del mouse (test in Editor).</summary>
        public const string MOUSE_POINT = "<Mouse>/position";
        /// <summary>Tasto sinistro del mouse (test in Editor).</summary>
        public const string MOUSE_CLICK = "<Mouse>/leftButton";
        /// <summary>Rotellina del mouse (test in Editor).</summary>
        public const string MOUSE_SCROLL = "<Mouse>/scroll";

        /// <summary>
        /// Crea un nuovo InputActionAsset in memoria con tutte le azioni della UI comune (non salvato su disco).
        /// Le azioni sono disabilitate: quelle dei puntatori le abilita InputSystemUIInputModule, "Back" chi la usa.
        /// </summary>
        /// <returns>L'asset creato, chiamato "UIInputActions".</returns>
        public static InputActionAsset CreateAsset()
        {
            InputActionAsset asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = "UIInputActions";

            // --- Puntatori (mouse in Editor + controller tracciati). ---
            InputActionMap pointer = asset.AddActionMap(POINTER_MAP);

            InputAction point = pointer.AddAction(ACTION_POINT, InputActionType.PassThrough, expectedControlLayout: "Vector2");
            point.AddBinding(MOUSE_POINT);

            InputAction click = pointer.AddAction(ACTION_CLICK, InputActionType.PassThrough, expectedControlLayout: "Button");
            click.AddBinding(MOUSE_CLICK);
            click.AddBinding(LEFT_TRIGGER);
            click.AddBinding(RIGHT_TRIGGER);

            InputAction scroll = pointer.AddAction(ACTION_SCROLL, InputActionType.PassThrough, expectedControlLayout: "Vector2");
            scroll.AddBinding(MOUSE_SCROLL);

            InputAction position = pointer.AddAction(ACTION_TRACKED_POSITION, InputActionType.PassThrough, expectedControlLayout: "Vector3");
            position.AddBinding(LEFT_POSITION);
            position.AddBinding(RIGHT_POSITION);

            InputAction rotation = pointer.AddAction(ACTION_TRACKED_ORIENTATION, InputActionType.PassThrough, expectedControlLayout: "Quaternion");
            rotation.AddBinding(LEFT_ROTATION);
            rotation.AddBinding(RIGHT_ROTATION);

            // --- Navigazione: indietro. ---
            InputActionMap navigation = asset.AddActionMap(NAVIGATION_MAP);
            InputAction back = navigation.AddAction(ACTION_BACK, InputActionType.Button);
            back.AddBinding(LEFT_BACK);
            back.AddBinding(RIGHT_BACK);
            back.AddBinding(KEYBOARD_BACK);
            back.AddBinding(KEYBOARD_BACK_ALT);

            return asset;
        }
    }
}
