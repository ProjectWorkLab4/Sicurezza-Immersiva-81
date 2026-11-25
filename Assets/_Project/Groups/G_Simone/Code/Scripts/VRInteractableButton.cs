using System;
using System.Collections.Generic;
using SI81.Interaction;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Adapter between a shared <see cref="Interactable"/> and the UnityEvents / C# events used by
/// the Gas Leak and Blackout scripts.
///
/// The component lives on the same GameObject as the <see cref="Interactable"/> (or on a parent)
/// and forwards hover, select and grab to the existing callbacks. It does not read input, cast
/// rays or implement grabbing: the rig's <c>RayInteractor</c> owns all of that.
/// </summary>
[DisallowMultipleComponent]
public class VRInteractableButton : MonoBehaviour
{
    [Header("Eventi")]
    [Tooltip("Invocato quando l'oggetto viene selezionato (raggio o grab).")]
    [SerializeField] UnityEvent onSelected = new UnityEvent();
    [Tooltip("Invocato quando il puntatore entra sull'oggetto.")]
    [SerializeField] UnityEvent onHoverEnter = new UnityEvent();
    [Tooltip("Invocato quando il puntatore esce dall'oggetto.")]
    [SerializeField] UnityEvent onHoverExit = new UnityEvent();
    [Tooltip("Invocato quando la selezione finisce (grilletto rilasciato o grab lasciato).")]
    [SerializeField] UnityEvent onDeselected = new UnityEvent();

    [Header("Comportamento")]
    [Tooltip("Se disattivato l'oggetto resta visibile ma non risponde piu' all'interazione.")]
    [SerializeField] bool available = true;
    [Tooltip("Tempo minimo tra due pressioni, per evitare doppie attivazioni select+grab.")]
    [SerializeField] float selectCooldown = 0.3f;
    [Tooltip("Se la lista e' vuota vengono raccolti gli Interactable del GameObject e dei figli.")]
    [SerializeField] List<Interactable> interactables = new List<Interactable>();

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

    readonly List<Interactable> _targets = new List<Interactable>();
    readonly Dictionary<Renderer, Color> _originalColors = new Dictionary<Renderer, Color>();
    readonly Dictionary<Interactable, bool> _selectable = new Dictionary<Interactable, bool>();
    readonly Dictionary<Interactable, bool> _grabbable = new Dictionary<Interactable, bool>();
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
        ApplyAvailability();
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
        ApplyAvailability();

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

        CollectTargets();

        foreach (Interactable target in _targets)
        {
            _selectable[target] = target.Selectable;
            _grabbable[target] = target.Grabbable;
            target.HoverEntered += HandleHoverEntered;
            target.HoverExited += HandleHoverExited;
            target.SelectEntered += HandleSelectEntered;
            target.SelectExited += HandleSelectExited;
            target.GrabEntered += HandleGrabEntered;
            target.GrabExited += HandleGrabExited;
        }

        _subscribed = true;
    }

    void Unsubscribe()
    {
        if (!_subscribed)
        {
            return;
        }

        foreach (Interactable target in _targets)
        {
            target.HoverEntered -= HandleHoverEntered;
            target.HoverExited -= HandleHoverExited;
            target.SelectEntered -= HandleSelectEntered;
            target.SelectExited -= HandleSelectExited;
            target.GrabEntered -= HandleGrabEntered;
            target.GrabExited -= HandleGrabExited;
        }

        _selectable.Clear();
        _grabbable.Clear();
        _subscribed = false;
    }

    void CollectTargets()
    {
        _targets.Clear();

        if (interactables != null)
        {
            foreach (Interactable target in interactables)
            {
                AddTarget(target);
            }
        }

        if (_targets.Count == 0)
        {
            foreach (Interactable target in GetComponentsInChildren<Interactable>(true))
            {
                AddTarget(target);
            }
        }
    }

    void AddTarget(Interactable target)
    {
        if (target == null || _targets.Contains(target))
        {
            return;
        }

        _targets.Add(target);
    }

    void ApplyAvailability()
    {
        if (!_subscribed)
        {
            return;
        }

        foreach (Interactable target in _targets)
        {
            target.Selectable = available && (_selectable.TryGetValue(target, out bool select) && select);
            target.Grabbable = available && (_grabbable.TryGetValue(target, out bool grab) && grab);
        }
    }

    void HandleHoverEntered(SI81.Input.Hand hand) => SetHovered(true);

    void HandleHoverExited(SI81.Input.Hand hand) => SetHovered(false);

    void HandleSelectEntered(SI81.Input.Hand hand)
    {
        SetHovered(true);
        Activate();
    }

    void HandleSelectExited(SI81.Input.Hand hand) => NotifyDeselected();

    void HandleGrabEntered(SI81.Input.Hand hand)
    {
        SetHovered(true);
        Activate();
    }

    void HandleGrabExited(SI81.Input.Hand hand) => NotifyDeselected();

    void NotifyDeselected()
    {
        onDeselected.Invoke();
        Deselected?.Invoke(this);
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
