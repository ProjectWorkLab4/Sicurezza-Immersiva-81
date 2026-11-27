# EVACUATION — Report prestazioni (E13, Quest)

Misurato nell'Editor Unity 6000.3.14f1 con `SCN_Evacuation` aperta, Play Mode, valori `UnityEditor.UnityStats` a regime e conteggi degli asset. **Editor Windows, non headset Quest**: i numeri sono un indicatore, la conferma finale resta il test in headset (vedi "Richieste all'utente" in HANDOFF).

## 1. Inventario (prima delle correzioni)

| Voce | Valore |
|---|---|
| GameObject nella scena | 415 (12 root) |
| MeshFilter / MeshRenderer | 207 / 207 · 112 mesh uniche · 574.890 triangoli sommati sulle istanze |
| Renderer totali / abilitati | 227 / 148 in Editor, 187 in Play (include il rig condiviso `SCN_Core`) |
| Materiali unici in scena | 80 · tutti URP/Lit opachi (`renderQueue < 3000`, `_Surface = 0`) · **0 trasparenze** |
| ParticleSystem | 20 = 10 `FIRESLOT` × (Fire + Smoke) · 60 `maxParticles` ciascuno · rate 26/s e 10/s → tetto 1200 (10 slot) / 600 (5 attivi in Crisis) |
| Luci | 1 Directional Light · **0 luci realtime extra** |
| Collider | 78 (55 Box dell'`Environment` + trigger porte/zone/NPC/focolai) |
| `staticBatches` / `staticBatchedDrawCalls` | 0 / 0 → **nessuno static batching attivo sulla scenografia visibile** |

La scenografia fissa (`MODEL_Office`, 111 MeshRenderer) aveva `BatchingStatic = false` su tutti i rendereri visibili: i 69 rendereri con il flag erano quelli disattivati, quindi inutili ai fini dei draw call.

## 2. Misurazioni a regime (UnityStats)

| Metrica | Prima · Guided | Dopo · Guided/Operational | Prima · Crisis | Dopo · Crisis |
|---|---|---|---|---|
| Draw calls | 377 | **320** (−57, −15%) | 385 | **329** (−56, −15%) |
| Batches | 376 | **229** (−147, −39%) | 376 | **229** (−147, −39%) |
| SetPass calls | 40 | 43 | 43 | 44 |
| Triangoli | 1.375.225 | 1.364.290 | 1.373.109 | 1.362.074 |
| Vertici | 1.909.505 | 1.902.638 | 1.908.615 | 1.901.548 |
| Static batches / draw call | 0 / 0 | **5 / 166** | 0 / 0 | **5 / 166** |
| Dynamic batches / instanced | 1 / 8 | 1 / 8 | 3 / 8 | 3 / 8 |
| ParticleSystem in gioco | 0 | 0 | 10 | 10 |
| Particelle vive | 0 | 0 | 95 | **70** |
| Tetto attivo (5 focolai) | 600 | 600 | 600 | **230** con RiduciEffetti (420 senza) |

Il delta Crisis–Guided resta ~9 draw call: il costo del fuoco è soprattutto particellare, non di geometry.

## 3. Correzioni applicate (solo dentro `G_Tiziano/`)

1. **Static batching** — impostato `StaticEditorFlags.BatchingStatic` sui **111 MeshRenderer** sotto `MODEL_Office` (scenografia immobile); scena `SCN_Evacuation.unity` salvata. Risultato: 5 static batch, 166 draw call servite, batches 376 → 229.
2. **Tetto particelle** — `Level/Prefabs/Evacuation/FireSlot.prefab` e le 10 istanze in scena: `fireRate` 26 → 18, `smokeRate` 10 → 8, `smokeCap` 60 → 40. Il codice calcola il cap fuoco come `rate*2+8` → **44** (prima 60); il fumo usa direttamente `smokeCap` → **40** (prima 60). Con `ReducedEffects` attivo in Crisis i valori si dimezzano (26 + 20 per slot): tetto complessivo **230** invece di 600.
3. **Esclusi dallo static batching** (intenzionalmente): porte, NPC, piani di evacuazione, allarme e cartello uscita. Sono dinamici o usano `MaterialPropertyBlock` per l'evidenziazione Guided: batcharli potrebbe rompere highlight e `UnityEvent`.

Non sono state toccate luci, shader, materiali, ProjectSettings, package, UI o animazioni; Chemical Alert è intoccato.

## 4. Verifica dopo le correzioni

- `Y` / `Evac/Run Smoke Test` → `[EV] SmokeTest summary 49/49 ALL PASS`
- `O` / `Evac/Run SelfCheck` → evaluator + configurazioni `ALL PASS`
- `F5` / `Evac/Validate Fire Lanes` → `[EV] Fire lane: 200/200 seed validi, 0 scartati.`
- `F11` → `[EV] E10 replay games: 5/5 PASS`; `F12` → `[EV] E10 reset stress: 10/10 PASS`
- Console: **0 errori**; scene `isDirty = false` dopo il salvataggio; flag e valori serializzati riverificati in Editor.

## 5. Giudizio

Con 320 draw call in Editor e nessuna trasparenza/post-processing/luce extra, il scenario resta nel budget indicativo per Quest; la riduzione del 39% dei batch e del tetto particelle da 600 a 230 limita il costo della modalità Crisis. Il giudizio definitivo richiede il profilo su headset reale (E13 opzionale, outside scope del gruppo).


## Aggiornamento E22 — ricostruzione con UFFICIO_STACCATO (2026-11-27)
- Conteggio **statico** in editor (MeshRenderer attivi di `SCN_Evacuation`, senza `UFFICIO_ASSEMBLATO_UNITO`, disattivato): 72 MeshRenderer, ≈ **387k triangoli** (struttura `MODEL_Staccato` ≈ 40k + 8 × `EvacDesk` 42.8k = 343k + porte/piani/allarme/pulsante/cartello). NPC (SkinnedMeshRenderer TIZIO) esclusi.
- Prima della ricostruzione: ≈ 575k tri (E13). Il guadagno viene dalla rimozione delle scrivanie incorporate nel modello (−531k) a fronte dei tavoli nuovi (+343k).
- **Non misurati**: draw call/batches/SetPass in Play e frame time su Quest (misure E13 non rifatte). Da fare su headset reale. Se serve, versione semplificata di `TAVOLO` (8 × 42.8k è la voce più pesante).
