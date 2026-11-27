# Scripts VR - Sicurezza Immersiva

Script di gameplay per gli oggetti in `Assets/_Project/Shared/Oggetti`.
Richiedono Meta XR SDK Core 209 + Meta XR Interaction SDK 209 (OVR backend).

| Script | Oggetto | Cosa fa |
|---|---|---|
| `VRMessageDisplay.cs` | - (pannello VR) | Stampa testi su uno schermo world-space davanti al viso. |
| `VRInteractableButton.cs` | tutti | Collega un Interactable (poke / ray / grab) a UnityEvent e a eventi C#. |
| `CountdownTimer.cs` | - (timer) | Conto alla rovescia con messaggio di "TEMPO SCADUTO" sullo schermo VR. |
| `TelefonoInterattivo.cs` | `Oggetti/Telefono` | Telefono con rubrica: sollevi la cornetta, scegli un numero, parte la chiamata. |
| `QuadroElettricoInterattivo.cs` | `Oggetti/Quadro Elettrico` | Quadro elettrico acceso/spento con luci, spie e messaggio a schermo. |

## 1. VRMessageDisplay (schermo VR)

1. Crea un GameObject vuoto nella scena (es. `VR_Screen`), aggiungi `VRMessageDisplay`.
2. Lascia vuoto `Follow Target` per usare `Camera.main`, oppure assegna la testa del rig.
3. Regola `Distance` (0.65 m di default), `Panel Size Meters`, colori e durata.
4. Non serve nessun Canvas in scena: il pannello viene creato a runtime.

Da codice: `VRMessageDisplay.GetOrCreate().Show("Titolo", "Testo", 4f);`
Se il componente non esiste in scena, `GetOrCreate()` lo crea da solo.

## 2. VRInteractableButton (glue con Interaction SDK)

1. Sull'oggetto che deve essere premuto aggiungi l'interactable ISDK
   (`PokeInteractable`, `RayInteractable`, `GrabInteractable` o `Interactable`).
2. Sullo stesso GameObject (o su un genitore) aggiungi `VRInteractableButton`.
3. Gli interactable vengono trovati da soli, figli compresi: non serve compilare `Interactables`.
4. Collega `On Selected` all'azione da eseguire, oppure usa l'evento C# `Selected`.

`SetAvailable(false)` lascia l'oggetto visibile ma non piu' premibile (usato dal telefono).

## 3. CountdownTimer (timer)

1. GameObject vuoto `Timer`, aggiungi `CountdownTimer`.
2. `Duration Seconds` = durata; `Auto Start` per farlo partire da solo.
3. `Countdown Label` (opzionale) = un TMP text per vedere il tempo residuo.
4. `Display` (opzionale) = il `VRMessageDisplay`; se vuoto viene creato automaticamente.
5. `Start Buttons` = pulsanti `VRInteractableButton` che avviano il timer.
6. Testi modificabili: `Expired Title`, `Expired Message`.

API: `StartTimer()`, `StartTimer(seconds)`, `Pause()`, `Resume()`, `Stop()`, `ResetTimer()`, `AddSeconds(f)`.
Eventi: `On Started`, `On Expired`, `On Tick`, `Expired`, `Started`.

## 4. TelefonoInterattivo (Oggetti/Telefono)

1. Trascina `Telefono.fbx` in scena e aggiungi `TelefonoInterattivo` alla radice del telefono.
2. Su ogni tasto/pulsante 3D del telefono aggiungi `VRInteractableButton` + il suo interactable ISDK
   (di solito `PokeInteractable`).
3. Nella lista `Pulsanti Opzione` trascina i pulsanti **nello stesso ordine** di `Rubrica`
   (Emergenze, Vigili del fuoco, Manutenzione...).
4. `Cornetta` = pulsante/interactable della cornetta (prima pressione solleva, seconda riappoggia).
   Se e' un `GrabInteractable`, attiva `Riponi Al Rilascio Cornetta`.
5. `Schermo Telefono` (opzionale) = TMP text sullo schermo del telefono: mostra rubrica e stato chiamata.
6. `Pulsante Chiudi` (opzionale) = pulsante che chiude la chiamata in corso.
7. Tempi: `Attesa Composizione`, `Durata Conversazione`, `Durata Fine`.
8. Audio: trascina i clip in `Audio Source` / `Suono Selezione` / `Suono Chiamata` / `Suono Fine Chiamata`.

API: `SollevaCornetta()`, `RiponiCornetta()`, `Seleziona(int indice)`, `ChiudiChiamata()`.
Eventi: `On Chiamata Avviata` (string), `On Chiamata Terminata` (string), `ChiamataAvviata`, `ChiamataTerminata`.

## 5. QuadroElettricoInterattivo (Oggetti/Quadro Elettrico)

1. Metti `Quadro Elettrico.fbx` in scena e aggiungi `QuadroElettricoInterattivo` alla radice.
2. `Interruttori` = pulsanti `VRInteractableButton` della leva/interruttore.
3. `Luci` / `Oggetti Alimentati` = cosa si accende o si spegne con il quadro.
4. `Spie` + `Materiale Spia Accesa/Spenta` = cambio materiale delle spie (opzionale).
5. `Leva` + rotazioni = animazione dell'interruttore (opzionale).
6. `Etichetta Stato` (TMP), `Display`, messaggi e audio: tutti opzionali.
7. `Acceso All Avvio` decide lo stato iniziale.

API: `Toggle()`, `ImpostaStato(bool)`, `SetPower(bool)`.
Eventi: `On Acceso`, `On Spento`, `CambioStato`.

## Note

- Se i testi TMP appaiono fucsia, importa i TMP Essential Resources
  (`Window > TextMeshPro > Import TMP Essential Resources`).
- Il progetto usa URP: nessuno script dipende dal render pipeline, funzionano in Built-in e URP.
