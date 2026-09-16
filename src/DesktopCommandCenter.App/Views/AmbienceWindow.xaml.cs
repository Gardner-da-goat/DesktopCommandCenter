using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using DesktopCommandCenter.App.Services;
using DesktopCommandCenter.Core.Settings;
using Microsoft.Win32;
using Brushes = System.Windows.Media.Brushes;
using Brush = System.Windows.Media.Brush;
using Point = System.Windows.Point;
using Color = System.Windows.Media.Color;
using Ellipse = System.Windows.Shapes.Ellipse;
using Path = System.Windows.Shapes.Path;
using Polygon = System.Windows.Shapes.Polygon;
using Rectangle = System.Windows.Shapes.Rectangle;

namespace DesktopCommandCenter.App.Views;

public partial class AmbienceWindow : Window
{
    private const int GwlExStyle = -20;
    private const long WsExTransparent = 0x00000020L;
    private const long WsExToolWindow = 0x00000080L;
    private const long WsExNoActivate = 0x08000000L;

    private readonly Random _random = new();
    private readonly List<FishActor> _fishActors = [];

    private string _mode = "Aquarium";
    private string _backgroundPreset = AmbienceSceneCatalog.CoralReef;
    private int _population = 10;
    private double _speed = 1;
    private double _ambienceOpacity = 0.78;
    private double _backgroundOpacity = 0.58;
    private double _backgroundBrightness = 1;
    private double _backgroundMotion = 0.35;
    private double _creatureSize = 1;
    private double _bubbleIntensity = 0.55;
    private double _particleIntensity = 0.4;
    private double _glowIntensity = 0.45;

    private bool _backgroundEnabled = true;
    private bool _creaturesEnabled = true;
    private bool _effectsEnabled = true;
    private bool _randomDirection = true;
    private bool _schooling = true;
    private bool _performanceMode = true;
    private bool _allMonitors = true;
    private bool _overApps;
    private int _generation;

    public AmbienceWindow(AppSettings settings)
    {
        InitializeComponent();

        SourceInitialized += OnSourceInitialized;
        Loaded += OnLoaded;
        Closed += OnClosed;
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;

        ApplySettings(settings);
    }

    public void ApplySettings(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var oldMode = _mode;
        var oldPreset = _backgroundPreset;
        var oldPopulation = _population;
        var oldSpeed = _speed;
        var oldBrightness = _backgroundBrightness;
        var oldMotion = _backgroundMotion;
        var oldCreatureSize = _creatureSize;
        var oldBubbleIntensity = _bubbleIntensity;
        var oldParticleIntensity = _particleIntensity;
        var oldGlowIntensity = _glowIntensity;
        var oldBackgroundEnabled = _backgroundEnabled;
        var oldCreaturesEnabled = _creaturesEnabled;
        var oldEffectsEnabled = _effectsEnabled;
        var oldRandomDirection = _randomDirection;
        var oldSchooling = _schooling;
        var oldPerformanceMode = _performanceMode;
        var oldAllMonitors = _allMonitors;

        _mode = NormalizeMode(settings.AmbienceMode);
        _backgroundPreset = AmbienceSceneCatalog.NormalizeName(settings.AmbienceBackgroundPreset);
        _population = Math.Clamp(settings.AmbiencePopulation, 2, 30);
        _speed = Math.Clamp(settings.AmbienceSpeed, 0.35, 2.5);
        _ambienceOpacity = Math.Clamp(settings.AmbienceOpacity, 0.15, 1);
        _backgroundOpacity = Math.Clamp(settings.AmbienceBackgroundOpacity, 0, 1);
        _backgroundBrightness = Math.Clamp(settings.AmbienceBackgroundBrightness, 0.4, 1.6);
        _backgroundMotion = Math.Clamp(settings.AmbienceBackgroundMotion, 0, 1);
        _creatureSize = Math.Clamp(settings.AmbienceCreatureSize, 0.6, 1.6);
        _bubbleIntensity = Math.Clamp(settings.AmbienceBubbleIntensity, 0, 1);
        _particleIntensity = Math.Clamp(settings.AmbienceParticleIntensity, 0, 1);
        _glowIntensity = Math.Clamp(settings.AmbienceGlowIntensity, 0, 1);

        _backgroundEnabled = settings.AmbienceBackgroundEnabled;
        _creaturesEnabled = settings.AmbienceCreaturesEnabled;
        _effectsEnabled = settings.AmbienceEffectsEnabled;
        _randomDirection = settings.AmbienceRandomDirection;
        _schooling = settings.AmbienceSchooling;
        _performanceMode = settings.AmbiencePerformanceMode;
        _allMonitors = settings.AmbienceAllMonitors;
        _overApps = settings.AmbienceOverApps;

        Topmost = _overApps;
        BackgroundLayer.Opacity = _backgroundOpacity;
        CreatureLayer.Opacity = _ambienceOpacity;
        EffectLayer.Opacity = _ambienceOpacity;

        if (!IsLoaded)
        {
            return;
        }

        if (oldAllMonitors != _allMonitors)
        {
            UpdateBounds();
            RebuildAll();
            return;
        }

        var backgroundChanged =
            !string.Equals(oldPreset, _backgroundPreset, StringComparison.Ordinal) ||
            oldBrightness != _backgroundBrightness ||
            oldMotion != _backgroundMotion ||
            oldBackgroundEnabled != _backgroundEnabled ||
            oldPerformanceMode != _performanceMode;

        var creatureChanged =
            !string.Equals(oldMode, _mode, StringComparison.Ordinal) ||
            oldPopulation != _population ||
            oldSpeed != _speed ||
            oldCreatureSize != _creatureSize ||
            oldCreaturesEnabled != _creaturesEnabled ||
            oldRandomDirection != _randomDirection ||
            oldSchooling != _schooling ||
            oldPerformanceMode != _performanceMode;

        var effectsChanged =
            !string.Equals(oldPreset, _backgroundPreset, StringComparison.Ordinal) ||
            oldSpeed != _speed ||
            oldBubbleIntensity != _bubbleIntensity ||
            oldParticleIntensity != _particleIntensity ||
            oldGlowIntensity != _glowIntensity ||
            oldEffectsEnabled != _effectsEnabled ||
            oldPerformanceMode != _performanceMode;

        if (backgroundChanged)
        {
            RebuildBackground();
        }

        if (creatureChanged)
        {
            RebuildCreatures();
        }

        if (effectsChanged)
        {
            RebuildEffects();
        }
    }

