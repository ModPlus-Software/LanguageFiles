namespace LangFilesEditor.Utils;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

/// <summary>
/// Вспомогательные методы для работы с визуальным деревом WPF и перепривязки элементов.
/// </summary>
public static class WpfUtils
{
    // todo: а нужно ли оно вообще если вроде бы есть relative source с parent? Только для гридов? Нужно подумать.
    /// <summary>
    /// Рекурсивно ищет первый дочерний элемент заданного типа в визуальном дереве.
    /// </summary>
    /// <typeparam name="T">Тип искомого элемента.</typeparam>
    /// <param name="obj">Корневой объект визуального дерева для обхода.</param>
    /// <returns>Найденный элемент или <see langword="null"/>.</returns>
    public static T FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
        {
            var child = VisualTreeHelper.GetChild(obj, i);
            if (child is T match)
            {
                return match;
            }

            var childOfChild = FindVisualChild<T>(child);
            if (childOfChild != null)
            {
                return childOfChild;
            }
        }

        return null;
    }

    // todo: вроде бы этот метод относится к Dock панелям. Словно это можно было бы в них и поместить. Ну или по крайней мере в файлик рядом с этим
    /// <summary>
    /// Отсоединяет элемент от текущего родителя и помещает его в содержимое <see cref="ContentControl"/>.
    /// </summary>
    /// <param name="element">Перемещаемый элемент интерфейса.</param>
    /// <param name="host">Контейнер, который станет новым родителем элемента.</param>
    public static void ReparentToContentControl(FrameworkElement element, ContentControl host)
    {
        Detach(element);
        host.Content = element;
    }

    // todo:мб не здесь должно быть?
    /// <summary>
    /// Отсоединяет элемент от текущего визуального или логического родителя.
    /// </summary>
    /// <param name="element">Элемент, который нужно удалить из текущего контейнера.</param>
    public static void Detach(FrameworkElement element)
    {
        switch (element.Parent)
        {
            case Panel panel:
                panel.Children.Remove(element);
                break;
            case ContentControl contentControl when ReferenceEquals(contentControl.Content, element):
                contentControl.Content = null;
                break;
            case ContentPresenter presenter when ReferenceEquals(presenter.Content, element):
                presenter.Content = null;
                break;
            case Decorator decorator when ReferenceEquals(decorator.Child, element):
                decorator.Child = null;
                break;
        }
    }
}