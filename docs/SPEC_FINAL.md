
# Snowblind-Mod Player — Funktionale Spezifikation (FINAL)

Stand: 2026-01-24T22:31:54

Diese Spezifikation beschreibt **funktional** den gewünschten Umfang der App „Snowblind‑Mod Player“. Sie ist so formuliert, dass sie an eine Person oder KI übergeben werden kann, um die App implementieren zu lassen.

---

## 0. Zielbild / Produktidee
Eine Windows‑Desktop‑App, die eine lokale Videobibliothek verwaltet (Import durch Kopieren), ein **Standardvideo** (Favorit) unterstützt und Videos in einem **separaten Player‑Fenster** (Fenster/Vollbild, Multi‑Monitor) abspielen kann. Die App läuft als **Single‑Instance** und bleibt beim Schließen des Hauptfensters im **Tray** aktiv. Autoplay (bei App‑Start) ist möglich, Startverzögerung inklusive. Logging wird in Dateien geschrieben und im UI angezeigt.

---

## 1. Medienverwaltung (Bibliothek) — FINAL

### 1.1 Grundprinzip
- Videos werden **importiert** und **physisch in einen App‑verwalteten Medienordner kopiert** (keine reine Referenzierung).
- Entfernen löscht **Eintrag + Videodatei + Thumbnail**.
- Ein Video kann als **Standardvideo/Favorit** markiert werden.

### 1.2 Medienordner
- Default‑Pfad: `%AppData%\SnowblindModPlayer\media`.
- Medienordner ist in Settings änderbar.
- Beim Wechsel fragt die App:
  - „**Medien in neuen Ordner verschieben?**“
    - **Ja:** Migration (Videos + Thumbnails) + Update aller `storedPath`/`thumbnailPath`.
    - **Nein:** neuer Ordner nur für zukünftige Imports, bestehende bleiben am alten Ort.

### 1.3 Datenmodell (funktional)
Jeder Medieneintrag enthält mindestens:
- `id` (eindeutig)
- `displayName` (Dateiname ohne Extension)
- `originalSourcePath` (Importquelle, zur Duplikatprüfung/Info)
- `storedPath` (Pfad im Medienordner)
- `dateAdded`
- `thumbnailPath`
- Standardvideo wird über `defaultVideoId` in Settings oder `isDefault` abgebildet (max. 1 Standardvideo).

### 1.4 Import (Hinzufügen)
Ablauf (1..n Dateien):
1) Validieren: Datei existiert, ist lesbar, Endung in Whitelist.
2) Duplikatprüfung: **B1** — gleicher `originalSourcePath` darf nur einmal importiert werden → Hinweis, kein Import.
3) Kopieren in Medienordner.
4) Namenskollision **C1**: erzeugt eindeutigen Zielnamen (z. B. `name (1).mp4` oder GUID‑Suffix).
5) Eintrag erstellen und persistieren.
6) Thumbnail erzeugen (siehe 1.6).
7) UI/Tray aktualisieren.

Whitelist (MVP): `.mp4, .mkv, .avi, .mov, .wmv, .webm`.

### 1.5 Entfernen
- Sicherheitsabfrage: „Video wirklich entfernen? **Die Datei wird dauerhaft gelöscht.**“
- Bei Bestätigung:
  - Eintrag löschen
  - Videodatei (`storedPath`) löschen
  - Thumbnail (`thumbnailPath`) löschen
  - Wenn Standardvideo: Standard zurücksetzen.

### 1.6 Thumbnails
- Thumbnail wird beim Import erzeugt.
- Frame‑Zeitpunkt: **5% der Videolänge**.
- Fallback: wenn Länge unbekannt/zu kurz → **1 Sekunde** oder erstes decodierbares Frame.
- Format/Größe: **JPG**, Breite **320px**, Seitenverhältnis **16:9** beibehalten.

### 1.7 Bereinigung beim Start
- Da Medien app‑verwaltet sind: Einträge ohne existierende `storedPath` werden beim Start **automatisch aus der Bibliothek entfernt** (E1).

