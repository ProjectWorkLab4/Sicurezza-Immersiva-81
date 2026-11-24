# Dati per SO_Mission_ElectricShock

Da copiare in `SO_Mission_ElectricShock` quando la UI comune è nel progetto (guida di integrazione §3). Testi in italiano, limiti rispettati.

| Campo | Valore |
| --- | --- |
| `Id` | `10` |
| `Title` | `Electric Shock` |
| `Context` (≤ 280) | Sei un lavoratore del turno. Un collega ha preso la scossa da un generatore portatile ed è a terra, ancora a contatto con il cavo. Vicino c'è un paramedico sotto shock e sotto il generatore c'è una pozza di benzina. Isola la zona e metti tutti in salvo. |
| `Objectives` (1–4, ≤ 30) | `Isola il generatore` · `Spegni l'incendio` · `Evacua e isola la zona` · `Assisti collega e paramedico` |
| `Criteria` Positivo | Generatore spento, persone evacuate, collega e paramedico in salvo |
| `Criteria` Parziale | Generatore disattivato ma zona non isolata |
| `Criteria` Negativo | Estintore errato, persone ferite o zona non isolata |
| `Levels` | Guided: sbloccato, `TimeLimitSecondsOverride` 0 · Operational: 240 · Crisis: 180 |
| `Complexity` | 4 |
| `Icon` | `ICN_` fulmine (da scegliere tra le icone comuni) |
| `SceneName` | `SCN_ElectricShock` |

Gli stessi valori sono oggi nei campi `[TEMP] Dati missione` di `MNG_Scenario` (sostituto locale di ScenarioController).
