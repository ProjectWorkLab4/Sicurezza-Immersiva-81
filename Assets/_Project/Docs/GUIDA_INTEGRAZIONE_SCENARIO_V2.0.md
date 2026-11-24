# Guida di integrazione scenario · UI comune SI81 — V2.0

Caso reale documentato: **`SCN_ForkliftChaos` · missione 07 "Forklift Chaos"** (gruppo **G_Ludovica**). Owner: **Team Ludovica**. Versione guida: **2.0** (23 novembre 2026).

Questa V2 si basa su `GUIDA_INTEGRAZIONE_SCENARIO.md` (guida 0.6, allineata a kit scenario v0.6.0 e UI comune v0.7.6) e aggiunge i casi che quella guida non copriva:

- la missione era **già nel catalogo** e non andava creata né duplicata;
- la card Home era **"Non disponibile"** finché scena e Build Settings non erano allineati;
- la scena è stata **rinominata mantenendo il GUID**;
- per una prima prova statica è bastato il **`ScenarioController` di base**, senza sottoclasse;
- i prefab mancanti sono stati sostituiti da **placeholder nominali**;
- è stato aggiunto un **trigger temporaneo A/E** per chiudere la missione;
- sono riportati i risultati reali di validazione, test del flusso e build Android.

> **La guida 0.6 resta il riferimento per le regole generali** (API, livelli, punteggio, naming, cosa non fare, validatore, prompt AI). Questa V2 non la sostituisce e **non va modificata**: la integra con i passaggi effettivamente eseguiti. In caso di dubbio su una regola generale, vale sempre la 0.6; chiedi al Team Ludovica prima di procedere.

**Indice V2**

| § | Argomento | Quando ti serve |
| --- | --- | --- |
| 1 | Cosa cambia rispetto alla guida 0.6 | per capire subito i casi aggiunti |
| 2 | Stato di partenza | prima di toccare i file |
| 3 | Passaggi reali (1–7) | mentre lavori |
| 4 | Validazione | prima di consegnare |
| 5 | Test del flusso completo | prima di consegnare |
| 6 | Build Quest e errore Android SDK | quando la build si blocca |
| 7 | Cosa non è stato fatto e cosa resta | alla consegna |
| 8 | Checklist V2 | alla consegna |
| 9 | Prompt per assistente AI (caso reale) | se usi un assistente |

---

## 1. Cosa cambia rispetto alla guida 0.6

| Caso non coperto dalla 0.6 | Cosa fare nella pratica |
| --- | --- |
| La missione 07 esiste già in `SO_MissionCatalog` | **Non** usare `SI81/Scenari/Nuovo scenario...` e **non** duplicare `SO_Mission_*`: controlli solo che `Id`, `SceneName` e i dati siano giusti. Vedi §3.2. |
| La card in Home dice "Non disponibile" | Non è un problema di Home: significa che la scena richiesta da `SceneName` non è attiva nelle Build Settings. Vedi §3.3. |
| La scena si chiama `SCN_Forklift_Chaos` (con underscore) | Rinominala in `SCN_ForkliftChaos` e **preserva il GUID** del `.meta`. Vedi §3.1. |
| Non c'è ancora gameplay, solo oggetti statici | Lascia il componente base `SI81.UI.ScenarioController` su `MNG_Scenario`; la sottoclasse arriva con la logica vera. Vedi §3.4. |
| Mancano 4 prefab di arredi | Metti placeholder con gli **stessi nomi** degli originali, così non perdi i riferimenti. Vedi §3.5. |
| Serve provare l'uscita dalla missione prima del gameplay | Aggiungi un trigger **temporaneo** che chiami `EndMission` con A / E, e rimuovilo dopo. Vedi §3.7. |
| In build Android compaiono errori SDK strani | Non è colpa della scena: è un difetto noto di `com.unity.collab-proxy`. Vedi §6. |

---

## 2. Stato di partenza

Prima delle modifiche:

- `SO_Mission_ForkliftChaos` era **già nel catalogo** con `Id: 07` e `DesignerNote` che iniziava con `(esempio)`.
- La scena si chiamava `SCN_Forklift_Chaos`, era fuori dalla struttura del gruppo e **non era nelle Build Settings**.
- Esisteva un `MNG_CoreRig` duplicato dentro la scena del scenario (il rig corretto è solo quello di `SCN_Core`).
- Alcuni prefab di muletti e pallet erano referenziati ma mancanti.
- La card 07 in Home era cliccabile solo come voce, con stato "Non disponibile".
- Il progetto aveva già la UI comune, `SCN_Core`, il validatore e il kit scenario: **nulla di tutto questo è stato modificato**.

