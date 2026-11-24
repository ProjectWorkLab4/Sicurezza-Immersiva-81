# Pilot — migrazione al Player Rig e alla UI comune

**Branch:** `pilot/rig-ui-migration-tiziano`
**Base:** `origin/group_tiziano` (`21f0ed9`)
**Merge:** `integration/testing-ui` (`96f52e8`)
**Data:** 24 novembre 2026
**Scope:** integrazione di rig, UI e input condivisi sul branch Tiziano. Il gameplay di
`SCN_ChemicalAlert` non è stato riscritto: i punti che richiedono una decisione del team sono
elencati sotto.

## 1. Cosa è stato verificato

- Il merge di `integration/testing-ui` è pulito: gli unici conflitti erano
  `ProjectSettings/ProjectSettings.asset`, `Assets/XR/Settings/OpenXR Package Settings.asset` e
  `Guida_Design-Interactions.md.meta`, risolti tenendo la versione di `main`.
- `Packages/manifest.json` e `packages-lock.json` non contengono `com.unity.collab-proxy`.
- Il branch Tiziano conteneva un pacchetto locale `com.coplaydev.unity-mcp` con percorso
  `file:C:/Unity-Projects/unity-mcp-beta/MCPForUnity`: Unity non apriva il progetto. Sostituito con
  il manifest di `main` (commit `1fffd74`). È un errore da mettere in checklist per tutti i gruppi.
- `SCN_Core` è indice 0 delle Build Settings, seguita da `SCN_MockScenario`.
- Il branch contiene il rig condiviso (`SI81.Rig`), l'adattatore `SI81.UI.Rig.RigUIBridge`,
  `InputService` e `Interactable`/`RayInteractor`.
- Compilazione Unity del branch: **OK** (exit 0, `Logs/pilot-compile2.log`).
- Validazione di `SCN_Core` eseguita in batchmode: **1 `RigRegistration`, 1 camera attiva,
  1 `EventSystem`, 2 `UIPointer`, 1 `RigUIBridge` con tutti i riferimenti, 1 `UIScreenManager`**.
  Il controllo è passato (exit 0, `Logs/pilot-validation2.log`).

## 2. Stato di `SCN_ChemicalAlert`

La scena ha ancora il rig locale:

- 1 istanza `OVRCameraRig` (prefab Meta XR Core SDK) con 2 `ScenarioHandInteractor`,
  1 `ScenarioInputBindings`, `ChemicalAlertDirector`, `DebugCommands`, `PlayerPPEState`,
  `PlayerHealth`;
- 26 oggetti con `ScenarioInteractable`;
- nessun `SO_Mission_*`, nessun `ScenarioController`, nessun `MNG_SpawnPoint`;
- la scena non è nelle Build Settings.

Quindi il branch **non è ancora una scena migrata**: è il punto di partenza su cui applicare la
[Guida_Migrazione_Rig_UI.md](../../../Docs/Guida_Migrazione_Rig_UI.md).

## 3. Blocchi che richiedono una decisione del team Tiziano

1. **Tasto Hint (A/X).** `ScenarioInputBindings.GetHintDown` legge `OVRInput.Button.One`.
   `InputService` non mappa A/X di proposito (il design condiviso usa il grilletto per la UI).
   Scegliere: Select, Menu, oppure una nuova azione dedicata in `InputBindings`.
2. **Interazione vicino alla mano.** `ScenarioHandInteractor` usa `OverlapSphere` attorno alla
   mano e grip/trigger. Il rig condiviso offre `RayInteractor` + `Interactable` a raggio.
   I 26 `ScenarioInteractable` devono diventare `Interactable` (o avere un adattatore che ne
   inoltra gli eventi) e va deciso se la presa resta a raggio.
3. **Rilevamento della mano.** `ExitSafeZone` e `SubstanceContactSensor` cercano un
   `ScenarioHandInteractor` nel collider per capire se è una mano. Con il rig condiviso vanno
   rilegati a `RayInteractor` o a un componente marcatore sulle ancore del rig.
4. **Flusso missione.** La scena non ha `SO_Mission_ChemicalAlert` né `ScenarioController`:
   il validatore non può passare. Va creato il contenuto missione (titolo, obiettivi, criteri,
   `SceneName`) e aggiunto `MNG_Scenario`.
5. **Spawn.** Aggiungere `MNG_SpawnPoint` con `SpawnPoint` e assegnarlo a `ScenarioController`.

## 4. Ordine consigliato per completare la migrazione

1. Creare `SO_Mission_ChemicalAlert` e `MNG_Scenario` con `ScenarioController` (vedi
   `GUIDA_INTEGRAZIONE_SCENARIO_V2.0.md`).
2. Scegliere il mapping del tasto Hint e aggiornare `ScenarioInputBindings` o sostituirlo con
   `InputService`.
3. Sostituire `ScenarioHandInteractor` con `Interactable` + gli eventi condivisi; adattare
   `ExitSafeZone` e `SubstanceContactSensor`.
4. Eliminare l'istanza `OVRCameraRig` dalla scena e spostare i sistemi di scenario su
   `MNG_Scenario`; aggiungere `MNG_SpawnPoint`.
5. Aggiungere `SCN_ChemicalAlert` alle Build Settings dopo `SCN_Core` e `SCN_MockScenario`.
6. Eseguire validatore, test PlayMode e checklist Quest.

## 5. Nota

Il pilot dimostra che il merge di rig/UI non introduce conflitti con il contenuto Tiziano, che il
branch compila con la nuova pila condivisa e che `SCN_Core` ha un solo rig, una sola camera attiva
e l'adattatore UI collegato. La migrazione della scena resta un lavoro di contenuto: non è stata
eseguita per non inventare decisioni di gameplay (tasto Hint, presa a raggio, missione).
