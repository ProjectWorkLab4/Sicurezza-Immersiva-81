using System;
using UnityEngine;

/// <summary>
/// Numeric keypad for the fixed phone (GGD: "Telefono fisso - Digitazione numeri").
///
/// Assign twelve <see cref="VRInteractableButton"/> in this order:
/// 0..9 = digits, 10 = cancel, 11 = call.
/// Every press is forwarded to <see cref="TelefonoInterattivo"/>.
/// </summary>
[DisallowMultipleComponent]
public class TastierinoTelefono : MonoBehaviour
{
    [Header("Riferimenti")]
    [SerializeField] TelefonoInterattivo telefono;
    [Tooltip("0-9 = cifre, 10 = cancella, 11 = chiama.")]
    [SerializeField] VRInteractableButton[] tasti = new VRInteractableButton[12];

    [Header("Comportamento")]
    [Tooltip("Se attivo i tasti rispondono solo con la cornetta sollevata.")]
    [SerializeField] bool richiedeCornetta = true;

    [Tooltip("Cifra prodotta dai primi dieci tasti, nell’ordine in cui sono assegnati. " +
             "L’indice 10 cancella, l’indice 11 chiama.")]
    [SerializeField] string sequenzaCifre = "1234567890";

    void Awake()
    {
        if (telefono == null)
        {
            telefono = GetComponentInParent<TelefonoInterattivo>();
        }
    }

    void OnEnable()
    {
        if (tasti == null)
        {
            return;
        }

        foreach (VRInteractableButton tasto in tasti)
        {
            if (tasto != null)
            {
                tasto.Selected += HandleTasto;
            }
        }
    }

    void OnDisable()
    {
        if (tasti == null)
        {
            return;
        }

        foreach (VRInteractableButton tasto in tasti)
        {
            if (tasto != null)
            {
                tasto.Selected -= HandleTasto;
            }
        }
    }

    void HandleTasto(VRInteractableButton tasto)
    {
        if (telefono == null)
        {
            return;
        }

        if (richiedeCornetta && !telefono.CornettaSollevata)
        {
            return;
        }

        int indice = Array.IndexOf(tasti, tasto);

        if (indice < 0)
        {
            return;
        }

        if (indice <= 9)
        {
            if (string.IsNullOrEmpty(sequenzaCifre) || indice >= sequenzaCifre.Length)
            {
                return;
            }

            telefono.AggiungiCifra(sequenzaCifre[indice]);
        }
        else if (indice == 10)
        {
            telefono.CancellaCifra();
        }
        else
        {
            telefono.ComponiNumero(telefono.NumeroComposto);
        }
    }
}
