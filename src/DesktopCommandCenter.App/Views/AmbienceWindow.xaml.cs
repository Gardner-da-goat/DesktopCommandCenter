using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using Color = System.Windows.Media.Color;
using Brushes = System.Windows.Media.Brushes;
using Rectangle = System.Windows.Shapes.Rectangle;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;
using DesktopCommandCenter.Core.Settings;
using Microsoft.Win32;

namespace DesktopCommandCenter.App.Views;

public partial class AmbienceWindow : Window
{
    private const int GwlExStyle = -20;
    private const long WsExTransparent = 0x00000020L;
    private const long WsExToolWindow = 0x00000080L;
    private const long WsExNoActivate = 0x08000000L;

    private readonly Random _random = new();
    private readonly DispatcherTimer _timer;
    private readonly List<FishAgent> _fish = [];
    private readonly List<FireflyAgent> _fireflies = [];

    private PetAgent? _pet;
    private string _mode = "Aquarium";
    private int _population = 10;
    private int _maxPopulation = 24;
    private bool _breedingEnabled = true;
    private double _speed = 1;
    private double _ambienceOpacity = 0.72;
    private bool _allMonitors = true;
    private bool _overApps = true;
    private DateTime _lastFrame = DateTime.UtcNow;
    private double _breedingAccumulator;
    private bool _closingForService;

    public AmbienceWindow(AppSettings settings)
    {
        InitializeComponent();

        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(33)
        };
        _timer.Tick += OnTick;

        SourceInitialized += OnSourceInitialized;
        Loaded += OnLoaded;
        Closed += OnClosed;
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;