---

## 2. Wiedergabe (Player) — FINAL

### 2.1 Player‑Fenster
- Wiedergabe erfolgt in einem **separaten Player‑Fenster**.
- Zustände: geschlossen / Fenstermodus / Vollbild.

### 2.2 Steuerung / Hotkeys
- Space: Play/Pause
- Left/Right: Seek ±5s
- Shift+Left/Right: Seek ±30s
- Up/Down: Volume ±5%
- M: Mute toggle
- F11: Vollbild toggle
- ESC im Vollbild: zurück in Fenstermodus (nicht stoppen)
- ESC im Fenstermodus: **Stop + Player schließen**

Maus:
- Klick: Play/Pause
- Doppelklick: Vollbild toggle
- Mausrad: Volume

### 2.3 Ende/Loop
- Loop an: restart bei 0
- Loop aus: Player bleibt offen im „Ende“-Zustand (letztes Frame)

### 2.4 Skalierung
- Default: **„Bildschirm füllen“** (stretch/fill)
- Zusätzlich als Option in Settings: „Seitenverhältnis beibehalten“ (Letterbox)

### 2.5 Start eines neuen Videos während Playback
- **3A**: sofort umschalten (altes stoppt, neues startet), keine Nachfrage.

### 2.6 Startverzögerung
- Bei Autoplay/Startverzögerung: Player öffnet **erst nach Ablauf** der Verzögerung.

### 2.7 Fehlerfälle
- Wenn Video nicht geladen/abspielbar: Fehleroverlay + Logeintrag.

---

## 3. Multi‑Monitor‑Auswahl — FINAL

### 3.1 Zielmonitor
- Nutzer wählt **einen Zielmonitor** für Vollbild.
- Auswahl wird gespeichert und beim Start/Vollbild angewendet.

### 3.2 UI‑Darstellung
- **Grafisches Monitorlayout** wie Windows Anzeigeeinstellungen (Rechtecke, Positionen, Nummern).
- Klick auf Monitor wählt ihn aus.
- Kein „Identifizieren“-Button erforderlich.

### 3.3 Verfügbarkeit
- Wenn gespeicherter Monitor nicht verfügbar: Auswahl wird zurückgesetzt (kein Fallback), Autoplay kann nicht starten.

---

## 4. Navigation & dynamische Titel‑Leiste — FINAL

### 4.1 Seiten
- Hauptfenster mit Sidebar (links): **Videos**, **Logs**, **Settings**.

### 4.2 Titel‑Leiste (kontextabhängig)
- Videos: Buttons in Titel‑Leiste (fixiert, nicht scrollend)
  - `+ Add`
  - Trash/Eimer (Entfernen)
  - Stern (Favorit/Standard)
- Logs:
  - Refresh **immer aktiv**
  - Delete nur bei Auswahl
  - Ordner öffnen immer aktiv
- Settings: keine Kontextbuttons.

### 4.3 Logs‑UI Scrolling
- Log-Dateiliste und Log-Inhalt sind **getrennte, unabhängig scrollbare Bereiche**.

### 4.4 Schließen‑Verhalten Hauptfenster
- X schließt nicht die App → minimiert in den Tray.
- Beenden erfolgt über Tray.
- Bei Autoplay startet die App im Tray (Hauptfenster verborgen).

### 4.5 Sidebar collapsible
- Sidebar ist einklappbar.
- Zustand (`sidebarCollapsed`) wird persistiert und beim Start wiederhergestellt.

---

## 5. Tray‑Integration — FINAL

### 5.1 Tray‑Menü
- Hauptfenster anzeigen
- Standardvideo abspielen
- Bestimmtes Video abspielen → Untermenü (dynamisch)
- Wiedergabe stoppen
- App beenden

### 5.2 Dynamische Videoliste
- Sortierung **C**: Standardvideo oben (★), Rest alphabetisch.