---

## 3. Passaggi reali

### 3.1 · Metti in ordine i file e rinomina la scena senza perdere il GUID

1. Sposta le cartelle del gruppo sotto `Assets/_Project/Groups/G_Ludovica/Assets/`:

   - `Art/`
   - `Level/`
   - `Scenes/`

   Percorso finale della scena: `Assets/_Project/Groups/G_Ludovica/Assets/Scenes/SCN_ForkliftChaos.unity`.

2. **Electric Shock: spostata solo la cartella.** Nessun contenuto, script o impostazione è stato cambiato. Se lavori su più scenari, tieni i progetti separati.

3. Elimina il **`MNG_CoreRig` duplicato** presente nella scena dello scenario. Il rig (`OVRCameraRig`, controller, raggi) deve restare **uno solo**, dentro `SCN_Core`. È la regola numero uno della guida 0.6 (§6).

4. Rinomina la scena da **`SCN_Forklift_Chaos`** a **`SCN_ForkliftChaos`** dalla finestra **Project** di Unity (non da Esplora file). Controlla che il GUID non cambi:

   - nel file `SCN_ForkliftChaos.unity.meta` deve restare `guid: e8459afe76c1b064d90d37202a1e41a8`;
   - lo stesso GUID deve comparire in `ProjectSettings/EditorBuildSettings.asset`.

   > Se rinomini il file da Esplora file, rinomina **anche il `.meta`**: senza `.meta` Unity genera un GUID nuovo e la voce nelle Build Settings e la `SceneName` della missione non puntano più alla scena. In quel caso la card torna "Non disponibile".

### 3.2 · Non creare la missione: era già nel catalogo

La guida 0.6 (§3 passo 1) dice di usare `Nuovo scenario...`. **Qui non si usa**, perché la missione 07 esiste già:

- file: `Assets/_Project/Home/UI/Data/Missions/SO_Mission_ForkliftChaos.asset`;
- `Id: 07`, `Title: Forklift Chaos`, `Complexity: 3`;
- `SceneName: SCN_ForkliftChaos` — già corretto dopo la rinomina del §3.1;
- 4 obiettivi (`Resta fuori dalla corsia`, `Segnala il carico instabile`, `Fai fermare il mezzo`, `Delimita la zona di rischio`);
- 3 criteri (Positivo / Parziale / Negativo).

Cosa **non** fare:

- non eseguire `Nuovo scenario...` (avrebbe dato "numero già usato" o creato un duplicato);
- non duplicare `SO_Mission_ForkliftChaos`;
- non togliere il marcatore **`(esempio)`** da `DesignerNote`: va lasciato invariato, come richiesto. Il team proprietario lo sostituirà quando arriverà il contenuto definitivo.

### 3.3 · Sbloccare la card "Non disponibile"

La card Home non dipende da codice scritto a mano: diventa disponibile quando la scena dichiarata in `SceneName` **esiste ed è attiva nelle Build Settings**.

Abbiamo aggiunto la scena in `File > Build Profiles > Scene List`, **subito dopo `SCN_Core`**, senza toccare la UI comune:

| Indice | Scena | Stato |
| --- | --- | --- |
| 0 | `Assets/_Project/Home/Level/Scenes/SCN_Core.unity` | attiva (non toccare) |
| 1 | `Assets/_Project/Groups/G_Ludovica/Assets/Scenes/SCN_ForkliftChaos.unity` | attiva (aggiunta qui) |
| 2 | `Assets/_Project/Home/Level/Scenes/Mock/SCN_MockScenario.unity` | attiva |

Dopo questo passaggio, `CanLaunch("07")` passa da `False` a `True` e la card 07 diventa cliccabile. Nessun file in `Assets/_Project/Home/` è stato modificato.

### 3.4 · Spawn point e `MNG_Scenario`

Nella scena `SCN_ForkliftChaos`:

- creato **`MNG_SpawnPoint`** in posizione **`(30, 0, 10)`**;
- `MNG_Scenario` configurato con:

  | Campo | Valore |
  | --- | --- |
  | `Mission` | `SO_Mission_ForkliftChaos` |
  | `Spawn Point` | `MNG_SpawnPoint` |
  | `editorLevel` | `Guided` |
  | `loadCoreInEditor` | `True` |

