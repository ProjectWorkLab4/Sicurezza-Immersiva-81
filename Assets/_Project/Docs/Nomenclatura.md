# Nomenclatura del Progetto

---

## Prefissi

| Prefisso | Cos'è |  
| `CANVAS_` | UI>Canvas |
| `BTN_` | Button |  
| `IMG_` | UI>Image |  
| `PANEL_` | UI>Panel |  
| `LBL_` | UI>Testo o label (stella, spunta, avviso, DPI) |
| `ICN_` | Icone |
| `HUD_` | Elementi in scena durante la missione |  
| `TEX_` | Texture |
| `MAT_` | Materiale |  
| `MESH_` | Modello 3D |  
| `SFX_` | Effetto sonoro breve |  
| `VFX_` | Effetto visivo (fumo, scintille, fuoco) | `VFX_` + fenomeno |  
| `SCN_` | Scena Unity | `SCN_` + nome |  
| `SO_` | ScriptableObject (dati di configurazione) |  
| `FNT_` | Font | `FNT_` + nome |  
| `ANIM_` | Animazione o clip |  
| `MNG_` | Manager |  

## 3. Regole generali

1. **Prefisso in maiuscolo, sempre seguito da underscore.** Il nome dopo il prefisso è in PascalCase senza underscore: `BTN_StartMission`, non `BTN_start_mission`.
2. **Niente spazi, accenti o caratteri speciali.** Evitare `è`, `'`, `/`, `&`, `-`.
3. **Varianti come suffisso.** `BTN_StartMission_Hover`, `TEX_BrickWall_Albedo`.
4. **Il nome descrive la funzione, non il colore.** `BTN_Retry` resta valido se cambia colore, `BTN_RedButton` no.
5. **Stessa cosa, stesso nome.** Un solo termine per ogni funzione, scritto nel README (ad esempio `BTN_Start`, non `BTN_Begin` in un altro punto).

## 4. Regole aggiuntive per UI e testi

- **Nomi tecnici in inglese** I testi finiscono in una tabella di localizzazione con chiavi come `ui.button.start_mission`
- **Il nome in scena corrisponde al nome dell'asset.**  

1. PascalCase per Funzioni, Variabili Pubbliche e Classi
1. camelCase per variabili private e protected
1. UPPERCASE per Costanti
1. Nomi parlanti  
1. Commenti ovunque pls e summary delle funzioni
