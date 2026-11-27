using System.Collections.Generic;

namespace SI81.MachineLock
{
    /// <summary>
    /// Every player-facing string of the Machine Lock scenario, in Italian, from the GDD §11.8 and §12.
    /// Code, identifiers and XML docs are English; only the values of these constants are Italian.
    /// Keep the HUD limits in mind: subtitles 32 characters × 2 lines, alerts have a title and a detail,
    /// hints are 60 characters or fewer.
    /// </summary>
    public static class MachineLockTexts
    {
        /// <summary>Speaker name for system subtitles.</summary>
        public const string SpeakerSystem = "Sistema";
        /// <summary>Speaker name for the supervisor (Preposto) subtitles.</summary>
        public const string SpeakerSupervisor = "Preposto";

        // ------------------------------------------------------------------ Intro and narrative

        /// <summary>Opening subtitle 1.</summary>
        public const string Intro1 = "Perdita olio a 120 gradi.";
        /// <summary>Opening subtitle 2.</summary>
        public const string Intro2 = "Pressa in movimento.";

        // ------------------------------------------------------------------ Tasks

        /// <summary>E-stop subtitle.</summary>
        public const string EStop = "Pressa fermata.";
        /// <summary>E-stop follow-up subtitle.</summary>
        public const string EStop2 = "Il display resta acceso.";
        /// <summary>Alarm subtitle.</summary>
        public const string Alarm = "Allarme reparto attivato.";
        /// <summary>Alarm follow-up subtitle.</summary>
        public const string Alarm2 = "Soccorsi in arrivo.";
        /// <summary>Main switch subtitle.</summary>
        public const string Panel = "Quadro generale aperto.";
        /// <summary>Main switch follow-up subtitle.</summary>
        public const string Panel2 = "Energia di macchina isolata.";
        /// <summary>Lockout subtitle.</summary>
        public const string Lockout = "Lockout applicato.";
        /// <summary>Lockout follow-up subtitle.</summary>
        public const string Lockout2 = "Nessuno può riarmare.";

        // ------------------------------------------------------------------ Tester

        /// <summary>Tester alert: the oil is still energised.</summary>
        public const string TesterLive = "Olio ancora in tensione.";
        /// <summary>Tester alert detail while the charge decays.</summary>
        public const string TesterLive2 = "Attendi che si scarichi.";
        /// <summary>Tester alert: zero residual energy confirmed.</summary>
        public const string TesterZero = "Energia zero confermata.";
        /// <summary>Try-start feedback when the machine stays still.</summary>
        public const string TryOk = "Nessun avvio: isolamento ok.";
        /// <summary>Try-start alert when the machine restarts.</summary>
        public const string TryFail = "La pressa riparte!";
        /// <summary>Try-start alert detail.</summary>
        public const string TryFail2 = "Isolamento non riuscito.";

        /// <summary>Tester readout when main power is on. Placeholder {0} is not used.</summary>
        public const string TesterReadLivePanel = "LIVE — quadro inserito";
        /// <summary>Tester readout when main power is off and charge remains.</summary>
        public const string TesterReadLiveCharge = "LIVE — energia residua";
        /// <summary>Tester readout when no residual energy is left.</summary>
        public const string TesterReadZero = "ZERO — energia assente";

        // ------------------------------------------------------------------ PPE

        /// <summary>Format for an equipped item (hover, shelf status, action feedback): {0} is the Italian item name.</summary>
        public const string PpeEquippedFormat = "{0} indossato";
        /// <summary>Format for an equipped item: {0} is the item name.</summary>
        public const string PpeEquipFormat = "{0} indossato";
        /// <summary>Format for hovering an unequipped PPE item: {0} is item name, {1} is Italian slot name.</summary>
        public const string PpeHoverFormat = "{0} ({1}) — TRIGGER per indossare";
        /// <summary>Format for a wrong item: {0} is the item name.</summary>
        public const string PpeWrongFormat = "{0}: non idoneo.";
        /// <summary>Alert when the four correct PPE items are worn.</summary>
        public const string PpeFull = "DPI completi: 4 su 4.";
        /// <summary>Italian label for the Gloves PPE slot.</summary>
        public const string PpeSlotGloves = "Mani";
        /// <summary>Italian label for the Body PPE slot.</summary>
        public const string PpeSlotBody = "Corpo";
        /// <summary>Italian label for the Face PPE slot.</summary>
        public const string PpeSlotFace = "Viso";
        /// <summary>Italian label for the Feet PPE slot.</summary>
        public const string PpeSlotFeet = "Piedi";
        /// <summary>Hover feedback for the closed PPE locker.</summary>
        public const string HoverPpeCabinetClosed = "Armadio DPI — TRIGGER per aprire";

