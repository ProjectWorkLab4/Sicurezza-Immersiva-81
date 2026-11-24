# Checklist sul visore — 10 · Electric Shock

Build: build normale del progetto (Build Settings: `SCN_Core` all'indice 0, poi gli scenari).
Log in parallelo (Meta Quest Developer Hub o `adb logcat -s Unity | findstr "SI81"`). Segna ✔ / ✘ e scrivi cosa succede.

## Collegamento con la UI comune (verificabile già adesso)

- [ ] In Home compare la card **10 · Electric Shock** con l'icona del fulmine.
- [ ] Mission Brief: contesto, 4 obiettivi, 3 criteri, livelli Guided / Operational / Crisis sbloccati.
- [ ] Avvia: lo scenario si carica e il giocatore parte da `MNG_SpawnPoint`, guardando verso il generatore.
- [ ] Sul polso: obiettivo "1/4 Isola il generatore", fase, timer (Operational 4 min, Crisis 3 min).
- [ ] Avviso iniziale e sottotitolo "Aiuto! Ha preso la scossa!" in basso nella vista.
- [ ] Guided: etichette nel mondo su cavo, pozza, arresto di emergenza, estintori, collega (max 3 rischi insieme).
- [ ] Senza fare nulla: a 40-70 s i colleghi toccano la vittima → esito Negativo → Debrief con le righe "!" / "-" → Score & Replay → Home.
- [ ] Menu sinistro (≡) durante la missione = Pausa della UI (lo scenario non usa quel tasto).
- [ ] Suoni 3D: ronzio del generatore e crepitio del cavo si sentono avvicinandosi.
- [ ] Fiamme, fumo, scintille visibili (non rosa né quadrati); prestazioni fluide.

## Interazioni (quando arriva il VR Interaction System)

- [ ] PB-01 con il grilletto ferma il generatore; dopo circa 2 s la pozza prende fuoco.
- [ ] Estintore: grip prende, A/X legge l'etichetta, grilletto spruzza; il fuoco si spegne in circa 4 s anche da vicino.
- [ ] Barriere agganciate al varco: "Varco chiuso".
- [ ] Radio: grilletto = chiamata al 112.
- [ ] B/Y: "Fermi! Allontanatevi!" e i colleghi escono verso il punto di raccolta.
- [ ] Collega: A/X coscienza e respiro, grilletto breve posizione, grilletto tenuto trascina.
- [ ] PM-1: grilletto tenuto 3 s, poi ti segue fino al cerchio verde.

## Percorsi completi (uno per livello, con le interazioni)

- [ ] Guided: percorso corretto → esito Positivo.
- [ ] Operational: nessuna barriera, poi aspetta PM-2 con zona sicura → Parziale (riga sul varco aperto nel Debrief).
- [ ] Crisis: lascia bruciare il fuoco e resta vicino al generatore → esplosione → Negativo.
