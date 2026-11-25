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

## Controlli e interazioni (rig condiviso)

- [ ] Il rig parte a terra su `MNG_SpawnPoint` e non cade; levetta sinistra cammina, levetta destra gira di 45°.
- [ ] Il raggio dalla mano evidenzia gli oggetti (chiave, estintori, barriere, radio, collega, PM-1, colleghi) fino a 5 m.
- [ ] Chiave del generatore (PB-01): grilletto → la chiave gira, il generatore si ferma; dopo circa 2 s la pozza prende fuoco.
- [ ] Estintore: grilletto legge l'etichetta; grip tenuto lo mette in mano; grilletto tenuto spruzza; il fuoco si spegne in circa 4 s anche da vicino; lasciato il grip resta dritto a terra.
- [ ] Barriere: grip le prende, lasciate vicino ai segni gialli si agganciano; con tutte e 5 "Varco chiuso". Il giocatore le attraversa.
- [ ] Radio: grilletto = chiamata al 112 (facile da puntare anche se piccola).
- [ ] Grilletto su un collega in piedi: "Fermi! Allontanatevi!" e i colleghi escono verso il punto di raccolta.
- [ ] Collega a terra: grilletto 1ª volta coscienza e respiro, 2ª volta posizione; grip tenuto lo trascina.
- [ ] Collega o cavo toccati con la corrente presente: vibrazione forte e folgorazione.
- [ ] PM-1: grilletto tenuto 3 s, poi ti segue fino al cerchio verde.
- [ ] Puntando un pannello della UI il raggio clicca la UI e non l'oggetto dietro.

## Percorsi completi (uno per livello, con le interazioni)

- [ ] Guided: percorso corretto → esito Positivo.
- [ ] Operational: nessuna barriera, poi aspetta PM-2 con zona sicura → Parziale (riga sul varco aperto nel Debrief).
- [ ] Crisis: lascia bruciare il fuoco e resta vicino al generatore → esplosione → Negativo.
