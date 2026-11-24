# Pilot — migrazione al Player Rig e alla UI comune

**Branch:** `pilot/rig-ui-migration-tiziano`
**Base:** `origin/group_tiziano` (`21f0ed9`)
**Merge:** `integration/testing-ui` (`96f52e8`)
**Data:** 24 novembre 2026

## 1. Cosa è stato verificato

- Merge di `integration/testing-ui` pulito: conflitti solo su
  `ProjectSettings/ProjectSettings.asset`, `Assets/XR/Settings/OpenXR Package Settings.asset` e
  `Guida_Design-Interactions.md.meta`, risolti tenendo `main`.
- Rimosso il pacchetto locale `com.coplaydev.unity-mcp` (`file:C:/...`) che impediva a Unity di
  aprire il progetto; ora si usa il manifest di `main`.
- `Packages/manifest.json` senza `com.unity.collab-proxy`.
- Compilazione Unity del branch: **OK** (exit 0).
- `SCN_Core` valida: 1 `RigRegistration`, 1 camera attiva, 1 `EventSystem`, 2 `UIPointer`,
  `RigUIBridge` con tutti i riferimenti, 1 `UIScreenManager`.
- `SCN_ChemicalAlert` migrata e valida: 0 rig, 0 camera, 0 `EventSystem`, 27
  `ScenarioInteractable` con 27 `Interactable`, 3 zone PPE, 1 `ChemicalAlertDirector`,
  `ScenarioController` + `MNG_SpawnPoint`, 0 script mancanti.

## 2. Migrazione eseguita su `SCN_ChemicalAlert`

1. L'istanza `OVRCameraRig` è stata unpacked e la sua radice è diventata `MNG_Scenario`; così i
   sistemi di scenario (`DebugCommands`, `PlayerPPEState`, …) hanno mantenuto i riferimenti.
2. Rimossi i componenti `OVRCameraRig`, `OVRManager`, `OVRHeadsetEmulator` e la gerarchia OVR
   (TrackingSpace e ancore).
3. Ricreate le tre zone PPE che stavano sotto la radice del rig:
   `PPEEquipZone_Head` (0, 1.6, 0.3), `PPEEquipZone_Torso` (0, 1, 0.2),
   `PPEEquipZone_Hands` (0, 1.2, 0.5), scala 0.35, `SphereCollider` trigger.
   Il componente `PPEZoneRigAnchor` le riaggancia alla radice del rig condiviso appena
   `RigRegistration` è disponibile.
4. Riscritti `PlayerPPEState.zones` e i 9 array `PPEItemObject.equipZones` sulle nuove zone.
5. Creato `SO_Mission_ChemicalAlert` boilerplate (`Id: 05`, `SceneName: SCN_ChemicalAlert`) e
   aggiunto a `SO_MissionCatalog`.
6. Aggiunto `ScenarioController` su `MNG_Scenario` con missione e `MNG_SpawnPoint` (posizione del
   `DebugSpawnMarker`).
7. Aggiunto `Interactable` ai 27 `ScenarioInteractable`; `ScenarioInteractable` ora fa da adattatore
   sugli eventi di `Interactable` (hover, select, grab) e gestisce Inspected mentre l'oggetto è
   tenuto.
8. Rimossi `ScenarioHandInteractor` e `ScenarioInputBindings`; il tasto Hint usa
   `InputService.WasMenuPressed()`. `ExitSafeZone` e `SubstanceContactSensor` riconoscono
   `RayInteractor` e `RigRegistration`.
9. Aggiunta `SCN_ChemicalAlert` alle Build Settings dopo `SCN_Core` e `SCN_MockScenario`.

## 3. Errori trovati durante la migrazione

1. **Zone PPE perse.** Erano figlie della radice `OVRCameraRig`, non del solo TrackingSpace:
   eliminando il rig sono sparite. Ricreate con `PPEZoneRigAnchor` (aggancio tardivo a
   `RigRegistration`).
2. **Id missione duplicato.** `Id: 08` era già usato; portato a `05`.
3. **Menu = Hint + Pausa.** `MissionFlowDriver` usa lo stesso tasto Menu per la pausa. In Guided,
   premere Menu per il suggerimento apre anche la pausa. Da decidere dopo il test su visore.
4. **Riferimenti alle zone.** `PlayerPPEState.zones` e `PPEItemObject.equipZones` puntavano alle
   zone eliminate; riscritti.

## 4. Build

APK: `Build/Sicurezza-Immersiva-81_ver1.apk` — **build riuscita**, 106.150.007 byte
(esito in `Logs/pilot-build2.log`). La build parte da `SCN_Core`; la missione Chemical Alert è la
card 05 del catalogo.

La prima build è fallita per **disco pieno** (`LLVM ERROR: IO failure on output stream:
No space left on device` durante il link di `libil2cpp.so`), non per errori di codice. Sono state
cancellate le vecchie build (`Builds/*.apk`) e le cache `Library`/`Bee`, poi la build è stata
rilanciata ed è passata.

## 5. Cosa resta

- Compilare obiettivi, criteri e testi reali della missione (ora boilerplate).
- Decidere il conflitto Hint/Menu (pausa).
- Provare su Quest: rig, spawn, UI, presa a raggio, zone PPE, Hint, fine missione.
