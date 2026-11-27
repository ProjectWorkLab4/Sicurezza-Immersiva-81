# Chemical Alert — integrazione UI e rig condivisi

Stato al 25 novembre 2026. Questo documento riguarda esclusivamente rig, input e UI condivisi nello scenario Chemical Alert.

## Completato

- La scena `SCN_ChemicalAlert` non contiene più il rig/camera locale (`OVRCameraRig`, `OVRManager`, `OVRHeadsetEmulator`), Canvas, EventSystem o listener audio della camera.
- Il vecchio root è diventato `MNG_Scenario`; conserva `PlayerPPEState` e `DebugCommands` e ora ospita `ChemicalAlertScenarioController`.
- Il marker `SPAWN_Player` è diventato `MNG_SpawnPoint` con `SI81.Rig.SpawnPoint`. Il controller usa la missione condivisa `SO_Mission_ChemicalAlert` senza modificarla.
- Rimossi l’interactor e i binding locali basati su `OVRInput`. I 14 oggetti con `ScenarioInteractable` sono collegati a `SI81.Interaction.Interactable`; il sensore di contatto riconosce le mani del rig condiviso.
- Aggiunto `OBJ_HintButton3D`, visibile/selezionabile solo in Guided; il prossimo obiettivo globale con indicazioni pratiche è mostrato dal popup evento della HUD condivisa (`ShowEventAlert`), senza etichette 3D dedicate.
- `ChemicalAlertScenarioController` collega hint, avvisi e risultato del Director alla HUD e alla conclusione comune della missione. I timer locali sono allineati ai livelli: Guided senza conto alla rovescia, Operational 600 s, Crisis 300 s.
- Guided non ha sistema di salute: `hearts=0` non produce `Failure` perché l'evaluator ignora gli HP per quel livello.
- La HUD segue i quattro obiettivi dell’asset missione (`Accendi ventilazione`, `Leggi SDS`, `Recupera DPI`, `Contieni la sostanza`); il completamento è segnato dal gameplay anche se le azioni avvengono in ordine libero, e Pausa/polso mostrano check coerenti.
- La scheda SDS rimane 3D e world-anchored. `MNG_ChemicalAlertFeedback` gestisce la scheda e il cue audio senza Canvas locale.
- La vignetta tossica viene creata a runtime solo come overlay dell’istanza `MNG_HUD` mentre Chemical Alert è attivo; usa l’intensità e il colore del pericolo, sfuma gradualmente e viene rimossa allo scaricamento dello scenario. Nessun prefab o asset condiviso è stato modificato.
- `loadCoreInEditor` è disattivato su `MNG_Scenario`: in Play Mode dell’Editor il caricamento di `SCN_Core` è affidato a `RigDirectPlay`, evitando il doppio caricamento osservato inizialmente.
- `SCN_ChemicalAlert` è stata aggiunta ai Build Settings all’indice 2. `SCN_Core` resta 0 e `SCN_MockScenario` resta 1.
- Creato il build Android Development ARM64 `Build/Sicurezza-Immersiva-81_ver2.apk`; Unity ha riportato build riuscita, 0 errori e 363 avvisi.
- Per il tracciamento obiettivi sono stati modificati anche `MissionFlow.cs`, `ScenarioController.cs` e `PauseScreen.cs` sotto `Assets/_Project/Home/`; nessuna modifica sotto `Assets/_Project/Shared/`.

## Da completare o verificare

1. **Test sul Quest:** da eseguire dal proprietario del visore. Il build non è stato installato né avviato su dispositivo.
2. **Smoke test rig/UI:** la validazione statica della scena è pulita (0 missing script e prefab rotti), ma il PlayMode dell’Editor non ha dato un esito affidabile: il Test Runner si è interrotto con un `NullReferenceException` interno; durante l’avvio diretto sono comparsi assert `InputActionMap` e `NullReferenceException` in `UIPointer`. Verificare sul Quest con `SCN_Core` caricato una sola volta: hand-off di testa/origine/mani, HUD pronta, puntamento, selezione, grab, locomozione e vignetta. Non è stato dichiarato un pass del rig.

## File di integrazione principali

- `Assets/_Project/Groups/G_Tiziano/Level/Scenes/SCN_ChemicalAlert.unity`
- `Assets/_Project/Groups/G_Tiziano/Code/Scripts/Systems/ChemicalAlertScenarioController.cs`
- `Assets/_Project/Groups/G_Tiziano/Code/Scripts/Systems/ChemicalAlertDirector.cs`
- `Assets/_Project/Groups/G_Tiziano/Code/Scripts/Interaction/ScenarioInteractable.cs`
- `Assets/_Project/Groups/G_Tiziano/Code/Scripts/Interaction/ChemicalAlertHintButton.cs`
- `Assets/_Project/Groups/G_Tiziano/Code/Scripts/Interaction/ChemicalAlertPanelUI.cs`