        // ------------------------------------------------------------------ Hover Action Feedback

        /// <summary>Hover feedback for Main Disconnect Switch while power is ON.</summary>
        public const string HoverMainSwitchOn = "Quadro generale: ATTIVO — TRIGGER per isolare";
        /// <summary>Hover feedback for Main Disconnect Switch after power is OFF.</summary>
        public const string HoverMainSwitchOff = "Quadro generale: SPENTO (isolato)";
        /// <summary>Hover feedback for Lockout Station before main power is cut.</summary>
        public const string HoverLockoutBlocked = "Lockout: BLOCCATO — Apri prima il quadro generale";
        /// <summary>Hover feedback for Lockout Station once main power is off and ready.</summary>
        public const string HoverLockoutReady = "Lockout: PRONTO — TRIGGER per bloccare il quadro";
        /// <summary>Hover feedback for Lockout Station after lockout is applied.</summary>
        public const string HoverLockoutApplied = "Lockout: APPLICATO (riarmo impedito)";
        /// <summary>Hover feedback for Voltage Tester while main power is ON.</summary>
        public const string HoverTesterLivePanel = "Tester tensione: LIVE — TRIGGER per leggere";
        /// <summary>Hover feedback for Voltage Tester after main power is OFF, before ZERO confirmed.</summary>
        public const string HoverTesterCheckResidual = "Tester tensione — TRIGGER per verifica energia residua";
        /// <summary>Hover feedback for Voltage Tester after ZERO confirmed.</summary>
        public const string HoverTesterZeroConfirmed = "Tester tensione: ZERO — Energia assente confermata";
        /// <summary>Hover feedback for the open oil leak valve.</summary>
        public const string HoverValveOpen = "Valvola olio — TRIGGER per chiudere la perdita";
        /// <summary>Hover feedback for the closed oil leak valve.</summary>
        public const string HoverValveClosed = "Valvola olio: CHIUSA";
        /// <summary>Hover feedback for a grabbable cone.</summary>
        public const string HoverCone = "Cono — GRIP per afferrare e posare agli angoli";
        /// <summary>Hover feedback for a grabbable absorbent mat.</summary>
        public const string HoverAbsorbentMat = "Tappeto assorbente — GRIP per posare sulla pozza";
        /// <summary>Hover feedback for a grabbable absorbent sock.</summary>
        public const string HoverAbsorbentSock = "Sacco assorbente — GRIP per posare sulla pozza";
        /// <summary>Hover feedback for the Emergency Stop button.</summary>
        public const string HoverEStop = "Fungo di emergenza — TRIGGER per fermare la pressa";
        /// <summary>Hover feedback for the Test Start button.</summary>
        public const string HoverTryStart = "Prova riavvio — TRIGGER per verificare isolamento";
        /// <summary>Hover feedback for the department alarm button.</summary>
        public const string HoverAlarm = "Allarme reparto — TRIGGER per chiamare i soccorsi";
        /// <summary>Hover feedback for the water extinguisher distractor.</summary>
        public const string HoverWaterExtinguisher = "Estintore ad acqua — PERICOLO su olio bollente";
        /// <summary>Hover feedback for the exit door panic bar.</summary>
        public const string HoverExitPanicBar = "Maniglione antipanico — TRIGGER o GRIP per uscire";

        /// <summary>Negative feedback when clicking Lockout before opening Main Disconnect (Issue #46).</summary>
        public const string LockoutBlockedFeedback = "Apri prima il quadro generale";
        /// <summary>Negative feedback when releasing a delimitation cone outside a valid Containment Zone (Issue #46).</summary>
        public const string ConeWrongSpot = "Cono fuori posizione: posalo agli angoli della pozza";
        /// <summary>Negative feedback when releasing an absorbent mat or sock outside a valid puddle quadrant (Issue #46).</summary>
        public const string AbsorbentWrongSpot = "Assorbente fuori posizione: posalo sulla pozza";