    public void CloseForService() => Close();

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
            // WPF IsHitTestVisible=false still keeps the ambience non-interactive.
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateBounds();
        RebuildAll();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        StopFishAnimations();
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        BackgroundLayer.Children.Clear();
        CreatureLayer.Children.Clear();
        EffectLayer.Children.Clear();
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            UpdateBounds();
            RebuildAll();
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

    private void RebuildAll()
    {
        RebuildBackground();
        RebuildCreatures();
        RebuildEffects();
    }

    private void RebuildBackground()
    {
        BackgroundLayer.Children.Clear();
        BackgroundLayer.Opacity = _backgroundOpacity;

        if (!_backgroundEnabled || Width <= 1 || Height <= 1)
        {
            return;
        }

        var preset = AmbienceSceneCatalog.Get(_backgroundPreset);
        var top = AdjustBrightness(preset.TopColor, _backgroundBrightness);
        var bottom = AdjustBrightness(preset.BottomColor, _backgroundBrightness);

        var backdrop = new Rectangle
        {
            Width = Width,
            Height = Height,
            Fill = new LinearGradientBrush(
                top,
                bottom,
                new Point(0.5, 0),
                new Point(0.5, 1))
        };
        BackgroundLayer.Children.Add(backdrop);

        switch (preset.Name)
        {
            case AmbienceSceneCatalog.CoralReef:
                BuildCoralReefBackground(preset);
                break;
            case AmbienceSceneCatalog.DeepOcean:
                BuildDeepOceanBackground(preset);
                break;
            case AmbienceSceneCatalog.KelpForest:
                BuildKelpForestBackground(preset);
                break;
            case AmbienceSceneCatalog.FirefliesNight:
                BuildFirefliesNightBackground(preset);
                break;
            case AmbienceSceneCatalog.Space:
                BuildSpaceBackground(preset);
                break;
            default:
                BuildMinimalGradientBackground(preset);
                break;
        }
    }

    private void BuildCoralReefBackground(AmbienceScenePreset preset)
    {
        AddWaterLightRays(5, 0.16 + _backgroundMotion * 0.08);

        var seabed = new Rectangle
        {
            Width = Width,
            Height = Math.Max(90, Height * 0.13),
            Fill = new LinearGradientBrush(
                Color.FromArgb(235, 22, 61, 67),
                Color.FromArgb(245, 9, 33, 47),
                new Point(0, 0),
                new Point(0, 1))
        };
        Canvas.SetTop(seabed, Height - seabed.Height);
        BackgroundLayer.Children.Add(seabed);

        var coralCount = _performanceMode ? 7 : 12;
        for (var index = 0; index < coralCount; index++)
        {
            var coral = CreateCoral(
                index % 2 == 0 ? preset.AccentColor : preset.SecondaryAccent,
                0.7 + _random.NextDouble() * 0.8);

            Canvas.SetLeft(coral, 24 + index * Math.Max(70, Width / coralCount));
            Canvas.SetTop(coral, Height - 78 - coral.Height);
            BackgroundLayer.Children.Add(coral);
        }

        AddRocks(_performanceMode ? 6 : 10);
    }

    private void BuildDeepOceanBackground(AmbienceScenePreset preset)
    {
        AddWaterLightRays(4, 0.08 + _backgroundMotion * 0.06);
        AddRocks(_performanceMode ? 3 : 6);

        if (_backgroundMotion <= 0)
        {
            return;
        }

        var glow = new Ellipse
        {
            Width = Math.Max(320, Width * 0.36),
            Height = Math.Max(180, Height * 0.22),
            Fill = new RadialGradientBrush(
                Color.FromArgb(52, preset.AccentColor.R, preset.AccentColor.G, preset.AccentColor.B),
                Colors.Transparent),
            IsHitTestVisible = false
        };
        Canvas.SetLeft(glow, Width * 0.12);
        Canvas.SetTop(glow, Height * 0.18);
        BackgroundLayer.Children.Add(glow);

        var drift = new DoubleAnimation
        {
            From = -30 * _backgroundMotion,
            To = 40 * _backgroundMotion,
            Duration = TimeSpan.FromSeconds(18),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };
        glow.BeginAnimation(Canvas.LeftProperty, drift);
    }

