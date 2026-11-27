# Audio — SCN_ForkliftChaos

Tutti i clip sono **CC0 1.0** (pubblico dominio): uso commerciale e modifiche consentiti senza attribuzione.

| File | Sorgente | Licenza |
|---|---|---|
| `SFX_FKC_PalletImpact.wav` | Wyzowl SFX Kit — `sounds/impacts/impact-heavy-hard.wav` (https://github.com/wyzowl/sfx-kit) | CC0 1.0 |
| `SFX_FKC_Alarm.wav` | Wyzowl SFX Kit — `sounds/time/time-alarm.wav` (https://github.com/wyzowl/sfx-kit) | CC0 1.0 |
| `SFX_FKC_Horn.wav` | Generato localmente per questo progetto (t accordati + inviluppo) | CC0 1.0 (autore: progetto) |
| `SFX_FKC_Siren.wav` | Generato localmente per questo progetto (wail 620–920 Hz) | CC0 1.0 (autore: progetto) |
| `SFX_FKC_Scream.wav` | Generato localmente per questo progetto (tono discendente + rumore) | CC0 1.0 (autore: progetto) |

I clip sono caricati a runtime con `Resources.Load<AudioClip>("Audio/<nome>")` e riprodotti da `ForkliftChaosScenario` tramite un `AudioSource` 2D.

Mapping eventi:
- `SFX_FKC_Scream` → EV-01 urlo di Marco
- `SFX_FKC_Horn` → EV-02 clacson di Muletto 2
- `SFX_FKC_Siren` → arrivo soccorsi / fine scenario
- `SFX_FKC_PalletImpact` → impatto del pallet a terra
