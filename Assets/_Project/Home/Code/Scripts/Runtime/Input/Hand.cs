namespace SI81.Input
{
    /// <summary>
    /// Mano che ha prodotto un'azione. <see cref="Any"/> vale solo per le query (polling), mai per gli eventi:
    /// un evento riporta sempre la mano sinistra o destra che ha generato l'azione.
    /// </summary>
    public enum Hand
    {
        /// <summary>Controller sinistro.</summary>
        Left = 0,
        /// <summary>Controller destro.</summary>
        Right = 1,
        /// <summary>Entrambe le mani: solo per le query, non per gli eventi.</summary>
        Any = 2
    }

    /// <summary>
    /// Operazioni sulle mani usate dall'Input Service e dal rig.
    /// </summary>
    public static class HandExtensions
    {
        /// <summary>
        /// true se la mano identifica un singolo controller (sinistro o destro).
        /// </summary>
        /// <param name="hand">Mano da controllare.</param>
        public static bool IsSingle(this Hand hand)
        {
            return hand == Hand.Left || hand == Hand.Right;
        }

        /// <summary>
        /// Indice 0/1 usato dagli array interni; <see cref="Hand.Any"/> non è valido.
        /// </summary>
        /// <param name="hand">Mano singola.</param>
        /// <returns>0 per sinistra, 1 per destra.</returns>
        public static int Index(this Hand hand)
        {
            return (int)hand;
        }
    }
}