    private void BuildKelpForestBackground(AmbienceScenePreset preset)
    {
        AddWaterLightRays(4, 0.1 + _backgroundMotion * 0.05);

        var bed = new Rectangle
        {
            Width = Width,
            Height = Math.Max(70, Height * 0.1),
            Fill = new SolidColorBrush(Color.FromArgb(235, 4, 35, 38))
        };
        Canvas.SetTop(bed, Height - bed.Height);
        BackgroundLayer.Children.Add(bed);

        var kelpCount = _performanceMode ? 9 : 16;
        for (var index = 0; index < kelpCount; index++)
        {
            var kelp = CreateKelp(
                index % 2 == 0 ? preset.AccentColor : preset.SecondaryAccent,
                120 + _random.NextDouble() * Math.Max(100, Height * 0.35));

            Canvas.SetLeft(kelp, index * Math.Max(55, Width / kelpCount));
            Canvas.SetTop(kelp, Height - bed.Height - kelp.Height + 10);
            BackgroundLayer.Children.Add(kelp);

            if (_backgroundMotion > 0.02)
            {
                var rotate = new RotateTransform(0, kelp.Width / 2, kelp.Height);
                kelp.RenderTransform = rotate;

                rotate.BeginAnimation(
                    RotateTransform.AngleProperty,
                    new DoubleAnimation
                    {
                        From = -1.8 - _backgroundMotion * 2.2,
                        To = 1.8 + _backgroundMotion * 2.2,
                        Duration = TimeSpan.FromSeconds(4 + _random.NextDouble() * 3),
                        AutoReverse = true,
                        RepeatBehavior = RepeatBehavior.Forever,
                        EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
                    });
            }
        }
    }

    private void BuildFirefliesNightBackground(AmbienceScenePreset preset)
    {
        var hillBack = new Ellipse
        {
            Width = Width * 1.15,
            Height = Math.Max(180, Height * 0.34),
            Fill = new SolidColorBrush(Color.FromArgb(235, 18, 54, 55))
        };
        Canvas.SetLeft(hillBack, -Width * 0.08);
        Canvas.SetTop(hillBack, Height * 0.68);
        BackgroundLayer.Children.Add(hillBack);

        var hillFront = new Ellipse
        {
            Width = Width * 1.28,
            Height = Math.Max(170, Height * 0.29),
            Fill = new SolidColorBrush(Color.FromArgb(248, 7, 31, 36))
        };
        Canvas.SetLeft(hillFront, -Width * 0.12);
        Canvas.SetTop(hillFront, Height * 0.77);
        BackgroundLayer.Children.Add(hillFront);

        AddStars(_performanceMode ? 18 : 36, subtle: true);
    }

    private void BuildMinimalGradientBackground(AmbienceScenePreset preset)
    {
        if (_backgroundMotion <= 0.02)
        {
            return;
        }

        var orb = new Ellipse
        {
            Width = Math.Max(260, Width * 0.28),
            Height = Math.Max(260, Width * 0.28),
            Fill = new RadialGradientBrush(
                Color.FromArgb(50, preset.AccentColor.R, preset.AccentColor.G, preset.AccentColor.B),
                Colors.Transparent)
        };
        Canvas.SetLeft(orb, Width * 0.62);
        Canvas.SetTop(orb, Height * 0.13);
        BackgroundLayer.Children.Add(orb);

        orb.BeginAnimation(
            Canvas.TopProperty,
            new DoubleAnimation
            {
                From = Height * 0.11,
                To = Height * 0.19,
                Duration = TimeSpan.FromSeconds(12),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            });
    }

    private void BuildSpaceBackground(AmbienceScenePreset preset)
    {
        AddStars(_performanceMode ? 42 : 85, subtle: false);

        var nebula = new Ellipse
        {
            Width = Math.Max(420, Width * 0.48),
            Height = Math.Max(250, Height * 0.31),
            Fill = new RadialGradientBrush(
                Color.FromArgb(68, preset.AccentColor.R, preset.AccentColor.G, preset.AccentColor.B),
                Colors.Transparent),
            Opacity = 0.8
        };
        Canvas.SetLeft(nebula, Width * 0.1);
        Canvas.SetTop(nebula, Height * 0.16);
        BackgroundLayer.Children.Add(nebula);

        var nebula2 = new Ellipse
        {
            Width = Math.Max(340, Width * 0.36),
            Height = Math.Max(220, Height * 0.27),
            Fill = new RadialGradientBrush(
                Color.FromArgb(48, preset.SecondaryAccent.R, preset.SecondaryAccent.G, preset.SecondaryAccent.B),
                Colors.Transparent),
            Opacity = 0.7
        };
        Canvas.SetLeft(nebula2, Width * 0.57);
        Canvas.SetTop(nebula2, Height * 0.48);
        BackgroundLayer.Children.Add(nebula2);
    }

    private void RebuildCreatures()
    {
        StopFishAnimations();
        CreatureLayer.Children.Clear();
        CreatureLayer.Opacity = _ambienceOpacity;
        _generation++;

        if (!_creaturesEnabled || Width <= 1 || Height <= 1)
        {
            return;
        }

        switch (_mode)
        {
            case "Fireflies":
                BuildFireflyCreatures();
                break;
            case "Desktop Pet":
                BuildDesktopPet();
                break;
            default:
                BuildAnimatedFish();
                break;
        }
    }

