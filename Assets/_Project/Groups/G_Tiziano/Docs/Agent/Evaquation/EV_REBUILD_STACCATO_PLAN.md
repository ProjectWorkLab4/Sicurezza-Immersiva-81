# PIANO — Ricostruzione di SCN_Evacuation con UFFICIO_STACCATO + TAVOLO

Stato: **passi 1–10 completati (2026-11-27); restano solo test a mano nel Simulator e 3 decisioni (clip allarme, input hint, conferma annesso est)** — vedi `EV_HANDOFF.md` E22. Sotto: storia dei passi. La sezione "STATO LAVORI" qui sotto è la fonte di verità: le sezioni successive (analisi, mappatura, rischi) descrivono la situazione di partenza e in alcuni punti sono state corrette (vedi "Differenze rispetto all'analisi iniziale").

---

# STATO LAVORI (aggiornamento 2026-11-27)

Progetto Unity: `C:\Unity-Projects\Sicurezza-Immersiva-81`. Scena: `Assets/_Project/Groups/G_Tiziano/Level/Scenes/SCN_Evacuation.unity` (salvata in stato **intermedio**: non ancora giocabile fino al passo 8). Backup pre-modifica: `SCN_Evacuation_PRE_STACCATO.unity` (stessa cartella, 379.30 KB; senza `.meta`, Unity ne genera uno nuovo).

