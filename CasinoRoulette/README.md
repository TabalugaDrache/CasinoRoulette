# Roulette Journal

Eine Blazor-Webanwendung mit MudBlazor für die manuelle Erfassung von Ergebnissen beim europäischen Roulette (0 bis 36).

## Starten

Das Projekt benötigt das .NET 11 SDK. Im Projektverzeichnis ausführen:

```powershell
dotnet run
```

Anschließend die in der Konsole angezeigte Adresse im Browser öffnen.

## Verwendung

Eine ganze Zahl von **0 bis 36** ins Eingabefeld eingeben und mit **Enter** (alternativ **Speichern**) speichern. Nach erfolgreicher Speicherung wird das Feld geleert und bleibt für die nächste Eingabe fokussiert. Ungültige Eingaben werden mit einer Fehlermeldung abgewiesen; bei Speicherfehlern bleibt die Eingabe erhalten.

Das kompakte Dashboard passt auf Desktopbildschirme ab 1024 × 640 Pixeln ohne Seitenscrollen. Auf schmalen oder stark vergrößerten Ansichten bleiben die Inhalte durch ein untereinander angeordnetes Layout zugänglich. Der Verlauf zeigt zehn Ergebnisse pro Seite, die neuesten zuerst. Mit **Neuere** und **Ältere** sind alle Einträge ohne wachsende Liste erreichbar. Neue Einträge und Löschaktionen führen zurück zur neuesten Seite. Der letzte Eintrag kann entfernt oder der gesamte Verlauf nach Bestätigung gelöscht werden.

Das Dashboard zeigt die Anzahl der Drehs, die letzte und die häufigste Zahl sowie die Verteilung von Rot, Schwarz und Zero. Bei mehreren häufigsten Zahlen zeigt die Übersicht die kleinste Zahl und die Anzahl gleichauf liegender Zahlen; alle sind über den Tooltip verfügbar. Die fünf häufigsten Zahlen werden mit ihrer absoluten Häufigkeit und ihrem Anteil an allen Einträgen angezeigt. Bei gleicher Häufigkeit werden Zahlen aufsteigend sortiert.

Die Daten werden im lokalen Speicher des jeweiligen Browsers auf dem jeweiligen Gerät gespeichert, nicht auf dem Server. Das Löschen der Browserdaten kann den Verlauf entfernen; andere Browser und Geräte haben einen eigenen Verlauf.

Vergangene Ergebnisse erlauben keine Vorhersage zukünftiger Zahlen. Jeder Dreh ist unabhängig.
