namespace LangFilesEditor.Services.Diagnostics;

/// <summary>
/// Результат подсчёта встроенной диагностики для одного модуля.
/// </summary>
/// <param name="Errors">Число строк с ошибками валидации.</param>
/// <param name="Warnings">Число строк с предупреждениями валидации.</param>
public readonly record struct ModuleDiagnosticCounts(int Errors, int Warnings);