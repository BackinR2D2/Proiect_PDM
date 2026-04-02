using System.Collections.Specialized;
using Proiect_Planificare_Buget.Models;

namespace Proiect_Planificare_Buget.Controls;

public sealed class OverviewLineChartView : GraphicsView, IDrawable
{
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource),
        typeof(IEnumerable<OverviewMonthlyTrendItem>),
        typeof(OverviewLineChartView),
        default(IEnumerable<OverviewMonthlyTrendItem>),
        propertyChanged: OnItemsSourceChanged);

    private INotifyCollectionChanged? _observableItems;

    public OverviewLineChartView()
    {
        Drawable = this;
        HeightRequest = 240;
    }

    public IEnumerable<OverviewMonthlyTrendItem>? ItemsSource
    {
        get => (IEnumerable<OverviewMonthlyTrendItem>?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.SaveState();
        canvas.Antialias = true;

        var items = ItemsSource?.ToList() ?? [];
        if (items.Count == 0)
        {
            DrawEmptyState(canvas, dirtyRect, "Nu exista suficiente date pentru grafic.");
            canvas.RestoreState();
            return;
        }

        const float leftPadding = 22f;
        const float rightPadding = 20f;
        const float topPadding = 16f;
        const float bottomPadding = 32f;

        var chartRect = new RectF(
            leftPadding,
            topPadding,
            Math.Max(0, dirtyRect.Width - leftPadding - rightPadding),
            Math.Max(0, dirtyRect.Height - topPadding - bottomPadding));

        var maxValue = items
            .SelectMany(item => new[] { item.Income, item.Expense })
            .DefaultIfEmpty(1m)
            .Max();

        if (maxValue <= 0)
            maxValue = 1;

        DrawGrid(canvas, chartRect);
        DrawSeries(canvas, chartRect, items, maxValue, item => item.Income, "#16A34A");
        DrawSeries(canvas, chartRect, items, maxValue, item => item.Expense, "#DC2626");
        DrawLabels(canvas, chartRect, items);

        canvas.RestoreState();
    }

    private static void DrawGrid(ICanvas canvas, RectF chartRect)
    {
        canvas.StrokeColor = Color.FromArgb("#E5E7EB");
        canvas.StrokeSize = 1;

        for (var index = 0; index <= 4; index++)
        {
            var y = chartRect.Top + (chartRect.Height / 4f) * index;
            canvas.DrawLine(chartRect.Left, y, chartRect.Right, y);
        }
    }

    private static void DrawSeries(
        ICanvas canvas,
        RectF chartRect,
        IReadOnlyList<OverviewMonthlyTrendItem> items,
        decimal maxValue,
        Func<OverviewMonthlyTrendItem, decimal> valueSelector,
        string colorHex)
    {
        if (items.Count == 0)
            return;

        var lineColor = Color.FromArgb(colorHex);
        var path = new PathF();
        var points = new List<PointF>(items.Count);

        for (var index = 0; index < items.Count; index++)
        {
            var x = CalculateX(chartRect, index, items.Count);
            var value = valueSelector(items[index]);
            var y = CalculateY(chartRect, value, maxValue);
            var point = new PointF(x, y);
            points.Add(point);

            if (index == 0)
            {
                path.MoveTo(point.X, point.Y);
            }
            else
            {
                path.LineTo(point.X, point.Y);
            }
        }

        canvas.StrokeColor = lineColor;
        canvas.StrokeSize = 3;
        canvas.DrawPath(path);

        canvas.FillColor = lineColor;
        foreach (var point in points)
        {
            canvas.FillCircle(point.X, point.Y, 4.5f);
        }
    }

    private static void DrawLabels(ICanvas canvas, RectF chartRect, IReadOnlyList<OverviewMonthlyTrendItem> items)
    {
        canvas.FontColor = Color.FromArgb("#6B7280");
        canvas.FontSize = 11;

        for (var index = 0; index < items.Count; index++)
        {
            var x = CalculateX(chartRect, index, items.Count);
            canvas.DrawString(
                items[index].MonthLabel,
                x - 18,
                chartRect.Bottom + 6,
                36,
                16,
                HorizontalAlignment.Center,
                VerticalAlignment.Top);
        }
    }

    private static float CalculateX(RectF chartRect, int index, int count)
    {
        if (count <= 1)
            return chartRect.Center.X;

        var spacing = chartRect.Width / (count - 1);
        return chartRect.Left + spacing * index;
    }

    private static float CalculateY(RectF chartRect, decimal value, decimal maxValue)
    {
        var ratio = maxValue <= 0 ? 0 : (float)(value / maxValue);
        return chartRect.Bottom - ratio * chartRect.Height;
    }

    private static void DrawEmptyState(ICanvas canvas, RectF dirtyRect, string message)
    {
        canvas.FontColor = Color.FromArgb("#6B7280");
        canvas.FontSize = 13;
        canvas.DrawString(message, dirtyRect, HorizontalAlignment.Center, VerticalAlignment.Center);
    }

    private static void OnItemsSourceChanged(BindableObject bindable, object? oldValue, object? newValue)
    {
        if (bindable is OverviewLineChartView view)
        {
            view.AttachToCollection(oldValue as INotifyCollectionChanged, newValue as INotifyCollectionChanged);
            view.Invalidate();
        }
    }

    private void AttachToCollection(INotifyCollectionChanged? oldCollection, INotifyCollectionChanged? newCollection)
    {
        if (oldCollection is not null)
        {
            oldCollection.CollectionChanged -= OnCollectionChanged;
        }

        _observableItems = newCollection;

        if (_observableItems is not null)
        {
            _observableItems.CollectionChanged += OnCollectionChanged;
        }
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Invalidate();
    }
}
