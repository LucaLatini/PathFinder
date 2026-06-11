# PathFinder

PathFinder è un motore web-based per il calcolo e l'ottimizzazione di percorsi su planimetrie personalizzate, progettato specificamente per applicazioni di **Autonomous Mobile Robots (AMR)**. Sviluppato con .NET 10, integra l'algoritmo A* con ottimizzazioni post-elaborazione (Raycasting) per generare rotte fluide e prive di nodi ridondanti, ideali per la navigazione robotica.

Il sistema è progettato per colmare il divario tra la rappresentazione grafica della mappa e l'esecuzione fisica del movimento, fornendo logiche avanzate per il calcolo dell'orientamento (Heading) e la gestione dinamica delle velocità.

## Caratteristiche Tecniche per AMR

*   **Motore A*:** Ricerca su griglia con granularità delle celle configurabile per adattarsi a diversi ingombri robotici.
*   **Ottimizzazione Raycasting:** Riduzione dei nodi del percorso mantenendo la linea di vista (Line of Sight), fondamentale per minimizzare le fermate e le rotazioni del robot.
*   **Logica di Navigazione:** Calcolo dinamico della direzione (Stop-and-Turn) e valutazione delle velocità basata su profili fisici reali.
*   **Robustezza del Parsing:** Utilizzo di `InvariantCulture` per garantire interoperabilità tra sistemi di bordo e server web, evitando errori di interpretazione numerica.

## Stack Tecnologico

*   **Backend:** ASP.NET Core 10 (MVC)
*   **Grafica:** SkiaSharp per l'analisi e la visualizzazione delle mappe.
*   **Architettura:** Utilizzo estensivo del pattern Strategy per componenti modularizzate (Engines, LOS, Optimizers).

## Setup Rapido

1.  Assicurati di avere installato l'SDK di .NET 10.
2.  Ripristina le dipendenze: `dotnet restore`.
3.  Avvia l'applicazione: `dotnet run --project PathFinder`.

## Configurazione

Le logiche di navigazione possono essere calibrate agendo sui file JSON in `wwwroot`, in particolare `speed_profiles.json` per i limiti di velocità e le accelerazioni del robot.
