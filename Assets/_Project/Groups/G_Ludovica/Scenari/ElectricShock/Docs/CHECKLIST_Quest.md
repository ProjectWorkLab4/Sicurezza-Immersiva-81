# Checklist sul visore — 10 · Electric Shock

Build: Build Profiles → `BP_ElectricShock_Quest` → Build. Log in parallelo: `adb logcat -s Unity | findstr "SI81"`.
Segna ✔ / ✘ e scrivi cosa succede quando qualcosa non va.

## Rig temporaneo

- [ ] All'avvio sei in piedi a un'altezza credibile, guardando verso il generatore (est), e 1 vibrazione conferma Guided.
- [ ] Le due mani (piccoli parallelepipedi) seguono i controller; la mano non è ruotata in modo strano.
- [ ] Levetta sinistra: cammini nella direzione in cui guardi. Levetta destra: rotazione a scatti di 30°.
- [ ] Non attraversi le pareti; attraversi barriere, persone e collega a terra.
- [ ] Menu (sinistro): riavvio al livello successivo, con 2 e poi 3 vibrazioni.

## Interazioni

- [ ] Toccando PB-01 e premendo il grilletto il generatore si ferma (log "Generatore fermato").
- [ ] Grip su un estintore: lo prendi, l'ugello punta in avanti; A/X legge l'etichetta (log "Etichetta").
- [ ] Grilletto con l'estintore in mano: si vede lo spruzzo e il fuoco si spegne in circa 4 s, anche molto vicino alle fiamme.
- [ ] Lasciato il grip, l'estintore resta dritto a terra.
- [ ] Barriere: si prendono, si agganciano ai segni gialli sul varco; con entrambe agganciate il log scrive "Varco chiuso".
- [ ] Radio: grilletto = chiamata al 112 (log).
- [ ] B/Y: "Fermi! Allontanatevi!" e i colleghi escono verso il punto di raccolta.
- [ ] Collega: A/X controlla coscienza e respiro; grilletto breve lo mette in posizione; grilletto tenuto lo trascina.
- [ ] PM-1: grilletto tenuto 3 s, poi ti segue fino al cerchio verde.
- [ ] Toccare il cavo o il collega con il generatore acceso chiude il livello (Negativo).

## Scena ed effetti

- [ ] Fiamme, fumo, scintille del cavo e spruzzo sono visibili (non rosa né quadrati).
- [ ] Dopo l'arresto la pozza prende fuoco dopo circa 2 s.
- [ ] La pozza si riesce a vedere girando attorno al generatore.
- [ ] Guided: PB-01 e gli estintori corretti sono evidenziati in giallo.
- [ ] Prestazioni fluide (nessuno scatto evidente con il fuoco acceso).

## Percorsi completi (uno per livello)

- [ ] Guided: percorso corretto → log "Esito: Positive".
- [ ] Operational: nessuna barriera, poi aspetta PM-2 con zona sicura → "Esito: Partial" (riga LG-04 nel debrief).
- [ ] Crisis: lascia bruciare il fuoco e resta vicino al generatore → esplosione → "Esito: Negative" (LG-17).