## Scelte confermate / assunzioni
- Mappatura: ufficio 1 = x<0, ufficio 2 = x>0 (4 `EvacDesk` ciascuno); corridoio = fascia z 2.25…4.5; annesso **nord** = scale di emergenza; annesso **est** = scale interne (**assunzione mia, l'utente non ha mai risposto**: il modello lì contiene una scala).
- **Ascensore**: messo all'estremità **ovest** del corridoio (x −6…−5), non a est come diceva il piano, perché l'ingresso dell'annesso est occupa la fine est del corridoio. Spostabile se serve.
- Il modello `UFFICIO_STACCATO` sta a (0,0,0), scala 1, quindi **le coordinate del modello sono le coordinate di mondo**.

## Geometria di riferimento (coordinate di mondo)
- Sala: x −6…6, z −4.6…4.6. Uffici z −4.6…2.25, corridoio z 2.25…4.5 (largo ~2.1 m utili: z 2.35…4.45).
- Parete uffici/corridoio: piano z=2.25 (esiste già nel modello, `Plane.024/087`), 2 varchi larghi **1.2 m** a **x = ±3.0**, alti 2.0.
- Divisorio tra uffici: x=0, z −4.6…2.25 (esiste già nel modello, `Cube.007/041`).
- Annesso nord (scale emergenza): x −1.8…2.0, z 4.5…7.1. Ingresso da 1.25 m a x −0.05…1.2, h 2.2, sulla parete z=4.55. In fondo (z 7.1) c'è `PORTA_EMERGENZA_CHIUSA` di decoro (`NurbsPath.007`).
- Annesso est (scale interne): x 6.1…8.1, z 2.25…4.6. Ingresso dalla sala nella parete x=6.05: z 2.8…4.05 (1.25 m), h 2.3.
- Tavoli `EvacDesk` (centri): R1 x −4.73 / −2.10, R2 x 1.27 / 3.97; file z −2.37 e −0.20. Ogni `TAVOLO` include scrivania + sedia + PC + monitor (sedia sul lato +z). Collider 1.91×0.8×1.06.
- Varchi liberi fra tavoli: R1 centrale x −1.14…−0.1 (1.04 m); R2 est x 4.93…5.95 (1.02 m); striscia dietro la seconda fila z 0.33…2.15 (1.8 m).

## Fatto, passo per passo
1. **[FATTO]** Letti `EV_HANDOFF.md`, `EVAC_TECHNICAL_DOCS.md`, `REUSE_MAP.md` e mappatura concordata.
2. **[FATTO]** Backup scena.
3. **[FATTO]** `MODEL_Staccato` istanziato da `Shared/Art/Models/UFFICIO/UFFICIO_STACCATO.fbx` (riferimento, nessuna copia), unpack completo. Eliminate dall'istanza 70 mesh di arredo (scrivanie `Cube.009/086`, sedie, monitor, tastiere, basamenti `Cube.026/099`, basi sedia `Cylinder.002/083`, ecc.). Da 122 mesh / 571.134 tri a **52 mesh / 39.924 tri**. Verificato con screenshot.
4. **[FATTO]** Prefab `Level/Prefabs/Evacuation/EvacDesk.prefab`: root con `BoxCollider` (1.91×0.8×1.06, centro y 0.4), figlio `Visual` = istanza di `TAVOLO.fbx` con pivot centrato a terra, `BatchingStatic`. In scena: root `EvacDesks` con `EvacDesk_R1_01…04` e `EvacDesk_R2_01…04`, rotazione 0.
5. **[FATTO]** Collider `Environment` ricostruiti come soli `BoxCollider` (27 box, 22 `Wall_*`), nomi secondo la convenzione del validatore (`Wall_*`, `*_Lintel*`):
   - `Floor/`: `Floor_Hall`, `Floor_StairsEmergency`, `Floor_StairsInternal` (top a y=0).
   - `Walls/`: perimetro sala (`Wall_Hall_South/West/East(+Lintel0)/North`), `Wall_Offices_South_Seg0..2 + Lintel0/1`, `Wall_Offices_Partition_Seg0`, `Wall_StairsEm_*` (porta, ovest, est, nord), `Wall_StairsInt_*`. Le pareti con piani/allarme (z 2.25 e z 4.55) sono spesse 0.1 m, le altre 0.2.
   - `Elevator/`: `Elevator_Block` (x −6…−5), `Elevator_Door` (x −5…−4.9) + **`Elevator_Buttons` conservato** (porta la logica del pulsante).
   - `Stairs_Emergency` / `Stairs_Internal`: **vuote** (il modello non ha geometria di gradini nota; nessun gradino inventato).
   - `Props_R1/R2` (vecchie scrivanie graybox) eliminate. `ExitZone_*` conservate.
   - Verifiche in editor con capsula r 0.3: tutti i varchi percorribili, muro pieno e divisorio bloccati, nessun punto della sala senza terreno.
6. **[FATTO]** (6a) 49 marker in `ScenarioMarkers` riposizionati (stessi nomi). (6b) `ScenarioObjects`, pulsante e zone d'uscita riposizionati/creati. Dettagli sotto.
7. **[FATTO]** 7a (porte e piani) e 7b (allarme, pulsante, cartello, scale: vedi sotto).

### Marker (passo 6a)
- `SPAWN_Player` e `MNG_SpawnPoint`: (−0.62, 0, −3.7), ry 0 (angolo sud-est ufficio 1).
- `NPCSLOT_R1_01..06`: (−4.73,−3.55) (−2.10,−3.55) (−2.90,−1.30) (−3.90,1.30) (−2.10,0.80) (−4.20,−1.30). `NPCSLOT_R2_01..06`: (4.00,−3.55) (4.60,−1.30) (3.50,−1.30) (4.00,0.80) (2.20,0.80) (1.10,−1.30). Tutti fuori dai box dei tavoli, ≥1.3 m fra loro, ≥1.0 m dai focolai.
- `ROUTE_R1_01..09`: (−0.62,−3.7) (−0.62,−1.3) (−0.62,1.2) (−3.0,1.35) (−3.0,2.25) (−3.0,3.2) (0.575,3.4) (0.575,4.55) (0.575,5.8). `ROUTE_R2_01..09`: come R1 ma con i primi tre punti a x=**5.3** e il quarto/quinto/sesto a x=3.0 (cioè (5.3,−3.7) (5.3,−1.3) (5.3,1.2) (3.0,1.35) (3.0,2.25) (3.0,3.2) poi come R1). R2 a x=5.45 NON passava il controllo corsia (il box 1.2 m toccava il muro est).
- `ZONE_StairsEmergency` (0.4,0,5.9); `ZONE_StairsInternal` (7.2,0,3.4).
- `POS_Plan_R1` (−1.3,1.5,2.14), `POS_Plan_R2` (1.3,1.5,2.14), `POS_Plan_Corridor` (4.0,1.5,4.44) tutti ry 180; `POS_Alarm` (−3.5,2.9,4.38) ry 180; `POS_Elevator` (−5.5,0,3.4) ry 90; `POS_ElevatorButton` (−4.85,1.15,4.2) ry 90.
- Convenzione rotazioni dei marker: la rotazione indica **la direzione verso cui l'oggetto è rivolto** (normale), non verso cui guarda il giocatore.
- 10 `FIRESLOT_*` (marker **e** istanze in `FireSlots`, coincidono): 01 (−4.3,3.4) 02 (4.3,3.0) 03 (5.3,3.8) [corridoio, fuori dalle rotte]; 04 (−3.4,−3.6) 05 (−5.2,1.2) 06 (−5.2,−1.3) [ufficio 1]; 07 (2.6,−3.6) 08 (1.2,1.2) 09 (2.4,−1.3) 10 (0.9,−3.6) [ufficio 2].
- Controllo corsia (replica in editor della logica di `EvacFireLaneValidator`): R1 e R2 libere senza fuochi, ogni focolaio compatibile, **252/252 combinazioni da 5 su 10 valide**. Il validatore vero va eseguito in Play (F5).

### Oggetti di scena (passo 6b / 7a) — tutti sotto `ScenarioObjects`
- `Door_R1` (−3.0,0,2.25), `Door_R2` (3.0,0,2.25): rot Y 180, larghezza 1.2, h 2.0; `startsOpen=True` (id `evac.door.room1`, `evac.door.room2`).
- `Door_StairsEmergency` (0.575,0,4.55): rot Y 180, doppio prefab ridotto a 1.25 m, h 2.2; `startsOpen=False` (id `evac.door.stairs.emergency`, ha `LabelAnchor`).
- **`Door_StairsInternal`** (nuova, dal prefab `EvacDoor_StairsInternal`): (6.05,0,3.425), rot Y 270, 1.25 m, h 2.3; id **`evac.door.stairs.internal`** (assegnato da me, da verificare), `director` collegato.
- `Plan_R1`, `Plan_R2`, `Plan_Corridor` (nuovi, da `EvacPlan`): ids **`evac.plan.room1/room2/corridor`** (scelti da me, da verificare), `director` collegato. Il lato leggibile è lo **+Z locale**.
- `Alarm` (nuovo, da `EvacAlarm`) (−3.5,2.9,4.38) rot 180, `director` collegato. `alarmClip` ancora vuoto.
- `ExitSign_StairsEmergency` (nuovo, da `EvacExitSign`) (0.575,2.6,4.45) rot 180: **ancora il rettangolo verde graybox**.
- `Environment/Elevator/Elevator_Buttons` → (−4.85,1.15,4.2). `Environment/ExitZone_StairsEmergency` → (0.4,0,5.9). `Environment/ExitZone_StairsInternal` → (7.2,0,3.4).
- Blocker porte dimensionati esattamente sulle aperture (verificato).

### Visual porte e piani (passo 7a)
- **Misure FBX** (root con scala/rotazione particolari; per avere altezza su Y e spessore su Z): `PORTA_UFFICIO` rotazione (0,270,270) → 1.24×2.0×0.09, solo battente, maniglia sul lato +z; `PORTA_EMERGENZA_CHIUSA` rotazione (90,0,0) → 1.2×2.17×0.24, include **telaio + battente + maniglione** sul lato +z; `FOGLI` rotazione 0 → 1.05×1.33×0.21 (pannello **scuro**, non un foglio chiaro). `PORTA_EMERGENZA_APERTA` ha la **stessa mesh** di `_CHIUSA` (1632 tri): non serve per lo stato aperto.
- Tecnica usata (`AddLeaf`): figlio `Leaf` (wrapper con la scala di adattamento) sotto la cerniera + istanza dell'FBX con la rotazione sopra, offset per centrare/appoggiare a terra calcolato dai bounds delle mesh in spazio locale. I vecchi `Panel`/`Handle` graybox sono **disattivati** (non si possono distruggere figli di istanze di prefab).
- `Door_R1/R2` e `Door_StairsInternal`: `PORTA_UFFICIO` sulla cerniera (ruota di 90° come da `DoorInteractable`).
- `Door_StairsEmergency`: `leaves` svuotato (niente rotazione, il telaio ruoterebbe); battente `CHIUSA` sotto `Visual/Hinge_Left/Leaf`; 4 listener persistenti su `OnOpened`/`OnClosed` scambiano `Leaf` (chiusa) con `MODEL_Staccato/NurbsPath.003` (porta aperta autorata nel modello, tenuta disattivata di default). **Non testabile in editor**: verificare in Play. Nota: `DoorInteractable.Awake` non invoca gli eventi, quindi lo stato iniziale (chiusa) è impostato staticamente.
- Disattivati in `MODEL_Staccato` i battenti originali duplicati `Cylinder.120`, `Cylinder.121`, `Cylinder.001`; disattivati i residui `(Clone)` dell'E11 sotto `Visual` delle porte.
- Piani: `FOGLI` scalato 0.5 (≈0.53×0.66×0.11) al posto di `Board/RouteMark/RouteEnd`; `BoxCollider` del piano 0.56×0.70×0.10.

## Da fare

### 7b — altri Visual **[FATTO 2026-11-27]**

Esito (la lista sotto è la specifica originale; l'esito reale è qui):
- Tecnica: per ogni oggetto un figlio `Model` (wrapper con scala di adattamento) direttamente sotto la **root** dell'oggetto (non sotto `Visual`: quello del pulsante ha scala non uniforme 0.1/0.3/0.2) + istanza dell'FBX (`PrefabUtility.InstantiatePrefab`) con la rotazione sopra, ancorata ai bounds. `Visual` graybox **disattivato** (non distruggibile, è parte del prefab).
- **`Alarm`**: `ALLARME.fbx` scala 1 (0.40×0.40×0.205), rotazione istanza (270,0,0), retro a local z −0.12 (= muro z 4.5), centro y 2.9. Il modello è una **scatola bianca con una lente rossa 0.15×0.15 sul lato y=0**, che con la rotazione guarda verso la stanza. `AlarmSystem.pulseRenderers[0]` = renderer `Alarm/Model/ALLARME` (verificato). Sostituito il materiale rosso (submesh 0) con `Art/Materials/Evacuation/MAT_EV_AlarmModel_Red.mat`: copia di `Material.017` con keyword `_EMISSION` attivo, emissione (0.01,0.01,0.01), `RealtimeEmissive` — **necessario**: i materiali dell'FBX non hanno l'emissione abilitata e la `MaterialPropertyBlock` non avrebbe avuto effetto. (URP spegne `_EMISSION` se l'emissione è esattamente nera: per questo 0.01 e non 0.)
- **`Elevator_Buttons`**: `PULSANTE.fbx` scala 0.1 (piastra 0.057×0.20×0.20 con pulsante rosso), rotazione istanza (0,270,0), retro piastra a local x −0.05 (= faccia della porta ascensore, x −4.9), normale verso +x. `BoxCollider` 0.10×0.30×0.20 e componenti **invariati** (non toccato nulla).
- **`ExitSign_StairsEmergency`**: la texture di `INDICAZIONE_PORTA_EMERGENZA` è in realtà il cartello **rosso "PORTA TAGLIAFUOCO A CHIUSURA AUTOMATICA"**, non un'indicazione verde di uscita. Il modello conteneva già esattamente questo cartello come mesh `Cube` (0.35×0.44×0.02 a (0.56,2.47,4.49)): **disattivato** quel `Cube` in `MODEL_Staccato` e sostituito col nostro `Model` (scala 0.175 → 0.35×0.435×0.017, centro world y 2.47, retro sul muro z 4.5, nessuna rotazione; il lato con la texture guarda la stanza, testo non specchiato). Materiali `MAT_EV_ExitSignModel_Body.mat` / `_Face.mat` (copie con emissione abilitata, stessa ragione dell'allarme) perché `EvacGuidedHighlights` pulsa `_EmissionColor` sui renderer del cartello.
- Il cartello **verde "uscita"** autorato nel modello (mesh `Cube.027` a (3.03,1.11,4.49), a 1.1 m da terra) **resta**: è decoro del modello e non è usato dal gameplay. `Cube.028` (4.42,1.11) e `Cube.087` (−2.91,1.45) sono altri piccoli pannelli del modello a muro, lasciati.
- **`SCALE_SCARICATE.fbx` NON usato**: gli annessi del modello contengono già pianerottoli e rampe grigliate (verificato con screenshot dall'interno dell'annesso nord; il pavimento visivo coincide col `Floor_StairsEmergency`). Importarlo avrebbe duplicato geometria. `Stairs_Emergency`/`Stairs_Internal` restano vuote (nessun gradino percorribile: il giocatore resta sul pianerottolo e raggiunge `ExitZone_*`).
- Nessun mesh del modello nelle zone di allarme e pulsante (controllato prima di montare): nessun doppione.
- **Da fare nel passo 8** (non fatto qui): `EvacGuidedHighlights.exitSignRenderers` → renderer `ExitSign_StairsEmergency/Model/INDICAZIONE_PORTA_EMERGENZA` (se si lascia l'autodiscovery di `Awake` includerebbe anche il vecchio `Visual/Panel` disattivato: innocuo ma meglio assegnare esplicitamente).
- Scena salvata dopo 7b (sempre in stato intermedio, non ancora giocabile fino al passo 8). Screenshot in `Temp/evac_shots/7b_*.png`.

#### Specifica originale 7b (per riferimento)
- `Alarm`: sostituire il graybox (`Base`, `Dome`) con `ULTIME_TEXTURE_E_MODELLI/ALLARME.fbx` (0.40×0.20×0.40, 54 tri); mantenere `pulseRenderers` valido (l'`AlarmSystem` fa pulsare dei renderer: puntarli ai renderer del nuovo modello).
- `Elevator_Buttons`: sostituire `Visual` con `.../PULSANTE.fbx` (2 mesh, 28 tri, pivot fuori origine) lasciando il `BoxCollider` 0.10×0.30×0.20 per il raggio.
- `ExitSign_StairsEmergency`: sostituire il rettangolo con `.../INDICAZIONE_PORTA_EMERGENZA.fbx` (2.0×2.48×0.10, da scalare molto). Nota: nel modello c'è già un cartello uscita verde autorato sul muro del corridoio e un cartello rosso sopra la porta di emergenza: decidere se tenerli o disattivarli per evitare doppioni. `EvacGuidedHighlights.exitSignRenderers` dovrà puntare ai renderer del cartello usato.
- `SCALE_SCARICATE.fbx` (3.62×10.43×4.93, scala root ~0, controllare): il modello contiene già scale autorate negli annessi (`Plane.015` nord, `Material2` est). Valutare se serve davvero; se sì, wrapper e misure come sopra.
- Procedura per ogni FBX: misurare i bounds reali con la root inclusa, trovare la rotazione (altezza su Y), wrapper `Leaf`, verificare con screenshot.

### 8 — Fix e ricollegamenti **[FATTO parziale 2026-11-27 — restano 3 punti, vedi "Esito 8"]**

#### Esito 8 (ripreso da sessione interrotta: la sessione precedente non aveva lasciato nulla in scena/codice)
- **Hinge**: modificato `Interaction/DoorInteractable.cs` (additivo, stesso schema di E16 in `EvacNpc`): `OnEnable`/`RigRegistration.Registered` → `Physics.IgnoreCollision(collider, CharacterController del rig)` per tutti i collider **solidi** figli della porta che NON sono in `blockingColliders` (cioè `Hinge*`, `Hinge_OpenLeaf`). Il `Blocker` resta solido a porta chiusa; il raggio mano colpisce ancora le Hinge. Compila; **verifica reale solo in Play (passo 9)**.
- **Riferimenti collegati** (scena salvata): `EvacGuidedHighlights` (director, npcRegistry, 3 piani, `Door_StairsEmergency`, `exitSignRenderers` = solo `ExitSign_StairsEmergency/Model/INDICAZIONE_PORTA_EMERGENZA`); `EvacDebugCommands` (alarm, 3 piani, 4 porte, `Elevator_Buttons`, 2 `ExitZone_*`).
- `EvacuationDirector.rigRoot` e `NpcTrailFollower.head`: NULL in editor ma risolti a runtime da codice (Director L737, Head lazy da E10) → nessuna azione.
- ID assunti `evac.plan.room1/room2/corridor` e `evac.door.stairs.internal`: coerenti con quelli già in scena; la conferma vera è lo smoke test (Y) nel passo 9.
- **NON fatto / da decidere**: (a) `EvacHintInput` **non esiste più** nel progetto (né `.cs` né `ScenarioInputBindings`; il rig ora è `SI81.Rig` in `SCN_Core`): l'input A/X per gli hint va ricostruito sul nuovo rig oppure delegato alla UI (`Director.RequestNextHint()` esiste, tasto debug B funziona); (b) `AlarmSystem.alarmClip` ancora vuoto (nessuna clip scelta); (c) `LabelAnchor` su `Door_StairsInternal` (opzionale).

#### Specifica originale 8
- **Hinge solide**: `Interaction/Hinge*` (BoxCollider non trigger) non sono in `blockingColliders` → restano impassabili anche a porta aperta. Fix: `Physics.IgnoreCollision(hinge collider, CharacterController del rig)` come in E16 (controllare dove E16 lo faceva; il raggio d'interazione deve continuare a colpire l'Hinge). Vale anche per `Door_StairsInternal` e per la porta di emergenza (qui le Hinge non ruotano più: restano nel piano del varco).
- **Riferimenti vuoti** (verificati in scena): `EvacGuidedHighlights` (`director`, `plans[0]`, `emergencyDoors[0]`, `exitSignRenderers[0]`, `npcRegistry` NULL); `EvacDebugCommands` (`alarm` NULL, `plans[0]`, `doors[0]`, `elevatorButtons[0]`, `stairsZones[0]`); `NpcTrailFollower.head` NULL; `EvacuationDirector.rigRoot` NULL (probabilmente assegnato a runtime: verificare). Collegare i 3 piani, le 4 porte, `Alarm`, `Elevator_Buttons`, le 2 `ExitZone_*`, i renderer del cartello.
- **`EvacHintInput`**: assente dalla scena, da ricreare/ricollegare.
- `AlarmSystem.alarmClip` vuoto: decidere l'audio (cercare nel progetto la clip usata in precedenza).
- Verificare gli ID assunti (`evac.plan.*`, `evac.door.stairs.internal`) con quanto si aspettano Director/evaluator/test (smoke test).
- Opzionale: `LabelAnchor` su `Door_StairsInternal` (la porta di emergenza lo ha, titolo "Apri scale di emergenza").

### 9 — Test in Play
Come da elenco nel "Piano di lavoro" (Y smoke 50/50, F5 200/200, F11 5/5, F12 10/10, O ALL PASS, J/T percorsi a mano, 0 errori console). Punti specifici da guardare:
- apertura/chiusura di ogni porta, in particolare lo scambio visual della porta di emergenza e il passaggio attraverso tutte le porte **aperte**;
- il validatore F5 vero (la replica in editor dava 252/252, ma ignora i `Awake`);
- raggiungibilità: piani, pulsante ascensore (estremità ovest), `ExitZone_*`, NPC (3+2) visibili e non dentro l'arredo;
- nessuna caduta; percorsi tra i tavoli (varchi 1.0–1.1 m) comodi in VR;
- tempi 180 s (Operational) / 120 s (Crisis): le distanze sono cambiate (percorso più corto/lungo?), valutare se ritararli. Il punteggio non cambia.

### 10 — Chiusura
- Misurare prestazioni su Quest: stima triangoli ≈ 39.9k (struttura) + 8 × 42.8k (tavoli) ≈ **383k** + modelli porte/piani; l'analisi iniziale stimava >570k, quindi il rischio è minore, ma va misurato (draw call, tri) e riportato in `EVAC_PERF_REPORT.md`. Eventuale versione semplificata di `TAVOLO` se serve.
- **Pulizia**: in scena restano `UFFICIO_ASSEMBLATO_UNITO` (z 11.6…23.5, non sovrapposto) e `SEGNALEVERDE` dell'E21: decidere se eliminarli. Verificare che non restino riferimenti ai vecchi oggetti.
- Salvare la scena, riportare l'editor su `SCN_Core`, aggiornare `EV_HANDOFF.md` (stato corrente + log E22) e questo file.

## Differenze rispetto all'analisi iniziale (sezioni sotto)
- Le pareti **esistono** nel modello (uffici/corridoio a z=2.25 con 2 varchi, divisorio a x=0): non andavano "aggiunte" come visual, servivano solo i collider.
- La struttura senza scrivanie pesa solo ~40k tri (non >570k): stima prestazioni da rifare.
- Le scrivanie del modello non erano in due mesh grandi: c'erano ~66 oggetti nel blocco desk + 4 avanzi (basamenti/basi sedia), tutti eliminati.
- Il `TAVOLO` include la sedia; i box dei tavoli coprono anche la sedia.
- Le maniglie `Cylinder.120/121` erano in realtà battenti di porta aperti (stessa mesh di `PORTA_UFFICIO`), con varchi a x ±3.0 (non −2.4/3.6).
- Il varco corridoio→annesso est è nella parete x=6 (z 2.8…4.05), non una porta sulla parete sud dell'annesso.
- La rotta R2 sta a x=5.3 (non 5.45); l'ascensore è a ovest.

## Note tecniche per riprendere
- Strumenti usati: MCP `unityMCP` (`execute_code`, `manage_scene`, `manage_camera`) e `filesystem`. `execute_code` compila con **codedom (C# 6)**: niente funzioni locali, tuple, `out var`; usare `System.Action/Func` per gli helper, qualificare `UnityEngine.Object` e `UnityEditor.SceneManagement.EditorSceneManager`.
- Le istanze di prefab in scena: usare `Undo.RecordObject(...)` prima di modificare (registra gli override); i figli non si distruggono (usare `SetActive(false)`); aggiungere nuovi figli è consentito.
- Screenshot con `manage_camera` (`game_view` con `view_position`/`view_rotation`) salvati in `C:\Unity-Projects\Sicurezza-Immersiva-81\Temp\evac_shots` (fuori da `Assets`). Vista interna utile: posizione (0,5.6,−4.3), rotazione (48,0,0). Per le pareti: da (−1.8,1.6,−0.9) rot 0 (ufficio 1) e da (0.575,1.6,2.6) rot (−4,0,0) (porta emergenza).
- Il validatore (`EvacFireLaneValidator`) considera `Wall_*` non trigger e ignora `Lintel` e i segmenti che li toccano (euristica `TouchesLintel`, preesistente: per questo tutta la parete uffici/corridoio non conta come ostacolo).
- `Awake` non gira in editor: la verifica dello stato iniziale delle porte e del validatore reale è solo in Play.
- Nessun file è stato scritto fuori da `G_Tiziano/`; nessun C# modificato.
Data analisi: 2026-11-26. Da leggere insieme a `EV_HANDOFF.md`.

## Obiettivo
Demolire la scena attuale e ricostruirla con i modelli in `Assets/_Project/Shared/Art/Models/UFFICIO/`:
- struttura: `UFFICIO_STACCATO.fbx`
- 4 `TAVOLO.fbx` per stanza (2 stanze = 8 tavoli)
- NPC 3+2 come da handoff (prefab `EvacNpc`, già con TIZIO, spawn a runtime da `NpcRegistry`)
- gameplay e punteggio INTATTI (nessuna modifica ai C#, SO, Director, evaluator)

## Regole da rispettare
- Nessuna scrittura fuori `G_Tiziano/`. I FBX in `Shared` si referenziano, NON si modificano (né file né import settings).
- Prefab/scene/materiali nuovi solo in `G_Tiziano/`.
- Nessun git, nessuna UI/animazione, Chemical Alert intoccato.
- Backup prima di demolire: copiare `Level/Scenes/SCN_Evacuation.unity` in `SCN_Evacuation_PRE_STACCATO.unity` (stessa cartella).
- `Debug` è un namespace: usare `using UDebug = UnityEngine.Debug;`.

## Cosa ho trovato sui modelli

### UFFICIO_STACCATO.fbx (109 MB)
- 122 mesh, 571.134 triangoli, scala root 1, **nessun collider**.
- Bounds locali: x −6…8.1, y −0.13…7.15, z −4.6…7.24 (14.1 × 7.3 × 11.8 m).
- Dal render dall'alto (soffitto tagliato): sala unica ~12 × 9 m (x −6…6, z −4.55…4.55, coerente con `UFFICIO_ASSEMBLATO/PARETI.fbx` 12.1 × 9.2).
  - Annesso **nord** (~3.7 m largo, z ~4.5…7.2) con scala (griglia scura) e `PORTA_EMERGENZA_CHIUSA` a z 7.0–7.24.
  - Annesso **est** con pavimento in legno e scale.
  - 8 scrivanie con PC/sedie/monitor **già dentro la mesh**, in due blocchi da 4: mesh `Cube.009` (x −4.5) e `Cube.086` (x 1.6), 121.436 tri ciascuna. Righe a z ≈ −0.4 e −2.6; colonne x ≈ −4.5, −1.9, 1.5, 4.2.
  - Maniglie porta (`Cylinder.120/121`) a (−2.4, 1.0, 2.23) e (3.6, 1.0, 2.23): suggeriscono una parete uffici/corridoio a z≈2.2 con porte ai centri delle due metà.
  - **Nel render NON si vede nessuna parete divisoria** (né tra i due uffici né tra uffici e corridoio): va aggiunta (box collider ± visual).
- Da eliminare dall'istanza in scena (serve unpack): le 2 mesh grandi delle scrivanie e gli oggetti associati (`Cylinder.003/004/005/111` 41k tri, `NurbsPath.002…011` sedie, `Plane.*` monitor/tastiere, `Cube.010…025/059/060` ecc.). Verificare a vista cosa è desk e cosa è struttura.

### TAVOLO.fbx (47 MB)
- 1 mesh, 42.823 tri. Dimensioni reali **1.91 × 1.43 × 1.06 m**.
- Root con scala (−0.03, −0.19, −0.01) e rotazione (90, 270, 0); il pivot è lontano dall'origine (bounds min (−5.69, 0, −2.90), max (−3.78, 1.43, −1.84)).
- Serve un prefab wrapper `EvacDesk.prefab` in `G_Tiziano/Level/Prefabs/Evacuation/`: pivot centrato a terra, BoxCollider solido (~1.9 × 0.8 × 1.0, la parte alta è PC/monitor), batching static dove possibile.

### Altri pezzi utili (tutti in `Shared/Art/Models/UFFICIO/`)
| Uso | File | Note |
|---|---|---|
| Allarme | `ULTIME_TEXTURE_E_MODELLI/ALLARME.fbx` | 0.40×0.20×0.40 m, 54 tri |
| Pulsante ascensore | `.../PULSANTE.fbx` | 2 mesh, 28 tri, pivot fuori origine |
| Piani evacuazione | `.../FOGLI.fbx` | 1.05×1.33×0.21 m |
| Cartello uscita | `.../INDICAZIONE_PORTA_EMERGENZA.fbx` | 2.0×2.48×0.10 m (scalare) |
| Porta emergenza | `.../PORTA_EMERGENZA_CHIUSA.fbx` / `_APERTA.fbx` / `_APERTA_2.fbx` | 1.20×2.17 m, root rot 90° |
| Scale | `.../SCALE_SCARICATE.fbx` | 3.62×10.43×4.93 m, scala root ~0 → controllare |
| Porta ufficio | `UFFICIO/PORTA/PORTA_UFFICIO.fbx` | 1.06×2.0 m |
| Struttura a pezzi | `UFFICIO/STRUTTURA/*`, `UFFICIO_ASSEMBLATO/PARETI.fbx` | alternativa per pareti/tetto |
| NPC | `ULTIME_TEXTURE_E_MODELLI/TIZIO.fbx` | SkinnedMeshRenderer, già nel prefab `EvacNpc` |
Ogni FBX ha root con scala/rotazione/pivot particolari: misurare sempre i bounds reali con la matrice `localToWorld` includendo la root, e usare wrapper prefab.

## Mappatura proposta (DA CONFERMARE con l'utente)
| Parte del modello | Ruolo nel gioco |
|---|---|
| Metà sud sala, lato sinistro (x<0) | Ufficio 1 — 4 `EvacDesk` |
| Metà sud sala, lato destro (x>0) | Ufficio 2 — 4 `EvacDesk` |
| Parete a z≈2.2 (da aggiungere) | Divisorio uffici/corridoio con 2 porte (`PORTA_UFFICIO`) |
| Fascia nord della sala (z≈2.2…4.55) | Corridoio (piano corridoio, allarme) |
| Annesso nord | Scale di emergenza (porta + `SCALE_SCARICATE`) |
| Annesso est | Scale interne (**da confermare: potrebbe essere l'ascensore**) |
| Ascensore | `PULSANTE` + blocco pieno sul lato est del corridoio |
Domanda aperta all'utente: confermare la mappatura, in particolare cosa è l'annesso est.

## Cosa resta uguale (da NON toccare)
Tutti i C#; i nomi e il numero dei marker (49); 3 piani, porte (R1, R2, scale emergenza, scale interne), allarme, pulsante ascensore, 2 zone d'uscita (`StairsExitZone`), 10 `FIRESLOT`, 12 `NPCSLOT_*`, rotte `ROUTE_R1/R2`, Director/Registry/FireManager/Sensor/GuidedHighlights/HintInput/DebugCommands, SO e config.

## Cosa va rifatto
- Coordinate di muri, pavimenti, scale, collider (tutti box: il modello non ha collider; **mai MeshCollider su 571k tri**).
- Tutti i marker (`ScenarioMarkers`, 49) riportati sulle coordinate del nuovo modello, mantenendo i nomi.
- Istanze `FireSlots` (10) e marker `FIRESLOT_*` coerenti tra loro (oggi 06–09 sono diversi: i marker sono vecchi).
- `NPCSLOT_*`: 6 per ufficio, vicino ai tavoli ma NON dentro i collider di tavoli/sedie.
- Porte/piani/pulsante: riposizionare i prefab esistenti (`EvacDoor_*`, `EvacPlan`, `EvacElevatorButton`, `EvacAlarm`, `EvacExitSign`) e sostituire i Visual coi modelli nuovi, lasciando i collider di raycast.

## Rischi
1. **Corsia fuoco ≥ 1.2 m**: corridoio ~2.3 m (vs 3.6 m attuali) con focolai da 1.2 m di diametro → molte combinazioni scartate; il validatore potrebbe non dare 200/200. Potrebbe servire riposizionare/ridurre i `FIRESLOT` o allargare il corridoio (spostando il divisorio).
2. **Prestazioni Quest**: 8 TAVOLO ≈ 343k tri; la struttura da sola è molto pesante. Togliendo le scrivanie incorporate (~243k) il totale resta > 570k (E13). Valutare versione semplificata del tavolo o LOD. Rimisurare con `EVAC_PERF_REPORT.md`.
3. **Tempi**: 180 s (Operational) e 120 s (Crisis) tarati sulle distanze attuali. Il punteggio non cambia, la difficoltà percepita sì.
4. FBX da 109 MB e 47 MB: non duplicarli in `G_Tiziano` (E11 aveva copiato l'FBX, evitarlo) — referenziare quelli in `Shared`.

## Problemi di collisione della scena attuale (analisi 2026-11-26)
Da risolvere comunque nella ricostruzione (alcuni spariscono, altri vanno evitati):
1. **Corridoio senza pavimento**: mancavano `Floor_Corridor`, `Wall_Corridor_South_Seg0/1`, `Wall_StairsInt_West_Seg0`, `Wall_Elevator_East_Seg0`. Il rig ha gravità (`RigLocomotion`, CC h1.8 r0.3 step0.3) → si cade. 12 marker senza terreno.
2. **`Interaction/Hinge*` solide**: non sono in `blockingColliders` di `DoorInteractable` → porte aperte comunque impassabili (testato con capsule cast su R1, R2, scale emergenza). Fix: `Physics.IgnoreCollision(hinge, CharacterController)` come in E16 (il raggio le colpisce ancora, il rig passa).
3. Il modello ufficio non aveva collider (mesh `Cube.032` 569k tri) e solo una fascia era nell'area giocabile.
4. Scena senza: 3 `EvacuationPlanInspectable`, `AlarmSystem`, `EvacHintInput`, `Door_StairsInternal` (da ricreare/ricollegare).
5. `NPCSLOT_R2_01–04`, `R1_01`, `R1_03` dentro collider di sedie/scrivanie; `POS_Plan_R1/R2` e `POS_ElevatorButton` dentro muri; marker `FIRESLOT_06–09` ≠ istanze.
6. Minori: muri laterali che sporgono nel corridoio (z 8.1 vs 8.66), fessura 20 cm tra pareti, vuoto 0.5 m al pianerottolo scale interne, aperture porte 0.90 m (CC 0.6 + skin), prop porta fuori posto (`Cylinder.120/121(Clone)`, `NurbsPath.007(Clone)`).
Regola per la nuova scena: ogni marker con terreno sotto e senza overlap capsula (r 0.28, h 0.35–1.5) con collider solidi; porte percorribili da aperte.

## Piano di lavoro (ordine consigliato)
1. Leggere `EV_HANDOFF.md`, `EVAC_TECHNICAL_DOCS.md`, `REUSE_MAP.md`; confermare la mappatura con l'utente.
2. Backup scena → `SCN_Evacuation_PRE_STACCATO.unity`.
3. Istanziare `UFFICIO_STACCATO.fbx` (referenza a `Shared`), unpack, eliminare le scrivanie incorporate; verificare con render (PreviewRenderUtility o `manage_camera`).
4. Creare `EvacDesk.prefab` (wrapper di `TAVOLO.fbx`, pivot centrato, BoxCollider) e piazzare 4+4 tavoli.
5. Ricostruire i collider `Environment` come box dal modello (pavimenti incl. corridoio, pareti con varchi porta, scale, ascensore, divisorio uffici) in modo che il fire lane validator li veda.
6. Riposizionare i 49 marker (stessi nomi) + istanze `FireSlots`; poi `ScenarioObjects` (porte, piani, allarme, pulsante, cartello, zone uscita).
7. Sostituire i Visual con i modelli nuovi (`PORTA_*`, `ALLARME`, `PULSANTE`, `FOGLI`, `INDICAZIONE_*`, `SCALE_SCARICATE`).
8. Fix `Hinge` (IgnoreCollision col CC) e ricollegare i riferimenti del Director (piani, allarme, porte, `EvacHintInput`).
9. Test in Play (Game View con focus), da `SCN_Core` → Home → missione, oppure da `SCN_Evacuation`:
   - Y → smoke test, atteso `50/50 ALL PASS` (nel baseline E21 era 38/50 per riferimenti mancanti: devono tornare a posto)
   - F5 → `Fire lane: 200/200 seed validi, 0 scartati`
   - F11 → `E10 replay games: 5/5 PASS`
   - F12 → `E10 reset stress: 10/10 PASS`
   - O → `SelfCheck TOTAL: ALL PASS`
   - J/T → raggiungere a mano piano, porte, NPC, pulsante; passare da ogni porta aperta (nessun blocco); nessun punto in cui si cade.
10. Misurare prestazioni (draw call, triangoli) e aggiornare `EVAC_PERF_REPORT.md`; salvare la scena, riportare l'editor su `SCN_Core`; aggiornare `EV_HANDOFF.md` (stato corrente + log E22).

## Criteri di accettazione
- Punteggio/esiti identici: Success/Partial/Failure e log LG01–LG11 invariati.
- Smoke 50/50, F5 200/200, F11 5/5, F12 10/10, O ALL PASS, 0 errori console.
- 4 tavoli per ufficio, 3+2 NPC visibili e non dentro l'arredo, nessuna caduta, porte aperte attraversabili.
- Nessun file modificato fuori `G_Tiziano/` (a parte lo stato `isDirty` della scena e i `.meta` generati da Unity).
