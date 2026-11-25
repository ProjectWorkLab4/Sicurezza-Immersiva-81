# 10 · Electric Shock (Folgorazione) — LITE

Gruppo **G_Ludovica**. Implementazione della specifica `10_Electric_Shock_LITE_specifica_IT.md` (consegna 27/11/2026).
Stato: logica completa, **collegato alla UI comune** (missione 10 nel catalogo, Home → Mission Brief → scenario → Debrief),
**modelli segnaposto**, interazioni VR in attesa del VR Interaction System.

## Cartelle

| Percorso | Contenuto |
| --- | --- |
| `SCN_ElectricShock.unity` | Scena dello scenario (niente camera, rig, Canvas: guida UI §6); `MNG_Scenario` con `ElectricShockScenario` |
| `SO_Mission_ElectricShock.asset` | Missione 10 della UI comune (testi, obiettivi, criteri, livelli, icona); è nel `SO_MissionCatalog` al posto di quella d'esempio |
| `Scripts/` | Logica dello scenario (`ElectricShockScenario` deriva da `SI81.UI.ScenarioController`), pericoli, attrezzature, personaggi |
| `Scripts/Editor/ElectricShockSceneBuilder.cs` | Menu `SI81/G_Ludovica/Electric Shock/Costruisci scene (graybox)` (rigenera scena e missione) e `.../Registra nel catalogo e nelle Build Settings` |
| `Data/SO_ElectricShockSettings.asset` | Tempi e parametri per livello (specifica §5, §7, §9) |
| `Data/ElectricShock_DebriefPhrases.json` | Frasi del debrief LG-01…26 e percorso corretto (si modificano senza toccare il codice) |
| `Data/NAV_ElectricShock.asset` | NavMesh della scena (agente di default, nessuna modifica a ProjectSettings) |
| `Art/Materials/` | Materiali segnaposto `MAT_Placeholder*`, `MAT_VFX_Placeholder*` |
| `Temp/` | **[TEMP]** rig e interazioni temporanee (`TempRig`, `TempHand`, `TempInteractable`): oggi usati solo dai test, da sostituire con il VR Interaction System |
| `Tests/EditMode`, `Tests/PlayMode` | Test automatici (Window → General → Test Runner) |
| `*.asmdef` | Assembly propri: `SI81.Ludovica.TempCore`, `SI81.Ludovica.ElectricShock`, `.Editor`, `.Tests.*` (riferiscono `SI81.UI`) |

> Attenzione: rieseguire il menu del builder **riscrive** la scena. Le modifiche fatte a mano nella scena vanno perse.

## Come provarlo

**Editor, solo lo scenario:** apri `SCN_ElectricShock` e premi Play: `SCN_Core` (rig, menu, HUD) si carica da solo e lo scenario parte
in Guided (campo `editorLevel` su `MNG_Scenario`). Obiettivi, fasi, suggerimenti, avvisi, sottotitoli, etichette nel mondo,
esito e Debrief sono quelli della UI comune.

**Percorso completo:** apri `Home/Level/Scenes/SCN_Core` → Play → Home → card **10 · Electric Shock** → Mission Brief → Avvia.

