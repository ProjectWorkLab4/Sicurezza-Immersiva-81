using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace SI81.UI
{
    /// <summary>
    /// Diagnostica dell'input UI: <see cref="UIWorldRaycaster"/> riceve i raggi dei controller SOLO se nell'app non esiste
    /// nessun <c>TrackedDeviceRaycaster</c> (Input System, altrimenti, gli passa lui i puntatori tracciati) e deve esserci
    /// UN solo EventSystem con <c>InputSystemUIInputModule</c> (CLAUDE.md §3, §4.12).
    /// <see cref="FindConflicts"/> è riusabile (a runtime e dal validatore degli scenari, passo 7);
    /// <see cref="WarnOnce"/> scrive in Console UN avviso in italiano con i nomi degli oggetti colpevoli.
    /// </summary>
    public static class UIInputDiagnostics
    {
        /// <summary>Esito di <see cref="FindConflicts"/>: gli oggetti che rompono l'input UI comune.</summary>
        public class Conflicts
        {
            /// <summary>Componenti TrackedDeviceRaycaster trovati (vietati: tolgono i raggi a UIWorldRaycaster).</summary>
            public readonly List<TrackedDeviceRaycaster> TrackedDeviceRaycasters = new List<TrackedDeviceRaycaster>();
            /// <summary>EventSystem in più: tutti quelli trovati quando ce n'è più di uno (ne deve esistere uno solo, in MNG_UI).</summary>
            public readonly List<EventSystem> ExtraEventSystems = new List<EventSystem>();
            /// <summary>Moduli di input diversi da InputSystemUIInputModule (es. StandaloneInputModule).</summary>
            public readonly List<BaseInputModule> OtherInputModules = new List<BaseInputModule>();

            /// <summary>true se non c'è nessun conflitto.</summary>
            public bool IsClean => TrackedDeviceRaycasters.Count == 0 && ExtraEventSystems.Count == 0 && OtherInputModules.Count == 0;
        }

        // Firma dell'ultimo avviso scritto: lo stesso conflitto non viene ripetuto.
        private static string lastWarnedSignature;

        /// <summary>
        /// Azzera la firma all'avvio di ogni sessione di gioco (utile con il Domain Reload disattivato).
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            lastWarnedSignature = null;
        }

        /// <summary>
        /// Cerca nelle scene aperte i componenti che rompono l'input UI comune: TrackedDeviceRaycaster, più di un
        /// EventSystem, moduli di input diversi da InputSystemUIInputModule.
        /// </summary>
        /// <param name="includeInactive">true per contare anche gli oggetti disattivati (validatore in Editor); false a runtime.</param>
        /// <returns>L'elenco dei conflitti (mai null; <see cref="Conflicts.IsClean"/> se non ce ne sono).</returns>
        public static Conflicts FindConflicts(bool includeInactive = false)
        {
            FindObjectsInactive mode = includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude;
            var result = new Conflicts();

            result.TrackedDeviceRaycasters.AddRange(Object.FindObjectsByType<TrackedDeviceRaycaster>(mode, FindObjectsSortMode.None));

            EventSystem[] systems = Object.FindObjectsByType<EventSystem>(mode, FindObjectsSortMode.None);
            if (systems.Length > 1) result.ExtraEventSystems.AddRange(systems);

            foreach (BaseInputModule module in Object.FindObjectsByType<BaseInputModule>(mode, FindObjectsSortMode.None))
                if (!(module is InputSystemUIInputModule)) result.OtherInputModules.Add(module);

            return result;
        }

        /// <summary>
        /// Cerca i conflitti e, se ce ne sono, scrive UN avviso in italiano con i percorsi degli oggetti (scena/gerarchia).
        /// Lo stesso conflitto non viene riscritto; se sparisce e ricompare viene segnalato di nuovo.
        /// </summary>
        /// <returns>true se non ci sono conflitti.</returns>
        public static bool WarnOnce()
        {
            Conflicts conflicts = FindConflicts();
            if (conflicts.IsClean)
            {
                lastWarnedSignature = null;
                return true;
            }

            string message = Describe(conflicts);
            if (message != lastWarnedSignature)
            {
                lastWarnedSignature = message;
                Debug.LogWarning(message);
            }
            return false;
        }

        /// <summary>
        /// Costruisce il testo italiano dell'avviso (usabile anche dal validatore degli scenari).
        /// </summary>
        /// <param name="conflicts">Conflitti trovati.</param>
        /// <returns>Messaggio con un elenco di oggetti per ogni problema.</returns>
        public static string Describe(Conflicts conflicts)
        {
            var text = new StringBuilder("[MNG_UI] Input UI: configurazione non valida, i controller potrebbero non puntare i menu.");
            if (conflicts.TrackedDeviceRaycasters.Count > 0)
            {
                text.Append("\n- TrackedDeviceRaycaster presente (non ammesso: Input System gli passa i raggi dei controller e UIWorldRaycaster li perde). Rimuovere da: ");
                text.Append(JoinPaths(conflicts.TrackedDeviceRaycasters));
            }
            if (conflicts.ExtraEventSystems.Count > 0)
            {
                text.Append("\n- Più di un EventSystem (ne serve uno solo, quello di MNG_UI in SCN_Core). Trovati: ");
                text.Append(JoinPaths(conflicts.ExtraEventSystems));
            }
            if (conflicts.OtherInputModules.Count > 0)
            {
                text.Append("\n- Modulo di input diverso da InputSystemUIInputModule. Rimuovere da: ");
                text.Append(JoinPaths(conflicts.OtherInputModules));
            }
            return text.ToString();
        }

        /// <summary>
        /// Percorso leggibile di un componente: "Scena/Padre/Figlio".
        /// </summary>
        /// <param name="component">Componente.</param>
        /// <returns>Percorso nella gerarchia, preceduto dal nome della scena.</returns>
        public static string GetPath(Component component)
        {
            if (component == null) return "(distrutto)";
            var path = new StringBuilder(component.gameObject.name);
            for (Transform t = component.transform.parent; t != null; t = t.parent) path.Insert(0, t.name + "/");
            Scene scene = component.gameObject.scene;
            return (scene.IsValid() ? scene.name : "?") + ":" + path;
        }

        /// <summary>
        /// Unisce i percorsi dei componenti in un elenco separato da virgole.
        /// </summary>
        private static string JoinPaths<T>(List<T> components) where T : Component
        {
            var names = new List<string>(components.Count);
            foreach (T component in components) names.Add("\"" + GetPath(component) + "\"");
            return string.Join(", ", names);
        }
    }
}
