using System;
using SI81.Rig;
using UnityEngine;

/// <summary>
/// Trigger placed beyond the exit door.
///
/// When the player walks through it the scenario ends: the colleague counts as
/// "brought outside" only if he is following the player and has not fainted.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class AreaUscita : MonoBehaviour
{
    [Header("Riferimenti")]
    [SerializeField] GestoreScenarioGasLeak gestore;
    [SerializeField] CollegaNPC collega;

    [Header("Rilevamento giocatore")]
    [Tooltip("Tag usato dal rig del giocatore. Se vuoto viene usato il RigRegistration condiviso.")]
    [SerializeField] string tagGiocatore = "Player";

    [Header("Debug")]
    [SerializeField] bool mostraDebug = false;

    /// <summary>Fired with true when the colleague left the room with the player.</summary>
    public event Action<bool> PlayerUscito;

    bool _attivata;

    void Awake()
    {
        if (gestore == null)
        {
            gestore = FindFirstObjectByType<GestoreScenarioGasLeak>();
        }

        if (collega == null)
        {
            collega = FindFirstObjectByType<CollegaNPC>();
        }

        Collider collider = GetComponent<Collider>();
        collider.isTrigger = true;

        Rigidbody rigidbody = GetComponent<Rigidbody>();

        if (rigidbody == null)
        {
            rigidbody = gameObject.AddComponent<Rigidbody>();
        }

        rigidbody.isKinematic = true;
        rigidbody.useGravity = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_attivata || !EIlGiocatore(other))
        {
            return;
        }

        _attivata = true;

        bool conCollega = collega != null && collega.SegueGiocatore && !collega.Svenuto;

        if (mostraDebug)
        {
            Debug.Log($"[AreaUscita] Uscita rilevata. Con collega: {conCollega}", this);
        }

        PlayerUscito?.Invoke(conCollega);
        gestore?.SegnalaUscita(conCollega);
    }

    bool EIlGiocatore(Collider other)
    {
        if (!string.IsNullOrEmpty(tagGiocatore) && other.CompareTag(tagGiocatore))
        {
            return true;
        }

        RigRegistration rig = RigRegistration.Instance;

        return rig != null && (other.GetComponentInParent<RigRegistration>() == rig ||
                               other.transform.root == rig.transform.root);
    }

    void OnDrawGizmos()
    {
        Collider collider = GetComponent<Collider>();

        if (collider == null)
        {
            return;
        }

        Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.35f);

        if (collider is BoxCollider box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
        }
    }
}
