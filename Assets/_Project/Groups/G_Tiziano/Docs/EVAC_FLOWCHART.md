# EVACUATION — Flowchart decisioni, conseguenze, eventi, esiti

Legenda: ogni nodo riporta la conseguenza di gameplay, l'ID di log e/o l'evento C# emesso.
Il debrief associato è in `EVAC_DEBRIEF_TABLE.md`; la superficie del Director è in `EVAC_TECHNICAL_DOCS.md`.

## Flusso principale

```mermaid
flowchart TD
    S([StartScenario / Phase Action]) --> A["Allarme ON<br/>AlarmStateChanged(true)"]
    A --> T{"Timeout?"}
    T -- "Guided: solo cronometro" --> TG["Nessuna sconfitta: il gioco continua"]
    T -- "Operational 180s / Crisis 120s" --> L10["lg10 Missed · Time 0<br/>TimeUp → Failure"]
    TG --> P
    A --> P{"DP1: piano ispezionato?<br/>hover+trigger o sguardo 0.75s"}
    P -- "sì" --> L1["lg01 Correct · Awareness 100<br/>PlanInspected"]
    P -- "no" --> L2["lg02 Missed · Awareness 30"]
    L1 --> R{"DP2: 3 colleghi U1 + 2 U2 raccolti?"}
    L2 --> R
    R -- "U1 completo" --> L3["lg03 Correct · Execution<br/>NpcCollected(1,total)"]
    R -- "U2 completo" --> L4["lg04 Correct · Execution<br/>NpcCollected(2,total)"]
    R -- "NPC già in fila" --> DIA["NpcDialogueRequested<br/>evac.npc.dialogue.already"]
    R --> F{"DP4 · solo Crisis: contatto fuoco?"}
    F -- "1° o 2° contatto" --> L7["lg07 Risky · Safety -25<br/>FireContact(n,intensity)"]
    F -- "3° contatto · cuori 0" --> L8["lg08 Critical<br/>DamageTaken → Died → Failure"]
    F -- "nessuno" --> E
    L7 --> E
    L3 --> E
    L4 --> E
    E{"DP3: uscita raggiunta"}
    E -- "ascensore · Activated" --> EL{"Difficulty?"}
    EL -- "Guided" --> PE["Partial · lg06 · Safety 0<br/>DangerLevelChanged(Down)"]
    EL -- "Operational / Crisis" --> FE["Failure · lg06 · Safety 0"]
    E -- "ZONE_StairsInternal" --> PI["Partial · lg11 · Decision -40<br/>ExitReported(Internal)"]
    E -- "ZONE_StairsEmergency" --> CK{"piano ispezionato e 5/5 NPC?"}
    CK -- "sì" --> SU["Success · lg09<br/>ExitReported(Emergency)"]
    CK -- "no" --> PM["Partial · lg05 o lg02<br/>Decision -30"]
    PE --> RR
    FE --> RR
    PI --> RR
    SU --> RR
    PM --> RR
    L8 --> RR
    L10 --> RR
    RR([ResultReady / ScenarioResult + ActionLog + sotto-punteggi])
```

## I tre livelli

```mermaid
flowchart LR
    G["Guided<br/>cronometro, niente sconfitta<br/>0 cuori · no fuoco<br/>hint + highlight · 0/0/0 punti"]
    O["Operational<br/>180 s · 3 cuori · no fuoco<br/>nessun aiuto · 30/10/0 punti"]
    C["Crisis<br/>120 s · 1 cuore · fuoco 5/10<br/>effetti ridotti · 50/20/0 punti"]
    G --> O --> C
```

Note:

- In Guided l'ascensore **non** è sconfitta: evento di pericolo `Down` + `evac.lg06`, esito massimo `Partial`.
- In Operational/Crisis l'ascensore è `Failure` immediato (basta premere il pulsante, non serve entrare).
- Il timer non sconfigge mai in Guided; `TimeUp` e `evac.lg10` sono emessi solo negli altri due livelli.
- Il fuoco esiste solo in Crisis; i contatti sono un contatore separato dai cuori, con cooldown di 2 s sullo stesso ingresso.
- A ogni run `ReplayStarted(seed, slot NPC, slot fuoco)` e `evac.replay.start` nel log: posizione NPC e focolai cambiano col seed.
- `ResetScenario()` riporta tutto allo stato iniziale e teletrasporta il rig a `SPAWN_Player`; nessuno stato statico sopravvive.
