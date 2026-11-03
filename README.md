# LanguageFiles
Language files of ModPlus localization

## Проверка, сборка и публикация

Все алгоритмы работы с файлами локализации — в библиотеке `ModPlus.LanguageTools`
(её используют редактор `LangFilesEditor`, программа сборки ModPlusBuilder и расширение Visual Studio
ModPlusVSTools). Библиотека собирается под `net8.0` и `netstandard2.0` — вторая сборка нужна расширению VS,
которое работает на .NET Framework; выгрузка (`Publishing`) есть только в `net8.0`, поэтому в библиотеке
нельзя использовать API .NET 5+ вне папки `Publishing`.
Версия пакета локализации хранится в `LanguageFiles/Langs.xml`.

Консольная утилита `ModPlus.LanguageTools.Cli`:

```
dotnet run --project ModPlus.LanguageTools.Cli -- check   --root LanguageFiles
dotnet run --project ModPlus.LanguageTools.Cli -- build   --root LanguageFiles --output <папка>
dotnet run --project ModPlus.LanguageTools.Cli -- publish --root LanguageFiles --output <папка>
```

- `check` — проверка без изменения файлов (ошибки → код возврата 1, предупреждения только выводятся);
- `build` — исправление кавычек в исходных файлах, проверка, сборка с версией из `Langs.xml`;
- `publish` — то же плюс поднятие версии (вторая часть +1 от большей из версий в `Langs.xml`
  и в хранилище) и выгрузка на серверы и в хранилище. Параметры выгрузки — переменные окружения,
  список — `dotnet run --project ModPlus.LanguageTools.Cli -- --help`.

GitHub Actions: `.github/workflows/check-languages.yml` проверяет каждый pull request,
`.github/workflows/publish-languages.yml` публикует локализацию после слияния в `master`
и коммитит новую версию `Langs.xml`.