    private void BuildAnimatedFish()
    {
        var count = _performanceMode
            ? Math.Min(_population, 12)
            : Math.Min(_population, 24);

        var schoolDirection = _random.Next(0, 2) == 0;
        var schoolBands = new[]
        {
            Height * 0.24,
            Height * 0.42,
            Height * 0.61,
            Height * 0.76
        };

        for (var index = 0; index < count; index++)
        {
            var species = RandomSpecies();
            var traits = GetFishTraits(species);
            var scale = traits.Scale *
                        _creatureSize *
                        (0.82 + _random.NextDouble() * 0.38);

            var directionRight = _randomDirection
                ? (_schooling
                    ? (index % 4 == 0 ? !schoolDirection : schoolDirection)
                    : _random.Next(0, 2) == 0)
                : true;

            var visual = CreateFishVisual(species, traits, scale, directionRight);
            var baseY = _schooling
                ? schoolBands[index % schoolBands.Length] + (_random.NextDouble() - 0.5) * 52
                : 45 + _random.NextDouble() * Math.Max(80, Height - 150);

            var startX = _random.NextDouble() * Math.Max(100, Width - 130);
            Canvas.SetLeft(visual, startX);
            Canvas.SetTop(visual, Math.Clamp(baseY, 24, Math.Max(24, Height - 110)));
            CreatureLayer.Children.Add(visual);

            var transforms = (TransformGroup)visual.RenderTransform;
            var sway = (TranslateTransform)transforms.Children[1];

            sway.BeginAnimation(
                TranslateTransform.YProperty,
                new DoubleAnimation
                {
                    From = -6 - _random.NextDouble() * 8,
                    To = 6 + _random.NextDouble() * 8,
                    Duration = TimeSpan.FromSeconds(2.8 + _random.NextDouble() * 3.6),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                    EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
                });

            var actor = new FishActor(
                visual,
                sway,
                traits.Speed * (0.84 + _random.NextDouble() * 0.32),
                directionRight,
                _generation);

            _fishActors.Add(actor);
            StartFishTransit(actor, initial: true);
        }
    }

    private FrameworkElement CreateFishVisual(
        FishSpecies species,
        FishTraits traits,
        double scale,
        bool directionRight)
    {
        var root = new Canvas
        {
            Width = 122,
            Height = 72,
            IsHitTestVisible = false,
            CacheMode = new BitmapCache()
        };

        if (!_performanceMode)
        {
            root.Effect = new DropShadowEffect
            {
                Color = Color.FromArgb(155, 0, 18, 28),
                BlurRadius = 8,
                ShadowDepth = 2,
                Opacity = 0.32
            };
        }

        var tail = new Path
        {
            Data = Geometry.Parse("M 28,36 C 17,28 9,15 2,12 C 7,28 7,44 2,60 C 10,57 18,45 28,36 Z"),
            Fill = new LinearGradientBrush(
                traits.Accent,
                Darken(traits.Accent, 0.62),
                new Point(0, 0.5),
                new Point(1, 0.5)),
            RenderTransformOrigin = new Point(0.85, 0.5)
        };
        var tailRotate = new RotateTransform();
        tail.RenderTransform = tailRotate;

        var bodyBrush = new LinearGradientBrush
        {
            StartPoint = new Point(0.2, 0),
            EndPoint = new Point(0.8, 1)
        };
        bodyBrush.GradientStops.Add(new GradientStop(Lighten(traits.Body, 1.28), 0));
        bodyBrush.GradientStops.Add(new GradientStop(traits.Body, 0.48));
        bodyBrush.GradientStops.Add(new GradientStop(Darken(traits.Body, 0.68), 1));

        var body = new Path
        {
            Data = Geometry.Parse(
                species == FishSpecies.Angelfish
                    ? "M 24,36 C 39,7 77,4 105,34 C 79,68 42,68 24,36 Z"
                    : "M 23,36 C 37,14 75,10 107,35 C 78,59 39,60 23,36 Z"),
            Fill = bodyBrush,
            Stroke = new SolidColorBrush(Color.FromArgb(95, 255, 255, 255)),
            StrokeThickness = 1.1
        };

        var dorsal = new Path
        {
            Data = Geometry.Parse(
                species == FishSpecies.Angelfish
                    ? "M 44,20 C 54,1 72,0 83,20 Z"
                    : "M 43,19 C 56,7 70,8 80,20 Z"),
            Fill = new SolidColorBrush(Color.FromArgb(185, traits.Accent.R, traits.Accent.G, traits.Accent.B))
        };

        var lowerFin = new Path
        {
            Data = Geometry.Parse("M 49,51 C 58,66 72,66 79,50 Z"),
            Fill = new SolidColorBrush(Color.FromArgb(155, traits.Accent.R, traits.Accent.G, traits.Accent.B))
        };

        root.Children.Add(tail);
        root.Children.Add(dorsal);
        root.Children.Add(lowerFin);
        root.Children.Add(body);

        switch (species)
        {
            case FishSpecies.Neon:
            {
                var stripe = new Path
                {
                    Data = Geometry.Parse("M 34,34 C 51,27 79,26 99,34 C 79,38 54,40 34,37 Z"),
                    Fill = new SolidColorBrush(Color.FromRgb(89, 242, 255)),
                    Opacity = 0.95
                };
                root.Children.Add(stripe);

                var redStripe = new Path
                {
                    Data = Geometry.Parse("M 55,40 C 72,38 88,37 101,35 C 89,47 71,49 55,45 Z"),
                    Fill = new SolidColorBrush(Color.FromRgb(247, 69, 91)),
                    Opacity = 0.85
                };
                root.Children.Add(redStripe);
                break;
            }

            case FishSpecies.Goldfish:
            {
                var cheek = new Ellipse
                {
                    Width = 14,
                    Height = 10,
                    Fill = new RadialGradientBrush(
                        Color.FromArgb(180, 255, 216, 111),
                        Colors.Transparent)
                };
                Canvas.SetLeft(cheek, 82);
                Canvas.SetTop(cheek, 33);
                root.Children.Add(cheek);
                break;
            }

            case FishSpecies.Angelfish:
            {
                for (var stripeIndex = 0; stripeIndex < 3; stripeIndex++)
                {
                    var stripe = new Rectangle
                    {
                        Width = 4,
                        Height = 32 + stripeIndex * 2,
                        RadiusX = 2,
                        RadiusY = 2,
                        Fill = new SolidColorBrush(Color.FromArgb(105, 24, 23, 44))
                    };
                    Canvas.SetLeft(stripe, 51 + stripeIndex * 15);
                    Canvas.SetTop(stripe, 19 - stripeIndex);
                    root.Children.Add(stripe);
                }
                break;
            }

            default:
            {
                var spot = new Ellipse
                {
                    Width = 12,
                    Height = 8,
                    Fill = new SolidColorBrush(Color.FromArgb(135, 255, 222, 122))
                };
                Canvas.SetLeft(spot, 67);
                Canvas.SetTop(spot, 27);
                root.Children.Add(spot);
                break;
            }
        }

        var eyeWhite = new Ellipse
        {
            Width = 9,
            Height = 9,
            Fill = Brushes.White,
            Stroke = new SolidColorBrush(Color.FromArgb(100, 0, 0, 0)),
            StrokeThickness = 0.8
        };
        Canvas.SetLeft(eyeWhite, 91);
        Canvas.SetTop(eyeWhite, 28);
        root.Children.Add(eyeWhite);

        var pupil = new Ellipse
        {
            Width = 4.2,
            Height = 4.2,
            Fill = new SolidColorBrush(Color.FromRgb(8, 18, 28))
        };
        Canvas.SetLeft(pupil, 95);
        Canvas.SetTop(pupil, 31);
        root.Children.Add(pupil);

        var highlight = new Ellipse
        {
            Width = 1.6,
            Height = 1.6,
            Fill = Brushes.White
        };
        Canvas.SetLeft(highlight, 96);
        Canvas.SetTop(highlight, 31.4);
        root.Children.Add(highlight);

        tailRotate.BeginAnimation(
            RotateTransform.AngleProperty,
            new DoubleAnimation
            {
                From = -7,
                To = 7,
                Duration = TimeSpan.FromMilliseconds(230 + _random.Next(0, 140)),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            });

        var transforms = new TransformGroup();
        transforms.Children.Add(new ScaleTransform(
            directionRight ? scale : -scale,
            scale,
            61,
            36));
        transforms.Children.Add(new TranslateTransform());
        root.RenderTransform = transforms;

        return root;
    }

