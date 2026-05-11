# Baldic

Baldic — это современный мод-лоадер для **Baldi's Basics Plus**, построенный на .NET и Unity Doorstop. Он предоставляет безопасный и расширяемый способ создавать, загружать и управлять модами без зависимости от BepInEx.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

---

## Возможности

- **Нативная интеграция** — использует [Unity Doorstop](https://github.com/NeighTools/UnityDoorstop) для раннего внедрения в игру.
- **Модульное API** — разделено по областям:
  - `Baldic.API.Core` — ядро, конфигурация, логирование
  - `Baldic.API.UI` — кастомные опции, категории, слайдеры, тогглы
  - `Baldic.API.SaveSystem` — сохранение и загрузка данных модов
  - `Baldic.API.Gameplay` — геймплейные хуки
  - `Baldic.API.Generation` — процедурная генерация (генераторы уровней)
  - `Baldic.API.Resources` — загрузка ресурсов, AssetBundle'ы, локализация
- **Патчинг** — встроенная поддержка Harmony и Cecil.
- **CLI-инструмент** — `baldic` для установки, диагностики, управления модами.
- **Roslyn-анализаторы** — проверка кода модов на этапе компиляции.
- **Кроссплатформенность** — Windows, Linux, macOS.

---

## Структура проекта

```
Baldic/
├── src/
│   ├── Baldic.Bootstrap.Managed/   # Управляемая точка входа (Doorstop → Initialize)
│   ├── Baldic.Loader/              # Загрузчик модов, разрешение зависимостей
│   ├── Baldic.Loader.Abstractions/ # Контракты, модели манифеста
│   ├── Baldic.Patching/            # Harmony / Cecil интеграция
│   ├── Baldic.API.* /              # Публичное API для модов
│   ├── Baldic.Analyzers/           # Roslyn-анализаторы
│   ├── Baldic.Cli/                 # Командная утилита `baldic`
│   └── Baldic.SDK.MSBuild/         # MSBuild SDK для модов
├── tests/                          # Юнит- и интеграционные тесты
├── samples/                        # Примеры модов
│   ├── HelloBaldic/                # Минимальный мод
│   ├── OptionsAndSave/             # Опции и система сохранений
│   ├── CustomItem/                 # Кастомный предмет
│   └── GeneratorAddend/            # Кастомный генератор
├── docs/                           # ADR и спецификации
├── schemas/                        # JSON Schema для baldic.mod.json
└── fingerprints/                   # Фингерпринты версий BB+
```

---

## Быстрый старт

### 1. Установка лоадера

Скопируйте файлы Doorstop (`winhttp.dll` / `libdoorstop.so`) и `doorstop_config.ini` в корень игры. Затем выполните:

```bash
baldic install-loader --game "path/to/BaldisBasicsPlus"
```

### 2. Создание мода

Создайте проект .NET Standard 2.0 и добавьте ссылку на `Baldic.Loader.Abstractions`:

```bash
dotnet new classlib -n MyMod -f netstandard2.0
cd MyMod
dotnet add reference ../Baldic/src/Baldic.Loader.Abstractions/Baldic.Loader.Abstractions.csproj
```

Напишите точку входа:

```csharp
using Baldic.Loader.Abstractions;
using Baldic.Loader.Abstractions.Entrypoints;

public class MyMod : IBaldicModInitializer
{
    public void OnInitialize(ModInitializationContext context)
    {
        Console.WriteLine($"[MyMod] Loaded version {context.Mod.Version}!");
    }
}
```

Добавьте `baldic.mod.json`:

```json
{
  "schemaVersion": 1,
  "id": "my_mod",
  "version": "1.0.0",
  "name": "My First Mod",
  "game": {
    "id": "baldis-basics-plus",
    "versions": [">=0.14.0 <0.15.0"]
  },
  "assemblies": ["lib/MyMod.dll"],
  "entrypoints": {
    "main": ["MyMod"]
  }
}
```

### 3. Установка мода

```bash
baldic install --mod ./MyMod --game "path/to/BaldisBasicsPlus"
```

---

## Спецификация манифеста

Полная спецификация `baldic.mod.json` доступна в [`docs/spec/baldic-mod-json-v1.md`](docs/spec/baldic-mod-json-v1.md). JSON Schema: [`schemas/baldic.mod.schema.json`](schemas/baldic.mod.schema.json).

---

## CLI

```bash
baldic --help
```

Основные команды:

| Команда | Описание |
|---|---|
| `baldic install-loader` | Установить/обновить лоадер в папке игры |
| `baldic uninstall-loader` | Удалить лоадер |
| `baldic install --mod <path>` | Установить мод |
| `baldic list` | Список установленных модов |
| `baldic doctor` | Диагностика окружения |
| `baldic cache clear` | Очистить кэш |

---

## Сборка из исходников

```bash
git clone https://github.com/BaldicTeam/Baldic.git
cd Baldic
dotnet build Baldic.sln
dotnet test
```

---

## Лицензия

Распространяется под лицензией [MIT](LICENSE).  
Unity Doorstop — отдельный проект под лицензией LGPL-2.1, см. `THIRD_PARTY_NOTICES`.

---

## Благодарности

- [NeighTools/UnityDoorstop](https://github.com/NeighTools/UnityDoorstop) — бинарный бутстрап
- Сообщество моддеров Baldi's Basics Plus

---

*Создано с ❤️ командой BaldicTeam.*
