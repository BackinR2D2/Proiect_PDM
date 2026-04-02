using System.Collections.Specialized;
using Proiect_Planificare_Buget.Models;

namespace Proiect_Planificare_Buget.Controls;

public sealed class OverviewDoughnutChartView : GraphicsView, IDrawable
{
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource),
        typeof(IEnumerable<OverviewExpenseCategoryItem>),
        typeof(OverviewDoughnutChartView),
        default(IEnumerable<OverviewExpenseCategoryItem>),
        propertyChanged: OnBindablePropertyChanged);

    public static readonly BindableProperty CenterPrimaryTextProperty = BindableProperty.Create(
        nameof(CenterPrimaryText),
        typeof(string),
        typeof(OverviewDoughnutChartView),
        string.Empty,
        propertyChanged: OnBindablePropertyChanged);

    public static readonly BindableProperty CenterSecondaryTextProperty = BindableProperty.Create(
        nameof(CenterSecondaryText),
        typeof(string),
        typeof(OverviewDoughnutChartView),
        string.Empty,
        propertyChanged: OnBindablePropertyChanged);

    private INotifyCollectionChanged? _observableItems;

    public OverviewDoughnutChartView()
    {
        Drawable = this;
        HeightRequest = 260;
    }

    public IEnumerable<OverviewExpenseCategoryItem>? ItemsSource
    {
        get => (IEnumerable<OverviewExpenseCategoryItem>?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public string CenterPrimaryText
    {
        get => (string)GetValue(CenterPrimaryTextProperty);
        set => SetValue(CenterPrimaryTextProperty, value);
    }

    public string CenterSecondaryText
    {
        get => (string)GetValue(CenterSecondaryTextProperty);
        set => SetValue(CenterSecondaryTextProperty, value);
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.SaveState();
        canvas.Antialias = true;

        var items = ItemsSource?.Where(item => item.Amount > 0).ToList() ?? [];
        var total = items.Sum(item => item.Amount);
        if (items.Count == 0 || total <= 0)
        {
            DrawEmptyState(canvas, dirtyRect, "Nu exista cheltuieli pentru doughnut chart.");
            canvas.RestoreState();
            return;
        }

        var size = Math.Min(dirtyRect.Width, dirtyRect.Height);
        var ringSize = Math.Min(size * 0.22f, 34f);
        var radius = Math.Min(dirtyRect.Width, dirtyRect.Height) * 0.34f;
        var circleRect = new RectF(
            dirtyRect.Center.X - radius,
            dirtyRect.Center.Y - radius,
            radius * 2,
            radius * 2);

        canvas.StrokeSize = ringSize;
        canvas.StrokeLineCap = LineCap.Round;

        var currentAngle = -90f;
        foreach (var item in items)
        {
            var sweepAngle = total <= 0 ? 0f : (float)(item.Amount / total * 360m);
            if (sweepAngle <= 0)
                continue;

            canvas.StrokeColor = Color.FromArgb(item.AccentColor);
            canvas.DrawArc(
                circleRect.X,
                circleRect.Y,
                circleRect.Width,
                circleRect.Height,
                currentAngle,
                currentAngle + sweepAngle,
                true,
                false);

            currentAngle += sweepAngle;
        }

        DrawCenterText(canvas, dirtyRect);
        canvas.RestoreState();
    }

    private void DrawCenterText(ICanvas canvas, RectF dirtyRect)
    {
        canvas.FontColor = Color.FromArgb("#111827");
        canvas.FontSize = 16;
        canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;
        canvas.DrawString(
            CenterPrimaryText,
            dirtyRect.Center.X - 70,
            dirtyRect.Center.Y - 18,
            140,
            22,
            HorizontalAlignment.Center,
            VerticalAlignment.Center);

        canvas.FontColor = Color.FromArgb("#6B7280");
        canvas.FontSize = 12;
        canvas.Font = Microsoft.Maui.Graphics.Font.Default;
        canvas.DrawString(
            CenterSecondaryText,
            dirtyRect.Center.X - 70,
            dirtyRect.Center.Y + 6,
            140,
            20,
            HorizontalAlignment.Center,
            VerticalAlignment.Center);
    }

    private static void DrawEmptyState(ICanvas canvas, RectF dirtyRect, string message)
    {
        canvas.FontColor = Color.FromArgb("#6B7280");
        canvas.FontSize = 13;
        canvas.DrawString(message, dirtyRect, HorizontalAlignment.Center, VerticalAlignment.Center);
    }

    private static void OnBindablePropertyChanged(BindableObject bindable, object? oldValue, object? newValue)
    {
        if (bindable is OverviewDoughnutChartView view)
        {
            if (ReferenceEquals(oldValue, newValue))
            {
                view.Invalidate();
                return;
            }

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
