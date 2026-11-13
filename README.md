# Остання адреса (The Last Letter)

Наративна 2D-пригода. Навчальний проєкт з геймдизайну, ІПЗ-3/2.
Демо: пролог + глава 1 (25–30 хв). Реліз-білд — до 25.11.2026.

Команда: Гаврилюк Артем (програміст), Мухамедчин Даніл (продюсер і дизайнер), Яковець Софія (художниця).

## Що потрібно

- Unity **6000.3** (шаблон Universal 2D) через Unity Hub
- Git
- Ink Unity Integration — ставиться через Package Manager (Add package from git URL):
  `https://github.com/inkle/ink-unity-integration.git#upm`

## Як почати

1. `git clone <посилання на репозиторій>`
2. З кореня репозиторію один раз запустити налаштування Smart Merge:
   `powershell -ExecutionPolicy Bypass -File tools/setup-smartmerge.ps1`
3. Unity Hub → Add → Add project from disk → вибрати папку репозиторію.
4. Перше відкриття довге: Unity збирає папку `Library` (її немає в Git — так і має бути).

## Структура

```
Assets/_Project/
  Art/          Backgrounds, Characters, Items, UI, ThreadTiles
  Audio/        Music, SFX, Ambience
  Ink/          сценарії діалогів (.ink)
  Prefabs/      Characters, Interactables, UI, Managers
  Scenes/       MainMenu, L1_PostSquare, L1_TailorStreet, L1_TailorHouse, DemoEnd; Sandbox/ — прототипи
  ScriptableObjects/  Items, Recipes, Letters, ThreadPuzzles
  Scripts/      Core, Player, Interaction, Inventory, Dialogue, Letters, Puzzles, Map, UI, Save, Companion
docs/           GDD.docx
```

Усе своє кладемо лише в `Assets/_Project`. Сторонні асети — у власні папки, як їх імпортує Unity.

## Правила

- Гілки: `feature/<ім'я>-<задача>`, наприклад `feature/artem-dialogue`. У `main` — лише робочий код.
- Власники сцен: Артем — MainMenu, DemoEnd, префаб GameManager; Софія — L1_*; Даніл — UI_Map, UI_ThreadPuzzle.
  Чужу сцену не правимо: просимо власника або робимо префаб.
- Змінив число в Inspector — тим самим комітом онови рядок у `docs/GDD.docx`.
  Формат коміту: «GDD: що змінили — чому».
- Перед закриттям GDD.docx у LibreOffice перевір, що файл збережено як .docx.
