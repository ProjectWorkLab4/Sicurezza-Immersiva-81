using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Stato2
{
    public static void Esegui()
    {
        var sb = new StringBuilder();

        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name != "Aula" && root.name != "P_W_4_aula" && root.name != "Quadro Elettrico" && root.name != "Timer")
            {
                continue;
            }

            var t = root.transform;
            sb.AppendLine($"[{root.name}] active={root.activeSelf} pos={t.position} rot={t.eulerAngles} scale={t.localScale} parent={(t.parent == null ? "-" : t.parent.name)}");

            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                sb.AppendLine($"    R {r.name} bounds={r.bounds.size}@{r.bounds.center}");
            }

            foreach (var c in root.GetComponents<Component>())
            {
                sb.AppendLine($"    C {(c == null ? "NULL" : c.GetType().Name)}");
            }
        }

        Debug.Log(sb.ToString());
    }
}
