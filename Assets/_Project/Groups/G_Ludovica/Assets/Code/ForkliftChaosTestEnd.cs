using System.Collections;
using System.Collections.Generic;
using SI81.UI;
using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

namespace G_Ludovica.ForkliftChaos
{
    /// <summary>
    /// TEST TEMPORANEO — Solo per verificare il collegamento Menu -> Scenario -> Debrief.
    /// Premi il tasto A del controller destro (oppure E in Editor) mentre la missione e' in corso
    /// per chiudere con OutcomeType.Positive e vedere Esito / Debrief / Score &amp; Replay.
    /// Da cancellare insieme all'oggetto MNG_TestEnd quando arriva la logica di gioco vera.
    /// </summary>
    public sealed class ForkliftChaosTestEnd : MonoBehaviour
    {
        [Tooltip("ScenarioController di MNG_Scenario. Vuoto = cerca in scena.")]
        [SerializeField] private ScenarioController controller;

        [Tooltip("Tasto tastiera usato in Editor (sul visore si usa sempre il tasto A).")]
        [SerializeField] private KeyCode editorKey = KeyCode.E;

        private Coroutine hintRoutine;

        private void Awake()
        {
            if (controller == null) controller = FindFirstObjectByType<ScenarioController>();
            hintRoutine = StartCoroutine(ShowHintWhenRunning());
        }

        private IEnumerator ShowHintWhenRunning()
        {
            while (controller == null || !controller.IsRunning)
                yield return new WaitForSeconds(0.25f);

            yield return new WaitForSeconds(0.8f);
            if (controller != null && controller.IsRunning)
                controller.Subtitle("Operatore", "Test: premi A sul controller (E in Editor) per chiudere la missione.", 7f);
        }

        private void Update()
        {
            if (controller == null || !controller.IsRunning) return;

            bool pressed = false;
#if UNITY_EDITOR
            pressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#endif
            if (!pressed)
                pressed = OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch);

            if (!pressed) return;

            if (hintRoutine != null)
            {
                StopCoroutine(hintRoutine);
                hintRoutine = null;
            }

            controller.Subtitle("Operatore", "Segnale dato: carrello fermo, corsia sgombra.", 3f);
            controller.EndMission(
                OutcomeType.Positive,
                100,
                null,
                new List<string> { "+ Test completato: accesso al magazzino e HUD verificati." });
        }
    }
}
