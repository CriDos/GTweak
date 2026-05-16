using System;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using GTweak.Utilities.Animation;
using GTweak.Utilities.Controls;

namespace GTweak.Assets.UserControls
{
    public partial class DescriptionBlock
    {
        private CancellationTokenSource _scrollCts;

        internal string DefaultText
        {
            get => (string)GetValue(DefaultTextProperty);
            set => SetValue(DefaultTextProperty, value);
        }

        public static readonly DependencyProperty DefaultTextProperty =
            DependencyProperty.Register("DefaultText", typeof(string), typeof(DescriptionBlock), new UIPropertyMetadata(string.Empty));

        internal string LegendText
        {
            get => (string)GetValue(LegendTextProperty);
            set => SetValue(LegendTextProperty, value);
        }

        public static readonly DependencyProperty LegendTextProperty =
            DependencyProperty.Register("LegendText", typeof(string), typeof(DescriptionBlock), new UIPropertyMetadata(string.Empty, OnLegendTextChanged));

        internal string Text
        {
            get => FunctionDescription?.Text ?? string.Empty;
            set
            {
                string safeValue = value ?? string.Empty;

                _scrollCts?.Cancel();

                if (Scroller != null && FunctionDescription != null)
                {
                    UpdateFlowDirection();

                    Scroller.BeginAnimation(ScrollViewerBehavior.VerticalOffsetProperty, null);
                    Scroller.ScrollToVerticalOffset(0);
                    Scroller.UpdateLayout();

                    TypewriterAnimation.Create(safeValue, FunctionDescription, safeValue.Length <= 50 ? TimeSpan.FromMilliseconds(200) : safeValue.Length <= 200 ? TimeSpan.FromMilliseconds(400) : TimeSpan.FromMilliseconds(550));

                    _scrollCts = new CancellationTokenSource();
                    _ = StartAutoScrollAsync(safeValue, _scrollCts.Token);
                }
            }
        }

        public DescriptionBlock()
        {
            InitializeComponent();
            UpdateLegendVisibility();

            Loaded += delegate
            {
                App.LanguageChanged += OnLanguageChanged;
                UpdateLegendVisibility();

                if (FunctionDescription != null)
                {
                    UpdateFlowDirection();
                    TypewriterAnimation.Create(DefaultText, FunctionDescription, TimeSpan.FromMilliseconds(300));
                }
            };

            Unloaded += delegate
            {
                App.LanguageChanged -= OnLanguageChanged;
                _scrollCts?.Cancel();
            };
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            _scrollCts?.Cancel();
            UpdateLegendVisibility();
            if (FunctionDescription != null)
            {
                UpdateFlowDirection();
                TypewriterAnimation.Create(DefaultText, FunctionDescription, TimeSpan.Zero);
            }
        }

        private static void OnLegendTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DescriptionBlock descriptionBlock)
            {
                descriptionBlock.UpdateLegendVisibility();
            }
        }

        private void UpdateLegendVisibility()
        {
            if (ToggleStateLegendPanel != null)
            {
                ToggleStateLegendPanel.Visibility = string.IsNullOrWhiteSpace(LegendText) ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private void UpdateFlowDirection()
        {
            if (FunctionDescription != null && !DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                try
                {
                    FlowDirection flowDirection = CultureInfo.GetCultureInfo(SettingsEngine.Language).TextInfo.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
                    FunctionDescription.FlowDirection = flowDirection;
                    if (ToggleStateLegend != null)
                    {
                        ToggleStateLegend.FlowDirection = flowDirection;
                    }
                }
                catch (CultureNotFoundException)
                {
                    FunctionDescription.FlowDirection = FlowDirection.LeftToRight;
                    if (ToggleStateLegend != null)
                    {
                        ToggleStateLegend.FlowDirection = FlowDirection.LeftToRight;
                    }
                }
            }
        }

        private async Task StartAutoScrollAsync(string text, CancellationToken token)
        {
            await Task.Delay(2000, token);

            if (text != DefaultText && !token.IsCancellationRequested)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    if (FunctionDescription != null && Scroller != null)
                    {
                        FunctionDescription.UpdateLayout();
                        Scroller.UpdateLayout();
                        Scroller.ScrollToVerticalOffset(0);
                    }
                });

                if (Scroller != null && !token.IsCancellationRequested)
                {
                    double maxOffset = Scroller.ScrollableHeight;
                    if (maxOffset > 0)
                    {
                        double durationSeconds = Math.Max(2.0, maxOffset / 20);

                        await Dispatcher.InvokeAsync(() => { Scroller?.BeginAnimation(ScrollViewerBehavior.VerticalOffsetProperty, FactoryAnimation.CreateIn(0, maxOffset, durationSeconds, useCubicEase: true)); });

                        try { await Task.Delay(TimeSpan.FromSeconds(durationSeconds), token); }
                        catch (Exception ex) { ErrorLogging.LogDebug(ex); }
                    }
                }
            }
        }

        private static class ScrollViewerBehavior
        {
            internal static readonly DependencyProperty VerticalOffsetProperty =
                DependencyProperty.RegisterAttached("VerticalOffset", typeof(double), typeof(ScrollViewerBehavior), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnVerticalOffsetChanged));

            internal static double GetVerticalOffset(ScrollViewer viewer) => (double)(viewer?.GetValue(VerticalOffsetProperty) ?? 0.0);

            internal static void SetVerticalOffset(ScrollViewer viewer, double value) => viewer?.SetValue(VerticalOffsetProperty, value);

            private static void OnVerticalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
                if (d is ScrollViewer viewer && e.NewValue is double offset)
                {
                    viewer.ScrollToVerticalOffset(offset);
                }
            }
        }
    }
}
