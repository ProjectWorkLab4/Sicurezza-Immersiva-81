using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SI81.UI
{
    /// <summary>
    /// Stati visivi comuni dei componenti interattivi (CLAUDE.md §4.1).
    /// </summary>
    public enum UIVisualState
    {
        /// <summary>Riposo.</summary>
        Normal,
        /// <summary>Puntatore sopra il componente.</summary>
        Hover,
        /// <summary>Grilletto premuto sul componente.</summary>
        Pressed,
        /// <summary>Componente selezionato (richiede <see cref="UIStateVisual.Selectable"/>).</summary>
        Selected,
        /// <summary>Componente non interattivo.</summary>
        Disabled
    }

    /// <summary>
    /// UNICO punto delle regole di stato dei componenti (pulsanti, card, righe): nessun componente le riscrive.
    /// - Hover: scala <c>Motion.HoverScale</c> (1,04) + ombra hover (sollevamento);
    /// - Pressed: scala <c>Motion.PressedScale</c> (0,98) + ombra card;
    /// - Selected: sollevamento (ombra selected.lift più profonda) + glow morbido brand.light + spunta in un cerchio bianco.
    ///   NESSUN BORDO su nessuno stato (CLAUDE.md §4.1);
    /// - Disabled: nessun hover, nessuna ombra, nessuna scala (i colori "disabilitato" li applica il componente).
    /// Non gestisce i clic (li gestisce un Button uGUI sullo stesso GameObject) né i colori di riempimento e testo:
    /// i componenti si iscrivono a <see cref="StateChanged"/> e colorano da soli. Tutti i valori (scale, durate, ombre)
    /// vengono da SO_UITheme; le transizioni usano <see cref="UITween"/> a tempo non scalato. Nessun Animator.
    /// Le ombre sono sprite 9-slice pre-sfocati (TEX_*): <see cref="ShadowRestSprite"/> per card/pressed, <see cref="ShadowLiftSprite"/>
    /// per hover/selected; il builder (ComponentsBuilder.AddStateVisual) crea figli e sprite.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("SI81/UI/UI State Visual")]
    public class UIStateVisual : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        /// <summary>Tema comune SO_UITheme.</summary>
        [Header("Riferimenti (assegnati dal builder)")]
        [Tooltip("Tema comune SO_UITheme.")]
        public UITheme Theme;
        /// <summary>RectTransform che viene scalato (di solito la radice visiva). Vuoto = questo GameObject.</summary>
        [Tooltip("RectTransform che viene scalato (di solito la radice visiva). Vuoto = questo GameObject.")]
        public RectTransform ScaleTarget;
        /// <summary>IMG_Shadow: ombra 9-slice (opzionale).</summary>
        [Tooltip("IMG_Shadow: ombra 9-slice (opzionale).")]
        public Graphic Shadow;
        /// <summary>IMG_Glow: glow brand.light dello stato selezionato (opzionale).</summary>
        [Tooltip("IMG_Glow: glow brand.light dello stato selezionato (opzionale).")]
        public Graphic Glow;
        /// <summary>Cerchio bianco con la spunta, attivo solo da selezionato (opzionale).</summary>
        [Tooltip("Cerchio bianco con la spunta, attivo solo da selezionato (opzionale).")]
        public GameObject CheckMark;

        /// <summary>Hover = scala del tema (1,04) + ombra hover.</summary>
        [Header("Comportamento")]
        [Tooltip("Hover = scala del tema (1,04) + ombra hover.")]
        public bool ScaleOnHover = true;
        /// <summary>Il componente supporta lo stato Selected (sollevamento + glow + spunta).</summary>
        [Tooltip("Il componente supporta lo stato Selected (sollevamento + glow + spunta).")]
        public bool Selectable;
        /// <summary>true = il componente ha l'ombra card anche a riposo; false = l'ombra compare solo in hover/selezione.</summary>
        [Tooltip("true = il componente ha l'ombra card anche a riposo; false = l'ombra compare solo in hover/selezione.")]
        public bool ShadowAtRest = true;
        /// <summary>Scala di hover diversa da quella del tema (0 = usa il tema). Es. PANEL_TrainingCard 1,02.</summary>
        [Tooltip("Scala di hover diversa da quella del tema (0 = usa il tema). Es. PANEL_TrainingCard 1,02.")]
        public float HoverScaleOverride;
        /// <summary>Scala di pressione diversa da quella del tema (0 = usa il tema). Es. PANEL_TrainingCard 0,99.</summary>
        [Tooltip("Scala di pressione diversa da quella del tema (0 = usa il tema). Es. PANEL_TrainingCard 0,99.")]
        public float PressedScaleOverride;

        /// <summary>Sprite dell'ombra a riposo / pressed (geometria del ruolo card).</summary>
        [Header("Ombre (assegnate dal builder)")]
        [Tooltip("Sprite dell'ombra a riposo / pressed (geometria del ruolo card).")]
        public Sprite ShadowRestSprite;
        /// <summary>Sprite dell'ombra di hover / selezione (geometria del ruolo selected.lift).</summary>
        [Tooltip("Sprite dell'ombra di hover / selezione (geometria del ruolo selected.lift).")]
        public Sprite ShadowLiftSprite;
        /// <summary>Raggio degli angoli della forma, in unità Canvas: serve a calcolare di quanto allargare ombra e glow.</summary>
        [Tooltip("Raggio degli angoli della forma, in unità Canvas: serve a calcolare di quanto allargare ombra e glow.")]
        public float CornerRadius = 10f;

        [SerializeField] private bool selected;
        [SerializeField] private bool interactable = true;

        // Stato del puntatore.
        private bool hovered;
        private bool pressed;
        // Ultimo stato notificato e modo dell'ultimo Refresh.
        private UIVisualState lastState = UIVisualState.Normal;
        private bool lastInstant = true;
        // Animazioni in corso (null se nessuna).
        private Coroutine scaleRoutine;
        private Coroutine shadowRoutine;
        private Coroutine glowRoutine;

        /// <summary>Evento: lo stato visivo è stato (ri)calcolato. I componenti ricolorano testi e icone qui. Deve essere idempotente.</summary>
        public event Action<UIVisualState> StateChanged;

        /// <summary>true se il puntatore è sopra il componente.</summary>
        public bool IsHovered => hovered;
        /// <summary>true se il grilletto è premuto sul componente.</summary>
        public bool IsPressed => pressed;
        /// <summary>true se l'ultimo <see cref="Refresh"/> è stato istantaneo (chi ricolora può evitare la dissolvenza).</summary>
        public bool LastRefreshInstant => lastInstant;

        /// <summary>
        /// Selezionato: sollevamento (ombra più profonda) + glow + spunta, nessun bordo. Ignorato se <see cref="Selectable"/> è false.
        /// </summary>
        public bool Selected
        {
            get => selected;
            set
            {
                if (selected == value) return;
                selected = value;
                Refresh();
            }
        }

        /// <summary>
        /// false = aspetto Disabled (token disabled del tema, niente hover, niente ombra). Il componente applica i colori.
        /// </summary>
        public bool Interactable
        {
            get => interactable;
            set
            {
                if (interactable == value) return;
                interactable = value;
                if (!interactable)
                {
                    hovered = false;
                    pressed = false;
                }
                Refresh();
            }
        }

        /// <summary>Stato visivo attuale (Disabled &gt; Pressed &gt; Selected &gt; Hover &gt; Normal).</summary>
        public UIVisualState State
        {
            get
            {
                if (!interactable) return UIVisualState.Disabled;
                if (pressed) return UIVisualState.Pressed;
                if (Selectable && selected) return UIVisualState.Selected;
                if (hovered) return UIVisualState.Hover;
                return UIVisualState.Normal;
            }
        }

        /// <summary>RectTransform scalato: <see cref="ScaleTarget"/> oppure questo oggetto.</summary>
        private RectTransform Target => ScaleTarget != null ? ScaleTarget : (RectTransform)transform;

        /// <summary>
        /// All'attivazione applica lo stato corrente senza animazioni.
        /// </summary>
        private void OnEnable()
        {
            Refresh(true);
        }

        /// <summary>
        /// Alla disattivazione azzera hover e pressione (nessuno stato rimasto appeso) e riporta tutto a riposo.
        /// </summary>
        private void OnDisable()
        {
            hovered = false;
            pressed = false;
            scaleRoutine = null;
            shadowRoutine = null;
            glowRoutine = null;
            Refresh(true);
        }

        /// <summary>
        /// Forza lo stato visivo (hover, pressed, selected, disabled o riposo) senza un puntatore: serve alla galleria della
        /// sandbox e alle prove per mostrare ogni stato fianco a fianco. Applica subito, senza transizione. Un puntatore reale
        /// che entra o esce dal componente riprende poi il controllo dello stato.
        /// </summary>
        /// <param name="forced">Stato da mostrare.</param>
        public void ForceState(UIVisualState forced)
        {
            interactable = forced != UIVisualState.Disabled;
            hovered = forced == UIVisualState.Hover;
            pressed = forced == UIVisualState.Pressed;
            if (forced == UIVisualState.Selected && Selectable) selected = true;
            else if (forced != UIVisualState.Disabled) selected = false;
            Refresh(true);
        }

        /// <summary>
        /// Ricalcola scala, ombra, glow e spunta dallo stato corrente e avvisa i componenti con <see cref="StateChanged"/>.
        /// </summary>
        /// <param name="instant">true = applica subito, senza transizione.</param>
        public void Refresh(bool instant = false)
        {
            lastInstant = instant;
            UIVisualState state = State;
            bool isSelected = interactable && Selectable && selected;
            if (Theme != null) ApplyVisuals(state, isSelected, instant);
            if (CheckMark != null && CheckMark.activeSelf != isSelected) CheckMark.SetActive(isSelected);
            lastState = state;
            StateChanged?.Invoke(state);
        }

        /// <summary>
        /// Calcola e applica scala, ombra e glow dello stato.
        /// </summary>
        private void ApplyVisuals(UIVisualState state, bool isSelected, bool instant)
        {
            UITheme.MotionSettings motion = Theme.Motion;
            float hoverScale = HoverScaleOverride > 0f ? HoverScaleOverride : motion.HoverScale;
            float pressedScale = PressedScaleOverride > 0f ? PressedScaleOverride : motion.PressedScale;

            // Scala: pressed 0,98; hover 1,04 (anche su un elemento selezionato); altrimenti 1.
            float scale = 1f;
            if (state == UIVisualState.Pressed) scale = pressedScale;
            else if (state != UIVisualState.Disabled && hovered && ScaleOnHover) scale = hoverScale;
            float scaleDuration = state == UIVisualState.Pressed || lastState == UIVisualState.Pressed ? motion.PressDuration : motion.HoverDuration;
            ApplyScale(scale, scaleDuration, instant);

            // Ombra: pressed = card; hover/selezionato = sollevamento; riposo = card se ShadowAtRest; disabilitato = nessuna.
            string role = null;
            bool lift = false;
            if (state == UIVisualState.Pressed) role = ShadowRoles.PRESSED;
            else if (state != UIVisualState.Disabled)
            {
                if (isSelected) { role = ShadowRoles.SELECTED_LIFT; lift = true; }
                else if (hovered) { role = ShadowRoles.HOVER; lift = true; }
                else if (ShadowAtRest) role = ShadowRoles.CARD;
            }
            ApplyShadow(role, lift, motion.HoverDuration, instant);

            // Glow brand.light solo da selezionato.
            ApplyGlow(isSelected, motion.SelectDuration, instant);
        }

        /// <summary>
        /// Porta la scala del bersaglio al valore dato (UITween, tempo non scalato).
        /// </summary>
        private void ApplyScale(float scale, float duration, bool instant)
        {
            RectTransform target = Target;
            if (instant || duration <= 0f || !isActiveAndEnabled || !Application.isPlaying)
            {
                if (scaleRoutine != null) StopCoroutine(scaleRoutine);
                scaleRoutine = null;
                if (!Mathf.Approximately(target.localScale.x, scale)) target.localScale = Vector3.one * scale;
                return;
            }
            scaleRoutine = UITween.Play(this, scaleRoutine, UITween.Scale(target, scale, duration, () => scaleRoutine = null));
        }

        /// <summary>
        /// Imposta sprite, misure e colore dell'ombra per il ruolo dato (null = nascosta).
        /// </summary>
        private void ApplyShadow(string role, bool lift, float duration, bool instant)
        {
            if (Shadow == null) return;
            UITheme.ShadowToken token = role != null ? Theme.GetShadow(role) : null;
            Color target = token != null ? token.Color : Shadow.color;
            if (token == null) target.a = 0f;
            if (token != null)
            {
                Sprite wanted = lift ? ShadowLiftSprite : ShadowRestSprite;
                if (Shadow is Image image && wanted != null && image.sprite != wanted) image.sprite = wanted;
                // Allargamento del RectTransform: lo spostamento verticale è già nello sprite.
                float expand = PanelSprites.GetShadowLayout(CornerRadius, token).PaddingUnits;
                PanelLayout.SetRect(Shadow.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-expand, -expand), new Vector2(expand, expand));
            }
            UIComponentUtil.SetColor(this, ref shadowRoutine, Shadow, target, duration, instant);
        }

        /// <summary>
        /// Mostra o nasconde il glow brand.light della selezione.
        /// </summary>
        private void ApplyGlow(bool show, float duration, bool instant)
        {
            if (Glow == null) return;
            UITheme.ShadowToken token = Theme.GetShadow(ShadowRoles.SELECTED_GLOW);
            if (token == null) return;
            Color target = token.Color;
            if (!show) target.a = 0f;
            float expand = PanelSprites.GetShadowLayout(CornerRadius, token).PaddingUnits;
            PanelLayout.SetRect(Glow.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-expand, -expand), new Vector2(expand, expand));
            UIComponentUtil.SetColor(this, ref glowRoutine, Glow, target, duration, instant);
        }

        /// <summary>Puntatore sopra il componente: hover.</summary>
        /// <param name="eventData">Dati del puntatore.</param>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!interactable) return;
            hovered = true;
            Refresh();
        }

        /// <summary>Puntatore fuori dal componente: fine hover e annulla la pressione.</summary>
        /// <param name="eventData">Dati del puntatore.</param>
        public void OnPointerExit(PointerEventData eventData)
        {
            if (!hovered && !pressed) return;
            hovered = false;
            pressed = false;
            Refresh();
        }

        /// <summary>Grilletto/clic premuto: stato pressed.</summary>
        /// <param name="eventData">Dati del puntatore.</param>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (!interactable || eventData.button != PointerEventData.InputButton.Left) return;
            pressed = true;
            Refresh();
        }

        /// <summary>Grilletto/clic rilasciato: fine pressed.</summary>
        /// <param name="eventData">Dati del puntatore.</param>
        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || !pressed) return;
            pressed = false;
            Refresh();
        }
    }
}
