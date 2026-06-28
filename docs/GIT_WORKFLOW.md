# Git- und Branch-Workflow – ClimbConnect

## Grundregel

**Nie direkt auf `main` pushen.** Jede Änderung läuft über einen Feature-Branch
und einen Pull Request.

## Branch-Konvention

| Prefix     | Wann verwenden                          | Beispiel                        |
|------------|------------------------------------------|---------------------------------|
| `feat/`    | Neues Feature                            | `feat/favoriten-localStorage`   |
| `fix/`     | Bugfix                                   | `fix/bugs-153-152-155`          |
| `refactor/`| Code-Umbau ohne neue Funktionalität      | `refactor/program-cs-aufteilen` |
| `docs/`    | Nur Dokumentation                        | `docs/architecture-update`      |
| `test/`    | Tests hinzufügen                         | `test/integrationstests`        |

Branch-Namen in Kleinbuchstaben, Worte mit Bindestrich getrennt.

## Workflow Schritt für Schritt

```
1. main aktualisieren
   git checkout main
   git pull origin main

2. Feature-Branch erstellen
   git checkout -b feat/mein-feature

3. Implementieren, committen
   git add <dateien>
   git commit -m "feat: kurze Beschreibung (closes #123)"

4. Pushen
   git push origin feat/mein-feature

5. Pull Request auf GitHub erstellen
   → Base: main  ←  Compare: feat/mein-feature
   → Titel: aussagekräftig, Issue-Nummer im Body
   → Review abwarten, dann mergen

6. Branch nach Merge löschen (GitHub macht das automatisch)
```

## Commit-Messages

Format: `<typ>: <kurze Beschreibung> [(closes #NNN)]`

| Typ        | Bedeutung                              |
|------------|----------------------------------------|
| `feat`     | Neues Feature                          |
| `fix`      | Bugfix                                 |
| `refactor` | Umbau ohne funktionale Änderung        |
| `docs`     | Nur Dokumentation                      |
| `test`     | Tests hinzufügen oder reparieren       |
| `chore`    | Build, Abhängigkeiten, Konfiguration   |

Beispiele:
```
feat: Favoriten-System mit localStorage (closes #93)
fix: bevorzugte Gradskala in PublicProfile verwenden (closes #155)
refactor: Program.cs in separate Endpoint-Klassen aufteilen
docs: ARCHITECTURE.md und GIT_WORKFLOW.md vervollständigen
```

## Beispiel-Pull-Request

**Titel:** `feat: Termin-Detail inline aufklappen (closes #159)`

**Body:**
```markdown
## Was wurde gemacht
- Klick auf Termin klappt Detail mit Teilnehmerliste und Ø Grad auf
- GET /api/appointments/{id} wird beim ersten Aufklappen geladen und gecacht

## Test plan
- [ ] Termin aufklappen → Teilnehmerliste sichtbar
- [ ] Durchschnittsgrad erscheint wenn Teilnehmer Begehungen haben
```

## Wichtige Regeln

- Kein `--force`-Push auf `main`
- Kein `git commit --no-verify` (Hooks nicht umgehen)
- Branches **immer** von aktuellem `main` abzweigen
- Issue-Nummern in Commit-Messages → GitHub schließt Issues automatisch beim Merge
