using Avalonia.Controls;
using Avalonia.Input;
using Algorithm_lab2.Controls;
using Algorithm_lab2.Fractals;

namespace Algorithm_lab2.Views;

public partial class FractalView : UserControl, IShortcutHandler
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
            DepthRow.Value = "—";
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
        DepthRow.Value = fractal.DefaultDepth.ToString();
    }

    private void OnDepthChanged(int depth)
    {
        DepthRow.Value = depth.ToString();
        Canvas.Depth = depth;
    }

    public bool HandleShortcut(Key key)
    {
        if (!DepthSlider.IsEnabled || key is not (Key.Left or Key.Right))
            return false;

        DepthSlider.Value = Math.Clamp(DepthSlider.Value + (key == Key.Right ? 1 : -1), 0, DepthSlider.Maximum);
        return true;
    }

    private void OnBuilt(object? sender, FractalBuiltEventArgs e)
    {
        CallsStat.Value = $"{e.Result.Calls:N0}";
        // A polygon has as many sides as points, a polyline one fewer.
        var segments = e.Result.Shapes.Sum(s => s.Closed ? s.Points.Length : s.Points.Length - 1);
        SegmentsStat.Value = $"{segments:N0}";
        TimeStat.Value = TimeFormat.Format(e.Elapsed);
    }
}