**Quest:** build normale del progetto (Build Settings: `SCN_Core` all'indice 0, poi gli scenari).

**Interazioni:** il rig di `SCN_Core` per ora non ha camminata né afferrare (arrivano con il VR Interaction System, sul `main`).
Finché non c'è, nello scenario si guarda la scena e gli eventi (colleghi, accensione, PM-2, esplosione) procedono da soli;
le azioni del giocatore sono verificate dai test Play Mode con il rig temporaneo di prova.

## Interazioni previste (oggi nel rig temporaneo, poi nel VR Interaction System)

| Oggetto | Azione |
| --- | --- |
| PB-01 arresto di emergenza | grilletto toccandolo |
| Estintori (rack RK-02) | grip = prendi · A/X = leggi etichetta · grilletto = spruzza |
| Barriere (rack RK-01) | grip = prendi · lasciale vicino ai segni gialli sul varco DR-03 (si agganciano) |
| Radio RD-01 | grilletto = chiama il 112 |
| Collega VT-01 | A/X = coscienza e respiro · grilletto breve = posizione · grilletto tenuto = trascina · restare entro 2 m per 10 s = "resta con lui" |
| PM-1 | grilletto tenuto 3 s = ti segue; portalo al cerchio verde SAFE-01 |
| Colleghi, PM-2 | B/Y (parla) entro 15 m |

Toccare cavo o collega con la corrente presente = folgorazione (errore critico). Il tasto Menu sinistro è la Pausa della UI: lo scenario non lo usa.

## Test automatici

Test Runner → **EditMode** (8 test, meno di 1 s) e **PlayMode** (12 test, circa 75 s, con `SCN_Core` e la UI comune caricate; il tempo di gioco scorre 3 volte più veloce).
Da riga di comando: `unity command test run --mode editor --filter SI81.Ludovica.ElectricShock.Tests.EditMode --filter-type Assembly`
e `unity command test run --mode playmode --async-tests true --filter SI81.Ludovica.ElectricShock.Tests.PlayMode --filter-type Assembly`.

- **EditMode:** pesi provvisori delle 5 dimensioni, registro senza doppioni, frasi per tutti gli ID LG-01…26 (senza "SPI"),
  formato delle righe di debrief e taglio a 10 righe, valori dei livelli (§9), regole della guida UI sulla scena
  (niente Camera, AudioListener, Canvas, EventSystem; un solo `MNG_Scenario` con la missione giusta; nessuno script mancante),
  missione valida (§6) e registrata: una sola missione con Id 10 nel catalogo, scena nelle Build Settings con `SCN_Core` all'indice 0.
- **PlayMode:** percorso corretto → Positivo; collega toccato a corrente inserita → Negativo (LG-08); cavo pericoloso solo con corrente;
  acqua → Negativo (LG-11); spruzzo da vicino; colleghi ignorati → Negativo (LG-15); grido → i colleghi escono (LG-14);
  PM-2 alla barriera chiusa → entra quando è sicuro, barriere che si aprono e richiudono, PM-1 portato via da PM-2;
  varco aperto e zona non sicura → Negativo (LG-23); giocatore nella vampata → Negativo (LG-17); collega trascinato fuori → Parziale (LG-16);
  PM-1 gira attorno al generatore (NavMesh).

## Cosa è verificato

Oltre ai test automatici, prove in Play Mode (Editor, rig desktop, azioni chiamate da script):
- percorso corretto in Guided → **Positivo**, punteggio 96/100, 10 righe di debrief corrette;
- PM-2 si ferma alla barriera chiusa con zona non sicura, entra quando la zona è sicura (le barriere si aprono di 80° e si richiudono),
  porta PM-1 al punto sicuro, poi va dal collega; il livello aspetta che il giocatore finisca di assistere → Positivo;
- spruzzo da vicino (ugello dentro la zona del fuoco) → fuoco spento;
- esplosione con il collega trascinato fuori dal raggio durante l'avviso → LG-16 corretto, nessun ferito → **Parziale**;
- collega toccato a corrente inserita (Guided) → Negativo (LG-08);
- estintore ad acqua (Operational, tempo 240 s) → Negativo (LG-11);
- nessuna azione in Crisis (tempo 180 s) → i colleghi toccano la vittima → Negativo (LG-15);
- validatore della UI comune (`SI81/Scenari/Valida scenario`) su missione e scena: **0 errori, 0 avvisi**.

**Non verificato:** tutto ciò che riguarda il visore (input reali, altezza, mani, leggibilità visiva dei segnaposto, volume dei suoni)
e la build APK. Checklist per il visore: [CHECKLIST_Quest.md](CHECKLIST_Quest.md).

## Limiti noti

- Gli NPC camminano sulla NavMesh (girano attorno a pareti, rack, generatore, collega a terra e fuoco acceso), ma non hanno fisica né animazioni (capsule che scivolano); il giocatore non li urta (collisioni ignorate di proposito).
- Le barriere si aprono da sole per far passare i colleghi che escono e PM-2 autorizzato; per la logica il varco resta chiuso.

## Modelli in scena e segnaposto rimasti

I modelli veri stanno in `Shared/Art/Models` e sono messi in scena dal builder (istanze collegate all'FBX, nome = nome dell'asset;
le scale sono corrette **in scena**, non nelle impostazioni di importazione). Gli oggetti ancora da sostituire si chiamano `PLACEHOLDER_*`:
collider e script stanno sulla radice, quindi basta cambiare i figli.

| Radice | In scena | Note |
| --- | --- | --- |
| `GEN01_Generator` | `MESH_Folgorazione_Generatore` | ruotato di 90°; collider dal modello; area NavMesh "Not Walkable" (è alto 0,78 m, sotto lo step di 0,75 m sarebbe scalabile) |
| `PB01_EmergencyStop` | `Chiave (Interagibile)` del generatore | **la chiave è l'arresto di emergenza**: col grilletto gira di 90° come la sua animazione |
| `EXT1/2/3_*` | `MESH_Folgorazione_Estintore` ×2 in scena (≈ 0,55 m) | il tipo resta segnato dalla banda colorata `PLACEHOLDER_ExtinguisherLabel` (acqua blu, polvere bianca, CO2 nera) finché non ci sono etichette |
| `BAR1…5_Barrier` | `MESH_Magazzino_Barriera` (0,75 m) | **5 pezzi** e 5 slot sul varco DR-03 (z 6,4 … 9,6) |
| `RD01_Radio` | `MESH_Magazzino_Radio` ×0,385 in scena (≈ 0,22 m) | |
| rack estintori | `MESH_Folgorazione_CartelloEstintore` ×4,4 in scena (≈ 0,3 m) | cartello sopra il pannello del rack |
| `DEC_Decor` | 2 × `MESH_Folgorazione_Barile` | solo arredo (non sostituisce la tanica) |
| `ENV_Graybox` | `PLACEHOLDER_*` | **manca**: stanza / capannone |
| `FC01_FuelTank` | `PLACEHOLDER_FuelTank` | **manca**: tanica di carburante (il barile non la sostituisce) |
| `PZ01_FuelPool` | `PLACEHOLDER_FuelPool` | **manca**: decal pozza di benzina |
| `CB01_LiveCable` | LineRenderer | facoltativo: parte dalle prese del generatore |
| `PLACEHOLDER_Rack*` | rack | **manca**: rack estintori, rack barriere/radio |
| `VT01_Victim`, `BY01/02/03_Colleague`, `PM1/PM2` | capsule | **mancano**: tutti i personaggi |
| `SAFE01_SafePoint`, `AP02_AssemblyPoint` | dischi verdi | facoltativo: segnaletica a terra |
| `VFX_*`, `SFX_*` | particellari e suoni generati da codice | Common Core (fuoco, fumo, esplosione, suoni) |

Elenco completo con priorità: [ElectricShock_AssetCheck.pdf](ElectricShock_AssetCheck.pdf).

## [TEMP] Cosa sostituire quando arrivano i sistemi comuni

| Sostituto temporaneo | Sistema definitivo | Come |
| --- | --- | --- |
| `TempRig`, `TempHand`, `TempInteractable` (base di estintori, barriere, radio, PB-01, vittima, PM-1, cavo) | VR Interaction System sul rig di `SCN_Core` | Ricollegare grab / use / inspect agli stessi metodi; la logica dello scenario ascolta solo gli eventi C# degli oggetti. `PlayerLocator` è l'unico punto che sa dov'è il giocatore |
| `BarrierPanel` / `BarrierSnapLine`, `EmergencyRadio`, `InjuredPerson` | prefab barriera + snap zone, radio + dialogo 112, sistema "ferito" dello scenario 07 | Stessi eventi (`ClosedChanged`, `Called`, `Checked`…) |
| `ChoiceLog` + `DebriefPhraseTable` | `RecordChoice` della UI comune (previsto, non ancora implementato) + Registro delle scelte dello Score Manager | Stesso formato dati; oggi le righe vanno a `EndMission` con prefisso `+ ` / `- ` / `! ` |
| Pesi delle dimensioni (base 60, +20/−20/−25/−60) | Game System comune | Valori provvisori in `ChoiceLog.ComputeDimensions`; il punteggio sicurezza è la media delle 5 dimensioni (`EndMission(..., -1, ...)`) |

## Scelte e semplificazioni da confermare

- Il collega respira **sempre** (modifica 1): niente RCP né DAE.
- La pozza è nascosta dal generatore se guardata da P0: per osservarla (LG-01) bisogna spostarsi. Avviso a due canali: sottotitolo "[Odore di benzina]" entro 9 m.
- Le barriere bloccano solo PM-2 (logica); i colleghi che evacuano passano comunque dal varco.
- Con il varco chiuso e la zona non sicura PM-2 aspetta; appena la zona è sicura (corrente tolta, fuoco spento) entra da solo.
- Dopo l'esplosione lo scenario si chiude sempre (se nessuno è ferito l'esito è Parziale).
- Distrattori e allarme di fondo sono solo sottotitoli; i suggerimenti seguono §9 (Guided illimitati dopo 20 s, Operational max 3 dopo 45 s, Crisis nessuno).
- ID aggiunti oltre la tabella §11.1: LG-22 aria nociva, LG-23 PM-2 seconda vittima, LG-24 cavo toccato, LG-25 collega nella vampata, LG-26 altre persone nella vampata.
