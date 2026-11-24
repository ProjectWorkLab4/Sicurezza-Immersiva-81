# Gruppo-XX_PW4_v01_Flowchart07 — Forklift Chaos

> Sostituire `XX` con il numero ufficiale del gruppo prima della consegna.
> Diagramma in Mermaid: renderizzabile su GitHub, convertibile in PDF per `COMMON_BUILD`.

```mermaid
flowchart TD
    S([EV-01 urlo · timer 300 s]) --> DP1{DP1 · osservi prima<br/>di intervenire?}
    DP1 -->|Osserva dalla soglia| O1[LG-01 Awareness ✓<br/>controlla angolo cieco]
    DP1 -->|Corri in corsia| O2[LG-03 Time ✗<br/>esposto a M2 e pallet]
    DP1 -->|Afferra il pallet| O3[LG-19 Safety ✗✗<br/>PA-01 cade su Marco]
    DP1 -->|Timeout pannello| O4[LG-02 Awareness ✗<br/>angolo cieco non controllato]

    O1 --> DP2{DP2 · spegni M1<br/>e delimita BL-01?}
    O2 --> DP2
    O4 --> DP2
    DP2 -->|M1 spento + barriera + coni| A1[LG-04, LG-06 Safety ✓<br/>corsia delimitata]
    DP2 -->|Solo radio| A2[LG-05 Safety ✗<br/>niente Successo]
    DP2 -->|Sposta Marco| FAIL1[LG-11 Errors critical<br/>FALLIMENTO]

    A1 --> DP3{DP3 · prendi la radio<br/>e chi chiami?}
    A2 --> DP3
    DP3 -->|112 + capo turno| R1[LG-07 Decision ✓<br/>soccorsi in 120-210 s]
    DP3 -->|Solo capo turno / nessuno| R2[LG-07 Decision ✗<br/>soccorsi non programmati]

    R1 --> EV3{EV-03 · colleghi<br/>entro 60-90 s?}
    R2 --> EV3
    EV3 -->|BL-01 chiusa o capo turno| C1[LG-17 Safety ✓<br/>colleghi fuori]
    EV3 -->|Barriera aperta| C2[LG-18 Decision ✗<br/>corsia bloccata, +60 s soccorsi]

    C1 --> DP4{DP4 · soccorso Marco}
    C2 --> DP4
    DP4 -->|Parla + controlla respiro| F1[LG-13 Execution ✓]
    DP4 -->|Sposta senza pericolo| FAIL2[LG-11 Errore critico<br/>FALLIMENTO]
    DP4 -->|Attendi| F2[LG-14 Execution ✗]
    F1 --> FA{Primo soccorso}
    FA -->|Comprimi + copri| F3[LG-14 Execution ✓]
    FA -->|Sposta per pericolo immediato| F4[LG-12 Decision ✓<br/>max 3 m, testa sostenuta]

    F3 --> AP{Apnea?}
    F4 --> AP
    AP -->|Guided: no| M2
    AP -->|Operational 1/3 · Crisis 1/2| DP4A{DP4b · RCP 30:2 + DAE?}
    DP4A -->|Sì entro 120 s| F5[LG-15 Execution ✓]
    DP4A -->|No / timeout| FAIL3[LG-16 Errore critico<br/>FALLIMENTO]

    F5 --> EV2
    M2 --> EV2{EV-02 · clacson<br/>6 s prima di partire}
    EV2 --> DP5{DP5 · come fermi M2?}
    DP5 -->|Barriera BL-01| M1[LG-06, LG-09 ✓<br/>Mezzo fermo all'accesso]
    DP5 -->|Radio al capo turno| M3[LG-09 Decision ✓<br/>fermo in 8,5 s]
    DP5 -->|Corri nella corsia / niente| FAIL4[LG-10 Errore critico<br/>investimento → FALLIMENTO]

    M1 --> EV6
    M3 --> EV6
    EV6{EV-06 · soccorsi arrivati?}
    EV6 -->|Corsia libera e non ritardati| OK([SUCCESSO<br/>SPI medio, 5 dimensioni])
    EV6 -->|Corsia bloccata / oltre 4 min / passo mancato| PART([PARZIALE<br/>debrief: condizione mancata])
    EV6 -->|Timeout 300 s| FAIL5([FALLIMENTO<br/>TIMEOUT])

    EV4[EV-04 · PA-01: sosta in LZ-B > 5 s] -.->|4 s di preavviso| PAL{caduta}
    PAL -->|su Marco| FAIL6[LG-19 FALLIMENTO]
    PAL -->|di lato| WARN1[avviso Safety]
    WARN1 --> DP2

    EV7[EV-07 · PA-02: LZ-C > 5 s] -.->|4 s di preavviso| PAL2{caduta in corsia}
    PAL2 -->|sotto il giocatore| FAIL7[EV-07 FALLIMENTO]
    PAL2 -->|corsia ostruita| DELAY[+60 s soccorsi → PARZIALE]

    EV8[EV-08 · SL-01 corsa vicino a Marco] -.-> WARN2[LG-21 Safety ✗<br/>rallentamento, nessuno spostamento forzato]
    WARN2 --> DP4

    EV5[EV-05 · Marco non coperto a 180 s] -.-> PEN[+10 penalità Execution<br/>avviso]
    PEN --> DP4
```

## Legenda

- `✓` scelta corretta · `✗` rischioso o mancato · `✗✗` / "Errore critico" errore che chiude il livello.
- Le linee tratteggiate sono eventi dinamici che possono interrompere il flusso principale in qualsiasi momento.
- Ogni nodo con ID LG-* produce una riga nel debrief con minuto, esito e conseguenza.
- Il percorso corretto completo è riportato in fondo al debrief e nel SDD (§9).