        // ------------------------------------------------------------------ Spill and containment

        /// <summary>Valve closed feedback.</summary>
        public const string Valve = "Valvola chiusa: perdita fermata.";
        /// <summary>Format for a placed cone: {0} is the placed count.</summary>
        public const string ConeFormat = "Cono posizionato ({0}/4).";
        /// <summary>Feedback for a placed absorbent mat.</summary>
        public const string Mat = "Tappeto assorbente posizionato.";
        /// <summary>Feedback for a placed absorbent sock.</summary>
        public const string Sock = "Sacco assorbente posizionato.";
        /// <summary>Alert when delimitation and containment are complete.</summary>
        public const string Containment = "Area delimitata e contenuta.";

        // ------------------------------------------------------------------ Critical events

        /// <summary>Hot oil contact alert.</summary>
        public const string HotOil = "Contatto con olio bollente.";
        /// <summary>Hot oil contact alert detail.</summary>
        public const string HotOil2 = "Ustione: allontanati subito.";
        /// <summary>Live oil shock alert.</summary>
        public const string Shock = "Scossa elettrica!";
        /// <summary>Live oil shock alert detail.</summary>
        public const string Shock2 = "Olio ancora in tensione.";
        /// <summary>Water extinguisher alert.</summary>
        public const string Water = "Acqua sull'olio bollente!";
        /// <summary>Water extinguisher alert detail.</summary>
        public const string Water2 = "Vapore e schizzi: allontanati.";

        // ------------------------------------------------------------------ Exit and time

        /// <summary>Exit confirmation question shown on HUD_DecisionPanel (ADR-0006).</summary>
        public const string ExitConfirmQuestion = "Vuoi uscire dal reparto adesso?";
        /// <summary>Exit confirmation affirmative option.</summary>
        public const string ExitConfirmYes = "Sì, esci dal reparto";
        /// <summary>Exit confirmation cancel option.</summary>
        public const string ExitConfirmNo = "No, resta nella stanza";
        /// <summary>Supervisor line after a safe handover.</summary>
        public const string PrepostoSafe = "Sala in sicurezza. Ottimo lavoro.";
        /// <summary>Supervisor line after an unsafe handover.</summary>
        public const string PrepostoUnsafe = "La sala non è ancora sicura.";
        /// <summary>Time warning alert title.</summary>
        public const string TimeWarning = "Tempo";
        /// <summary>Time warning alert detail.</summary>
        public const string TimeWarning2 = "Concludi la messa in sicurezza.";
        /// <summary>Bad Ending 2 debrief line.</summary>
        public const string BadEnding2 = "La pressa ha continuato a lavorare.";

        // ------------------------------------------------------------------ Inaction / smoke (GDD §9.12)

        /// <summary>Inaction stage 1 alert title: smoke in the room.</summary>
        public const string SmokeStage1 = "Fumo in sala";
        /// <summary>Inaction stage 1 alert detail.</summary>
        public const string SmokeStage1Detail = "La pressa continua.";
        /// <summary>Inaction stage 2 alert title: unbreathable air.</summary>
        public const string SmokeStage2 = "Aria irrespirabile";
        /// <summary>Inaction stage 2 alert detail.</summary>
        public const string SmokeStage2Detail = "Ferma la pressa.";

        // ------------------------------------------------------------------ Hints (GDD §12.3)

        /// <summary>Start hint format: {0} is the observed target count.</summary>
        public const string HintStartFormat = "Osserva la scena: {0}/6";
        /// <summary>Hint after the E-stop (landmark phrasing, zero cardinal directions — Issue #46).</summary>
        public const string HintAlarm = "Attiva l'allarme di reparto";
        /// <summary>Hint after the alarm (landmark phrasing, zero cardinal directions — Issue #46).</summary>
        public const string HintMainPower = "Apri il quadro elettrico e togli tensione";
        /// <summary>Hint after main power off.</summary>
        public const string HintLockout = "Applica il lockout sul quadro";
        /// <summary>Hint after the lockout.</summary>
        public const string HintTester = "Usa il tester di tensione";
        /// <summary>Hint after the tester reads zero.</summary>
        public const string HintTryStart = "Prova il riavvio: non deve partire";
        /// <summary>Hint after zero energy is verified.</summary>
        public const string HintPpe = "Indossa i 4 DPI nell'armadio DPI";
        /// <summary>Partial PPE hint format: {0} is the number of correct items.</summary>
        public const string HintPpePartialFormat = "DPI {0}/4 — completa nell'armadio DPI";
        /// <summary>Hint after the full PPE set.</summary>
        public const string HintValveAndContainment = "Chiudi la valvola e delimita la pozza";
        /// <summary>Partial containment hint format: {0} cones, {1} absorbents.</summary>
        public const string HintContainmentPartialFormat = "Coni {0}/4 · Assorbenti {1}/4";
        /// <summary>Hint after containment is complete (landmark phrasing, zero cardinal directions — Issue #46).</summary>
        public const string HintExit = "Usa il maniglione della porta d'uscita";

