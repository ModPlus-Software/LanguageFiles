namespace LangFilesEditor.UI.Infrastructure;

using System.Windows;
using System.Windows.Media;

/// <summary>
/// Обрезка элемента по скруглённому прямоугольнику.
/// </summary>
/// <remarks>
/// <see cref="System.Windows.Controls.Border"/> рисует скругление только у собственного фона
/// и рамки, а содержимое выводит поверх них без обрезки. Непрозрачный потомок во всю ширину
/// (грид, шапка панели) закрашивает углы, и скругление пропадает.
/// Свойство ставится на тот же элемент, у которого задан CornerRadius, с тем же радиусом.
/// </remarks>
public static class CornerClip
{
    /// <summary>
    /// Радиус обрезки в независимых от устройства единицах. Ноль отключает обрезку.
    /// </summary>
    public static readonly DependencyProperty RadiusProperty = DependencyProperty.RegisterAttached(
        "Radius",
        typeof(double),
        typeof(CornerClip),
        new PropertyMetadata(0d, OnRadiusChanged));

    /// <summary>
    /// Задаёт радиус обрезки.
    /// </summary>
    /// <param name="element">Элемент, содержимое которого обрезается.</param>
    /// <param name="value">Радиус обрезки.</param>
    public static void SetRadius(DependencyObject element, double value) =>
        element.SetValue(RadiusProperty, value);

    /// <summary>
    /// Возвращает радиус обрезки.
    /// </summary>
    /// <param name="element">Элемент, содержимое которого обрезается.</param>
    /// <returns>Радиус обрезки.</returns>
    public static double GetRadius(DependencyObject element) =>
        (double)element.GetValue(RadiusProperty);

    private static void OnRadiusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
        {
            return;
        }

        // Подписка снимается всегда: свойство может меняться повторно, а двойная
        // подписка на SizeChanged пересчитывала бы геометрию лишний раз.
        element.SizeChanged -= OnSizeChanged;

        if (e.NewValue is not double radius || radius <= 0)
        {
            element.Clip = null;
            return;
        }

        element.SizeChanged += OnSizeChanged;
        ApplyClip(element, radius);
    }

    private static void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        var element = (FrameworkElement)sender;
        ApplyClip(element, GetRadius(element));
    }

    private static void ApplyClip(FrameworkElement element, double radius)
    {
        if (radius <= 0 || element.ActualWidth <= 0 || element.ActualHeight <= 0)
        {
            element.Clip = null;
            return;
        }

        var geometry = new RectangleGeometry(
            new Rect(0, 0, element.ActualWidth, element.ActualHeight),
            radius,
            radius);

        // Заморозка: геометрия пересоздаётся на каждое изменение размера,
        // а замороженная не тянет за собой отслеживание изменений.
        geometry.Freeze();
        element.Clip = geometry;
    }
}
