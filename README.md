# NuExt.Minimal.Mvvm.Desktop.Persistence

`NuExt.Minimal.Mvvm.Desktop.Persistence` is a **SQLite persistence layer for desktop MVVM applications**. It combines [NuExt.Minimal.Mvvm.Desktop](https://github.com/nu-ext/NuExt.Minimal.Mvvm.Desktop) with [NuExt.System.Data.SQLite](https://github.com/nu-ext/NuExt.System.Data.SQLite) and holds the data access layer for MVVM models, without referencing a single UI framework.

[![NuGet](https://img.shields.io/nuget/v/NuExt.Minimal.Mvvm.Desktop.Persistence.svg)](https://www.nuget.org/packages/NuExt.Minimal.Mvvm.Desktop.Persistence)
[![Ask DeepWiki](https://img.shields.io/badge/Ask-DeepWiki-blue)](https://deepwiki.com/nu-ext/NuExt.Minimal.Mvvm.Desktop.Persistence)
[![Build](https://github.com/nu-ext/NuExt.Minimal.Mvvm.Desktop.Persistence/actions/workflows/ci.yml/badge.svg)](https://github.com/nu-ext/NuExt.Minimal.Mvvm.Desktop.Persistence/actions/workflows/ci.yml)
[![License](https://img.shields.io/github/license/nu-ext/NuExt.Minimal.Mvvm.Desktop.Persistence?label=license)](https://github.com/nu-ext/NuExt.Minimal.Mvvm.Desktop.Persistence/blob/main/LICENSE)
[![Downloads](https://img.shields.io/nuget/dt/NuExt.Minimal.Mvvm.Desktop.Persistence.svg)](https://www.nuget.org/packages/NuExt.Minimal.Mvvm.Desktop.Persistence)

## Why NuExt.Minimal.Mvvm.Desktop.Persistence?

- **Settings in a database** - `KeyValueSetting` models are read from and written to a settings table without a separate DTO layer.
- **Change-aware writes** - only rows whose stored value or value type actually changed are written, independent of the change tracking state.
- **Explicit transactions** - operations run inside a caller-owned `SQLiteDbContext`, so settings reads and writes join the surrounding unit of work.
- **Provider foundation** - built on the `NuExt.System.Data.SQLite` DAL infrastructure instead of an ORM.
- **No UI framework** - no WPF, Avalonia, WinUI, or MAUI reference.

## API Overview

- **`KeyValueSettingDal`**: reads and writes key/value settings of an owner row - loads `KeyValueSetting` models and saves only changed rows.

## Settings storage

`KeyValueSettingDal` expects a table with an owner id column plus the name, raw value, and value type columns (`setting_name`, `setting_value`, `value_type`). Rows are mapped to `KeyValueSetting` from NuExt.Minimal.Mvvm.Desktop:

- the raw value is stored as an invariant string;
- `KeyValueSetting.ValueType` is a `Type`, converted to and from the persisted token only at this boundary;
- writes compare the stored values as well as the change tracking state, so a newly created setting is persisted even though `IsChanged` is still `false`, and unchanged rows are skipped.

The table name and the owner id column name are supplied by the application; the settings themselves are loaded into a `KeyValueSettingCollection` subclass, which can be filled with `KeyValueSettingCollectionExtensions.Load` from NuExt.Minimal.Mvvm.Desktop.

## Compatibility

- **.NET Standard 2.0+, .NET 8/9/10/11, and .NET Framework 4.6.2+**
- SQLite provider: `NuExt.System.Data.SQLite`
- Does not reference any UI framework

## Installation

Via [NuGet](https://www.nuget.org/):

```shell
dotnet add package NuExt.Minimal.Mvvm.Desktop.Persistence
```

Or via Visual Studio:

1. Go to `Tools -> NuGet Package Manager -> Manage NuGet Packages for Solution...`.
2. Search for `NuExt.Minimal.Mvvm.Desktop.Persistence`.
3. Click "Install".

## Ecosystem

- [NuExt.System](https://github.com/nu-ext/NuExt.System)
- [NuExt.System.Data](https://github.com/nu-ext/NuExt.System.Data)
- [NuExt.System.Data.SQLite](https://github.com/nu-ext/NuExt.System.Data.SQLite)
- [NuExt.Minimal.Mvvm](https://github.com/nu-ext/NuExt.Minimal.Mvvm)
- [NuExt.Minimal.Mvvm.SourceGenerator](https://github.com/nu-ext/NuExt.Minimal.Mvvm.SourceGenerator)
- [NuExt.Minimal.Behaviors.Wpf](https://github.com/nu-ext/NuExt.Minimal.Behaviors.Wpf)
- [NuExt.Presentation.Wpf](https://github.com/nu-ext/NuExt.Presentation.Wpf)
- [NuExt.Minimal.Mvvm.Wpf](https://github.com/nu-ext/NuExt.Minimal.Mvvm.Wpf)
- [NuExt.Minimal.Mvvm.AvalonDock](https://github.com/nu-ext/NuExt.Minimal.Mvvm.AvalonDock)
- [NuExt.Minimal.Mvvm.MahApps.Metro](https://github.com/nu-ext/NuExt.Minimal.Mvvm.MahApps.Metro)
- [NuExt.Minimal.Mvvm.Desktop](https://github.com/nu-ext/NuExt.Minimal.Mvvm.Desktop)

## Contributing

Issues and PRs are welcome. Keep changes minimal and performance-conscious.

## License

MIT. See LICENSE.
