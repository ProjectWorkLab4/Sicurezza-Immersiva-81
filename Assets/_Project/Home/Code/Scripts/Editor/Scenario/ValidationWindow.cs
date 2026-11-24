using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SI81.UI.EditorTools
{
    /// <summary>
    /// Finestra dei risultati del validatore degli scenari: elenco raggruppato per gravità (errori, avvisi, informazioni).
    /// Un clic su una riga seleziona (e evidenzia) l'oggetto o l'asset collegato al problema.
    /// </summary>
    internal sealed class ValidationWindow : EditorWindow
    {
        private List<ValidationIssue> issues = new List<ValidationIssue>();
        private string reportTitle = "";
        private Vector2 scroll;

        /// <summary>
        /// Apre (o riusa) la finestra e mostra i risultati.
        /// </summary>
        /// <param name="title">Titolo del rapporto (scena o catalogo).</param>
        /// <param name="results">Problemi da mostrare.</param>
        public static void ShowResults(string title, List<ValidationIssue> results)
        {
            var window = GetWindow<ValidationWindow>(false, "Validatore scenari", true);
            window.reportTitle = title;
            window.issues = new List<ValidationIssue>(results);
            window.minSize = new Vector2(480, 260);
            window.Show();
            window.Repaint();
        }

        /// <summary>
        /// Disegna la finestra: riepilogo e tre gruppi di problemi.
        /// </summary>
        private void OnGUI()
        {
            int e = Count(ValidationSeverity.Error), w = Count(ValidationSeverity.Warning);
            EditorGUILayout.LabelField(reportTitle, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox($"{e} errori, {w} avvisi.", e > 0 ? MessageType.Error : w > 0 ? MessageType.Warning : MessageType.Info);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawGroup("Errori", ValidationSeverity.Error, MessageType.Error);
            DrawGroup("Avvisi", ValidationSeverity.Warning, MessageType.Warning);
            DrawGroup("Informazioni", ValidationSeverity.Info, MessageType.Info);
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// Disegna un gruppo di problemi della stessa gravità; il clic seleziona l'oggetto collegato.
        /// </summary>
        private void DrawGroup(string header, ValidationSeverity severity, MessageType type)
        {
            if (Count(severity) == 0) return;
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField(header, EditorStyles.boldLabel);
            foreach (ValidationIssue issue in issues)
            {
                if (issue.Severity != severity) continue;
                EditorGUILayout.HelpBox(issue.Message, type);
                Rect rect = GUILayoutUtility.GetLastRect();
                if (issue.Context != null && Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
                {
                    Selection.activeObject = issue.Context;
                    EditorGUIUtility.PingObject(issue.Context);
                    Event.current.Use();
                }
            }
        }

        /// <summary>Numero di problemi di una gravità.</summary>
        private int Count(ValidationSeverity severity)
        {
            int n = 0;
            foreach (ValidationIssue i in issues) if (i.Severity == severity) n++;
            return n;
        }
    }
}
