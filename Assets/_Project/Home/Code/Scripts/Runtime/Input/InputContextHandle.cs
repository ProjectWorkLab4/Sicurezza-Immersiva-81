using System;

namespace SI81.Input
{
    /// <summary>
    /// Handle restituito da <see cref="InputService.PushContext"/>: rilasciarlo (Dispose) toglie il contesto
    /// dell'owner indicato. Dispose è idempotente e non tocca i contesti di altri owner.
    /// </summary>
    public readonly struct InputContextHandle : IDisposable
    {
        private readonly InputService service;
        private readonly InputChannel channel;
        private readonly object owner;

        /// <summary>
        /// Crea un handle per un contesto già inserito dal servizio.
        /// </summary>
        /// <param name="service">Servizio che possiede il contesto.</param>
        /// <param name="channel">Canale del contesto.</param>
        /// <param name="owner">Owner del contesto.</param>
        internal InputContextHandle(InputService service, InputChannel channel, object owner)
        {
            this.service = service;
            this.channel = channel;
            this.owner = owner;
        }

        /// <summary>
        /// Rimuove il contesto. Chiamate successive non fanno nulla.
        /// </summary>
        public void Dispose()
        {
            if (service != null) service.PopContext(channel, owner);
        }
    }
}