        // ------------------------------------------------------------------ Pause Menu Detailed Checklist (Issue #46)

        /// <summary>Pause checklist: observation progress ({0}/6).</summary>
        public const string ChecklistObserveFormat = "Osserva la sala e i rischi ({0}/6)";
        /// <summary>Pause checklist: press E-Stop.</summary>
        public const string ChecklistEStop = "Premi il fungo di emergenza";
        /// <summary>Pause checklist: raise department alarm.</summary>
        public const string ChecklistAlarm = "Attiva l'allarme di reparto";
        /// <summary>Pause checklist: open main disconnect switch.</summary>
        public const string ChecklistMainOff = "Apri il sezionatore generale sul quadro";
        /// <summary>Pause checklist: apply lockout.</summary>
        public const string ChecklistLockout = "Applica il lockout sul quadro";
        /// <summary>Pause checklist: verify zero residual energy with digital meter.</summary>
        public const string ChecklistTesterZero = "Verifica energia zero col tester digitale";
        /// <summary>Pause checklist: try-start verification.</summary>
        public const string ChecklistTryStart = "Esegui la prova di riavvio sul piedistallo";
        /// <summary>Pause checklist: equip all 4 correct PPE items ({0}/4).</summary>
        public const string ChecklistPpeFormat = "Indossa i 4 DPI corretti ({0}/4)";
        /// <summary>Pause checklist: close oil leak valve.</summary>
        public const string ChecklistValve = "Chiudi la valvola della perdita olio";
        /// <summary>Pause checklist: place 4 cones and 4 absorbents ({0}/4 cones, {1}/4 absorbents).</summary>
        public const string ChecklistContainmentFormat = "Contieni la pozza (coni {0}/4 · assorbenti {1}/4)";
        /// <summary>Pause checklist: exit through the safety exit door.</summary>
        public const string ChecklistExit = "Esci dal reparto dalla porta d'uscita";

        // ------------------------------------------------------------------ HUD sound events

        /// <summary>Subtitled sound event for the alarm siren.</summary>
        public const string SoundAlarmSiren = "Allarme reparto";
        /// <summary>Subtitled sound event for the steam burst.</summary>
        public const string SoundSteamBurst = "Getto di vapore";
        /// <summary>Subtitled sound event for the electric shock.</summary>
        public const string SoundShock = "Scossa elettrica";

        // ------------------------------------------------------------------ Debrief (GDD §11.8)

