# NuExt.Minimal.Mvvm.Desktop.Persistence

SQLite persistence layer for desktop MVVM applications. It combines [NuExt.Minimal.Mvvm.Desktop](https://github.com/nu-ext/NuExt.Minimal.Mvvm.Desktop) with [NuExt.System.Data.SQLite](https://github.com/nu-ext/NuExt.System.Data.SQLite) and holds the data access layer for MVVM models, without referencing a single UI framework.

## Contents

| Type | Purpose |
|---|---|
| `KeyValueSettingDal` | Reads and writes key/value settings of an owner row: loads `KeyValueSetting` models and saves only changed rows. |

## Settings storage

`KeyValueSettingDal` expects a table with an owner id column plus the name, raw value and value type columns (`setting_name`, `setting_value`, `value_type`). Rows are mapped to `KeyValueSetting` from NuExt.Minimal.Mvvm.Desktop:

- the raw value is stored as an invariant string;
- `KeyValueSetting.ValueType` is a `Type`, converted to and from the persisted token only at this boundary;
- writes compare the stored values as well as the change tracking state, so a newly created setting is persisted even though `IsChanged` is still `false`, and unchanged rows are skipped.

The table name and the owner id column name are supplied by the application; the settings themselves are loaded into a `KeyValueSettingCollection` subclass, which can be filled with `KeyValueSettingCollectionExtensions.Load` from NuExt.Minimal.Mvvm.Desktop.

## Targets

`netstandard2.0`, `netstandard2.1`, `net462`, `net471`, `net8.0`, `net9.0`, `net10.0`, `net11.0`.

## License

MIT.
