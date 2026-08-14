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
        // A deploy landed while the app was open; see UpdateNotice.razor.
        // Bug reports. The reporter is told exactly what travels with the text — nobody should have to guess
        // what leaves their device — and the contact field says plainly what it is used for.
        ["bug.title"] = ("Report a problem", "Fehler melden"),
        ["bug.hint"] = ("What happened, and what did you expect instead? The more precise, the better the chance of a fix.",
                        "Was ist passiert, und was hättest du stattdessen erwartet? Je genauer, desto größer die Chance auf eine Lösung."),
        ["bug.placeholder"] = ("e.g. I played Politics and the turn could not be ended …",
                               "z. B. Ich habe Politik gespielt und konnte den Zug nicht beenden …"),
        ["bug.contact"] = ("Contact (optional)", "Kontakt (optional)"),
        ["bug.contactPlaceholder"] = ("Email, Discord, Reddit …", "E-Mail, Discord, Reddit …"),
        ["bug.contactHint"] = ("Only used to ask you back about this report or to tell you it is fixed.",
                               "Wird nur für Rückfragen zu dieser Meldung oder eine Rückmeldung benutzt."),
        ["bug.context"] = ("Sent along", "Wird mitgeschickt"),
        ["bug.contextNone"] = ("no session · app version and browser", "keine Session · App-Version und Browser"),
        ["bug.send"] = ("Send", "Senden"),
        ["bug.thanks"] = ("Thank you — the report is in.", "Danke, die Meldung ist angekommen."),
        ["bug.failed"] = ("That did not go through. Please try again in a moment.",
                          "Das hat nicht geklappt. Bitte gleich noch einmal versuchen."),
        ["update.available"] = ("A new version is available.", "Eine neue Version ist verfügbar."),
        // What the reload does, and that it is safe: the game itself lives on the server, so nothing is lost.
        ["update.hint"] = ("Reload to switch to it — the running game is not affected.",
                           "Neu laden, um sie zu übernehmen — die laufende Partie bleibt davon unberührt."),
        ["update.reload"] = ("Reload now", "Jetzt neu laden"),
        ["update.stuck"] = ("Close every tab of this page, then open it again to finish updating.",
                            "Schließe alle Tabs dieser Seite und öffne sie neu, um das Update abzuschließen."),
        // Start page: the senate backdrop can be switched off (and stays off), see SessionStore.SenateEnabled.
        ["home.senateOff"] = ("Hide the senate", "Senat ausblenden"),
        ["home.senateOn"] = ("Show the senate", "Senat einblenden"),
        ["home.recent"] = ("Resume a session", "Session fortsetzen"),
        ["home.recentTitle"] = ("Sessions on this device", "Sessions auf diesem Gerät"),
        ["home.recentAs"] = ("as", "als"),
        ["home.recentPick"] = ("choose a player", "Spieler wählen"),
        // Scanning the wall's QR from inside the app (QrScanModal). The reason it exists is iOS: a code
        // scanned with the camera app opens Safari, not the app you installed for notifications.
        ["scan.title"] = ("Scan the join code", "Beitritts-Code scannen"),
        ["scan.hint"] = ("Point the camera at the code on the screen.",
                         "Die Kamera auf den Code auf dem Bildschirm richten."),
        ["scan.denied"] = ("No camera access. Allow it for this site in your browser settings, then try again.",
                           "Kein Kamerazugriff. In den Browser-Einstellungen für diese Seite erlauben und erneut versuchen."),
        ["scan.noCamera"] = ("No camera found on this device.", "Auf diesem Gerät wurde keine Kamera gefunden."),
        ["scan.switch"] = ("Switch camera", "Kamera wechseln"),
        ["scan.failed"] = ("The camera could not be started.", "Die Kamera konnte nicht gestartet werden."),
        ["home.recentUnnamed"] = ("Session", "Session"),
        // The timestamp on each row: when the session was CREATED. The list is still ordered by this device's
        // last visit, but the date shown is the game's own — that is what identifies an evening.
        ["home.recentWhenHint"] = ("When this session was created",
                                   "Wann diese Session erstellt wurde"),
        ["home.recentNone"] = ("No sessions on this device yet — create one or join with a code.",
                               "Auf diesem Gerät noch keine Sessions — erstelle eine oder tritt mit einem Code bei."),
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
        // Closing the session on this device is two different things, and a yes/no box could never ask which:
        // do you want your seat back next time, or do you want to be asked who you are?
        ["nav.exitTitle"] = ("Close this session on this device?", "Diese Session auf diesem Gerät schließen?"),
        ["nav.exitClose"] = ("Close and keep my seat", "Schließen und Platz behalten"),
        ["nav.exitCloseHint"] = ("The session stays in your list, and opening it again puts you straight back into {0}.",
                                 "Die Session bleibt in deiner Liste, und beim nächsten Öffnen bist du wieder {0}."),
        ["nav.exitLeave"] = ("Give up my seat", "Platz freigeben"),
        ["nav.exitLeaveHint"] = ("Stays in your list too, but next time you choose which player you join as.",
                                 "Bleibt ebenfalls in der Liste, aber beim nächsten Mal wählst du, als welcher Spieler du beitrittst."),

        ["phase.Setup"] = ("Setup", "Aufstellung"),
        ["phase.Strategy"] = ("Strategy", "Strategie"),
        ["phase.Action"] = ("Action", "Aktion"),
        ["phase.Status"] = ("Status", "Status"),
        ["phase.Agenda"] = ("Agenda", "Agenda"),

        ["turn.round"] = ("Round", "Runde"),
        ["turn.active"] = ("Active player", "Aktiver Spieler"),
        ["turn.advance"] = ("Next turn", "Nächster Zug"),
        ["turn.previous"] = ("Previous turn", "Vorheriger Zug"),
        // Short form for the action row, where four buttons of one height have to fit a phone.
        ["turn.previousShort"] = ("Prev. turn", "Zug zurück"),
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
        // "What is Red Tape?" (RedTapeHelpModal). Our own summary of the two variants' mechanics and of what
        // the app does about them — NOT a copy of the authors' posts, which are linked instead. Each block is
        // one string with `\n` between the bullets, like the strategy cards' ability text.
        ["redtape.helpTitle"] = ("What is Red Tape?", "Was ist Red Tape?"),
        ["redtape.helpLead"] = (
            "Two community variants that lay every public objective face-up at the start of the game, with a marker over the victory points of most of them. A taped objective cannot be scored until its marker comes off — so the whole table can plan ahead, whether or not anybody remembers the deck. What differs between the variants is how a marker comes off, and how many objectives can ever score.",
            "Zwei Community-Varianten: Alle öffentlichen Aufträge liegen von Anfang an offen, bei den meisten aber mit einem Marker über den Siegpunkten. Ein versiegelter Auftrag kann erst gewertet werden, wenn sein Marker fällt — so kann der ganze Tisch vorausplanen, ob sich jemand die Kartensätze merkt oder nicht. Die Varianten unterscheiden sich darin, wie ein Marker fällt und wie viele Aufträge überhaupt gewertet werden können."),
        ["redtape.helpBy"] = ("variant by {0}", "Variante von {0}"),
        ["redtape.helpSetup"] = ("Setup", "Aufbau"),
        ["redtape.helpLimits"] = ("Scoring limits", "Wertungsgrenzen"),
        ["redtape.helpDuring"] = ("During the game", "Im Spiel"),
        ["redtape.helpInApp"] = ("In this app", "In dieser App"),
        ["redtape.helpPost"] = ("The original post on Reddit", "Der Originalbeitrag auf Reddit"),
        ["redtape.helpBurSetup"] = (
            "Five Stage I and five Stage II objectives go on the table face-up.\n" +
            "Every one of them gets a marker except the first two Stage I, which count as revealed.",
            "Fünf Stufe-I- und fünf Stufe-II-Aufträge liegen offen aus.\n" +
            "Alle bekommen einen Marker außer den ersten zwei Stufe-I-Aufträgen, die als aufgedeckt gelten."),
        ["redtape.helpBurPlay"] = (
            "One strategy card carries the ability — Diplomacy or Imperial, whichever your table replaces.\n" +
            "Its primary removes one marker of your choice; not from a Stage II objective in the first three rounds.\n" +
            "And on taking the card: one further marker per trade good that was lying on it.\n" +
            "The status phase reveals nothing any more (everything is already face-up) — instead the game ends there if no unrevealed public objective is left.",
            "Eine Strategiekarte trägt die Fähigkeit — Diplomatie oder Imperium, je nachdem, welche euer Tisch ersetzt.\n" +
            "Ihre primäre Fähigkeit entfernt einen Marker nach Wahl; in den ersten drei Runden keinen von einem Stufe-II-Auftrag.\n" +
            "Und beim Nehmen der Karte: ein weiterer Marker pro Handelsware, die darauf lag.\n" +
            "In der Statusphase wird nichts mehr aufgedeckt (es liegt schon alles offen) — stattdessen endet dort das Spiel, wenn kein nicht aufgedeckter öffentlicher Auftrag mehr übrig ist."),
        ["redtape.helpBurApp"] = (
            "Pick \"Bureaucracy\" and say which card carries it. The app tapes the objectives, refuses to score a taped one and holds Stage II shut for three rounds — you can overrule that, and the override is logged.\n" +
            "The card shows its added text, the status phase shows the game-end check instead of the reveal step, and playing the card asks which marker comes off.",
            "„Bureaucracy“ wählen und sagen, welche Karte sie trägt. Die App versiegelt die Aufträge, verweigert die Wertung eines versiegelten und hält Stufe II drei Runden zu — das lässt sich übergehen, und das wird protokolliert.\n" +
            "Die Karte zeigt ihren Zusatztext, die Statusphase zeigt die Spielende-Prüfung statt des Aufdeck-Schritts, und beim Spielen der Karte fragt die App, welcher Marker fällt."),
        ["redtape.helpLiteSetup"] = (
            "Seven (or six) Stage I and five Stage II objectives go on the table face-up.\n" +
            "Every one of them gets a marker except the first Stage I objectives.",
            "Sieben (oder sechs) Stufe-I- und fünf Stufe-II-Aufträge liegen offen aus.\n" +
            "Alle bekommen einen Marker außer den ersten Stufe-I-Aufträgen."),
        ["redtape.helpLiteLimits"] = (
            "Only five Stage I objectives can ever score.\n" +
            "The moment the fifth marker comes off Stage I, the ones still taped leave the game for good.\n" +
            "No Stage II marker comes off before those five are clear.",
            "Nur fünf Stufe-I-Aufträge können überhaupt gewertet werden.\n" +
            "Sobald der fünfte Marker von Stufe I fällt, verlassen die noch versiegelten das Spiel endgültig.\n" +
            "Kein Stufe-II-Marker fällt, bevor diese fünf frei sind."),
        ["redtape.helpLitePlay"] = (
            "The ability sits on Diplomacy as printed — Lite replaces no card.\n" +
            "Whoever took Diplomacy removes one marker of their choice with the primary; that round nothing comes off at random.\n" +
            "If nobody took it, one marker comes off AT RANDOM instead: in round 1 right after the strategy phase, and afterwards at the end of every status phase.",
            "Die Fähigkeit liegt auf der gedruckten Diplomatie — Lite ersetzt keine Karte.\n" +
            "Wer Diplomatie genommen hat, entfernt mit der primären Fähigkeit einen Marker nach Wahl; in dieser Runde fällt keiner zufällig.\n" +
            "Hat sie niemand genommen, fällt stattdessen ein ZUFÄLLIGER Marker: in Runde 1 direkt nach der Strategiephase, danach jeweils am Ende der Statusphase."),
        ["redtape.helpLiteApp"] = (
            "Pick \"Red Tape Lite\" — the carrier is always Diplomacy. The app applies all of the above and greys out exactly what the rules forbid.\n" +
            "The two irreversible steps are only ever PROPOSED: purging the leftover Stage I objectives and the random removal are questions the host answers, and both go into the match log.",
            "„Red Tape Lite“ wählen — Trägerkarte ist immer Diplomatie. Die App setzt alles Obige um und graut genau das aus, was die Regeln verbieten.\n" +
            "Die zwei unumkehrbaren Schritte werden nur VORGESCHLAGEN: das Entfernen der übrigen Stufe-I-Aufträge und die Zufallsentnahme sind Fragen, die der Host beantwortet, und beide landen im Spielprotokoll."),
        // The card's own name once Bureaucracy replaces Imperial with it (see CardDisplay).
        ["redtape.bureaucracyCard"] = ("Bureaucracy", "Bürokratie"),
        ["redtape.card"] = ("Card that removes the tape", "Karte, die das Band entfernt"),
        ["redtape.cardHint"] = (
            "Replace either — but not both — Diplomacy or Imperial with the matching Bureaucracy card. Everything else on it stays the printed card.",
            "Ersetze entweder Diplomatie oder Imperium — nicht beide — durch die passende Bürokratie-Karte. Alles andere darauf bleibt die gedruckte Karte."),
        // Lite replaces no card, so there is nothing to choose: it is published for Diplomacy.
        ["redtape.cardLiteHint"] = (
            "Red Tape Lite replaces no card — the ability sits on Diplomacy as printed, so there is nothing to choose.",
            "Red Tape Lite ersetzt keine Karte — die Fähigkeit liegt auf der gedruckten Diplomatie, also gibt es nichts zu wählen."),
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
        // Lite keeps the step but nothing is revealed IN it: the objective for this round either came clear
        // through the carrier card, or one is drawn at random when the phase ends. {0} = that card. Our own
        // summary of the variant, not the author's text.
        ["redtape.stepLiteTaken"] = (
            "Red Tape Lite: {0} was played this round, so the objective for it is already clear — nothing is revealed here.",
            "Red Tape Lite: In dieser Runde wurde {0} gespielt, der Auftrag dafür ist also schon aufgedeckt — hier wird nichts aufgedeckt."),
        ["redtape.stepLiteRandom"] = (
            "Red Tape Lite: nobody took {0} this round, so a marker comes off at random when the status phase ends — the app will ask.",
            "Red Tape Lite: Niemand hat in dieser Runde {0} genommen — beim Ende der Statusphase kommt ein zufälliger Marker herunter, die App fragt danach."),
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
        // ("redtape.randomHint" lived here — the band in the objectives tab announcing the coming random
        //  removal. Both are gone: the status phase's reveal step says it at the moment it matters.)
        // Red Tape Lite's two questions. The app proposes, the host answers — see RedTapeModal for why
        // neither of them happens on its own any more.
        ["redtape.randomAsk"] = ("Remove a tape at random?", "Band zufällig entfernen?"),
        ["redtape.randomAskBody"] = (
            "Nobody took {0} this round, so the variant takes one tape off at random instead of letting anybody choose.",
            "Diese Runde hat niemand {0} genommen, also nimmt die Variante ein zufälliges Band ab, statt jemanden wählen zu lassen."),
        ["redtape.randomAskWarn"] = (
            "The app picks one of the tapes it would be allowed to pull. Nothing happens until you say so.",
            "Die App wählt eines der Bänder, die sie abnehmen darf. Bis du zustimmst, passiert nichts."),
        ["redtape.randomYes"] = ("Remove one at random", "Zufällig entfernen"),
        ["redtape.randomNo"] = ("Not this round", "Diese Runde nicht"),
        // …and what the draw produced. The wall shows the same card large until this is closed.
        ["redtape.randomResult"] = ("This one came clear", "Dieser wurde freigegeben"),
        ["redtape.randomResultBody"] = ("The tape came off this objective — it can be scored from now on.",
                                        "Bei diesem Auftrag ist das Band abgegangen — er kann ab jetzt gewertet werden."),
        ["redtape.purgeAsk"] = ("Take the rest out of the game?", "Die übrigen aus dem Spiel nehmen?"),
        ["redtape.purgeAskBody"] = (
            "{0} Stage I objectives are clear, so under Red Tape Lite only those can ever score. These are still taped:",
            "{0} Stufe-I-Aufträge sind frei, also können bei Red Tape Lite nur diese je punkten. Diese sind noch versiegelt:"),
        ["redtape.purgeAskWarn"] = (
            "Taken out, they can never be scored and their tape never comes off. This cannot be undone — leaving them in is a perfectly good answer.",
            "Herausgenommen können sie nie mehr gewertet werden und ihr Band geht nie mehr ab. Das lässt sich nicht zurücknehmen — sie drin zu lassen ist eine völlig gute Antwort."),
        ["redtape.purgeYes"] = ("Take them out of the game", "Aus dem Spiel nehmen"),
        ["redtape.purgeNo"] = ("Leave them in", "Drin lassen"),
        // Why a tape cannot be pulled right now (the server refuses the same, see RedTape). The `*Long` forms
        // are the same rule as a sentence, for the dialog that offers to overrule it.
        ["redtape.purged"] = ("PURGED — CAN NEVER BE SCORED", "ENTFERNT — NICHT MEHR WERTBAR"),
        ["redtape.lockedRounds"] = ("STAGE II — LOCKED FOR THE FIRST 3 ROUNDS", "STUFE II — DIE ERSTEN 3 RUNDEN GESPERRT"),
        ["redtape.lockedRoundsLong"] = (
            "Bureaucracy seals the Stage II objectives for the first three rounds — this one cannot be scored until round 4.",
            "Bürokratie hält die Stufe-II-Aufträge die ersten drei Runden versiegelt — dieser ist erst ab Runde 4 wertbar."),
        ["redtape.lockedStageI"] = ("STAGE II — NOT UNTIL 5 STAGE I ARE CLEAR", "STUFE II — ERST WENN 5 STUFE-I-AUFTRÄGE FREI SIND"),
        ["redtape.lockedStageILong"] = (
            "Red Tape Lite keeps Stage II sealed until the five scorable Stage I objectives are clear.",
            "Red Tape Lite hält Stufe II versiegelt, bis die fünf wertbaren Stufe-I-Aufträge frei sind."),
        // What the wall shows on a taped objective: the STATE, nothing else. The rule and the action belong on
        // a device somebody can act on.
        ["redtape.sealed"] = ("SEALED", "VERSIEGELT"),
        // Tapping a tape a timing rule holds shut: which rule, and the option to overrule it anyway.
        ["redtape.lockedAskTitle"] = ("This objective is sealed", "Dieser Auftrag ist versiegelt"),
        ["redtape.lockedAskWarn"] = (
            "You can take the tape off anyway — it is your table's game. The match log records that the rule was overruled.",
            "Du kannst das Band trotzdem abnehmen — es ist euer Spiel. Im Protokoll steht, dass die Regel übergangen wurde."),
        ["redtape.removeAnyway"] = ("Remove it anyway", "Trotzdem entfernen"),
        ["redtape.leaveSealed"] = ("Leave it sealed", "Versiegelt lassen"),
        // The carrier card's own ability: "remove a marker of your choice".
        // Plural: the card allows one marker per trade good on top of the primary, so it is rarely just one.
        ["redtape.pickTitle"] = ("Remove markers", "Marker entfernen"),
        // Done with a count that does not match the allowance. Neither is refused — the table is asked once.
        ["redtape.tooManyTitle"] = ("More markers than the card allows", "Mehr Marker als die Karte erlaubt"),
        ["redtape.tooManyBody"] = (
            "{0} more tape(s) are off than this card removes. That changes who can score — leave it like this?",
            "Es sind {0} Band/Bänder mehr ab, als diese Karte entfernt. Das ändert, wer werten kann — so lassen?"),
        ["redtape.tooManyOk"] = ("Leave it like this", "So lassen"),
        ["redtape.tooFewTitle"] = ("Markers left over", "Marker übrig"),
        ["redtape.tooFewBody"] = ("You may still take {0} tape(s) off. Finish anyway?",
                                  "Du darfst noch {0} Band/Bänder abnehmen. Trotzdem fertig?"),
        ["redtape.tooFewOk"] = ("Finish anyway", "Trotzdem fertig"),
        ["redtape.pickHint"] = ("Tap a card to pull its tape — tap it again to put the tape back.",
                                "Karte antippen, um das Band abzuziehen — nochmal antippen legt es wieder drauf."),
        ["redtape.pickNone"] = ("No tape can come off right now.", "Gerade kann kein Band abgenommen werden."),
        // The allowance: one for the primary ability plus the card's trade goods (see GameRules).
        ["redtape.pickCounted"] = ("markers removed", "Marker entfernt"),
        ["redtape.tapeOff"] = ("tape off", "Band ab"),
        ["redtape.tapeBackOn"] = ("Tap to put the tape back", "Antippen, um das Band wieder aufzulegen"),
        ["redtape.pickGoods"] = ("{0} trade good(s) were on the card", "{0} Handelsware(n) lagen auf der Karte"),
        ["redtape.takeOff"] = ("Tap the tape to remove it", "Zum Entfernen auf das Band tippen"),
        // The tape is the only label — a separate "SEALED" chip said the same thing twice, which is also
        // why this string is the ACTION and not the state: it is only ever shown where the tape really can
        // be tapped. The wall renders the band with no text at all (see ObjectiveDisplayCard).
        ["redtape.tapToRemove"] = ("TAP THE TAPE", "AUF DAS BAND TIPPEN"),
        ["redtape.confirmRemove"] = ("TAP AGAIN TO REMOVE", "NOCHMAL TIPPEN ZUM ENTFERNEN"),
        ["redtape.seal"] = ("Seal", "Versiegeln"),
        // The table-wide prompt after a Technology action (TechPromptModal) and its table option.
        ["tech.optionTitle"] = ("Technology action", "Technologie-Aktion"),
        ["tech.promptTitle"] = ("Record the technologies you researched",
                                "Erforschte Technologien erfassen"),
        ["tech.promptOption"] = ("Ask for the technologies afterwards",
                                 "Danach zur Techeingabe auffordern"),
        ["tech.promptHint2"] = (
            "Asks the whole table for its technologies once the Technology action is resolved. A reminder, not a requirement.",
            "Fragt den ganzen Tisch nach seinen Technologien, sobald die Technologie-Aktion abgehandelt ist. Eine Erinnerung, keine Pflicht."),
        ["tech.promptHint"] = (
            "Did you research anything? Record it — or skip. The clock is standing still until the table is through.",
            "Etwas erforscht? Dann erfassen — oder überspringen. Bis der Tisch durch ist, steht die Uhr."),
        ["tech.promptHintHost"] = (
            "Everyone can enter their own on their own device; you can enter it for anyone here. \"Next turn\" ends it for the whole table.",
            "Jeder kann auf seinem Gerät selbst erfassen; du kannst es hier für jeden eintragen. „Nächster Zug“ beendet es für den ganzen Tisch."),
        ["tech.promptClockStopped"] = ("the clock is stopped", "die Uhr steht"),
        ["tech.promptRecord"] = ("Record tech", "Techs erfassen"),
        ["tech.promptRecordFor"] = ("Record technologies for {0}", "Technologien für {0} erfassen"),
        ["tech.promptChange"] = ("Change", "Ändern"),
        ["tech.promptSkip"] = ("Skip", "Überspringen"),
        ["tech.promptSkipped"] = ("skipped", "übersprungen"),
        ["tech.promptRecorded"] = ("{0} recorded", "{0} erfasst"),
        ["tech.promptNextTurn"] = ("Next turn", "Nächster Zug"),
        // Per-player turn timer (informational only — never enforced).
        ["timer.remaining"] = ("Time left this round", "Restzeit diese Runde"),
        ["timer.over"] = ("Time budget used up", "Zeitbudget aufgebraucht"),
        ["timer.title"] = ("Turn timer", "Zug-Timer"),
        ["timer.perRound"] = ("Minutes per player per round", "Minuten pro Spieler pro Runde"),
        ["timer.off"] = ("Off", "Aus"),
        ["timer.custom"] = ("Own time…", "Eigene Zeit…"),
        ["timer.customLabel"] = ("Minutes per player per round", "Minuten pro Spieler pro Runde"),
        ["timer.minutes"] = ("min", "Min."),
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
        // Why "Next" / "Start game" is off — shown as the button's tooltip AND as text (a touch device has no
        // tooltips, and the table is holding one).
        ["setup.needReady"] = ("Everyone has to be ready first — faction and colour for every player.",
                               "Erst müssen alle bereit sein — Fraktion und Farbe für jeden Spieler."),
        ["setup.searchFaction"] = ("Search faction…", "Fraktion suchen…"),
        ["setup.needFactionColor"] = ("Choose a faction and a colour to ready up.", "Wähle Fraktion und Farbe, um bereit zu sein."),
        // The speaker is settled on the SEATING step — it decides who picks first, so it belongs with the
        // order around the table, and the step will not let the host past without one.
        ["setup.speakerLbl"] = ("Speaker set — tap a row to change it.", "Sprecher festgelegt — zum Ändern eine Zeile antippen."),
        ["setup.noSpeakerYet"] = ("No speaker yet — use \"Make speaker\", or roll for it.",
                                  "Noch kein Sprecher — „Sprecher machen\" nutzen oder auslosen."),
        ["setup.randomSpeaker"] = ("Randomly assign speaker", "Sprecher auslosen"),
        ["setup.needSpeaker"] = ("Pick a speaker to carry on — they decide who picks a strategy card first.",
                                 "Wähle einen Sprecher, um weiterzukommen — er entscheidet, wer zuerst eine Strategiekarte nimmt."),
        // The "is the seating order right?" dialog is gone — the seating step IS that question, and the host
        // cannot reach "start" without passing it, so its title/confirm strings went with it.
        ["setup.confirmSeatDragHint"] = (
            "Drag a row by its handle to change the order, or use the arrows.",
            "Zeile am Griff ziehen, um die Reihenfolge zu ändern — oder die Pfeile nutzen."),
        ["setup.dragSeat"] = ("Drag to reorder", "Ziehen zum Umsortieren"),
        ["setup.maxPlayers"] = ("Maximum of 8 players reached.", "Maximal 8 Spieler erreicht."),
        ["setup.deselectColor"] = ("Click to deselect (frees the colour)", "Klicken zum Abwählen (gibt die Farbe frei)"),
        ["setup.seatOrder"] = ("Seat order", "Sitzreihenfolge"),
        ["setup.speaker"] = ("Speaker", "Sprecher"),
        // The verb, not the noun: it is a button that DOES something, and it reads the same in the seating
        // step and the Players tab so the two are recognisably the same control.
        ["setup.makeSpeaker"] = ("Make speaker", "Sprecher machen"),
        ["setup.moveUp"] = ("Move up", "Nach oben"),
        ["setup.moveDown"] = ("Move down", "Nach unten"),
        ["setup.objectives"] = ("Starting objectives", "Anfangsaufträge"),
        ["setup.objectivesHint"] = ("At the end of setup, record the 2 public objectives you drew.", "Trage am Ende der Aufstellung die 2 gezogenen öffentlichen Aufträge ein."),
        ["setup.objectivesHintRedTape"] = (
            "Red Tape: record every public objective you laid out — the first two Stage I are open, the rest come up taped.",
            "Red Tape: Trage alle ausgelegten öffentlichen Aufträge ein — die ersten zwei Stufe-I-Aufträge liegen frei, der Rest kommt versiegelt."),
        // Setup runs as steps (host view). Non-hosts always see the player step — see SetupView.
        // Name + options share one step ("what game is this"), so there is one key for it now.
        ["setup.step.session"] = ("Session & options", "Session & Optionen"),
        ["setup.step.players"] = ("Players", "Spieler"),
        ["setup.step.seating"] = ("Seating", "Sitzordnung"),
        ["setup.step.objectives"] = ("Objectives", "Aufträge"),
        ["setup.nameHint"] = ("What is this game called? It shows on the wall display.",
                              "Wie heißt diese Partie? Der Name steht auf der Wandanzeige."),
        ["setup.sessionStepHint"] = ("What game is this, and how does your table play it?",
                                     "Was für eine Partie ist das, und wie spielt euer Tisch sie?"),
        // Which game. Only TI4 works today; Twilight's Fall is its own game mode and none of it is modelled.
        ["setup.gameVariant"] = ("Game", "Spiel"),
        ["setup.variantTi4"] = ("Twilight Imperium 4", "Twilight Imperium 4"),
        ["setup.variantTwilightsFall"] = ("Twilight's Fall", "Twilight's Fall"),
        ["setup.gameVariantHint"] = ("Twilight's Fall is a separate game mode with its own factions and cards — not modelled yet.",
                                     "Twilight's Fall ist ein eigener Spielmodus mit eigenen Fraktionen und Karten — noch nicht abgebildet."),
        ["setup.expansionsHint"] = ("Decides which factions, technologies and cards the app offers. The base game is always in.",
                                    "Bestimmt, welche Fraktionen, Technologien und Karten die App anbietet. Das Grundspiel ist immer dabei."),
        ["setup.seatingHint"] = ("Drag by the handle or use the arrows. Then say who the speaker is — or roll for it.",
                                 "Am Griff ziehen oder die Pfeile nutzen. Dann den Sprecher festlegen — oder auslosen."),
        ["host.only"] = ("Host", "Host"),
        ["host.onlyHint"] = ("Only the host controls the phases.", "Nur der Host steuert die Phasen."),
        // Acting for the player who is up. It is a MODE again (2026-08-12): always-on meant the host's device
        // permanently showed somebody else's turn buttons, which at a table of phones is not clear.
        ["host.takeOver"] = ("Take over", "Übernehmen"),
        ["host.takeOverEnd"] = ("End take-over", "Übernahme beenden"),
        ["host.takeOverHint"] = ("You are acting for {0}.", "Du handelst für {0}."),
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
        // Why there is no way out of the status phase yet (see PhaseView).
        ["status.finishStages"] = ("Walk through the steps first", "Erst die Schritte durchgehen"),
        ["status.scoreFor"] = ("{0} may score", "{0} darf werten"),
        ["status.tapToScore"] = ("Tap an objective card to score it.", "Auftragskarte antippen, um sie zu werten."),
        // Shown instead on a device that is not the one scoring: scoring is the player's own decision now,
        // so every other phone watches. Without it the cards just look broken.
        ["status.watchOnly"] = ("{0} is scoring — you can act when it is your turn.",
                                "{0} wertet gerade — du bist dran, wenn du an der Reihe bist."),
        ["status.nothingLeft"] = ("This player has already scored every revealed objective.",
                                 "Dieser Spieler hat schon alle aufgedeckten Aufträge gewertet."),
        // The scoring list only holds what can be acted on, so it can legitimately be empty (everything on
        // the table is sealed, for instance).
        ["status.nothingScorable"] = ("Nothing here can be scored right now.",
                                     "Hier ist gerade nichts wertbar."),
        ["status.scoredTapToUndo"] = ("Scored", "Gewertet"),
        ["status.tapToUnscore"] = ("Tap to take this score back", "Antippen, um die Wertung zurückzunehmen"),
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
        ["players.remove"] = ("Remove player", "Spieler entfernen"),
        // Removing takes the player's scores and technologies with them, so it asks twice.
        ["players.removeConfirm"] = ("Tap again to remove", "Nochmal tippen zum Entfernen"),
        ["players.removeWarn"] = ("Their scores and technologies go too.", "Ihre Wertungen und Technologien gehen mit."),
        ["players.speaker"] = ("Speaker", "Sprecher"),
        // Same wording as the seating step, because it is the same control.
        ["players.makeSpeaker"] = ("Make speaker", "Sprecher machen"),
        // Everything editable about one player, in a popup (name, faction, colour, removal).
        ["players.edit"] = ("Edit player", "Spieler bearbeiten"),
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
        // Recording a technology as a popup (TechPickModal) — where the action was played, not a tab away.
        ["tech.pickTitle"] = ("Record technology", "Technologie erfassen"),
        ["tech.pickHint"] = ("Pick a colour, or search — then click a card to take it.",
                            "Farbe wählen oder suchen — dann eine Karte anklicken."),
        ["tech.pickOwned"] = ("{0} researched", "{0} erforscht"),
        ["tech.pickClockStopped"] = ("your clock is stopped", "deine Uhr steht"),
        ["tech.noneFound"] = ("Nothing matches.", "Keine Treffer."),
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
        // Offered but off: none of its content is in the master DB yet (see common.comingSoon).
        ["exp.DiscordantStars"] = ("Discordant Stars", "Discordant Stars"),

        ["common.save"] = ("Save", "Speichern"),
        ["common.cancel"] = ("Cancel", "Abbrechen"),
        ["common.done"] = ("Done", "Fertig"),
        ["common.close"] = ("Close", "Schließen"),
        ["common.comingSoon"] = ("coming soon", "kommt noch"),
        ["common.back"] = ("Back", "Zurück"),
        ["common.continue"] = ("Carry on", "Weiter geht's"),
        // Secondary abilities of a strategy action (only with the turn timer in use).
        // The dialog can be closed without answering, and since 2026-08-13 nothing is blocked by it either —
        // it is a reminder, so these say how to get back to it rather than what it prevents.
        ["politics.cancelHint"] = ("You can come back to it from the bar below.",
                                   "Über die Leiste unten kommst du wieder hierher."),
        ["politics.stillOwed"] = ("The speaker has not been appointed yet.",
                                  "Der Sprecher ist noch nicht ernannt."),
        ["politics.reopen"] = ("Appoint now", "Jetzt ernennen"),
        ["politics.speaker"] = ("Appoint the speaker", "Sprecher bestimmen"),
        ["politics.modalHint"] = (
            "Politics: choose the new speaker. Anyone but the current speaker, yourself included.",
            "Politik: den neuen Sprecher wählen. Jeder außer dem bisherigen Sprecher, du selbst eingeschlossen."),
        ["politics.waitingFor"] = ("{0} is appointing the new speaker.", "{0} bestimmt den neuen Sprecher."),
        ["imperial.promptText"] = ("Imperial: you may score a public objective.", "Imperium: du darfst einen öffentlichen Auftrag werten."),
        ["imperial.promptOpen"] = ("Score an objective", "Auftrag werten"),
        // The popup: the same shape as the technology picker and Red Tape's "which tape comes off".
        ["imperial.pickTitle"] = ("Score a public objective", "Öffentlichen Auftrag werten"),
        ["imperial.pickHint"] = ("Only what this player may score is listed — nothing sealed, nothing they have scored already.",
                                "Aufgelistet ist nur, was dieser Spieler werten darf — nichts Versiegeltes und nichts, was er schon gewertet hat."),
        ["imperial.pickNone"] = ("There is nothing this player can score right now.",
                                "Dieser Spieler kann gerade nichts werten."),
        // The table option. Its own switch, but only usable with the turn timer on — the round exists to
        // separate time spent on a secondary from time on turn, and there is no clock to separate without it.
        ["secondary.optionTitle"] = ("Secondary abilities", "Sekundärfähigkeiten"),
        ["secondary.optionLabel"] = ("Track who is taking a secondary", "Mitverfolgen, wer eine Sekundärfähigkeit nimmt"),
        ["secondary.optionHint"] = (
            "After a strategy action, everyone taking the secondary gets their own clock and ticks themselves off when done — so deciding on a secondary is not counted as the next player's turn.",
            "Nach einer Strategieaktion bekommt jeder, der die Sekundärfähigkeit nimmt, eine eigene Uhr und hakt sich ab, wenn er fertig ist — so zählt das Überlegen nicht als Zug des nächsten Spielers."),
        ["secondary.optionNeedsTimer"] = (
            "Needs the turn timer: without a clock there is nothing to keep apart.",
            "Braucht den Zug-Timer: ohne Uhr gibt es nichts auseinanderzuhalten."),
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
        // Offered as a button on the player card; the opponent is chosen in a popup.
        ["combat.declare"] = ("Combat", "Kampf"),
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
        // The iOS home-screen requirement, walked through with the actual Safari glyphs (InstallHelpModal) —
        // it used to be this much instruction crammed onto a chip next to the bell.
        ["install.title"] = ("Add to the home screen", "Zum Home-Bildschirm hinzufügen"),
        ["install.why"] = (
            "On iPhone and iPad, notifications only exist for an app that lives on the home screen — Apple allows them nowhere else. It takes three taps:",
            "Auf iPhone und iPad gibt es Benachrichtigungen nur für eine App, die auf dem Home-Bildschirm liegt — Apple erlaubt sie sonst nicht. Das sind drei Tipps:"),
        ["install.step1"] = ("In Safari, tap the share button in the toolbar.",
                            "In Safari auf das Teilen-Symbol in der Leiste tippen."),
        ["install.step2"] = ("Choose \"Add to Home Screen\" and confirm.",
                            "„Zum Home-Bildschirm“ wählen und bestätigen."),
        ["install.step3"] = ("Open the app from the home screen, then switch the bell on there.",
                            "Die App vom Home-Bildschirm öffnen und dort die Glocke einschalten."),
        ["install.note"] = (
            "The home-screen app is the same session — your seat and the join code are still there.",
            "Die App vom Home-Bildschirm ist dieselbe Session — dein Platz und der Beitritts-Code sind weiterhin da."),
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