        /// <summary>Good line: press stopped and alarm raised.</summary>
        public const string DebriefGoodEStopAndAlarm = "Hai fermato la pressa e dato l'allarme → soccorsi chiamati";
        /// <summary>Good line: main power isolated and locked out.</summary>
        public const string DebriefGoodIsolation = "Hai isolato e bloccato il quadro → energia zero";
        /// <summary>Good line: zero energy verified.</summary>
        public const string DebriefGoodZeroEnergy = "Hai verificato l'assenza di energia → rischio elettrico escluso";
        /// <summary>Good line: full PPE worn.</summary>
        public const string DebriefGoodPpe = "Hai indossato i 4 DPI → protezione completa";
        /// <summary>Good line: spill delimited and contained.</summary>
        public const string DebriefGoodContainment = "Hai delimitato e contenuto la pozza → area sicura";
        /// <summary>Good line: leak valve closed before the handover (good practice, no score).</summary>
        public const string DebriefGoodValveClosed = "Hai chiuso la valvola → perdita fermata";
        /// <summary>Critical line: hot oil burn.</summary>
        public const string DebriefCriticalHotOil = "Contatto con olio bollente → ustione";
        /// <summary>Critical line: live oil shock.</summary>
        public const string DebriefCriticalShock = "Olio in tensione → folgorazione";
        /// <summary>Critical line: water extinguisher.</summary>
        public const string DebriefCriticalWater = "Estintore ad acqua → vapore e schizzi";
        /// <summary>Critical line: try-start before isolation.</summary>
        public const string DebriefCriticalTryStart = "Prova di riavvio senza isolamento → ripartenza";
        /// <summary>Critical line: press contact while running.</summary>
        public const string DebriefCriticalPressContact = "Contatto con la pressa in moto → schiacciamento";
        /// <summary>Critical line: wrong PPE contact.</summary>
        public const string DebriefCriticalWrongPpe = "Contatto con olio e DPI non idonei → ustione";
        /// <summary>Critical line: Bad Ending 2.</summary>
        public const string DebriefCriticalInaction = BadEnding2;
        /// <summary>Critical line: Early Exit while press is still running (ADR-0006).</summary>
        public const string DebriefCriticalMissingEStop = "Hai lasciato il reparto con la pressa ancora in funzione → rischio grave";
        /// <summary>Bad line: no alarm.</summary>
        public const string DebriefBadMissingAlarm = "Allarme non dato → reparto non avvisato";
        /// <summary>Bad line: missing main off or lockout.</summary>
        public const string DebriefBadMissingIsolation = "Quadro o lockout incompleti → possibile riarmo";
        /// <summary>Bad line: missing delimitation or containment.</summary>
        public const string DebriefBadMissingContainment = "Delimitazione o contenimento incompleti → area non sicura";
        /// <summary>Bad line: time limit exceeded.</summary>
        public const string DebriefBadTimeout = "Tempo massimo superato → chiusura forzata";

        /// <summary>
        /// Resolves a debrief line id to its Italian text, without prefix. The SPI adds the shared
        /// "+ ", "- " and "! " prefixes from <c>DebriefLinePrefix</c>.
        /// </summary>
        /// <param name="id">Debrief line identifier.</param>
        /// <returns>Italian text, or an empty string for an unknown id.</returns>
        public static string DebriefLine(DebriefLineId id)
        {
            switch (id)
            {
                case DebriefLineId.GoodEStopAndAlarm: return DebriefGoodEStopAndAlarm;
                case DebriefLineId.GoodIsolation: return DebriefGoodIsolation;
                case DebriefLineId.GoodZeroEnergy: return DebriefGoodZeroEnergy;
                case DebriefLineId.GoodPpe: return DebriefGoodPpe;
                case DebriefLineId.GoodContainment: return DebriefGoodContainment;
                case DebriefLineId.GoodValveClosed: return DebriefGoodValveClosed;
                case DebriefLineId.CriticalHotOil: return DebriefCriticalHotOil;
                case DebriefLineId.CriticalShock: return DebriefCriticalShock;
                case DebriefLineId.CriticalWater: return DebriefCriticalWater;
                case DebriefLineId.CriticalTryStart: return DebriefCriticalTryStart;
                case DebriefLineId.CriticalPressContact: return DebriefCriticalPressContact;
                case DebriefLineId.CriticalWrongPpe: return DebriefCriticalWrongPpe;
                case DebriefLineId.CriticalInaction: return DebriefCriticalInaction;
                case DebriefLineId.CriticalMissingEStop: return DebriefCriticalMissingEStop;
                case DebriefLineId.BadMissingAlarm: return DebriefBadMissingAlarm;
                case DebriefLineId.BadMissingIsolation: return DebriefBadMissingIsolation;
                case DebriefLineId.BadMissingContainment: return DebriefBadMissingContainment;
                case DebriefLineId.BadTimeout: return DebriefBadTimeout;
                default: return string.Empty;
            }
        }

        /// <summary>
        /// Builds the full debrief list for a run. Convenience wrapper used by the scenario.
        /// </summary>
        /// <param name="spi">SPI that recorded the run.</param>
        /// <returns>Prefixed Italian debrief lines, already capped.</returns>
        public static List<string> BuildDebriefLines(MachineLockSpi spi)
        {
            return spi == null ? new List<string>() : spi.BuildDebriefLines(DebriefLine);
        }
    }
}
