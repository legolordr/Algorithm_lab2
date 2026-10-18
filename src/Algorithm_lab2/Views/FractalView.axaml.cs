using Avalonia.Controls;
using Algorithm_lab2.Controls;
using Algorithm_lab2.Fractals;

namespace Algorithm_lab2.Views;

public partial class FractalView : UserControl
{
    // Add new fractals here; the list, depth slider and statistics pick them up automatically.
    private readonly Fractal[] _fractals =
    [
        new KochSnowflake(),
    ];

    public FractalView()
    {
        InitializeComponent();

        FractalList.ItemsSource = _fractals;
        FractalList.SelectionChanged += (_, _) => OnFractalSelected();
        DepthSlider.ValueChanged += (_, e) => OnDepthChanged((int)e.NewValue);
        Canvas.Built += OnBuilt;

        if (_fractals.Length == 0)
        {
            DepthSlider.IsEnabled = false;
            DepthLabel.Text = "Глубина рекурсии: —";
            DescriptionText.Text = "Фракталы пока не добавлены.";
            return;
        }

        FractalList.SelectedIndex = 0;
    }

    private void OnFractalSelected()
    {
        if (FractalList.SelectedItem is not Fractal fractal)
            return;

        DescriptionText.Text = fractal.Description;
        DepthSlider.Maximum = fractal.MaxDepth;
        DepthSlider.Value = fractal.DefaultDepth;
        Canvas.Depth = fractal.DefaultDepth;
        Canvas.Fractal = fractal;
        DepthLabel.Text = $"Глубина рекурсии: {fractal.DefaultDepth}";
    }

    private void OnDepthChanged(int depth)
    {
        DepthLabel.Text = $"Глубина рекурсии: {depth}";
        Canvas.Depth = depth;
    }

    private void OnBuilt(object? sender, FractalBuiltEventArgs e)
    {
        CallsText.Text = $"Рекурсивных вызовов: {e.Result.Calls:N0}";
        // A polygon has as many sides as points, a polyline one fewer.
        var segments = e.Result.Shapes.Sum(s => s.Closed ? s.Points.Length : s.Points.Length - 1);
        ShapesText.Text = $"Отрезков: {segments:N0}";
        TimeText.Text = $"Построение: {e.Elapsed.TotalMilliseconds:F1} мс";
    }
}
