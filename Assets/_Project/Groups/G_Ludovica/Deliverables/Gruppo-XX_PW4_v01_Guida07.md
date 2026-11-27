# Guida operativa — SCN_ForkliftChaos (07)

Obiettivi sul polso e suggerimenti in sequenza, dal primo sguardo alla fine dello scenario.
I tasti `F1`–`F6` restano **solo fallback Editor**; in Quest si usano i controller.

## Comandi Meta Quest (verificati)

| Azione | Controller |
|---|---|
| Cammina | **Stick sinistro** |
| Svolta | **Stick destro** |
| Punta / seleziona | Raggio dai controller, portata **5 m** |
| Azione normale | **Grilletto** |
| Prendi / muovi oggetto | **Grip** (tienilo premuto, rilascialo per posare) |
| Pausa | **Menu sinistro** |
| Indietro | **B/Y** |
| A/X | Non usati |

Regola: **puntare e tenere ~1 s** per osservare; **puntare + grilletto** per agire;
**puntare + grip** per radio e barriera.

## Obiettivi sul polso (testi brevi e operativi)

1. **Chiama il 112 e il capo turno**
2. **Posiziona la barriera BL-01**
3. **Posiziona i coni**
4. **Assisti Marco senza spostarlo**

## I suggerimenti in ordine (HUD, uno alla volta)

| # | Suggerimento | Azione concreta |
|---|---|---|
| 1 | `Osserva Marco e il muletto` | Punta Marco `(29.5,19.7)` o M1 `(29,15)` e tieni fermo un attimo |
| 2 | `Spegni il muletto 1` | Punta `MDL_Muletto1_VH01` `(29,15)` + **grilletto** |
| 3 | `Prendi radio e barriera` | Vai a `x=26.8, z=9.5`: **grip** su radio `(26.8,1.55,9.5)` e barriera `(28.2,10.6)` |
| 4 | `Chiama il 112 e il capo turno` | Con l'**altro controller** punta i pulsanti `112` e `CAPO` sulla radio + **grilletto** |
| 5 | `Posiziona la barriera` | Porta la barriera vicino al cartello `(29.3,9.1)` e **rilascia il grip** (entro 1,2 m) |
| 6 | `Posiziona i coni` | Punta i coni `(30.7,9.1)` + **grilletto** |
| 7 | `Valuta Marco` | Punta `VALUTA Marco` `(28.6,19.5)` + **grilletto** |
| 8 | `Tratta la ferita` | Punta `TRATTA LA FERITA` `(30.6,20)` + **grilletto**: compressione e copertura insieme, con conferma su schermo |
| 9 | `Rianima Marco (massaggio e DAE)` | **Solo Operational/Crisis**, se Marco va in apnea: punta `RIANIMA` `(27.2,18)` + **grilletto**. Nel livello Guidato non compare: il PDF di specifica prevede apnea solo in 1 partita su 3 (Operational) e 1 su 2 (Crisis) |
| 10 | `Ferma il muletto 2` | Al clacson punta `MDL_Muletto2_MS01` + **grilletto** entro 6 s. Se hai già posato barriera e coni, il muletto si **ferma da solo davanti alla linea BL-01** `(30,8.2)` |
| 11 | `Attendi i soccorsi vicino a Marco` | Non spostare Marco, resta vicino: arriva il soccorso |

Il primo suggerimento compare **subito all'avvio**; poi passa automaticamente al passo successivo.

## Pulsanti accanto a Marco — ora solo 3

`Guarda Marco` è una **osservazione** (sguardo prolungato), non un pulsante.

| Pulsante | Posizione | Sostituisce |
|---|---|---|
| `VALUTA Marco` | `(28.6, 19.5)` | Parla con Marco + Controlla respiro |
| `TRATTA LA FERITA` | `(30.6, 20)` | Comprimi ferita + Copri Marco |
| `RIANIMA (massaggio e DAE)` | `(27.2, 18)` | Massaggio cardiaco + DAE. **Nascosto in Guided**, attivato solo dall'apnea negli altri livelli |

`SPOSTA MARCO (emergenza)` `(27,21.5)` compare **solo** in pericolo immediato e sposta Marco
di **2,2 m verso ovest**, lontano dal muletto e dal rack.

I collider dei pulsanti attivi sono stati ridotti a **13 cm** (erano 26 cm) e il collider di Marco
è un piccolo bersaglio **sopra la testa**: puoi camminare attorno a lui e puntare i pulsanti senza
restare incastrato.

## Pallet in bilico vicino a Marco

Il rack `MDL_Rack_PA01` è a `(29, 0.9, 21.5)` e il pallet è a `(29, 1.7, 21.8)`,
sporgente dal bordo e leggermente inclinato: a circa **1,2 m da Marco**, quindi la caduta
lo colpisce davvero.

- Entrando in **LZ-B** per più di 1 s parte l'avviso *"Allontanati dalla pila"*.
- Hai **4 secondi** per uscire: se esci in tempo la caduta viene **annullata** e il pallet resta sul rack.
- Se resti dentro, il pallet cade su Marco → `LG-19` critical, livello fallito.

## Pavimento scivoloso

La pozza `SL01_PavimentoScivoloso` è stata ridotta a **1,8 m** e il suo collider è **disattivato**:
non blocca più il passaggio. La penalità scatta solo se **corri** dentro l'area vicino a Marco
(il rilevamento è basato sulla velocità della testa, non sulla fisica).

## Tempi dei soccorsi

Dopo la chiamata al **112** i soccorsi arrivano in **circa 2 minuti** (110–130 s), non dopo 5.
Se i colleghi entrano in corsia il soccorso subisce **+60 s** di ritardo.

Dal momento della chiamata il polso mostra un **conto alla rovescia** aggiornato ogni secondo,
affiancato alla prossima azione, per esempio `Soccorsi 1:45 · Valuta Marco col grilletto`.
Il timer da 5 minuti in alto resta il limite della missione, non l'attesa dei soccorsi.

## Audio

Il controller crea l'`AudioListener` se manca (`SCN_Core` non ne ha uno serializzato):
in Console appare `[FKC] AudioListener creato su ...`. Urlo all'avvio, clacson,
impatto del pallet e sirena finale sono riprodotti da `Resources/Audio/`.
I file e le licenze CC0 sono documentati in `ATTRIBUTION.md`.

## Verifica rapida prima del test

1. Reload della scena in Unity (se chiede "modifica esterna" → **Reload**; altrimenti chiudi
   senza salvare e riapri `SCN_ForkliftChaos`). **Non premere Ctrl+S prima del reload.**
2. Console: **0 errori CS** e `[FKC] AudioListener creato ...`.
3. Play con Quest Link, con `SCN_Core` e `SCN_ForkliftChaos` aperti.
4. Il primo suggerimento deve essere visibile subito.
5. Prova: entra in LZ-B ed esci entro 4 s → il pallet non deve cadere.