- **Mantenuto il componente base `SI81.UI.ScenarioController`**, senza creare una sottoclasse.

> Perché la base? Lo scenario è statico (muletti e pallet fermi, nessun gameplay) e serve solo a verificare l'integrazione Menu → Scenario → HUD → Debrief. Appena arriva la logica vera, segui la guida 0.6 §3 passo 5: aggiungi la tua classe che eredita da `ScenarioController`, trascina la stessa missione e lo stesso spawn point, poi rimuovi il componente base.

Premere **Play direttamente nella scena** funziona già: `loadCoreInEditor=True` carica `SCN_Core` in additivo e sposta l'origine del rig a `(30, 0, 10)`.

### 3.5 · Prefab mancanti: placeholder con gli stessi nomi

I 4 prefab referenziati ma assenti sono stati sostituiti da oggetti placeholder statici. **I nomi sono stati mantenuti identici** per non rompere i riferimenti già presenti in scena:

| Nome oggetto | Posizione (x, y, z) | Scala (x, y, z) |
| --- | --- | --- |
| `MDL_Muletto1_VH01` | `(29, 0, 15)` | `(1.2, 1.8, 2.6)` |
| `MDL_Muletto2_MS01` | `(36, 0, 5)` | `(1.2, 1.8, 2.6)` |
| `MDL_Pallet_PA01` | `(27, -0.07, 22.5)` | `(1.2, 0.15, 1.0)` |
| `MDL_Pallet_PA02` | `(31, 0, 13)` | `(1.2, 0.15, 1.0)` |

Regole applicate:

- **nessun gameplay**: i muletti non si muovono e non reagiscono;
- se un oggetto è già referenziato (etichette, zone, script), **non rinominarlo** solo per adattarlo al prefisso `MESH_` della guida 0.6 §5: la priorità è non perdere i riferimenti;
- i placeholder vanno sostituiti con i modelli definitivi senza cambiare i nomi, a meno che non si aggiornino anche tutti i riferimenti.

### 3.6 · Etichette nel mondo

Sono stati aggiunti **7 `LabelAnchor`** sugli oggetti chiave (niente Canvas e niente pannelli a mano).

- Per i pallet il testo usa la forma **"caduta del pallet"**.
- Le zone `ZONE_LZB_CadutaPA01` e `ZONE_LZB_CadutaPA02` corrispondono alla caduta dei pallet.
- Il numero di etichette Risk visibili entro 4 m resta entro il limite di 3 della guida 0.6.

### 3.7 · Trigger temporaneo per chiudere la missione (A / E)

Per provare l'uscita prima di avere il gameplay vero è stato aggiunto:

- oggetto **`MNG_TestEnd`** nella scena;
- script `Assets/_Project/Groups/G_Ludovica/Assets/Code/ForkliftChaosTestEnd.cs`.

Comportamento:

- sul visore: **tasto A del controller destro** → `OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch)`;
- in Editor: **tasto E** → `Keyboard.current.eKey.wasPressedThisFrame` (solo con `#if UNITY_EDITOR`);
- se la missione non è in corso non fa niente;
- quando premi, mostra un sottotitolo e chiama `EndMission` **una sola volta** con `OutcomeType.Positive` e esito + debrief di test.

Estratto significativo:

```csharp
if (!pressed)
    pressed = OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch);

if (!pressed) return;

controller.EndMission(
    OutcomeType.Positive,
    100,
    null,
    new List<string> { "+ Test completato: accesso al magazzino e HUD verificati." });
```

Riferimento Meta verificato: `OVRInput.Button.One` è il tasto A e `OVRInput.GetDown(Button, Controller)` è l'API corrente ([documentazione Meta, aggiornata il 2026-11-05](https://developers.meta.com/vr/documentation/unity/unity-tutorial-basic-controller-input)).

> **Temporaneo**: `MNG_TestEnd` e `ForkliftChaosTestEnd.cs` vanno **cancellati** quando arriva la logica di gioco vera, che chiamerà `EndMission` dai suoi percorsi reali. Non lasciare questo tasto nella build di consegna.
>
> Nota sul test automatico: l'automazione non ha potuto premere fisicamente A/E, quindi la verifica automatica ha chiamato `EndMission` direttamente. La mappatura del tasto è stata verificata contro la documentazione Meta e il flusso di uscita è stato confermato; la prova "dita sul controller" resta una verifica umana sul visore.

---

## 4. Validazione