### 5.3 Hinweis bei Schließen
- Beim Schließen des Hauptfensters (X) erscheint ein Windows‑Popup „App läuft weiter im Tray“.
- Option in Settings, um Hinweis auszuschalten. Default: **On**.

### 5.4 Tray‑Icon
- Grundidee: Schneeflocke + Play‑Symbol.
- Favorisierte Stilrichtung: **Option A** (Fluent minimal) mit **besser farblich abgestuftem Play‑Symbol**.
- Finale Gestaltung wird in Mockups/Assets ausgearbeitet.

---

## 6. Logging — FINAL

### 6.1 Logdateien
- Logverzeichnis: dedizierter Ordner (per UI „Ordner öffnen“ erreichbar).
- Pro Tag eine Datei: `YYYY-MM-DD.log`.
- Format je Zeile: ISO‑Timestamp + Level + Modul + Nachricht.

### 6.2 Level
- Error/Warn/Info/Debug.
- Default: **Warn**.
- Änderung wirkt sofort.

### 6.3 Logs‑Seite
- Linke Liste (scrollbar) + rechte Inhaltsansicht (scrollbar), monospaced.
- Aktionen: Refresh (immer), Delete (mit Abfrage), Ordner öffnen.

---

## 7. Settings — FINAL

### 7.1 Defaults
- Autostart: Off
- Autoplay: Off
- Startverzögerung: 0s
- Vollbild beim Start: On
- Loop: On
- Mute: On
- Lautstärke: 50%
- Skalierung: Bildschirm füllen
- Logging-Level: Warn
- Tray‑Hinweis beim Schließen: On
- Medienordner: `%AppData%\SnowblindModPlayer\media`

### 7.2 Autostart (PC‑Start)
- Checkbox „Beim Windows‑Start automatisch ausführen“.
- Funktional: Eintrag wird hinzugefügt/entfernt (Registry Run oder Task Scheduler — technische Wahl später).

### 7.3 View Mode Videos
- Einstellung „View mode“: **List** / **Tile**.
- Default wird gespeichert.

### 7.4 Sprache / Übersetzbarkeit (i18n)
- App nutzt standardmäßig **Windows‑Sprache**.
- Setting: „Systemsprache verwenden“ + Dropdown für feste Sprache.
- Sprachwechsel wirkt sofort und betrifft:
  - UI Texte
  - Tray‑Menü
  - Popups/Toasts

---

## 8. Validierung & Autoplay‑Startlogik — FINAL

### 8.1 Startreihenfolge
Bootstrap → Settings laden → Logging init → Library laden → Preconditions prüfen → UI Sichtbarkeit entscheiden → Autoplay (optional).

### 8.2 Preconditions für Autoplay
- Autoplay enabled
- Standardvideo gesetzt & existiert (gültiger `storedPath`)
- Zielmonitor ausgewählt & vorhanden

### 8.3 Verhalten
- Wenn Autoplay möglich: App bleibt im Tray, Player startet nach Delay.
- Wenn Autoplay nicht möglich: Hauptfenster öffnen + Hinweis (Grund: kein Standardvideo / Monitor fehlt / Fehler).
- Wenn Autoplay deaktiviert: Hauptfenster normal öffnen.

### 8.4 Single Instance
- App läuft nur einmal.
- Zweitstart fokussiert bestehende Instanz, startet **kein** Autoplay erneut.

---

## 9. Videos Seite — List & Tile View (Ergänzung)
- Nutzer kann zwischen **List** und **Tile** Ansicht wechseln (Dropdown in Toolbar).
- Toolbar ist **fixiert** und vom scrollbaren Content abgesetzt.
- In Tile‑Ansicht ist Standardvideo klar markiert (★ Badge/Overlay).

---

## 10. Mockups
Die zugehörigen Windows‑11‑Style Mockups wurden im Chat erzeugt (Videos List/Tile, Settings, Tray-Menü/Popup, Icon‑Varianten).
In diesem Paket liegt eine Datei `mockups/README.md` mit Referenzen und Hinweisen, wie die Bilder ergänzt werden können.
