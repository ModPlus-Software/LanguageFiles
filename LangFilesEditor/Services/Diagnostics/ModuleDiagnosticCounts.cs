namespace LangFilesEditor.Services.Diagnostics;

// todo: пока по моим ощущениям это лишний класс
/// <summary>
/// Результат подсчёта встроенной диагностики для одного модуля.
/// </summary>
/// <param name="Errors">Число строк с ошибками валидации.</param>
/// <param name="Warnings">Число строк с предупреждениями валидации.</param>
public readonly record struct ModuleDiagnosticCounts(int Errors, int Warnings);