Menu **`SI81/Scenari/Valida scenario`** con `SCN_ForkliftChaos` aperta.

Risultato reale:

| Contatore | Valore |
| --- | --- |
| Errori | **0** |
| Warning | **0** |
| Info | **0** |

Il validatore ha quindi confermato:

- missione 07 coerente (`SceneName`, catalogo, campi compilati);
- scena presente nelle Build Settings, attiva, con `SCN_Core` all'indice 0;
- un solo `ScenarioController` e un solo spawn point;
- nessuna camera, Canvas, EventSystem, rig duplicato o UI comune nella scena;
- nomi e `LabelAnchor` a posto.

I messaggi URP `On-Tile post processing feature is not supported on the graphics devices that don't support frame buffer fetch.` compaiono in Console ma **non sono errori SI81** e non dipendono da questa integrazione.

---

## 5. Test del flusso completo

Prova fatta in Play Mode partendo da `SCN_Core` (percorso vero del giocatore), non solo dalla scena dello scenario.

| Verifica | Risultato osservato |
| --- | --- |
| Servizio di lancio | `UIServices.Launcher = SI81.UI.SceneMissionLauncher` |
| Disponibilità missione 07 | `CanLaunch("07") = True` |
| Home → card 07 | card cliccabile, Mission Brief aperto |
| Mission Brief → **Avvia** | `SCN_ForkliftChaos` caricata in **additivo** sopra `SCN_Core` |
| Avvio scenario | `ScenarioController.IsRunning = True` |
| Spawn del giocatore | `TrackingOrigin = (30.00, 0.00, 10.00)` |
| HUD | HUD e `WorldLabelManager` pronti, nessun pannello davanti agli occhi |
| Chiusura missione | `EndMission` → **Esito** → **Debrief** → **Score & Replay** → **Home** |
| Pulizia | scenario scaricato, `MissionSession.IsActive = False`, card 07 ancora cliccabile |
| Console | 0 errori e 0 warning SI81 |

L'Editor è stato riavviato dopo una chiusura inattesa: le modifiche erano tutte conservate. L'Editor è stato lasciato **fuori Play Mode**, con `SCN_ForkliftChaos` aperta.

---

## 6. Build Quest e errore Android SDK

Durante la build Android/Quest sono comparsi errori di tipo SDK che **non avevano nulla a che fare con la scena o con il progetto SI81**.

Diagnosi:

