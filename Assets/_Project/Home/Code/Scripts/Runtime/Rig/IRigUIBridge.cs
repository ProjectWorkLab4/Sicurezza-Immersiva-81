using SI81.Input;

namespace SI81.Rig
{
    /// <summary>
    /// Unico punto di contatto tra il Player Rig e la UI comune. Vive in SI81.Rig, così il rig non referenzia
    /// l'assembly UI né il Meta SDK UI: l'adattatore della UI implementa questa interfaccia e chiama
    /// CoreRig/UIScreenManager/HUDRoot/UIPointer/HapticsService.
    /// </summary>
    public interface IRigUIBridge
    {
        /// <summary>
        /// Consegna testa, origine del tracciamento e mani alla UI/HUD.
        /// </summary>
        /// <param name="handoff">Riferimenti del rig.</param>
        void ApplyRig(RigHandoff handoff);

        /// <summary>
        /// true se il raggio di quella mano sta puntando un elemento UI.
        /// </summary>
        /// <param name="hand">Mano singola.</param>
        bool IsPointingAtUI(Hand hand);

        /// <summary>
        /// Impulso aptico su una mano. L'adattatore applica il gate controls.ui_haptics.
        /// </summary>
        /// <param name="hand">Mano singola.</param>
        /// <param name="amplitude">Ampiezza 0..1.</param>
        /// <param name="duration">Durata in secondi.</param>
        void Pulse(Hand hand, float amplitude, float duration);
    }
}
