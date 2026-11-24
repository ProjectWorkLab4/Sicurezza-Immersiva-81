// Permette ai builder Editor (assembly SI81.UI.Editor) di scrivere i dati degli asset SO_
// tramite metodi internal, senza esporre setter pubblici al codice di gioco.
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("SI81.UI.Editor")]