- il PATH del processo Unity risultava corrotto:

  ```text
  C:\Program Files\PlasticSCM5\client;System.Collections.Generic.List`1[System.String]
  ```

- errore in Console:

  ```text
  Win32Exception: ApplicationName='powershell' ... Native error= The system cannot find the file specified.
  ```

- causa: **`com.unity.collab-proxy` 2.12.4** (Unity Version Control). Nel file `FindTool.cs` la riga che concatenava la cartella Plastic con la lista dei PATH usava il separatore sbagliato (`Path.PathSeparator` invece del separatore di elenco del PATH), generando una stringa non valida.

- gli SDK erano già installati correttamente: platform-tools `36.0.0`, build-tools `36.0.0`, piattaforme `android-34`, `android-35`, `android-36`.

Soluzione da applicare (o verificare):

1. **Package Manager → Unity Version Control → Remove** (cioè rimuovere `com.unity.collab-proxy`).
2. Riavviare Unity.
3. Riprovare la build Android/Quest.

Al momento della diagnosi la dipendenza era presente; nel `Packages/manifest.json` attuale **non risulta più elencata**. Se gli stessi errori ricompaiono, controllare prima Package Manager e il `packages-lock.json`.

> Non modificare AndroidManifest, Gradle o gli SDK per "aggirare" l'errore: non è un problema del progetto.

---

## 7. Cosa non è stato fatto e cosa resta

**Non fatto, volutamente:**

- nessuna modifica a Home, UI comune, `SCN_Core`, prefab condivisi o validatore;
- Electric Shock: **solo spostamento di cartella**, contenuto invariato;
- `(esempio)` in `DesignerNote` della missione 07 lasciato invariato;
- missione 07 non creata né duplicata;
- nessun commit o push: tutte le modifiche sono ancora locali e **uncommitted**.

**Da fare prima della consegna definitiva:**

- [ ] rimuovere `MNG_TestEnd` e `ForkliftChaosTestEnd.cs` insieme all'arrivo del gameplay vero;
- [ ] sostituire i 4 placeholder con i modelli definitivi, mantenendo i nomi o aggiornando i riferimenti;
- [ ] far scrivere al team proprietario il contenuto definitivo della missione e togliere `(esempio)`;
- [ ] verificare che `com.unity.collab-proxy` non sia tornato, se la build Android dà ancora errori;
- [ ] prova umana sul Quest 3 del tasto A (l'automazione ha validato `EndMission` diretto).

---

## 8. Checklist V2

- [ ] Cartelle `Art/`, `Level/`, `Scenes/` sotto `Assets/_Project/Groups/G_Ludovica/Assets/`.
- [ ] Electric Shock spostata solo di cartella, contenuto invariato.
- [ ] `MNG_CoreRig` duplicato eliminato; un solo rig, quello di `SCN_Core`.
- [ ] Scena rinominata `SCN_ForkliftChaos` con GUID `e8459afe76c1b064d90d37202a1e41a8` preservato.
- [ ] Missione 07 esistente riutilizzata, non duplicata; `(esempio)` invariato.
- [ ] `SceneName: SCN_ForkliftChaos` e scena attiva nelle Build Settings all'indice 1, dopo `SCN_Core`.
- [ ] Card 07 cliccabile in Home; nessun file Home/UI comune modificato.
- [ ] `MNG_SpawnPoint` a `(30, 0, 10)`, collegato a `MNG_Scenario`.
- [ ] `MNG_Scenario` con `SI81.UI.ScenarioController` base, missione assegnata, `editorLevel=Guided`, `loadCoreInEditor=True`.
- [ ] 4 placeholder con i nomi originali, posizioni e scale del §3.5; nessun gameplay.
- [ ] 7 `LabelAnchor`; testi pallet con "caduta del pallet".
- [ ] Trigger temporaneo `MNG_TestEnd` funzionante con A / E e chiaramente segnato da rimuovere.
- [ ] `Valida scenario`: 0 errori, 0 warning, 0 info.
- [ ] Flusso completo verificato: Home → 07 → Brief → Avvia → HUD → EndMission → Esito → Debrief → Score & Replay → Home.
- [ ] Console pulita per SI81; eventuali errori URP `On-Tile` segnalati ma non attribuiti al progetto.
- [ ] Build Android: verificato che `com.unity.collab-proxy` non sia presente; Unity riavviato dopo l'eventuale rimozione.

---

## 9. Prompt per assistente AI (caso reale)

Se usi un assistente, dagli **tre file**: `UI_INTEGRATION_FOR_AI.md`, `GUIDA_INTEGRAZIONE_SCENARIO.md` (0.6) e questa V2. Poi copia:

```text
Leggi Assets/_Project/Docs/UI_INTEGRATION_FOR_AI.md,
Assets/_Project/Docs/GUIDA_INTEGRAZIONE_SCENARIO.md e
Assets/_Project/Docs/GUIDA_INTEGRAZIONE_SCENARIO_V2.0.md.

Siamo il gruppo G_Ludovica. Dobbiamo integrare la scena SCN_ForkliftChaos
per la missione 07 "Forklift Chaos", che e' gia' nel catalogo con
DesignerNote "(esempio)" da lasciare invariato.

La scena e' statica: muletti e pallet fermi, nessun gameplay.
Non creare una nuova missione, non toccare Home/UI comune, non aggiungere
camera, Canvas o rig. Controlla che:
- SceneName sia SCN_ForkliftChaos e la scena sia attiva nelle Build Settings
  dopo SCN_Core, mantenendo il GUID e8459afe76c1b064d90d37202a1e41a8;
- MNG_SpawnPoint sia a (30, 0, 10) e collegato a MNG_Scenario;
- sia mantenuto il ScenarioController base per ora;
- i placeholder MDL_Muletto1_VH01, MDL_Muletto2_MS01, MDL_Pallet_PA01 e
  MDL_Pallet_PA02 mantengano i nomi per non perdere i riferimenti;
- il trigger temporaneo MNG_TestEnd chiami EndMission una sola volta
  con tasto A (OVRInput.Button.One / RTouch) oppure E in Editor,
  e sia rimosso quando arriva il gameplay vero.

Spiegami passo per passo e dimmi come verificare che il flusso
Home -> Mission Brief -> HUD -> Debrief -> Score & Replay -> Home funzioni.
```

**Se la risposta non ti convince**: chiedi all'assistente in quale sezione della guida 0.6 o di questa V2 è scritto quello che dice. Se non lo trova, chiedi al Team Ludovica.