        ApplySettings(settings);
    }

    public void ApplySettings(AppSettings settings)
    {
        var previousMode = _mode;
        var previousPopulation = _population;
        var previousAllMonitors = _allMonitors;

        _mode = NormalizeMode(settings.AmbienceMode);
        _population = Math.Clamp(settings.AmbiencePopulation, 2, 30);
        _maxPopulation = Math.Clamp(
            Math.Max(settings.AmbienceMaxPopulation, _population),
            2,
            60);
        _breedingEnabled = settings.AmbienceBreedingEnabled;
        _speed = Math.Clamp(settings.AmbienceSpeed, 0.35, 2.5);
        _ambienceOpacity = Math.Clamp(settings.AmbienceOpacity, 0.15, 1);
        _allMonitors = settings.AmbienceAllMonitors;
        _overApps = settings.AmbienceOverApps;

        Topmost = _overApps;

        if (previousAllMonitors != _allMonitors || !IsLoaded)
        {
            UpdateBounds();
        }

        if (IsLoaded &&
            (!string.Equals(previousMode, _mode, StringComparison.Ordinal) ||
             previousPopulation != _population))
        {
            ResetScene();
        }
    }

    public void CloseForService()
    {
        _closingForService = true;
        Close();
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        try
        {
            var handle = new WindowInteropHelper(this).Handle;
            var styles = GetWindowLongPtr(handle, GwlExStyle).ToInt64();
            styles |= WsExTransparent | WsExToolWindow | WsExNoActivate;
            _ = SetWindowLongPtr(handle, GwlExStyle, (nint)styles);
        }
        catch
        {
            // The ambience is still non-interactive at the WPF level if native styling fails.
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateBounds();
        ResetScene();
        _lastFrame = DateTime.UtcNow;
        _timer.Start();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _timer.Stop();
        _timer.Tick -= OnTick;
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        SceneCanvas.Children.Clear();
        _fish.Clear();
        _fireflies.Clear();
        _pet = null;
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            UpdateBounds();
            ResetScene();
        }));
    }

    private void UpdateBounds()
    {
        if (_allMonitors)
        {
            Left = SystemParameters.VirtualScreenLeft;
            Top = SystemParameters.VirtualScreenTop;
            Width = Math.Max(1, SystemParameters.VirtualScreenWidth);
            Height = Math.Max(1, SystemParameters.VirtualScreenHeight);
        }
        else
        {
            var area = SystemParameters.WorkArea;
            Left = area.Left;
            Top = area.Top;
            Width = Math.Max(1, area.Width);
            Height = Math.Max(1, area.Height);
        }
    }

    private void ResetScene()
    {
        SceneCanvas.Children.Clear();
        _fish.Clear();
        _fireflies.Clear();
        _pet = null;
        _breedingAccumulator = 0;

        switch (_mode)
        {
            case "Fireflies":
                for (var index = 0; index < _population; index++)
                {
                    AddFirefly();
                }
                break;

            case "Desktop Pet":
                AddPet();
                break;

            default:
                for (var index = 0; index < _population; index++)
                {
                    AddFish(RandomSpecies(), isBaby: false);
                }
                break;
        }
    }

    private void OnTick(object? sender, EventArgs e)
    {
        var now = DateTime.UtcNow;
        var delta = Math.Clamp((now - _lastFrame).TotalSeconds, 0.001, 0.08);
        _lastFrame = now;

        switch (_mode)
        {
            case "Fireflies":
                UpdateFireflies(delta);
                break;

            case "Desktop Pet":
                UpdatePet(delta);
                break;

            default:
                UpdateFish(delta);
                break;
        }
    }

    private void UpdateFish(double delta)
    {
        if (Width <= 0 || Height <= 0)
        {
            return;
        }

        var scaledDelta = delta * _speed;

        for (var index = _fish.Count - 1; index >= 0; index--)
        {
            var fish = _fish[index];
            fish.AgeSeconds += scaledDelta;
            fish.BreedingCooldown = Math.Max(0, fish.BreedingCooldown - scaledDelta);
            fish.SteeringTimer -= scaledDelta;

            if (fish.SteeringTimer <= 0)
            {
                fish.SteeringTimer = 1.5 + _random.NextDouble() * 3.5;
                fish.VelocityY += (_random.NextDouble() - 0.5) * 12;
                fish.VelocityY = Math.Clamp(fish.VelocityY, -18, 18);
            }

            fish.X += fish.VelocityX * scaledDelta;
            fish.Y += fish.VelocityY * scaledDelta;

            var growth = fish.IsBaby
                ? Math.Clamp(0.45 + fish.AgeSeconds / 28 * 0.55, 0.45, 1)
                : 1;

            var visualWidth = 78 * fish.AdultScale * growth;
            var visualHeight = 40 * fish.AdultScale * growth;

            if (fish.VelocityX > 0 && fish.X > Width + visualWidth)
            {
                fish.X = -visualWidth;
            }
            else if (fish.VelocityX < 0 && fish.X < -visualWidth)
            {
                fish.X = Width + visualWidth;
            }

            if (fish.Y < 12)
            {
                fish.Y = 12;
                fish.VelocityY = Math.Abs(fish.VelocityY);
            }
            else if (fish.Y > Height - visualHeight - 22)
            {
                fish.Y = Math.Max(12, Height - visualHeight - 22);
                fish.VelocityY = -Math.Abs(fish.VelocityY);
            }

            fish.IsBaby = fish.AgeSeconds < 28;
            fish.Visual.Opacity = _ambienceOpacity;
            fish.Visual.RenderTransform = new ScaleTransform(
                fish.VelocityX >= 0 ? growth : -growth,
                growth,
                39,
                20);

            Canvas.SetLeft(fish.Visual, fish.X);
            Canvas.SetTop(fish.Visual, fish.Y);

            if (fish.AgeSeconds > fish.LifeSeconds &&
                _fish.Count > _population)
            {
                SceneCanvas.Children.Remove(fish.Visual);
                _fish.RemoveAt(index);
            }
        }

        if (!_breedingEnabled || _fish.Count >= _maxPopulation)
        {
            return;
        }

        _breedingAccumulator += scaledDelta;
        if (_breedingAccumulator < 1)
        {
            return;
        }

        _breedingAccumulator = 0;

        var adults = _fish
            .Where(fish =>
                !fish.IsBaby &&
                fish.AgeSeconds >= 32 &&
                fish.BreedingCooldown <= 0)
            .OrderBy(_ => _random.Next())
            .ToArray();

        foreach (var first in adults)
        {
            var partner = adults.FirstOrDefault(second =>
                !ReferenceEquals(first, second) &&
                second.Species == first.Species &&
                second.BreedingCooldown <= 0 &&
                Distance(first.X, first.Y, second.X, second.Y) < 130);

            if (partner is null || _random.NextDouble() > 0.045)
            {
                continue;
            }

            var childScale = Math.Clamp(
                ((first.AdultScale + partner.AdultScale) / 2) *
                (0.9 + _random.NextDouble() * 0.2),
                0.68,
                1.35);

            var childSpeed = Math.Clamp(
                ((Math.Abs(first.VelocityX) + Math.Abs(partner.VelocityX)) / 2) *
                (0.9 + _random.NextDouble() * 0.2),
                24,
                78);

            var child = AddFish(
                first.Species,
                isBaby: true,
                x: (first.X + partner.X) / 2,
                y: (first.Y + partner.Y) / 2,
                adultScale: childScale,
                speed: childSpeed);

            child.BreedingCooldown = 75;
            first.BreedingCooldown = 42 + _random.NextDouble() * 28;
            partner.BreedingCooldown = 42 + _random.NextDouble() * 28;
            break;
        }
    }

    private FishAgent AddFish(
        FishSpecies species,
        bool isBaby,
        double? x = null,
        double? y = null,
        double? adultScale = null,
        double? speed = null)
    {
        var traits = GetTraits(species);
        var scale = adultScale ?? traits.Scale * (0.88 + _random.NextDouble() * 0.24);
        var velocity = speed ?? traits.Speed * (0.86 + _random.NextDouble() * 0.28);
        var direction = _random.Next(0, 2) == 0 ? -1d : 1d;
        var color = MutateColor(traits.Color);

        var visual = CreateFishVisual(color, traits.Accent, species);
        SceneCanvas.Children.Add(visual);

        var fish = new FishAgent
        {
            Species = species,
            Visual = visual,
            X = x ?? _random.NextDouble() * Math.Max(100, Width),
            Y = y ?? 30 + _random.NextDouble() * Math.Max(80, Height - 100),
            VelocityX = velocity * direction,
            VelocityY = (_random.NextDouble() - 0.5) * 14,
            AdultScale = scale,
            IsBaby = isBaby,
            AgeSeconds = isBaby ? 0 : 35 + _random.NextDouble() * 120,
            LifeSeconds = 540 + _random.NextDouble() * 420,
            BreedingCooldown = isBaby ? 85 : 12 + _random.NextDouble() * 50,
            SteeringTimer = 1 + _random.NextDouble() * 3
        };

        visual.Opacity = _ambienceOpacity;
        _fish.Add(fish);
        return fish;
    }

    private FrameworkElement CreateFishVisual(
        Color bodyColor,
        Color accentColor,
        FishSpecies species)
    {
        var canvas = new Canvas
        {
            Width = 78,
            Height = 40,
            IsHitTestVisible = false
        };

        var bodyBrush = new SolidColorBrush(bodyColor);
        var accentBrush = new SolidColorBrush(accentColor);

        var tail = new Polygon
        {
            Points = new PointCollection
            {
                new(3, 20),
                new(21, 6),
                new(21, 34)
            },
            Fill = accentBrush,
            Opacity = 0.9
        };

        var body = new Ellipse
        {
            Width = 49,
            Height = species == FishSpecies.Angelfish ? 31 : 25,
            Fill = bodyBrush,
            Stroke = new SolidColorBrush(Color.FromArgb(110, 255, 255, 255)),
            StrokeThickness = 1
        };
        Canvas.SetLeft(body, 18);
        Canvas.SetTop(body, species == FishSpecies.Angelfish ? 4 : 7);

        if (species == FishSpecies.Neon)
        {
            var stripe = new Rectangle
            {
                Width = 36,
                Height = 3,
                RadiusX = 1.5,
                RadiusY = 1.5,
                Fill = new SolidColorBrush(Color.FromRgb(116, 241, 255)),
                Opacity = 0.95
            };
            Canvas.SetLeft(stripe, 27);
            Canvas.SetTop(stripe, 18);
            canvas.Children.Add(stripe);
        }

        var fin = new Polygon
        {
            Points = new PointCollection
            {
                new(38, 18),
                new(49, 5),
                new(52, 19)
            },
            Fill = accentBrush,
            Opacity = 0.72
        };

        var eye = new Ellipse
        {
            Width = 5,
            Height = 5,
            Fill = Brushes.White
        };
        Canvas.SetLeft(eye, 58);
        Canvas.SetTop(eye, 13);

        var pupil = new Ellipse
        {
            Width = 2.4,
            Height = 2.4,
            Fill = Brushes.Black
        };
        Canvas.SetLeft(pupil, 60);
        Canvas.SetTop(pupil, 14.3);

        canvas.Children.Add(tail);
        canvas.Children.Add(body);
        canvas.Children.Add(fin);
        canvas.Children.Add(eye);
        canvas.Children.Add(pupil);

        return canvas;
    }

    private void UpdateFireflies(double delta)
    {
        var scaledDelta = delta * _speed;

        foreach (var firefly in _fireflies)
        {
            firefly.X += firefly.VelocityX * scaledDelta;
            firefly.Y += firefly.VelocityY * scaledDelta;
            firefly.Phase += firefly.PhaseSpeed * scaledDelta;

            if (firefly.X < -12) firefly.X = Width + 12;
            if (firefly.X > Width + 12) firefly.X = -12;
            if (firefly.Y < -12) firefly.Y = Height + 12;
            if (firefly.Y > Height + 12) firefly.Y = -12;

            var pulse = 0.28 + 0.72 * ((Math.Sin(firefly.Phase) + 1) / 2);
            firefly.Visual.Opacity = _ambienceOpacity * pulse;
            Canvas.SetLeft(firefly.Visual, firefly.X);
            Canvas.SetTop(firefly.Visual, firefly.Y);
        }
    }

    private void AddFirefly()
    {
        var size = 5 + _random.NextDouble() * 6;
        var glow = new Ellipse
        {
            Width = size,
            Height = size,
            Fill = new SolidColorBrush(Color.FromRgb(255, 232, 120)),
            Effect = new DropShadowEffect
            {
                Color = Color.FromRgb(255, 218, 84),
                BlurRadius = 14,
                ShadowDepth = 0,
                Opacity = 0.9
            },
            IsHitTestVisible = false
        };

        SceneCanvas.Children.Add(glow);
        _fireflies.Add(new FireflyAgent
        {
            Visual = glow,
            X = _random.NextDouble() * Math.Max(100, Width),
            Y = _random.NextDouble() * Math.Max(100, Height),
            VelocityX = (_random.NextDouble() - 0.5) * 26,
            VelocityY = (_random.NextDouble() - 0.5) * 20,
            Phase = _random.NextDouble() * Math.PI * 2,
            PhaseSpeed = 1.5 + _random.NextDouble() * 2.8
        });
    }

    private void AddPet()
    {
        var visual = CreatePetVisual();
        SceneCanvas.Children.Add(visual);

        _pet = new PetAgent
        {
            Visual = visual,
            X = Math.Max(20, Width * 0.22),
            Direction = 1,
            Speed = 42,
            PauseSeconds = 0
        };
    }

    private FrameworkElement CreatePetVisual()
    {
        var canvas = new Canvas
        {
            Width = 92,
            Height = 68,
            IsHitTestVisible = false
        };

        var fur = new SolidColorBrush(Color.FromRgb(174, 181, 194));
        var dark = new SolidColorBrush(Color.FromRgb(72, 79, 92));

        var tail = new Polyline
        {
            Points = new PointCollection
            {
                new(18, 46),
                new(5, 42),
                new(2, 29)
            },
            Stroke = fur,
            StrokeThickness = 8,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };

        var body = new Ellipse
        {
            Width = 52,
            Height = 29,
            Fill = fur
        };
        Canvas.SetLeft(body, 18);
        Canvas.SetTop(body, 31);

        var head = new Ellipse
        {
            Width = 31,
            Height = 29,
            Fill = fur
        };
        Canvas.SetLeft(head, 56);
        Canvas.SetTop(head, 22);

        var leftEar = new Polygon
        {
            Points = new PointCollection { new(59, 27), new(63, 10), new(70, 25) },
            Fill = fur
        };

        var rightEar = new Polygon
        {
            Points = new PointCollection { new(72, 25), new(81, 10), new(84, 30) },
            Fill = fur
        };

        var eye1 = new Ellipse { Width = 3, Height = 4, Fill = dark };
        Canvas.SetLeft(eye1, 67);
        Canvas.SetTop(eye1, 32);

        var eye2 = new Ellipse { Width = 3, Height = 4, Fill = dark };
        Canvas.SetLeft(eye2, 77);
        Canvas.SetTop(eye2, 32);

        var nose = new Ellipse { Width = 4, Height = 3, Fill = new SolidColorBrush(Color.FromRgb(230, 143, 153)) };
        Canvas.SetLeft(nose, 73);
        Canvas.SetTop(nose, 39);

        canvas.Children.Add(tail);
        canvas.Children.Add(body);
        canvas.Children.Add(leftEar);
        canvas.Children.Add(rightEar);
        canvas.Children.Add(head);
        canvas.Children.Add(eye1);
        canvas.Children.Add(eye2);
        canvas.Children.Add(nose);

        return canvas;
    }

    private void UpdatePet(double delta)
    {
        if (_pet is null)
        {
            return;
        }

        var pet = _pet;
        var floorY = Math.Max(10, Height - 118);
        pet.PauseSeconds -= delta;

        if (pet.PauseSeconds <= 0)
        {
            pet.X += pet.Speed * pet.Direction * delta * _speed;

            if (_random.NextDouble() < 0.0017)
            {
                pet.PauseSeconds = 2 + _random.NextDouble() * 7;
            }
        }

        if (pet.X < 8)
        {
            pet.X = 8;
            pet.Direction = 1;
        }
        else if (pet.X > Width - 100)
        {
            pet.X = Math.Max(8, Width - 100);
            pet.Direction = -1;
        }

        pet.Visual.Opacity = _ambienceOpacity;
        pet.Visual.RenderTransform = new ScaleTransform(
            pet.Direction >= 0 ? 1 : -1,
            1,
            46,
            34);

        Canvas.SetLeft(pet.Visual, pet.X);
        Canvas.SetTop(pet.Visual, floorY);
    }

    private FishSpecies RandomSpecies()
    {
        var value = _random.NextDouble();
        if (value < 0.34) return FishSpecies.Guppy;
        if (value < 0.62) return FishSpecies.Neon;
        if (value < 0.86) return FishSpecies.Goldfish;
        return FishSpecies.Angelfish;
    }

    private static SpeciesTraits GetTraits(FishSpecies species) =>
        species switch
        {
            FishSpecies.Neon => new SpeciesTraits(
                Color.FromRgb(42, 146, 226),
                Color.FromRgb(47, 235, 227),
                0.72,
                62),
            FishSpecies.Goldfish => new SpeciesTraits(
                Color.FromRgb(255, 135, 57),
                Color.FromRgb(255, 198, 82),
                1.02,
                38),
            FishSpecies.Angelfish => new SpeciesTraits(
                Color.FromRgb(170, 143, 232),
                Color.FromRgb(100, 82, 151),
                1.17,
                33),
            _ => new SpeciesTraits(
                Color.FromRgb(91, 196, 154),
                Color.FromRgb(255, 197, 93),
                0.84,
                49)
        };

    private Color MutateColor(Color color)
    {
        var factor = 0.88 + _random.NextDouble() * 0.24;
        return Color.FromRgb(
            (byte)Math.Clamp((int)(color.R * factor), 0, 255),
            (byte)Math.Clamp((int)(color.G * factor), 0, 255),
            (byte)Math.Clamp((int)(color.B * factor), 0, 255));
    }

    private static string NormalizeMode(string? value) =>
        value switch
        {
            "Fireflies" => "Fireflies",
            "Desktop Pet" => "Desktop Pet",
            _ => "Aquarium"
        };

    private static double Distance(double x1, double y1, double x2, double y2)
    {
        var dx = x1 - x2;
        var dy = y1 - y2;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern nint GetWindowLongPtr64(nint hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLong32(nint hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern nint SetWindowLongPtr64(nint hWnd, int nIndex, nint newLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
    private static extern int SetWindowLong32(nint hWnd, int nIndex, int newLong);

    private static nint GetWindowLongPtr(nint hWnd, int nIndex) =>
        IntPtr.Size == 8
            ? GetWindowLongPtr64(hWnd, nIndex)
            : (nint)GetWindowLong32(hWnd, nIndex);

    private static nint SetWindowLongPtr(nint hWnd, int nIndex, nint value) =>
        IntPtr.Size == 8
            ? SetWindowLongPtr64(hWnd, nIndex, value)
            : (nint)SetWindowLong32(hWnd, nIndex, (int)value);

    private enum FishSpecies
    {
        Guppy,
        Neon,
        Goldfish,
        Angelfish
    }

    private sealed class FishAgent
    {
        public required FishSpecies Species { get; init; }
        public required FrameworkElement Visual { get; init; }
        public double X { get; set; }
        public double Y { get; set; }
        public double VelocityX { get; set; }
        public double VelocityY { get; set; }
        public double AdultScale { get; set; }
        public double AgeSeconds { get; set; }
        public double LifeSeconds { get; set; }
        public double BreedingCooldown { get; set; }
        public double SteeringTimer { get; set; }
        public bool IsBaby { get; set; }
    }

    private sealed class FireflyAgent
    {
        public required FrameworkElement Visual { get; init; }
        public double X { get; set; }
        public double Y { get; set; }
        public double VelocityX { get; set; }
        public double VelocityY { get; set; }
        public double Phase { get; set; }
        public double PhaseSpeed { get; set; }
    }

    private sealed class PetAgent
    {
        public required FrameworkElement Visual { get; init; }
        public double X { get; set; }
        public double Direction { get; set; }
        public double Speed { get; set; }
        public double PauseSeconds { get; set; }
    }

    private readonly record struct SpeciesTraits(
        Color Color,
        Color Accent,
        double Scale,
        double Speed);
}