    private void StartFishTransit(FishActor actor, bool initial)
    {
        if (!IsLoaded ||
            actor.Generation != _generation ||
            !CreatureLayer.Children.Contains(actor.Visual))
        {
            return;
        }

        var current = Canvas.GetLeft(actor.Visual);
        if (double.IsNaN(current))
        {
            current = actor.DirectionRight ? -150 : Width + 150;
        }

        if (!initial)
        {
            actor.DirectionRight = _randomDirection
                ? (_random.NextDouble() > 0.22
                    ? actor.DirectionRight
                    : !actor.DirectionRight)
                : true;

            var transforms = (TransformGroup)actor.Visual.RenderTransform;
            var scale = (ScaleTransform)transforms.Children[0];
            scale.ScaleX = Math.Abs(scale.ScaleX) * (actor.DirectionRight ? 1 : -1);

            current = actor.DirectionRight ? -150 : Width + 150;
            actor.Visual.BeginAnimation(Canvas.LeftProperty, null);
            Canvas.SetLeft(actor.Visual, current);
        }

        var target = actor.DirectionRight ? Width + 150 : -150;
        var distance = Math.Abs(target - current);
        var pixelsPerSecond = actor.Speed * _speed;
        var seconds = Math.Clamp(distance / Math.Max(20, pixelsPerSecond), 8, 70);

        var animation = new DoubleAnimation
        {
            From = current,
            To = target,
            Duration = TimeSpan.FromSeconds(seconds),
            FillBehavior = FillBehavior.Stop,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };

        animation.Completed += (_, _) =>
        {
            if (actor.Generation != _generation)
            {
                return;
            }

            actor.Visual.BeginAnimation(Canvas.LeftProperty, null);
            Canvas.SetLeft(actor.Visual, target);
            StartFishTransit(actor, initial: false);
        };

        actor.Visual.BeginAnimation(Canvas.LeftProperty, animation);
    }

    private void StopFishAnimations()
    {
        foreach (var actor in _fishActors)
        {
            actor.Visual.BeginAnimation(Canvas.LeftProperty, null);
            actor.Sway.BeginAnimation(TranslateTransform.YProperty, null);
        }

        _fishActors.Clear();
    }

