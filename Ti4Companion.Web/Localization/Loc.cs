using Ti4Companion.Shared;

namespace Ti4Companion.Web.Localization;

/// <summary>
/// Lightweight in-memory localization. UI strings live in <see cref="Strings"/> (EN/DE); game
/// content carries both languages from the API and is resolved with <see cref="Pick"/>.
/// </summary>
public class Loc
{
    public Language Lang { get; private set; } = Language.En;
    public event Action? OnChange;

    public void SetLanguage(Language lang)
    {
        if (Lang == lang) return;
        Lang = lang;
        OnChange?.Invoke();
    }

    public string Pick(string? en, string? de)
        => Lang == Language.De && !string.IsNullOrWhiteSpace(de) ? de! : (en ?? string.Empty);

    public string this[string key]
        => Strings.TryGetValue(key, out var v) ? Pick(v.En, v.De) : key;

    private static readonly Dictionary<string, (string En, string De)> Strings = new()
    {
        ["app.title"] = ("Twilight Imperium Companion", "Twilight Imperium Companion"),
        ["app.subtitle"] = ("Match companion", "Partie-Begleiter"),

        ["home.welcome"] = ("Choose how to start", "Wähle, wie du startest"),
        ["home.createSession"] = ("Create session", "Session erstellen"),
        ["home.joinSession"] = ("Join session", "Session beitreten"),
        // Third choice on the start page. PLACEHOLDER — it opens an empty long form that is still to be
        // filled; rename this key (and `home.moreHint`) once it is decided what goes in there.
        ["home.more"] = ("More", "Mehr"),
        ["home.moreHint"] = ("Nothing here yet.", "Hier ist noch nichts."),
        ["home.back"] = ("Back", "Zurück"),
        ["home.sessionName"] = ("Session name", "Session-Name"),
        ["home.yourName"] = ("Your name", "Dein Name"),
        ["home.joinCode"] = ("Join code", "Beitritts-Code"),
        ["home.expansions"] = ("Active expansions", "Aktive Erweiterungen"),
        ["home.create.button"] = ("Create & open", "Erstellen & öffnen"),
        ["home.join.button"] = ("Join", "Beitreten"),
        ["home.asPlayer"] = ("As player", "Als Spieler"),
        ["home.asDisplay"] = ("As display", "Als Display"),
        ["home.displayHint"] = ("Opens the shared wall view for this code (e.g. on a beamer/TV).", "Öffnet die Wandansicht für diesen Code (z. B. am Beamer/TV)."),
        ["home.openDisplay"] = ("Open display", "Display öffnen"),
        // The start page's list of sessions THIS DEVICE has played (max SessionStore.MaxRecent).
        ["home.recent"] = ("Resume a session", "Session fortsetzen"),
        ["home.recentTitle"] = ("Sessions on this device", "Sessions auf diesem Gerät"),
        ["home.recentAs"] = ("as", "als"),
        ["home.recentUnnamed"] = ("Session", "Session"),
        ["home.notfound"] = ("No session found for that code.", "Keine Session für diesen Code gefunden."),
        ["home.continue"] = ("Continue", "Weiter"),
        // Activity counters on the landing page. The label says WHAT, never the rule behind it.
        ["home.activeNow"] = ("Games running", "Laufende Partien"),
        ["home.active24h"] = ("Played today", "Partien heute"),
        ["join.pickIdentity"] = ("Who are you? Take over a player, or create a new one.", "Wer bist du? Übernimm einen Spieler oder erstelle einen neuen."),
        ["join.takeOver"] = ("Take over", "Übernehmen"),
        ["join.takeOverHost"] = ("Take over host", "Host übernehmen"),
        ["join.createNew"] = ("Create new player", "Neuen Spieler erstellen"),

        ["nav.beamer"] = ("Display", "Display"),
        ["nav.invite"] = ("Invite link", "Einladungslink"),
        ["nav.openDisplay"] = ("Open the wall display in a new tab", "Wandanzeige in neuem Tab öffnen"),
        ["nav.joinQr"] = ("QR code to join", "QR-Code zum Beitreten"),
        ["display.qrShort"] = ("QR", "QR"),
        ["nav.joinQrTitle"] = ("Join {0}", "{0} beitreten"),
        ["display.shows"] = ("Display shows", "Display zeigt"),
        ["display.control"] = ("Display control", "Displaysteuerung"),
        ["display.qrShow"] = ("Show the join QR code on the display", "QR-Code zum Beitreten auf dem Display zeigen"),
        ["display.qrHide"] = ("Hide the join QR code", "QR-Code zum Beitreten ausblenden"),
        ["display.objectives"] = ("Objectives", "Aufträge"),
        ["display.secondary"] = ("Secondary abilities", "Sekundärfähigkeiten"),
        ["display.secondaryShort"] = ("Secondary", "Sekundär"),
        ["display.tech"] = ("Technologies", "Technologien"),
        ["display.techShort"] = ("Tech", "Tech"),
        ["nav.phase"] = ("Phase", "Phase"),
        ["nav.objectives"] = ("Objectives", "Aufträge"),
        ["nav.techs"] = ("Technologies", "Technologien"),
        ["nav.players"] = ("Players", "Spieler"),
        ["nav.settings"] = ("Settings", "Einstellungen"),
        ["nav.leave"] = ("Leave", "Verlassen"),
        ["nav.leaveConfirm"] = ("Leave this session on this device?", "Diese Session auf diesem Gerät verlassen?"),

        ["phase.Setup"] = ("Setup", "Aufstellung"),
        ["phase.Strategy"] = ("Strategy", "Strategie"),
        ["phase.Action"] = ("Action", "Aktion"),
        ["phase.Status"] = ("Status", "Status"),
        ["phase.Agenda"] = ("Agenda", "Agenda"),

        ["turn.round"] = ("Round", "Runde"),
        ["turn.active"] = ("Active player", "Aktiver Spieler"),
        ["turn.advance"] = ("Next turn", "Nächster Zug"),
        ["turn.previous"] = ("Previous turn", "Vorheriger Zug"),
        ["turn.startGame"] = ("Start game", "Spiel starten"),
        ["turn.toAction"] = ("Begin action phase", "Aktionsphase beginnen"),
        ["turn.endAction"] = ("End action phase", "Aktionsphase beenden"),
        ["turn.toAgenda"] = ("Agenda phase", "Agendaphase"),
        ["turn.nextRound"] = ("Next round", "Nächste Runde"),
        ["turn.passed"] = ("Passed", "Gepasst"),
        ["turn.pass"] = ("Pass", "Passen"),
        ["turn.unpass"] = ("Un-pass", "Pass zurück"),
        ["turn.passHint"] = ("Play all your strategy actions before passing.", "Spiele erst alle deine Strategieaktionen, bevor du passt."),
        ["turn.allPassed"] = ("All players have passed.", "Alle Spieler haben gepasst."),
        ["turn.youAreUp"] = ("You're up", "Du bist dran"),
        ["turn.noActive"] = ("No active player.", "Kein aktiver Spieler."),
        ["turn.loggedInAs"] = ("You", "Du"),
        ["turn.actionDone"] = ("Action done", "Aktion ausgeführt"),
        ["turn.playAction"] = ("Play action", "Aktion spielen"),
        ["turn.strategyAction"] = ("Strategy action", "Strategieaktion"),
        ["turn.notDone"] = ("Not done", "Nicht ausgeführt"),
        ["turn.endHighlight"] = ("End highlight", "Hervorhebung beenden"),
        ["turn.takeOver"] = ("Take over", "Übernehmen"),
        ["turn.takeOverEnd"] = ("End takeover", "Übernahme beenden"),
        ["turn.endGame"] = ("End game", "Spiel beenden"),
        // Host-only game options (setup).
        ["options.title"] = ("Game options", "Spieloptionen"),
        ["cards.perPlayer"] = ("Strategy cards per player", "Strategiekarten pro Spieler"),
        // Short labels on purpose: the rule itself belongs in the hint next to the field, not inside a
        // <select> option, where the long form was wider than the whole panel on a phone.
        ["cards.auto"] = ("Automatic", "Automatisch"),
        ["cards.hint"] = (
            "Automatic follows the printed rule: 2 cards with up to 4 players, otherwise 1. Pin a number for variants like Feast or Famine, where a four-player table takes a single card each.",
            "„Automatisch“ folgt der gedruckten Regel: 2 Karten bei bis zu 4 Spielern, sonst 1. Feste Zahl für Varianten wie Feast or Famine, bei denen eine Vierer-Runde nur eine Karte pro Spieler nimmt."),
        // Red Tape: two published community variants. The app tapes the objectives, blocks scoring a taped
        // one and hands the removal to one strategy card; the rest of each variant's rules (purging, the
        // Stage II gate, when the random removal happens) stays with the table. Rules per their authors:
        // "Bureaucracy: Red Tape for TI4" by WildFalkon (BGG file 221470) and "Red Tape Lite" by
        // van nguyen (BGG thread 3553379).
        ["redtape.title"] = ("Red Tape variant", "Red-Tape-Variante"),
        ["redtape.off"] = ("Off", "Aus"),
        ["redtape.bureaucracy"] = ("Bureaucracy: Red Tape", "Bürokratie: Red Tape"),
        ["redtape.lite"] = ("Red Tape Lite", "Red Tape Lite"),
        ["redtape.hint"] = (
            "Community variants: every public objective lies face-up from the start with its points taped over, and one strategy card pulls the tape off. A taped objective cannot be scored — so everyone can plan ahead, veteran or not.",
            "Community-Varianten: Alle öffentlichen Aufträge liegen von Anfang an offen, ihre Punkte aber mit einem Band abgedeckt, und eine Strategiekarte entfernt das Band. Ein versiegelter Auftrag kann nicht gewertet werden — so kann jeder vorausplanen, ob Neuling oder Veteran."),
        ["redtape.bureaucracyHint"] = (
            "Setup: the speaker lays out five Stage I and five Stage II objectives face-up and puts a marker over the victory point value of every one except the first two, which count as revealed. A taped objective cannot be claimed and counts as unrevealed. Variant by WildFalkon.",
            "Aufbau: Der Sprecher legt fünf Stufe-I- und fünf Stufe-II-Aufträge offen aus und deckt bei allen außer den ersten zwei — die gelten als aufgedeckt — den Siegpunktwert mit einem Marker ab. Ein versiegelter Auftrag kann nicht gewertet werden und gilt als nicht aufgedeckt. Variante von WildFalkon."),
        ["redtape.liteHint"] = (
            "Setup: reveal seven (or six) Stage I and five Stage II objectives; the first Stage I objectives start untaped. Only five Stage I can ever score — when the fifth tape comes off, Stage I #6 and #7 are purged — and no Stage II tape comes off before those five are clear. Variant by van nguyen.",
            "Aufbau: sieben (oder sechs) Stufe-I- und fünf Stufe-II-Aufträge aufdecken; die ersten Stufe-I-Aufträge liegen frei. Nur fünf Stufe-I-Aufträge können überhaupt gewertet werden — sobald das fünfte Band fällt, werden Stufe I Nr. 6 und 7 entfernt — und kein Stufe-II-Band geht ab, bevor diese fünf frei sind. Variante von van nguyen."),
        ["redtape.card"] = ("Card that removes the tape", "Karte, die das Band entfernt"),
        ["redtape.cardHint"] = (
            "Replace either — but not both — Diplomacy or Imperial with the matching Bureaucracy card. Everything else on it stays the printed card.",
            "Ersetze entweder Diplomatie oder Imperium — nicht beide — durch die passende Bürokratie-Karte. Alles andere darauf bleibt die gedruckte Karte."),
        // The addition the variant prints on the carrier card, shown above its base text (see StrategyCardView).
        ["redtape.cardLabel"] = ("Red Tape", "Red Tape"),
        ["redtape.cardSpecial"] = (
            "SPECIAL: After selecting this strategy card, remove Red Tape counters equal to the number of trade goods on this card.",
            "SPEZIELL: Nachdem du diese Strategiekarte gewählt hast, entferne so viele Red-Tape-Marker, wie Handelswaren auf dieser Karte liegen."),
        ["redtape.cardPrimary"] = (
            "Remove 1 Red Tape counter from the public objective of your choice. You may not choose a Stage II objective in the first 3 rounds.",
            "Entferne 1 Red-Tape-Marker von einem öffentlichen Auftrag deiner Wahl. In den ersten 3 Runden darfst du keinen Stufe-II-Auftrag wählen."),
        // Bureaucracy replaces the status phase's reveal step (rulebook insert, step 12).
        ["redtape.stepReplaced"] = ("Check for the end of the game", "Spielende prüfen"),
        ["redtape.stepReplacedHint"] = (
            "Red Tape replaces \"reveal public objective\": nothing is revealed — the game ends if there are no unrevealed public objectives at the start of this step.",
            "Red Tape ersetzt „Öffentlichen Auftrag aufdecken“: Es wird nichts aufgedeckt — das Spiel endet, wenn zu Beginn dieses Schritts kein nicht aufgedeckter öffentlicher Auftrag mehr liegt."),
        // The card action itself (shown while that strategy action is on the table).
        ["redtape.action"] = ("Red Tape: remove tape", "Red Tape: Band entfernen"),
        ["redtape.actionHint"] = (
            "Remove the tape before resolving the rest of the card.",
            "Entferne das Band, bevor der Rest der Karte abgehandelt wird."),
        ["redtape.actionBureaucracy"] = (
            "Remove one tape of your choice, plus one per trade good that was on this card. No Stage II objective in the first three rounds.",
            "Entferne ein Band nach Wahl, dazu eines pro Handelsware, die auf dieser Karte lag. In den ersten drei Runden kein Stufe-II-Auftrag."),
        ["redtape.actionLite"] = (
            "Remove one tape of your choice — no Stage II until the five scorable Stage I are clear. No random removal this round.",
            "Entferne ein Band nach Wahl — kein Stufe II, solange die fünf wertbaren Stufe-I-Aufträge nicht frei sind. Diese Runde fällt die Zufallsentnahme weg."),
        ["redtape.openObjectives"] = ("Open objectives", "Aufträge öffnen"),
        ["redtape.randomHint"] = (
            "Nobody took the card this round, so one tape comes off at random — the app does it right after the strategy phase in round 1, and when the status phase ends after that.",
            "Diese Runde hat niemand die Karte genommen, also geht ein zufälliges Band ab — die App macht das in Runde 1 direkt nach der Strategiephase, danach jeweils am Ende der Statusphase."),
        // Why a tape cannot be pulled right now (the server refuses the same, see RedTape).
        ["redtape.purged"] = ("PURGED — CAN NEVER BE SCORED", "ENTFERNT — NICHT MEHR WERTBAR"),
        ["redtape.lockedRounds"] = ("STAGE II — LOCKED FOR THE FIRST 3 ROUNDS", "STUFE II — DIE ERSTEN 3 RUNDEN GESPERRT"),
        ["redtape.lockedStageI"] = ("STAGE II — NOT UNTIL 5 STAGE I ARE CLEAR", "STUFE II — ERST WENN 5 STUFE-I-AUFTRÄGE FREI SIND"),
        ["redtape.takeOff"] = ("Tap the tape to remove it", "Zum Entfernen auf das Band tippen"),
        // The tape is the only label — a separate "SEALED" chip said the same thing twice.
        ["redtape.blocked"] = ("SEALED — TAP THE TAPE", "VERSIEGELT — AUF DAS BAND TIPPEN"),
        ["redtape.confirmRemove"] = ("TAP AGAIN TO REMOVE", "NOCHMAL TIPPEN ZUM ENTFERNEN"),
        ["redtape.putBack"] = ("Put the tape back", "Band zurücklegen"),
        // Optional technology prompt after the Technology strategy action.
        ["tech.promptOption"] = ("Ask for the technology after the Technology action",
                                 "Nach der Technologie-Aktion zur Techeingabe auffordern"),
        ["tech.promptHint"] = (
            "Shows a shortcut to the technology tab after that action is played. A reminder, not a requirement.",
            "Zeigt nach dieser Aktion eine Verknüpfung zum Technologie-Reiter. Eine Erinnerung, keine Pflicht."),
        ["tech.promptText"] = ("Technology action played — record the researched technology?",
                               "Technologie-Aktion gespielt — erforschte Technologie erfassen?"),
        ["tech.promptOpen"] = ("Open technologies", "Technologien öffnen"),
        // Per-player turn timer (informational only — never enforced).
        ["timer.remaining"] = ("Time left this round", "Restzeit diese Runde"),
        ["timer.over"] = ("Time budget used up", "Zeitbudget aufgebraucht"),
        ["timer.title"] = ("Turn timer", "Zug-Timer"),
        ["timer.perRound"] = ("Minutes per player per round", "Minuten pro Spieler pro Runde"),
        ["timer.off"] = ("Off", "Aus"),
        ["timer.hint"] = (
            "Counts down only during that player's own turns in the strategy and action phase, pauses with the game, and resets each strategy phase. Running out is only signalled — nothing is blocked.",
            "Läuft nur während der eigenen Züge in Strategie- und Aktionsphase, pausiert mit dem Spiel und wird jede Strategiephase zurückgesetzt. Ein Ablauf wird nur signalisiert — blockiert wird nichts."),
        ["pause.pause"] = ("Pause game", "Spiel pausieren"),
        ["pause.resume"] = ("Resume", "Fortsetzen"),
        ["pause.title"] = ("Game paused", "Spiel pausiert"),
        ["pause.waiting"] = ("Waiting for the host to resume…", "Warte, bis der Host fortsetzt…"),
        ["turn.isUp"] = ("is up", "ist dran"),
        ["turn.notYourTurn"] = ("Not your turn", "Du bist nicht dran"),
        ["turn.notYour"] = ("It's not your turn — the host can take over.", "Du bist nicht dran — der Host kann übernehmen."),
        ["turn.primary"] = ("Primary", "Primär"),
        ["turn.secondary"] = ("Secondary", "Sekundär"),

        ["strategy.title"] = ("Strategy phase — pick cards", "Strategiephase — Karten wählen"),
        ["strategy.pickFor"] = ("Pick for", "Wählen für"),
        ["strategy.detailed"] = ("Detailed", "Detailliert"),
        ["strategy.namesOnly"] = ("Names only", "Nur Namen"),
        ["strategy.available"] = ("Available cards", "Verfügbare Karten"),
        ["strategy.maxReached"] = ("Card limit reached", "Kartenlimit erreicht"),
        ["strategy.tradeGood"] = ("trade good", "Handelsgut"),
        ["strategy.tradeGoods"] = ("trade goods", "Handelsgüter"),
        ["strategy.take"] = ("Take", "Nehmen"),
        ["strategy.unpick"] = ("Return", "Zurücklegen"),
        ["strategy.picking"] = ("Now choosing", "Es wählt"),
        ["strategy.allPicked"] = ("All cards chosen", "Alle Karten gewählt"),
        ["strategy.waitYourTurn"] = ("Wait — it's not your turn to choose yet.", "Warte — du bist noch nicht mit Wählen dran."),
        ["strategy.needAllPicks"] = ("All players must pick their cards first", "Erst müssen alle Spieler ihre Karten wählen"),

        ["setup.title"] = ("Setup — choose faction & color", "Aufstellung — Fraktion & Farbe wählen"),
        ["setup.chooseFaction"] = ("Faction", "Fraktion"),
        ["setup.chooseColor"] = ("Color", "Farbe"),
        ["setup.ready"] = ("Ready", "Bereit"),
        ["setup.readyCount"] = ("Ready", "Bereit"),
        ["setup.notReady"] = ("Not ready", "Nicht bereit"),
        ["setup.imReady"] = ("I'm ready", "Ich bin bereit"),
        ["setup.waiting"] = ("Waiting for players to get ready…", "Warte, bis die Spieler bereit sind…"),
        ["setup.startHint"] = ("The host starts the game once everyone is ready.", "Der Host startet das Spiel, sobald alle bereit sind."),
        ["setup.searchFaction"] = ("Search faction…", "Fraktion suchen…"),
        ["setup.needFactionColor"] = ("Choose a faction and a colour to ready up.", "Wähle Fraktion und Farbe, um bereit zu sein."),
        // The speaker is picked in the seat dialog (and is not required to start) — see PhaseView.
        ["setup.speakerLbl"] = ("Speaker set — tap a row to change it.", "Sprecher festgelegt — zum Ändern eine Zeile antippen."),
        ["setup.noSpeakerYet"] = ("No speaker yet — tap a row, or roll for it.", "Noch kein Sprecher — Zeile antippen oder auslosen."),
        ["setup.randomSpeaker"] = ("Pick at random", "Zufällig wählen"),
        // Seat-order confirmation dialog shown before the game actually starts.
        ["setup.confirmSeatTitle"] = ("Is the seating order right?", "Passt die Sitzreihenfolge?"),
        ["setup.confirmSeatHint"] = (
            "This is the order around the table (speaker marked). You can change it with the ▲▼ arrows in the player list.",
            "So sitzt ihr am Tisch (Sprecher markiert). Ändern kannst du die Reihenfolge mit den ▲▼-Pfeilen in der Spielerliste."),
        ["setup.confirmSeatDragHint"] = (
            "Drag a row by its handle to change the order, or use the arrows. The speaker is marked.",
            "Zeile am Griff ziehen, um die Reihenfolge zu ändern — oder die Pfeile nutzen. Der Sprecher ist markiert."),
        ["setup.dragSeat"] = ("Drag to reorder", "Ziehen zum Umsortieren"),
        ["setup.confirmSeatOk"] = ("Looks good — start", "Passt — Spiel starten"),
        ["setup.confirmSeatChange"] = ("Change it", "Noch ändern"),
        ["setup.maxPlayers"] = ("Maximum of 8 players reached.", "Maximal 8 Spieler erreicht."),
        ["setup.deselectColor"] = ("Click to deselect (frees the colour)", "Klicken zum Abwählen (gibt die Farbe frei)"),
        ["setup.seatOrder"] = ("Seat order", "Sitzreihenfolge"),
        ["setup.speaker"] = ("Speaker", "Sprecher"),
        ["setup.makeSpeaker"] = ("Speaker", "Sprecher"),
        ["setup.moveUp"] = ("Move up", "Nach oben"),
        ["setup.moveDown"] = ("Move down", "Nach unten"),
        ["setup.objectives"] = ("Starting objectives", "Anfangsaufträge"),
        ["setup.objectivesHint"] = ("At the end of setup, record the 2 public objectives you drew.", "Trage am Ende der Aufstellung die 2 gezogenen öffentlichen Aufträge ein."),
        ["setup.objectivesHintRedTape"] = (
            "Red Tape: record every public objective you laid out — the first two Stage I are open, the rest come up taped.",
            "Red Tape: Trage alle ausgelegten öffentlichen Aufträge ein — die ersten zwei Stufe-I-Aufträge liegen frei, der Rest kommt versiegelt."),
        // Setup runs as steps (host view). Non-hosts always see the player step — see SetupView.
        ["setup.step.name"] = ("Session", "Session"),
        ["setup.step.options"] = ("Variant & options", "Variante & Optionen"),
        ["setup.step.players"] = ("Players", "Spieler"),
        ["setup.step.seating"] = ("Seating", "Sitzordnung"),
        ["setup.step.objectives"] = ("Objectives", "Aufträge"),
        ["setup.nameHint"] = ("What is this game called? It shows on the wall display.",
                              "Wie heißt diese Partie? Der Name steht auf der Wandanzeige."),
        ["setup.expansionsHint"] = ("Decides which factions, technologies and cards the app offers. The base game is always in.",
                                    "Bestimmt, welche Fraktionen, Technologien und Karten die App anbietet. Das Grundspiel ist immer dabei."),
        ["setup.seatingHint"] = ("Drag by the handle or use the arrows, and tap a row to make that player speaker.",
                                 "Am Griff ziehen oder die Pfeile nutzen; für den Sprecher eine Zeile antippen."),
        ["host.only"] = ("Host", "Host"),
        ["host.onlyHint"] = ("Only the host controls the phases.", "Nur der Host steuert die Phasen."),
        ["host.takeOver"] = ("Play for this player", "Für diesen Spieler spielen"),
        ["host.pickFor"] = ("Pick for this player", "Für diesen Spieler wählen"),
        ["host.manage"] = ("Manage (host)", "Verwalten (Host)"),

        ["status.title"] = ("Status phase — score objectives", "Statusphase — Aufträge werten"),
        ["status.reveal"] = ("Reveal a new public objective", "Neuen öffentlichen Auftrag aufdecken"),
        ["status.hint"] = ("Score objectives, then reveal a new one and continue.", "Werte Aufträge, decke dann einen neuen auf und mach weiter."),
        // Scoring in initiative order (guided, not enforced) + the shared checklist of the later steps.
        ["status.scoringOrder"] = ("Scoring in initiative order", "Wertung in Initiativreihenfolge"),
        ["status.yourTurn"] = ("Your turn to score.", "Du bist mit dem Werten dran."),
        ["status.waitingFor"] = ("Waiting for {0}.", "Warte auf {0}."),
        ["status.allScored"] = ("Everyone has scored", "Alle haben gewertet"),
        ["status.done"] = ("Done", "Fertig"),
        ["status.undoDone"] = ("Take the turn back", "Zug zurücknehmen"),
        ["status.checklist"] = ("Remaining steps", "Restliche Schritte"),
        ["status.step.revealObjective"] = ("Reveal objective", "Ziel aufdecken"),
        ["status.step.drawActionCards"] = ("Draw action cards", "Aktionskarte ziehen"),
        ["status.step.commandTokens"] = ("Discard / gain command tokens", "Kommandomarker abwerfen/erhalten"),
        ["status.step.readyCards"] = ("Ready cards", "Karten bereitmachen"),
        ["status.step.repairUnits"] = ("Repair units", "Einheiten reparieren"),
        ["status.step.returnStrategyCards"] = ("Return strategy cards", "Strategiekarten zurückgeben"),
        // Not a rulebook step: the thing that gets forgotten once the checklist is ticked (Sol's Genesis).
        ["status.step.endAbilities"] = ("Abilities that trigger during or at the end of the status phase",
                                       "Fähigkeiten, die in oder am Ende der Statusphase auslösen"),
        // The three stages the phase is walked through.
        ["status.stage.scoring"] = ("Score", "Wertung"),
        ["status.stage.reveal"] = ("Reveal objective", "Auftrag aufdecken"),
        ["status.stage.checklist"] = ("Remaining steps", "Restliche Schritte"),
        ["status.next"] = ("Next", "Weiter"),
        ["status.scoreFor"] = ("{0} may score", "{0} darf werten"),
        ["status.tapToScore"] = ("Tap an objective card to score it.", "Auftragskarte antippen, um sie zu werten."),
        ["status.nothingLeft"] = ("This player has already scored every revealed objective.",
                                 "Dieser Spieler hat schon alle aufgedeckten Aufträge gewertet."),
        ["status.revealHint"] = ("The speaker reveals the next public objective; it is shown large on the display.",
                                "Der Sprecher deckt den nächsten öffentlichen Auftrag auf; er wird groß auf der Anzeige gezeigt."),
        // Long form for the WALL display. English is transcribed verbatim from the TI4 wiki's Status Phase
        // article (a verified source, see CLAUDE.md); the German is rendered with the table's own
        // terminology and STILL WANTS A CHECK against the printed German rulebook.
        ["status.detail.reveal"] = (
            "The speaker flips the next unrevealed public objective card face-up. The first stage II objective is not revealed until all stage I objectives have been revealed.",
            "Der Sprecher deckt die nächste verdeckte öffentliche Auftragskarte auf. Der erste Auftrag der Stufe II wird erst aufgedeckt, wenn alle Aufträge der Stufe I aufgedeckt sind."),
        ["status.detail.drawActionCards"] = (
            "In initiative order, each player draws one card from the top of the action card deck.",
            "In Initiativreihenfolge zieht jeder Spieler eine Karte vom Aktionskartenstapel."),
        ["status.detail.commandTokens"] = (
            "Each player removes all of their command tokens from the game board and returns them to their reinforcements. Then each player gains two command tokens and can redistribute the tokens on their command sheet among their strategy, tactic and fleet pools.",
            "Jeder Spieler nimmt alle seine Kommandomarker vom Spielplan und legt sie in seinen Nachschub zurück. Danach erhält jeder Spieler zwei Kommandomarker und darf die Marker auf seiner Kommandotafel neu auf Strategie-, Taktik- und Flottenpool verteilen."),
        ["status.detail.readyCards"] = (
            "Each player readies all of their exhausted cards, including strategy cards.",
            "Jeder Spieler macht alle seine erschöpften Karten bereit, auch die Strategiekarten."),
        ["status.detail.repairUnits"] = (
            "Each player repairs all of their damaged units by turning those units upright.",
            "Jeder Spieler repariert alle seine beschädigten Einheiten, indem er sie wieder aufrichtet."),
        ["status.detail.returnStrategyCards"] = (
            "Each player returns their strategy card to the common play area.",
            "Jeder Spieler legt seine Strategiekarte in den gemeinsamen Spielbereich zurück."),
        ["status.detail.endAbilities"] = (
            "Check for abilities that trigger during or at the end of the status phase — the Sol flagship Genesis, for example.",
            "Auf Fähigkeiten achten, die in oder am Ende der Statusphase auslösen — zum Beispiel das Sol-Flaggschiff Genesis."),

        ["agenda.freeVote"] = ("Free vote", "Freie Abstimmung"),
        ["agenda.freeVoteHint"] = ("Vote on something without an agenda card — pick what is being elected.", "Über etwas abstimmen, für das es keine Agendakarte gibt — wähle, was gewählt wird."),
        ["agenda.freeVoteTitle"] = ("What is being voted on?", "Worüber wird abgestimmt?"),
        ["agenda.freeVoteStart"] = ("Start the free vote", "Freie Abstimmung starten"),
        ["agenda.freeVoteEnd"] = ("End the free vote", "Freie Abstimmung beenden"),
        ["elect.ForAgainst"] = ("For / Against", "Dafür / Dagegen"),
        ["elect.Player"] = ("Elect a player", "Spieler wählen"),
        ["elect.Planet"] = ("Elect a planet", "Planet wählen"),
        ["elect.CulturalPlanet"] = ("Elect a cultural planet", "Kulturplanet wählen"),
        ["elect.HazardousPlanet"] = ("Elect a hazardous planet", "Gefahrenplanet wählen"),
        ["elect.IndustrialPlanet"] = ("Elect an industrial planet", "Industrieplanet wählen"),
        ["elect.NonHomePlanet"] = ("Elect a non-home planet", "Planet außerhalb der Heimat wählen"),
        ["elect.Law"] = ("Elect a law", "Gesetz wählen"),
        ["elect.StrategyCard"] = ("Elect a strategy card", "Strategiekarte wählen"),
        ["elect.ScoredSecret"] = ("Elect a scored secret objective", "Gewerteten geheimen Auftrag wählen"),
        ["agenda.title"] = ("Agenda phase", "Agendaphase"),
        ["agenda.search"] = ("Search agenda…", "Agenda suchen…"),
        ["agenda.law"] = ("Law", "Gesetz"),
        ["agenda.directive"] = ("Directive", "Direktive"),
        ["agenda.elect"] = ("Elect", "Wähle"),
        ["agenda.for"] = ("For", "Dafür"),
        ["agenda.against"] = ("Against", "Dagegen"),
        ["agenda.abstain"] = ("Abstain", "Enthalten"),
        ["agenda.votes"] = ("Votes", "Stimmen"),
        ["agenda.tally"] = ("Tally", "Auszählung"),
        ["agenda.revealNew"] = ("Reveal another agenda", "Weitere Agenda aufdecken"),
        ["agenda.end"] = ("End agenda phase", "Agendaphase beenden"),
        ["agenda.none"] = ("No agenda revealed. Search to reveal one.", "Keine Agenda aufgedeckt. Suche, um eine aufzudecken."),
        ["agenda.noVotes"] = ("No votes cast yet.", "Noch keine Stimmen abgegeben."),
        ["agenda.elected"] = ("Elected", "Gewählt"),
        ["agenda.leading"] = ("Leading", "Führend"),
        ["agenda.passed"] = ("Agenda passed", "Agenda angenommen"),
        ["agenda.rejected"] = ("Agenda rejected", "Agenda abgelehnt"),
        ["agenda.speakerDecides"] = ("Tie — the speaker decides", "Gleichstand — der Sprecher entscheidet"),
        ["agenda.choose"] = ("Choose candidate…", "Kandidat wählen…"),
        ["agenda.candidate"] = ("Candidate", "Kandidat"),
        ["agenda.freeText"] = ("Type a name…", "Namen eingeben…"),
        ["agenda.secret"] = ("Hide votes", "Wahl verdecken"),
        ["agenda.reveal"] = ("Reveal votes", "Stimmen aufdecken"),
        ["agenda.hidden"] = ("Votes are hidden until the host reveals them.", "Stimmen sind verdeckt, bis der Host sie aufdeckt."),
        // Intermediate step of a face-down vote: totals public, attribution still secret.
        ["agenda.revealTotals"] = ("Reveal totals only", "Nur Summen aufdecken"),
        ["agenda.totalsOnly"] = ("Totals only — who voted how is still hidden.",
                                 "Nur die Summen — wer wie gestimmt hat, bleibt verdeckt."),
        ["agenda.totalsShown"] = ("Totals are public.", "Die Summen sind aufgedeckt."),
        ["agenda.voted"] = ("Voted", "Abgestimmt"),
        ["agenda.waiting"] = ("Waiting…", "Wartet…"),
        ["agenda.voteFor"] = ("Vote", "Abstimmen"),
        ["agenda.lock"] = ("Lock", "Sperren"),
        ["agenda.set"] = ("Set", "Festlegen"),
        ["agenda.castVote"] = ("Cast vote", "Stimme abgeben"),
        ["agenda.locked"] = ("Locked", "Gesperrt"),
        ["agenda.youVoted"] = ("You voted — waiting for the others / the host.", "Du hast abgestimmt — warte auf die anderen / den Host."),
        ["agenda.allVoted"] = ("Everyone has voted.", "Alle haben abgestimmt."),
        ["agenda.repeatVote"] = ("Repeat vote", "Abstimmung wiederholen"),
        ["agenda.reset"] = ("Reset all votes", "Alle Stimmen zurücksetzen"),
        ["agenda.hostHint"] = ("Tap a player to enter their vote, then lock it before passing the tablet on.", "Tippe auf einen Spieler, um seine Stimme einzugeben, und sperre sie, bevor du das Tablet weitergibst."),
        ["agenda.lockAllFirst"] = ("Lock every vote before revealing.", "Erst alle Stimmen sperren, dann aufdecken."),
        ["agenda.influence"] = ("Influence", "Einfluss"),
        ["agenda.enterInfluence"] = ("Enter your available influence", "Trage deinen verfügbaren Einfluss ein"),
        ["agenda.influenceHint"] = ("Influence isn't a vote cap — you can vote beyond it.", "Einfluss ist keine Stimmen-Grenze — du kannst darüber hinaus stimmen."),
        ["agenda.waitingForHost"] = ("Waiting for the host to reveal an agenda…", "Warte, bis der Host eine Agenda aufdeckt…"),
        ["agenda.pick"] = ("Reveal an agenda", "Eine Agenda aufdecken"),
        ["agenda.startVote"] = ("Start vote", "Abstimmung starten"),
        ["agenda.startVoteHidden"] = ("Start hidden vote", "Verdeckte Abstimmung starten"),
        ["agenda.cancelVote"] = ("Cancel vote", "Abstimmung abbrechen"),
        ["agenda.castFor"] = ("Vote", "Stimmen"),
        ["agenda.notStarted"] = ("Voting hasn't started.", "Abstimmung läuft noch nicht."),

        // Match statistics + log
        ["stats.title"] = ("Match statistics", "Match-Statistik"),
        ["stats.button"] = ("Statistics", "Statistik"),
        ["stats.tabStats"] = ("Statistics", "Statistik"),
        ["stats.tabLog"] = ("Log", "Log"),
        ["stats.match"] = ("Match duration", "Match-Dauer"),
        ["stats.perRound"] = ("Time per round", "Zeit pro Runde"),
        ["stats.perPhase"] = ("Time per phase", "Zeit pro Phase"),
        ["stats.perPlayer"] = ("Time per player (on turn)", "Zeit pro Spieler (am Zug)"),
        ["stats.round"] = ("Round", "Runde"),
        ["stats.notStarted"] = ("The match hasn't started yet.", "Das Match hat noch nicht begonnen."),
        ["stats.empty"] = ("No log entries yet.", "Noch keine Log-Einträge."),
        ["stats.refresh"] = ("Refresh", "Aktualisieren"),

        // Log line templates ({0} = actor/target, {1} = detail)
        ["log.host"] = ("{0} created the match", "{0} hat das Match erstellt"),
        ["log.join"] = ("{0} joined", "{0} ist beigetreten"),
        ["log.phaseChange"] = ("Phase → {0}", "Phase → {0}"),
        ["log.roundChange"] = ("Round {0} started", "Runde {0} begonnen"),
        ["log.turnChange"] = ("{0}'s turn", "{0} ist am Zug"),
        ["log.speakerSet"] = ("{0} is now speaker", "{0} ist jetzt Sprecher"),
        ["log.strategyPick"] = ("{0} took {1}", "{0} nahm {1}"),
        ["log.strategyReturn"] = ("{0} returned {1}", "{0} gab {1} zurück"),
        ["log.strategyAction"] = ("{0} played {1}", "{0} spielte {1}"),
        ["log.pass"] = ("{0} passed", "{0} hat gepasst"),
        ["log.objectiveReveal"] = ("Revealed objective: {0}", "Auftrag aufgedeckt: {0}"),
        ["log.objectiveScore"] = ("{0} scored {1}", "{0} erzielte {1}"),
        ["log.techAdd"] = ("{0} researched {1}", "{0} erforschte {1}"),
        ["log.techRemove"] = ("{0} removed {1}", "{0} entfernte {1}"),
        ["log.agendaReveal"] = ("Agenda revealed: {0}", "Agenda aufgedeckt: {0}"),
        ["log.agendaNew"] = ("Awaiting a new agenda", "Neue Agenda ausstehend"),
        ["log.agendaStart"] = ("Voting started", "Abstimmung gestartet"),
        ["log.agendaStartHidden"] = ("Hidden voting started", "Verdeckte Abstimmung gestartet"),
        ["log.agendaCancel"] = ("Voting cancelled", "Abstimmung abgebrochen"),
        ["log.agendaReveal2"] = ("Votes revealed", "Stimmen aufgedeckt"),
        ["log.voteLock"] = ("{0} locked a vote", "{0} hat eine Stimme gesperrt"),
        ["log.influenceSet"] = ("{0} set influence to {1}", "{0} setzte Einfluss auf {1}"),
        ["log.agendaResult"] = ("Vote result", "Abstimmungsergebnis"),
        ["log.gamePaused"] = ("Game paused", "Spiel pausiert"),
        ["log.gameResumed"] = ("Game resumed", "Spiel fortgesetzt"),
        ["log.redTapeRandom"] = ("Red Tape removed at random: {0}", "Red Tape zufällig entfernt: {0}"),
        ["log.redTapePurge"] = ("Purged: {0}", "Entfernt aus dem Spiel: {0}"),
        ["log.generic"] = ("Event", "Ereignis"),
        ["log.system"] = ("System", "System"),

        // Legal / IP notice (Asmodee/FFG community-use guidelines)
        ["legal.disclaimer"] = (
            "Unofficial fan-made companion — not affiliated with, endorsed, or sponsored by Asmodee or Fantasy Flight Games.",
            "Inoffizieller, von Fans erstellter Begleiter – nicht verbunden mit, unterstützt oder gesponsert von Asmodee oder Fantasy Flight Games."),
        // The copyright line wraps inline links to Asmodee / Fantasy Flight Games (see MainLayout), so it
        // is split into a prefix (… © & ™ ) and a suffix (… All rights reserved.) around those anchors.
        ["legal.copyrightPre"] = (
            "Twilight Imperium, Prophecy of Kings, Thunder's Edge and all related names, marks, text and artwork are © & ™ ",
            "Twilight Imperium, Prophecy of Kings, Thunder's Edge sowie alle zugehörigen Namen, Marken, Texte und Grafiken sind © & ™ "),
        ["legal.copyrightPost"] = (". All rights reserved.", ". Alle Rechte vorbehalten."),
        ["legal.noncommercial"] = (
            "Free, non-commercial fan project based on Twilight Imperium 4th Edition — no sale, no advertising, no paywall. Voluntary donations to help cover the server costs are welcome.",
            "Kostenloses, nicht-kommerzielles Fan-Projekt auf Basis von Twilight Imperium 4. Edition – kein Verkauf, keine Werbung, keine Paywall. Freiwillige Spenden zur Deckung der Serverkosten sind willkommen."),
        ["legal.thanks"] = ("Special thanks to", "Besonderer Dank an"),
        // The studio name itself is rendered as a link in MainLayout (frostforge.studio), so the
        // key holds only the localized "created by" prefix.
        ["legal.createdBy"] = ("Created by", "Erstellt von"),
        ["legal.contact"] = (
            "Bugs, suggestions, feedback, questions, thanks via",
            "Bugs, Vorschläge, Kritik, Fragen, Danke via"),
        ["legal.donate"] = ("Support the server costs:", "Unterstütze die Serverkosten:"),

        ["players.title"] = ("Players", "Spieler"),
        ["players.faction"] = ("Faction", "Fraktion"),
        ["players.color"] = ("Color", "Farbe"),
        ["players.name"] = ("Name", "Name"),
        ["players.remove"] = ("Remove", "Entfernen"),
        ["players.speaker"] = ("Speaker", "Sprecher"),
        ["players.makeSpeaker"] = ("Make speaker", "Zum Sprecher"),
        ["players.you"] = ("You", "Du"),
        ["players.add"] = ("Add player", "Spieler hinzufügen"),
        ["players.chooseFaction"] = ("Choose faction…", "Fraktion wählen…"),
        ["players.editLocked"] = ("Only this player can edit (toggle in Settings)", "Nur dieser Spieler kann bearbeiten (in Einstellungen umschaltbar)"),

        ["obj.title"] = ("Public objectives", "Öffentliche Aufträge"),
        ["obj.stageI"] = ("Stage I", "Stufe I"),
        ["obj.stageII"] = ("Stage II", "Stufe II"),
        ["obj.reveal"] = ("Reveal objective…", "Auftrag aufdecken…"),
        ["obj.points"] = ("pts", "Pkt"),
        ["obj.remove"] = ("Remove", "Entfernen"),
        ["obj.none"] = ("No objectives revealed yet.", "Noch keine Aufträge aufgedeckt."),
        ["obj.scoredBy"] = ("Scored by", "Gewertet von"),
        ["obj.search"] = ("Search objective to reveal…", "Auftrag zum Aufdecken suchen…"),
        ["obj.custom"] = ("Add secret / custom objective", "Geheimen / eigenen Auftrag hinzufügen"),
        ["obj.customName"] = ("Objective name", "Auftragsname"),
        ["obj.add"] = ("Add", "Hinzufügen"),
        ["obj.secret"] = ("Secret", "Geheim"),
        ["obj.vp"] = ("VP", "SP"),

        ["tech.title"] = ("Technologies", "Technologien"),
        ["tech.showOverview"] = ("Show overview", "Übersicht zeigen"),
        ["tech.hideOverview"] = ("Hide overview", "Übersicht verbergen"),
        ["tech.add"] = ("Add technology…", "Technologie hinzufügen…"),
        ["tech.none"] = ("No technologies yet.", "Noch keine Technologien."),
        ["tech.search"] = ("Search technologies…", "Technologien suchen…"),
        ["tech.catalog"] = ("Technology browser", "Technologie-Browser"),
        ["tech.addToMe"] = ("Add", "Hinzufügen"),
        ["tech.addFor"] = ("Add for", "Hinzufügen für"),
        ["tech.pickColor"] = ("Pick a colour to browse, or search — then click a card to add it", "Farbe wählen oder suchen — dann Karte anklicken zum Hinzufügen"),
        ["tech.allFactions"] = ("All faction techs", "Alle Fraktionstechs"),
        ["tech.owned"] = ("Owned", "Im Besitz"),
        ["color.Biotic"] = ("Biotic", "Biotisch"),
        ["color.Cybernetic"] = ("Cybernetic", "Kybernetisch"),
        ["color.Propulsion"] = ("Propulsion", "Antrieb"),
        ["color.Warfare"] = ("Warfare", "Kriegsführung"),
        ["color.Unit"] = ("Unit", "Einheit"),
        ["tech.joinToAdd"] = ("Join as a player to add technologies to yourself.", "Tritt als Spieler bei, um dir Technologien hinzuzufügen."),
        ["tech.faction"] = ("Faction technology", "Fraktionstechnologie"),
        ["unit.cost"] = ("Cost", "Kosten"),
        ["unit.combat"] = ("Combat", "Kampf"),
        ["unit.move"] = ("Move", "BEWEG."),
        ["unit.capacity"] = ("Capacity", "Kapazität"),

        ["settings.title"] = ("Settings", "Einstellungen"),
        ["settings.language"] = ("Session default language", "Standardsprache der Session"),
        ["settings.expansions"] = ("Active expansions", "Aktive Erweiterungen"),
        ["settings.editAll"] = ("Allow everyone to edit all players", "Jeder darf alle Spieler bearbeiten"),
        ["settings.editAllHint"] = ("Off: each device edits only its own player.", "Aus: jedes Gerät bearbeitet nur seinen eigenen Spieler."),
        ["settings.retentionInfo"] = ("Inactive sessions are deleted automatically by the server after a configured time.", "Inaktive Sessions werden vom Server nach einer konfigurierten Zeit automatisch gelöscht."),
        ["settings.delete"] = ("Delete session", "Session löschen"),
        ["settings.deleteConfirm"] = ("Delete this session and all its data? This cannot be undone.", "Diese Session und alle Daten löschen? Das kann nicht rückgängig gemacht werden."),

        ["exp.Base"] = ("Base game", "Grundspiel"),
        ["exp.ProphecyOfKings"] = ("Prophecy of Kings", "Prophecy of Kings"),
        ["exp.Codex"] = ("Codex", "Codex"),
        ["exp.ThundersEdge"] = ("Thunder's Edge", "Thunder's Edge"),

        ["common.save"] = ("Save", "Speichern"),
        ["common.cancel"] = ("Cancel", "Abbrechen"),
        ["common.close"] = ("Close", "Schließen"),
        ["common.back"] = ("Back", "Zurück"),
        // Secondary abilities of a strategy action (only with the turn timer in use).
        ["politics.speaker"] = ("Appoint the speaker", "Sprecher bestimmen"),
        ["politics.modalHint"] = (
            "Politics: choose the new speaker first — the turn can't be ended until you have. Anyone but the current speaker, yourself included.",
            "Politik: zuerst den neuen Sprecher wählen — vorher lässt sich der Zug nicht beenden. Jeder außer dem bisherigen Sprecher, du selbst eingeschlossen."),
        ["politics.waitingFor"] = ("{0} is appointing the new speaker.", "{0} bestimmt den neuen Sprecher."),
        ["imperial.promptText"] = ("Imperial: you may score a public objective.", "Imperium: du darfst einen öffentlichen Auftrag werten."),
        ["imperial.promptOpen"] = ("Open objectives", "Aufträge öffnen"),
        ["secondary.title"] = ("Secondary ability", "Sekundärfähigkeit"),
        ["secondary.playedBy"] = ("played by {0}", "gespielt von {0}"),
        ["secondary.modalHint"] = (
            "Start the clock for everyone taking the secondary. Your own turn is over — theirs runs until each of them taps \"Done\".",
            "Starte die Uhr für alle, die die Sekundärfähigkeit nutzen. Dein Zug ist beendet — ihre Zeit läuft, bis jeder auf „Fertig“ tippt."),
        ["secondary.modalHintPlayer"] = (
            "Your clock is running for the secondary ability. Tap \"Done\" when you have resolved it.",
            "Deine Uhr läuft für die Sekundärfähigkeit. Tippe auf „Fertig“, wenn du sie abgehandelt hast."),
        ["secondary.start"] = ("Start", "Starten"),
        ["secondary.allDone"] = ("Everyone done", "Alle fertig"),
        ["secondary.open"] = ("Open", "Öffnen"),
        // Combat: the app only tracks that one is running (the wall shows the two sides, the clock stops).
        ["combat.title"] = ("Combat", "Kampf"),
        ["combat.hint"] = ("Pick your opponent — the wall shows both sides, and the turn clock stops until it is over.",
                           "Gegner wählen — die Wand zeigt beide Seiten, und die Zuguhr steht, bis der Kampf vorbei ist."),
        ["combat.running"] = ("Combat", "Kampf"),
        ["combat.vs"] = ("VS", "VS"),
        ["combat.clockStopped"] = ("the clock is stopped", "die Uhr steht"),
        ["combat.end"] = ("Combat over", "Kampf beendet"),
        // End of the match: the three ways out of the statistics dialog.
        ["endgame.continue"] = ("Continue", "Weiterspielen"),
        ["endgame.backToSetup"] = ("Back to setup", "Zurück zur Aufstellung"),
        ["endgame.exit"] = ("Exit", "Beenden"),
        ["endgame.note"] = (
            "\"Back to setup\" and \"Exit\" archive the match: the summary is kept, everything else is cleared. Back to setup keeps the players for another game; Exit closes the session.",
            "„Zurück zur Aufstellung“ und „Beenden“ archivieren die Partie: Die Zusammenfassung bleibt, alles andere wird gelöscht. Zurück zur Aufstellung behält die Spieler für eine weitere Partie, Beenden schließt die Session."),
        ["log.combatStart"] = ("Combat: {0} against {1}", "Kampf: {0} gegen {1}"),
        ["log.combatEnd"] = ("Combat over", "Kampf beendet"),
        ["log.seatClaim"] = ("{0}'s seat was taken over by another device", "{0} wurde von einem anderen Gerät übernommen"),
        ["secondary.running"] = ("Clock running", "Uhr läuft"),
        // "You're up" notification (per device).
        ["push.enable"] = ("Notify me when it's my turn", "Benachrichtige mich, wenn ich dran bin"),
        ["push.disable"] = ("Turn the notification off", "Benachrichtigung ausschalten"),
        ["push.iosHint"] = (
            "On iPhone and iPad this only works once the page is on the home screen: Share → Add to Home Screen, then open it from there.",
            "Auf iPhone und iPad geht das erst, wenn die Seite auf dem Home-Bildschirm liegt: Teilen → Zum Home-Bildschirm, dann von dort öffnen."),
        ["push.blocked"] = ("Notifications are blocked for this site in the browser settings.",
                           "Benachrichtigungen sind für diese Seite in den Browsereinstellungen gesperrt."),
        ["push.failed"] = ("The notification could not be set up.", "Die Benachrichtigung konnte nicht eingerichtet werden."),
        ["common.loading"] = ("Loading…", "Lädt…"),
        ["common.connecting"] = ("Connecting…", "Verbinde…"),
        ["common.code"] = ("Code", "Code"),
        ["common.copy"] = ("Copy", "Kopieren"),
        ["common.copied"] = ("Copied!", "Kopiert!"),
        ["common.confirm"] = ("Confirm", "Bestätigen"),
    };
}
