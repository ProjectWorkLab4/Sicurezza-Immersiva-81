using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Stato4
{
    public static void Esegui()
    {
        var tipi = new HashSet<string>();

        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (var c in root.GetComponentsInChildren<Component>(true))
            {
                if (c != null)
                {
                    tipi.Add(c.GetType().Name);
                }
            }
        }

        var sb = new StringBuilder("TIPI: ");
        var elenco = new List<string>(tipi);
        elenco.Sort();

        foreach (var t in elenco)
        {
            sb.Append(t).Append(", ");
        }

        Debug.Log(sb.ToString());
    }
}
