using System.Linq;
using NUnit.Framework;
using SI81.Ludovica.TempCore;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SI81.Ludovica.ElectricShock.Tests
{
    /// <summary>Test Edit Mode: dati, punteggio, debrief e regole della scena (guida UI §6, specifica §9-§11).</summary>
    public class ElectricShockDataTests
    {
        private const string ROOT = "Assets/_Project/Groups/G_Ludovica/Scenari/ElectricShock";

        /// <summary>Carica la tabella delle frasi dal JSON del progetto.</summary>
        private static DebriefPhraseTable LoadPhrases()
        {
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>(ROOT + "/Data/ElectricShock_DebriefPhrases.json");
            Assert.IsNotNull(json, "JSON delle frasi mancante");
            return DebriefPhraseTable.Load(json);
        }

        /// <summary>Le dimensioni partono da 60 e applicano +20 / -20 / -25 / -60, limitate a 0-100.</summary>
        [Test]
        public void ComputeDimensions_AppliesProvisionalWeights()
        {
            var log = new ChoiceLog();
            log.Record(1, "A", ChoiceOutcome.Correct, ScoreDimension.Safety);
            log.Record(2, "B", ChoiceOutcome.Correct, ScoreDimension.Safety);
            log.Record(3, "C", ChoiceOutcome.Risky, ScoreDimension.Awareness);
            log.Record(4, "D", ChoiceOutcome.Missed, ScoreDimension.Decision);
            log.Record(5, "E", ChoiceOutcome.CriticalError, ScoreDimension.Execution);
            CollectionAssert.AreEqual(new[] { 100, 40, 35, 0, 60 }, log.ComputeDimensions());
        }

        /// <summary>RecordOnce non registra due volte lo stesso ID.</summary>
        [Test]
        public void RecordOnce_IgnoresDuplicates()
        {
            var log = new ChoiceLog();
            Assert.IsNotNull(log.RecordOnce(1, "LG-01", ChoiceOutcome.Correct, ScoreDimension.Awareness));
            Assert.IsNull(log.RecordOnce(2, "LG-01", ChoiceOutcome.Correct, ScoreDimension.Awareness));
            Assert.AreEqual(1, log.Entries.Count);
        }

        /// <summary>Ogni ID registrato dallo scenario (LG-01...LG-26) ha una frase; il percorso corretto ha 8 passi.</summary>
        [Test]
        public void PhraseTable_CoversEveryActionId()
        {
            var table = LoadPhrases();
            for (int i = 1; i <= 26; i++)
            {
                string id = $"LG-{i:00}";
                var p = table.Find(id);
                Assert.IsNotNull(p, $"Frase mancante per {id}");
                StringAssert.DoesNotContain("SPI", p.phrase, "Mai scrivere SPI a schermo (guida UI §6)");
            }
            Assert.AreEqual(8, table.correctPath.Count);
        }

        /// <summary>Tempo, contesto e prefissi delle righe; nessun segnaposto rimasto.</summary>
        [Test]
        public void BuildLines_FormatsTimeContextAndPrefixes()
        {
            var table = LoadPhrases();
            var log = new ChoiceLog();
            var e = log.Record(75f, "LG-13", ChoiceOutcome.Correct, ScoreDimension.Execution);
            e.Context["tipo"] = "la polvere";
            e.Context["s"] = "40";
            log.Record(80f, "LG-11", ChoiceOutcome.CriticalError, ScoreDimension.Execution);
            log.Record(90f, "LG-19", ChoiceOutcome.Missed, ScoreDimension.Execution);

            var lines = table.BuildLines(log, includeWhatYouCouldDo: true);
            Assert.AreEqual(3, lines.Count);
            StringAssert.StartsWith(DebriefLinePrefix.GOOD + "Al minuto 1:15 hai spento il fuoco con la polvere", lines[0]);
            StringAssert.Contains("40 s di margine", lines[0]);
            StringAssert.StartsWith(DebriefLinePrefix.CRITICAL, lines[1]);
            StringAssert.Contains("Cosa potevi fare:", lines[1]);
            StringAssert.StartsWith(DebriefLinePrefix.BAD, lines[2]);
            foreach (var l in lines) StringAssert.DoesNotContain("{", l);
        }

        /// <summary>Oltre 10 righe: restano tutti gli errori critici e i mancati, ordinati per tempo.</summary>
        [Test]
        public void BuildLines_OverTen_KeepsCriticalAndMissed()
        {
            var table = LoadPhrases();
            var log = new ChoiceLog();
            for (int i = 0; i < 12; i++) log.Record(i, "LG-01", ChoiceOutcome.Correct, ScoreDimension.Awareness);
            log.Record(50, "LG-08", ChoiceOutcome.CriticalError, ScoreDimension.Safety);
            log.Record(60, "LG-19", ChoiceOutcome.Missed, ScoreDimension.Execution);

            var lines = table.BuildLines(log, false);
            Assert.AreEqual(DebriefPhraseTable.MAX_LINES, lines.Count);
            Assert.IsTrue(lines.Any(l => l.StartsWith(DebriefLinePrefix.CRITICAL)));
            Assert.IsTrue(lines.Last().Contains("senza assistenza"));
        }

        /// <summary>Valori dei livelli come nella specifica §9.</summary>
        [Test]
        public void Settings_MatchSpecification()
        {
            var s = AssetDatabase.LoadAssetAtPath<ElectricShockSettings>(ROOT + "/Data/SO_ElectricShockSettings.asset");
            Assert.IsNotNull(s);
            Assert.AreEqual(new Vector2(90, 120), s.For(DifficultyLevel.Guided).ExplosionDelay);
            Assert.AreEqual(new Vector2(60, 90), s.For(DifficultyLevel.Operational).ExplosionDelay);
            Assert.AreEqual(new Vector2(45, 70), s.For(DifficultyLevel.Crisis).ExplosionDelay);
            Assert.AreEqual(0, s.For(DifficultyLevel.Crisis).MaxHints);
            Assert.AreEqual(3, s.For(DifficultyLevel.Operational).MaxHints);
            Assert.IsTrue(s.For(DifficultyLevel.Guided).HighlightObjects);
            Assert.IsTrue(s.For(DifficultyLevel.Crisis).RandomizeRack);
            Assert.AreEqual(12f, s.ExplosionWarning);
            Assert.AreEqual(7f, s.BlastRadius);
        }

        /// <summary>
        /// Regole della guida UI per la scena dello scenario: niente camera, AudioListener, Canvas o EventSystem,
        /// un solo MNG_Scenario, obiettivi 1-4 da max 30 caratteri, nessuno script mancante.
        /// </summary>
        [Test]
        public void ScenarioScene_FollowsUiGuideRules()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            var scene = EditorSceneManager.OpenScene(ROOT + "/SCN_ElectricShock.unity", OpenSceneMode.Additive);
            try
            {
                var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject).ToList();
                Assert.IsFalse(all.Any(g => g.GetComponent<Camera>() != null), "Camera nella scena dello scenario");
                Assert.IsFalse(all.Any(g => g.GetComponent<AudioListener>() != null), "AudioListener nella scena dello scenario");
                Assert.IsFalse(all.Any(g => g.GetComponent<Canvas>() != null), "Canvas nella scena dello scenario");
                Assert.IsFalse(all.Any(g => g.GetComponent("EventSystem") != null), "EventSystem nella scena dello scenario");
                Assert.AreEqual(0, all.Sum(g => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(g)), "Script mancanti");

                var scenarios = all.Select(g => g.GetComponent<ElectricShockScenario>()).Where(c => c != null).ToList();
                Assert.AreEqual(1, scenarios.Count);
                Assert.AreEqual("MNG_Scenario", scenarios[0].name);
                var objectives = new SerializedObject(scenarios[0]).FindProperty("objectives");
                Assert.That(objectives.arraySize, Is.InRange(1, 4));
                for (int i = 0; i < objectives.arraySize; i++)
                    Assert.LessOrEqual(objectives.GetArrayElementAtIndex(i).stringValue.Length, 30, "Obiettivo oltre 30 caratteri");
                Assert.IsNotNull(all.FirstOrDefault(g => g.name == "MNG_SpawnPoint"));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                if (setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }
    }
}
