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
5. **App sbagliata sul visore.** La build usava l'application id di `main`
   (`com.UnityTechnologies.com.unity.template.urpblank`), quindi sul Quest restava aperta la
   vecchia app `com.si81.sicurezzaimmersiva` del 23 novembre con il menu vecchio. La build del
   pilot ora usa `com.si81.sicurezzaimmersiva` e sostituisce quella installata.
6. **Missione Chemical Alert duplicata.** Il catalogo aveva già `SO_Mission_ChemicalAlert` (Id 04,
   scena `SCN_ChemicalAlert`). Il pilot ne aveva creata una seconda (Id 05). La seconda è stata
   rimossa e `ScenarioController` punta alla 04.
7. **Id 05/06 mancanti.** Nel catalogo non esistono missioni con Id 05 o 06: le card mostrate sono
   solo quelle autorate. Non è un errore della Home; se servono quegli scenari vanno creati e
   aggiunti al catalogo.

## 4. Build

APK finale: `Build/Sicurezza-Immersiva-81_ver2.apk` — **build riuscita**, 106.150.259 byte,
package `com.si81.sicurezzaimmersiva`, versione `0.2.0` (version code 2). Installata con
`metavr app install -r` sopra la vecchia app e avviata sul Quest 3 `2G0YC5ZG1402NV`.
La build parte da `SCN_Core`; Chemical Alert è la missione Id 04 già presente nel catalogo.

La prima build (`ver1`) usava l'application id template di `main`
(`com.UnityTechnologies.com.unity.template.urpblank`): sul visore restava aperta la vecchia
`com.si81.sicurezzaimmersiva` e il menu sembrava vecchio. La `ver2` usa l'id del progetto.
La prima build era anche fallita per **disco pieno** durante il link di `libil2cpp.so`; risolto
cancellando vecchie build e cache. Dettagli in `Logs/pilot-build2.log` e `Logs/pilot-build3.log`.

## 5. Cosa resta

- Compilare obiettivi, criteri e testi reali della missione (ora boilerplate).
- Decidere il conflitto Hint/Menu (pausa).
- Provare su Quest: rig, spawn, UI, presa a raggio, zone PPE, Hint, fine missione.
