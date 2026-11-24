using System;
using System.Collections.Generic;
using Oculus.Interaction;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Glue between a Meta XR Interaction SDK interactable (PokeInteractable,
/// RayInteractable, GrabInteractable, plain Interactable, ...) and normal
/// UnityEvents / C# events.
///
/// Put this component on the same GameObject as the interactable (or on a
/// parent) and it will subscribe to every interactable found on itself and on
/// its children. No event wiring in the Inspector is required: when the object
/// is poked, clicked with the ray or grabbed, <see cref="onSelected"/> fires.
/// </summary>
[DisallowMultipleComponent]
public class VRInteractableButton : MonoBehaviour
{
    [Header("Eventi")]
    [Tooltip("Invocato quando l'oggetto viene selezionato (poke, raggio, grab).")]
    [SerializeField] UnityEvent onSelected = new UnityEvent();
    [Tooltip("Invocato quando il puntatore entra sull'oggetto.")]
    [SerializeField] UnityEvent onHoverEnter = new UnityEvent();
    [Tooltip("Invocato quando il puntatore esce dall'oggetto.")]
    [SerializeField] UnityEvent onHoverExit = new UnityEvent();
    [Tooltip("Invocato quando la selezione finisce (poke rilasciato, grab lasciato).")]
    [SerializeField] UnityEvent onDeselected = new UnityEvent();

    [Header("Comportamento")]
    [Tooltip("Se disattivato l'oggetto resta visibile ma non risponde piu' all'interazione.")]
    [SerializeField] bool available = true;
    [Tooltip("Tempo minimo tra due pressioni, per evitare doppie attivazioni poke+raggio.")]
    [SerializeField] float selectCooldown = 0.3f;
    [Tooltip("Se la lista e' vuota vengono raccolti automaticamente tutti gli Interactable del GameObject e dei figli.")]
    [SerializeField] List<MonoBehaviour> interactables = new List<MonoBehaviour>();

    [Header("Feedback (opzionale)")]
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip pressClip;
    [SerializeField] bool tintOnHover = false;
    [SerializeField] Color hoverColor = new Color(1f, 0.82f, 0.35f);
    [SerializeField] List<Renderer> tintedRenderers = new List<Renderer>();

    /// <summary>Fired every time the object is activated.</summary>
    public event Action<VRInteractableButton> Selected;

    /// <summary>Fired every time the object stops being selected.</summary>
    public event Action<VRInteractableButton> Deselected;

    /// <summary>Fired when hover starts (true) or ends (false).</summary>
    public event Action<VRInteractableButton, bool> HoverChanged;

    public bool Available
    {
        get => available;
        set => SetAvailable(value);
    }

    public bool IsHovered { get; private set; }

    readonly List<IInteractableView> _views = new List<IInteractableView>();
    readonly Dictionary<Renderer, Color> _originalColors = new Dictionary<Renderer, Color>();
    MaterialPropertyBlock _block;
    bool _subscribed;
    float _lastSelectedTime = -999f;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");

    void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (tintOnHover)
        {
            CaptureOriginalColors();
        }
    }

    void OnEnable()
    {
        Subscribe();
    }

    void OnDisable()
    {
        Unsubscribe();
        SetHovered(false);
    }

    /// <summary>Activates the button as if it was selected.</summary>
    public void Press()
    {
        Activate();
    }

    /// <summary>Enables or disables the interaction without hiding the object.</summary>
    public void SetAvailable(bool value)
    {
        available = value;

        if (!value)
        {
            SetHovered(false);
        }
    }

    /// <summary>
    /// Activates the button. Returns false when the cooldown is still running
    /// or the button is not available.
    /// </summary>
    public bool Activate()
    {
        if (!available)
        {
            return false;
        }

        if (Time.unscaledTime - _lastSelectedTime < selectCooldown)
        {
            return false;
        }

        _lastSelectedTime = Time.unscaledTime;

        if (audioSource != null && pressClip != null)
        {
            audioSource.PlayOneShot(pressClip);
        }

        onSelected.Invoke();
        Selected?.Invoke(this);
        return true;
    }

    void Subscribe()
    {
        if (_subscribed)
        {
            return;
        }

        CollectViews();

        foreach (IInteractableView view in _views)
        {
            view.WhenStateChanged += HandleStateChanged;
        }

        _subscribed = true;
    }

    void Unsubscribe()
    {
        if (!_subscribed)
        {
            return;
        }

        foreach (IInteractableView view in _views)
        {
            view.WhenStateChanged -= HandleStateChanged;
        }

        _subscribed = false;
    }

    void CollectViews()
    {
        _views.Clear();

        if (interactables != null && interactables.Count > 0)
        {
            foreach (MonoBehaviour behaviour in interactables)
            {
                AddView(behaviour);
            }

            return;
        }

        foreach (MonoBehaviour behaviour in GetComponentsInChildren<MonoBehaviour>(true))
        {
            AddView(behaviour);
        }
    }

    void AddView(MonoBehaviour behaviour)
    {
        if (behaviour == null || behaviour == this)
        {
            return;
        }

        if (!(behaviour is IInteractableView view))
        {
            return;
        }

        // InteractableGroupView aggregates its children: subscribing to it too
        // would report every press twice.
        if (behaviour.GetType().Name.Contains("GroupView"))
        {
            return;
        }

        if (_views.Contains(view))
        {
            return;
        }

        _views.Add(view);
    }

    void HandleStateChanged(InteractableStateChangeArgs args)
    {
        switch (args.NewState)
        {
            case InteractableState.Select:
                SetHovered(true);
                Activate();
                break;

            case InteractableState.Hover:
                if (args.PreviousState == InteractableState.Select)
                {
                    onDeselected.Invoke();
                    Deselected?.Invoke(this);
                }

                SetHovered(true);
                break;

            case InteractableState.Normal:
            case InteractableState.Disabled:
                if (args.PreviousState == InteractableState.Select)
                {
                    onDeselected.Invoke();
                    Deselected?.Invoke(this);
                }

                SetHovered(false);
                break;
        }
    }

    void SetHovered(bool hovered)
    {
        if (IsHovered == hovered)
        {
            return;
        }

        IsHovered = hovered;

        if (hovered)
        {
            onHoverEnter.Invoke();
        }
        else
        {
            onHoverExit.Invoke();
        }

        HoverChanged?.Invoke(this, hovered);
        ApplyTint(hovered);
    }

    void CaptureOriginalColors()
    {
        _originalColors.Clear();

        foreach (Renderer renderer in tintedRenderers)
        {
            if (renderer == null || renderer.sharedMaterial == null)
            {
                continue;
            }

            Material material = renderer.sharedMaterial;
            Color color = material.HasProperty(BaseColorId)
                ? material.GetColor(BaseColorId)
                : (material.HasProperty(ColorId) ? material.GetColor(ColorId) : Color.white);

            _originalColors[renderer] = color;
        }
    }

    void ApplyTint(bool hovered)
    {
        if (!tintOnHover || tintedRenderers == null || tintedRenderers.Count == 0)
        {
            return;
        }

        if (_block == null)
        {
            _block = new MaterialPropertyBlock();
        }

        foreach (Renderer renderer in tintedRenderers)
        {
            if (renderer == null)
            {
                continue;
            }

            Color color = hovered
                ? hoverColor
                : (_originalColors.TryGetValue(renderer, out Color original) ? original : Color.white);

            renderer.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, color);
            _block.SetColor(ColorId, color);
            renderer.SetPropertyBlock(_block);
        }
    }
}
