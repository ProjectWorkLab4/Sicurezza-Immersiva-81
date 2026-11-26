using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Contorno arancione/giallo per il livello Guidato: crea un figlio con la stessa mesh e lo
/// materiale inverted-hull, senza toccare i materiali originali degli oggetti.
/// La lista degli oggetti da evidenziare e' un array serializzato su GameManager.
/// </summary>
public static class EvidenziatoreContorno
{
    const string NOME_FIGLIO = "ContornoEvidenziazione";

    /// <summary>Aggiunge o rimuove il contorno su un singolo renderer.</summary>
    public static void Imposta(Renderer sorgente, Material materiale, bool attivo)
    {
        if (sorgente == null) return;

        Transform figlio = sorgente.transform.Find(NOME_FIGLIO);

        if (!attivo)
        {
            if (figlio != null)
            {
                Object.Destroy(figlio.gameObject);
            }
            return;
        }

        if (materiale == null || figlio != null) return;

        MeshFilter filtro = sorgente.GetComponent<MeshFilter>();
        if (filtro == null || filtro.sharedMesh == null) return;

        GameObject host = new GameObject(NOME_FIGLIO);
        host.transform.SetParent(sorgente.transform, false);
        host.transform.localPosition = Vector3.zero;
        host.transform.localRotation = Quaternion.identity;
        host.transform.localScale = Vector3.one;

        MeshFilter nuovoFiltro = host.AddComponent<MeshFilter>();
        nuovoFiltro.sharedMesh = filtro.sharedMesh;

        MeshRenderer nuovoRenderer = host.AddComponent<MeshRenderer>();
        nuovoRenderer.sharedMaterial = materiale;
        nuovoRenderer.shadowCastingMode = ShadowCastingMode.Off;
        nuovoRenderer.receiveShadows = false;
    }

    /// <summary>Rimuove tutti i contorni generati sui renderer indicati.</summary>
    public static void Rimuovi(Renderer[] sorgenti)
    {
        if (sorgenti == null) return;

        foreach (Renderer sorgente in sorgenti)
        {
            Imposta(sorgente, null, false);
        }
    }
}