    private void BuildFireflyCreatures()
    {
        var count = _performanceMode
            ? Math.Min(_population, 14)
            : Math.Min(_population, 28);

        for (var index = 0; index < count; index++)
        {
            var size = 5 + _random.NextDouble() * 6;
            var glow = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = new SolidColorBrush(Color.FromRgb(255, 236, 127)),
                Effect = _performanceMode
                    ? null
                    : new DropShadowEffect
                    {
                        Color = Color.FromRgb(255, 212, 77),
                        BlurRadius = 15,
                        ShadowDepth = 0,
                        Opacity = 0.8
                    }
            };

            var x = _random.NextDouble() * Math.Max(100, Width - 20);
            var y = 40 + _random.NextDouble() * Math.Max(80, Height - 120);
            Canvas.SetLeft(glow, x);
            Canvas.SetTop(glow, y);
            CreatureLayer.Children.Add(glow);

            glow.BeginAnimation(
                Canvas.LeftProperty,
                new DoubleAnimation
                {
                    From = Math.Max(-10, x - 32),
                    To = Math.Min(Width, x + 32),
                    Duration = TimeSpan.FromSeconds(5 + _random.NextDouble() * 8),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                    EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
                });

            glow.BeginAnimation(
                Canvas.TopProperty,
                new DoubleAnimation
                {
                    From = Math.Max(5, y - 26),
                    To = Math.Min(Height - 20, y + 26),
                    Duration = TimeSpan.FromSeconds(4 + _random.NextDouble() * 7),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                    EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
                });

            glow.BeginAnimation(
                OpacityProperty,
                new DoubleAnimation
                {
                    From = 0.2,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(0.8 + _random.NextDouble() * 2.2),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever
                });
        }
    }

    private void BuildDesktopPet()
    {
        var cat = CreateCatVisual();
        Canvas.SetLeft(cat, 24);
        Canvas.SetTop(cat, Math.Max(20, Height - 112));
        CreatureLayer.Children.Add(cat);

        var width = Math.Max(40, Width - 150);
        cat.BeginAnimation(
            Canvas.LeftProperty,
            new DoubleAnimation
            {
                From = 20,
                To = width,
                Duration = TimeSpan.FromSeconds(
                    Math.Clamp(34 / _speed, 15, 70)),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            });

        var translate = new TranslateTransform();
        cat.RenderTransform = translate;
        translate.BeginAnimation(
            TranslateTransform.YProperty,
            new DoubleAnimation
            {
                From = -1,
                To = 2,
                Duration = TimeSpan.FromMilliseconds(430),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            });
    }

    private FrameworkElement CreateCatVisual()
    {
        var root = new Canvas
        {
            Width = 118,
            Height = 82,
            CacheMode = new BitmapCache()
        };

        var fur = new LinearGradientBrush(
            Color.FromRgb(203, 211, 224),
            Color.FromRgb(122, 132, 151),
            new Point(0, 0),
            new Point(1, 1));
        var dark = new SolidColorBrush(Color.FromRgb(44, 50, 63));
        var innerEar = new SolidColorBrush(Color.FromRgb(226, 151, 161));

        var tail = new Path
        {
            Data = Geometry.Parse("M 28,58 C 11,63 5,50 12,37 C 16,29 12,22 7,20"),
            Stroke = fur,
            StrokeThickness = 10,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            RenderTransformOrigin = new Point(0.25, 0.7)
        };
        var tailRotate = new RotateTransform();
        tail.RenderTransform = tailRotate;

        var body = new Ellipse
        {
            Width = 65,
            Height = 37,
            Fill = fur
        };
        Canvas.SetLeft(body, 27);
        Canvas.SetTop(body, 38);

        var chest = new Ellipse
        {
            Width = 24,
            Height = 28,
            Fill = new SolidColorBrush(Color.FromArgb(120, 245, 247, 250))
        };
        Canvas.SetLeft(chest, 68);
        Canvas.SetTop(chest, 45);

        var head = new Ellipse
        {
            Width = 39,
            Height = 36,
            Fill = fur
        };
        Canvas.SetLeft(head, 76);
        Canvas.SetTop(head, 25);

        var earLeft = new Polygon
        {
            Points = new PointCollection
            {
                new(79, 31),
                new(84, 9),
                new(93, 29)
            },
            Fill = fur
        };
        var earRight = new Polygon
        {
            Points = new PointCollection
            {
                new(96, 29),
                new(108, 10),
                new(111, 35)
            },
            Fill = fur
        };
        var earInner = new Polygon
        {
            Points = new PointCollection
            {
                new(84, 27),
                new(86, 16),
                new(91, 28)
            },
            Fill = innerEar
        };

        var eye1 = new Ellipse { Width = 4.4, Height = 6.2, Fill = dark };
        Canvas.SetLeft(eye1, 88);
        Canvas.SetTop(eye1, 39);

        var eye2 = new Ellipse { Width = 4.4, Height = 6.2, Fill = dark };
        Canvas.SetLeft(eye2, 101);
        Canvas.SetTop(eye2, 39);

        var nose = new Polygon
        {
            Points = new PointCollection
            {
                new(96, 47),
                new(101, 47),
                new(98.5, 51)
            },
            Fill = innerEar
        };

        root.Children.Add(tail);
        root.Children.Add(body);
        root.Children.Add(chest);
        root.Children.Add(earLeft);
        root.Children.Add(earRight);
        root.Children.Add(earInner);
        root.Children.Add(head);
        root.Children.Add(eye1);
        root.Children.Add(eye2);
        root.Children.Add(nose);

        tailRotate.BeginAnimation(
            RotateTransform.AngleProperty,
            new DoubleAnimation
            {
                From = -7,
                To = 8,
                Duration = TimeSpan.FromSeconds(1.1),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            });

        return root;
    }

    private void RebuildEffects()
    {
        EffectLayer.Children.Clear();
        EffectLayer.Opacity = _ambienceOpacity;

        if (!_effectsEnabled || Width <= 1 || Height <= 1)
        {
            return;
        }

        var preset = AmbienceSceneCatalog.Get(_backgroundPreset);

        if (preset.OceanScene || _mode == "Aquarium")
        {
            AddAnimatedBubbles();
        }

        if (_backgroundPreset == AmbienceSceneCatalog.FirefliesNight)
        {
            AddAmbientFireflies();
        }
        else
        {
            AddFloatingParticles();
        }
    }

    private void AddAnimatedBubbles()
    {
        var baseCount = _performanceMode ? 12 : 24;
        var count = (int)Math.Round(baseCount * _bubbleIntensity);

        for (var index = 0; index < count; index++)
        {
            var size = 4 + _random.NextDouble() * 11;
            var bubble = new Ellipse
            {
                Width = size,
                Height = size,
                Stroke = new SolidColorBrush(
                    Color.FromArgb(
                        (byte)(70 + 90 * _glowIntensity),
                        210,
                        244,
                        255)),
                StrokeThickness = 1,
                Fill = new SolidColorBrush(Color.FromArgb(10, 255, 255, 255))
            };

            var x = _random.NextDouble() * Math.Max(50, Width - 20);
            var startY = Height + _random.NextDouble() * Height * 0.35;
            Canvas.SetLeft(bubble, x);
            Canvas.SetTop(bubble, startY);
            EffectLayer.Children.Add(bubble);

            bubble.BeginAnimation(
                Canvas.TopProperty,
                new DoubleAnimation
                {
                    From = startY,
                    To = -25,
                    Duration = TimeSpan.FromSeconds(
                        (10 + _random.NextDouble() * 20) / _speed),
                    RepeatBehavior = RepeatBehavior.Forever
                });

            var translate = new TranslateTransform();
            bubble.RenderTransform = translate;
            translate.BeginAnimation(
                TranslateTransform.XProperty,
                new DoubleAnimation
                {
                    From = -8 - _random.NextDouble() * 10,
                    To = 8 + _random.NextDouble() * 10,
                    Duration = TimeSpan.FromSeconds(2.5 + _random.NextDouble() * 4),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                    EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
                });
        }
    }

    private void AddFloatingParticles()
    {
        var baseCount = _performanceMode ? 18 : 34;
        var count = (int)Math.Round(baseCount * _particleIntensity);

        for (var index = 0; index < count; index++)
        {
            var size = 1.4 + _random.NextDouble() * 3.2;
            var particle = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = new SolidColorBrush(
                    Color.FromArgb(
                        (byte)(38 + 80 * _glowIntensity),
                        220,
                        232,
                        255))
            };

            var x = _random.NextDouble() * Width;
            var y = _random.NextDouble() * Height;
            Canvas.SetLeft(particle, x);
            Canvas.SetTop(particle, y);
            EffectLayer.Children.Add(particle);

            particle.BeginAnimation(
                OpacityProperty,
                new DoubleAnimation
                {
                    From = 0.15,
                    To = 0.75,
                    Duration = TimeSpan.FromSeconds(2 + _random.NextDouble() * 5),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever
                });

            particle.BeginAnimation(
                Canvas.TopProperty,
                new DoubleAnimation
                {
                    From = y,
                    To = Math.Max(-10, y - 35 - _random.NextDouble() * 90),
                    Duration = TimeSpan.FromSeconds(8 + _random.NextDouble() * 15),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                    EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
                });
        }
    }

    private void AddAmbientFireflies()
    {
        var baseCount = _performanceMode ? 14 : 28;
        var count = (int)Math.Round(baseCount * Math.Max(0.2, _particleIntensity));

        for (var index = 0; index < count; index++)
        {
            var size = 3.5 + _random.NextDouble() * 4.5;
            var firefly = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = new SolidColorBrush(Color.FromRgb(255, 229, 113)),
                Effect = _performanceMode
                    ? null
                    : new DropShadowEffect
                    {
                        Color = Color.FromRgb(255, 211, 78),
                        BlurRadius = 10 + 10 * _glowIntensity,
                        ShadowDepth = 0,
                        Opacity = 0.8
                    }
            };

            var x = _random.NextDouble() * Width;
            var y = Height * 0.25 + _random.NextDouble() * Height * 0.62;
            Canvas.SetLeft(firefly, x);
            Canvas.SetTop(firefly, y);
            EffectLayer.Children.Add(firefly);

            firefly.BeginAnimation(
                Canvas.LeftProperty,
                new DoubleAnimation
                {
                    From = Math.Max(-5, x - 22),
                    To = Math.Min(Width, x + 22),
                    Duration = TimeSpan.FromSeconds(4 + _random.NextDouble() * 8),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                    EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
                });

            firefly.BeginAnimation(
                OpacityProperty,
                new DoubleAnimation
                {
                    From = 0.12,
                    To = 0.92,
                    Duration = TimeSpan.FromSeconds(0.8 + _random.NextDouble() * 2.6),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever
                });
        }
    }

    private void AddWaterLightRays(int count, double opacity)
    {
        for (var index = 0; index < count; index++)
        {
            var x = Width * (0.05 + index / (double)Math.Max(1, count) * 0.9);
            var width = Math.Max(90, Width * 0.08);

            var ray = new Polygon
            {
                Points = new PointCollection
                {
                    new(x, -10),
                    new(x + width * 0.35, -10),
                    new(x + width, Height * 0.68),
                    new(x - width * 0.6, Height * 0.68)
                },
                Fill = new LinearGradientBrush(
                    Color.FromArgb((byte)(255 * opacity), 205, 244, 255),
                    Colors.Transparent,
                    new Point(0.5, 0),
                    new Point(0.5, 1))
            };
            BackgroundLayer.Children.Add(ray);

            if (_backgroundMotion > 0.03)
            {
                var translate = new TranslateTransform();
                ray.RenderTransform = translate;
                translate.BeginAnimation(
                    TranslateTransform.XProperty,
                    new DoubleAnimation
                    {
                        From = -18 * _backgroundMotion,
                        To = 18 * _backgroundMotion,
                        Duration = TimeSpan.FromSeconds(8 + index * 1.4),
                        AutoReverse = true,
                        RepeatBehavior = RepeatBehavior.Forever,
                        EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
                    });
            }
        }
    }

    private Canvas CreateCoral(Color color, double scale)
    {
        var canvas = new Canvas
        {
            Width = 70 * scale,
            Height = 95 * scale,
            Opacity = 0.88
        };

        var brush = new LinearGradientBrush(
            Lighten(color, 1.15),
            Darken(color, 0.68),
            new Point(0, 0),
            new Point(0, 1));

        AddCoralBranch(canvas, brush, 31, 22, 9, 70, 0);
        AddCoralBranch(canvas, brush, 20, 42, 7, 42, -20);
        AddCoralBranch(canvas, brush, 45, 37, 7, 49, 21);
        AddCoralBranch(canvas, brush, 13, 57, 6, 29, -34);
        AddCoralBranch(canvas, brush, 52, 57, 6, 31, 34);

        return canvas;
    }

    private static void AddCoralBranch(
        Canvas canvas,
        Brush brush,
        double left,
        double top,
        double width,
        double height,
        double angle)
    {
        var branch = new Rectangle
        {
            Width = width,
            Height = height,
            RadiusX = width / 2,
            RadiusY = width / 2,
            Fill = brush,
            RenderTransform = new RotateTransform(angle, width / 2, height)
        };
        Canvas.SetLeft(branch, left);
        Canvas.SetTop(branch, top);
        canvas.Children.Add(branch);
    }

    private Canvas CreateKelp(Color color, double height)
    {
        var canvas = new Canvas
        {
            Width = 48,
            Height = height,
            RenderTransformOrigin = new Point(0.5, 1)
        };

        var stem = new Path
        {
            Data = Geometry.Parse($"M 23,{height} C 14,{height * 0.72} 35,{height * 0.5} 23,0"),
            Stroke = new SolidColorBrush(Darken(color, 0.78)),
            StrokeThickness = 7,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };
        canvas.Children.Add(stem);

        for (var index = 0; index < 5; index++)
        {
            var leaf = new Ellipse
            {
                Width = 18,
                Height = 8,
                Fill = new SolidColorBrush(Color.FromArgb(205, color.R, color.G, color.B)),
                RenderTransform = new RotateTransform(index % 2 == 0 ? -28 : 28)
            };

            Canvas.SetLeft(leaf, index % 2 == 0 ? 4 : 25);
            Canvas.SetTop(leaf, height - 28 - index * Math.Max(18, height / 7));
            canvas.Children.Add(leaf);
        }

        return canvas;
    }

    private void AddRocks(int count)
    {
        for (var index = 0; index < count; index++)
        {
            var width = 55 + _random.NextDouble() * 90;
            var height = 28 + _random.NextDouble() * 45;
            var rock = new Ellipse
            {
                Width = width,
                Height = height,
                Fill = new LinearGradientBrush(
                    Color.FromArgb(220, 58, 78, 84),
                    Color.FromArgb(235, 19, 39, 48),
                    new Point(0, 0),
                    new Point(1, 1))
            };

            Canvas.SetLeft(rock, _random.NextDouble() * Math.Max(1, Width - width));
            Canvas.SetTop(rock, Height - height - _random.NextDouble() * 24);
            BackgroundLayer.Children.Add(rock);
        }
    }

    private void AddStars(int count, bool subtle)
    {
        for (var index = 0; index < count; index++)
        {
            var size = subtle
                ? 1 + _random.NextDouble() * 1.8
                : 1 + _random.NextDouble() * 2.8;

            var star = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = new SolidColorBrush(
                    subtle
                        ? Color.FromArgb(125, 220, 233, 238)
                        : Color.FromArgb(215, 235, 239, 255))
            };

            Canvas.SetLeft(star, _random.NextDouble() * Width);
            Canvas.SetTop(star, _random.NextDouble() * Height * 0.78);
            BackgroundLayer.Children.Add(star);

            if (!subtle && !_performanceMode && index % 4 == 0)
            {
                star.BeginAnimation(
                    OpacityProperty,
                    new DoubleAnimation
                    {
                        From = 0.25,
                        To = 1,
                        Duration = TimeSpan.FromSeconds(1 + _random.NextDouble() * 3),
                        AutoReverse = true,
                        RepeatBehavior = RepeatBehavior.Forever
                    });
            }
        }
    }

    private FishSpecies RandomSpecies()
    {
        var value = _random.NextDouble();
        if (value < 0.32) return FishSpecies.Guppy;
        if (value < 0.6) return FishSpecies.Neon;
        if (value < 0.84) return FishSpecies.Goldfish;
        return FishSpecies.Angelfish;
    }

    private static FishTraits GetFishTraits(FishSpecies species) =>
        species switch
        {
            FishSpecies.Neon => new(
                Color.FromRgb(40, 112, 192),
                Color.FromRgb(52, 219, 229),
                0.78,
                78),
            FishSpecies.Goldfish => new(
                Color.FromRgb(245, 122, 43),
                Color.FromRgb(255, 194, 78),
                1.04,
                56),
            FishSpecies.Angelfish => new(
                Color.FromRgb(153, 136, 207),
                Color.FromRgb(99, 84, 159),
                1.13,
                48),
            _ => new(
                Color.FromRgb(69, 175, 141),
                Color.FromRgb(255, 184, 86),
                0.88,
                68)
        };

    private static string NormalizeMode(string? value) =>
        value switch
        {
            "Fireflies" => "Fireflies",
            "Desktop Pet" => "Desktop Pet",
            _ => "Aquarium"
        };

    private static Color AdjustBrightness(Color color, double factor) =>
        Color.FromRgb(
            (byte)Math.Clamp((int)(color.R * factor), 0, 255),
            (byte)Math.Clamp((int)(color.G * factor), 0, 255),
            (byte)Math.Clamp((int)(color.B * factor), 0, 255));

    private static Color Lighten(Color color, double factor) =>
        AdjustBrightness(color, factor);

    private static Color Darken(Color color, double factor) =>
        AdjustBrightness(color, factor);

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

    private sealed class FishActor(
        FrameworkElement visual,
        TranslateTransform sway,
        double speed,
        bool directionRight,
        int generation)
    {
        public FrameworkElement Visual { get; } = visual;
        public TranslateTransform Sway { get; } = sway;
        public double Speed { get; } = speed;
        public bool DirectionRight { get; set; } = directionRight;
        public int Generation { get; } = generation;
    }

    private readonly record struct FishTraits(
        Color Body,
        Color Accent,
        double Scale,
        double Speed);
}
