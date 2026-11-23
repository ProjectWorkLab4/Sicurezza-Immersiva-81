namespace SI81.Input
{
    /// <summary>
    /// Canale di azioni dell'Input Service. <see cref="World"/> è il mondo 3D (movimento, selezione, presa),
    /// <see cref="UI"/> è il menu/HUD. Il canale World può essere sospeso da uno o più owner; il canale UI resta attivo.
    /// </summary>
    public enum InputChannel
    {
        /// <summary>Azioni del mondo: Select, Grab, Move, Turn.</summary>
        World = 0,
        /// <summary>Azioni della UI: Point, Click, Scroll, Navigate, Back.</summary>
        UI = 1
    }
}
