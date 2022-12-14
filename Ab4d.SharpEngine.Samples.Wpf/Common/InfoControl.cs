using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Ab4d.SharpEngine.Samples.Wpf.Common
{
    public class InfoControl : Grid
    {
        private TextBlock _tooltipTextBlock;

        public static DependencyProperty InfoTextProperty = DependencyProperty.Register("InfoText", typeof(object), typeof(InfoControl),
                 new FrameworkPropertyMetadata(null, OnTextChanged));

        /// <summary>
        /// Text that will be shown as ToolTip.
        /// </summary>
        public object InfoText
        {
            get
            {
                return (string)GetValue(InfoTextProperty);
            }
            set
            {
                SetValue(InfoTextProperty, value);
            }
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInfoControl = (InfoControl)d;

            if (e.NewValue is string)
            {
                thisInfoControl._tooltipTextBlock.Text = ((string)e.NewValue).Replace("\\n", Environment.NewLine);
                thisInfoControl.ToolTip = thisInfoControl._tooltipTextBlock;
            }
            else
            {
                thisInfoControl.ToolTip = e.NewValue;
            }
        }



        public static DependencyProperty InfoWidthProperty = DependencyProperty.Register("InfoWidth", typeof(double), typeof(InfoControl),
                 new FrameworkPropertyMetadata(0.0, OnInfoWidthChanged));

        /// <summary>
        /// Width of the ToolTip TextBlock. Longer text will be automatically wrapped.
        /// Default value is 0 that does not limit the TextBlock width.
        /// </summary>
        public double InfoWidth
        {
            get
            {
                return (double)GetValue(InfoWidthProperty);
            }
            set
            {
                SetValue(InfoWidthProperty, value);
            }
        }

        private static void OnInfoWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInfoControl = (InfoControl)d;
            var newWidth = (double)e.NewValue;

            if (newWidth == 0)
                newWidth = double.NaN;

            thisInfoControl._tooltipTextBlock.Width = newWidth;
        }


        public static DependencyProperty ShowDurationProperty = DependencyProperty.Register("ShowDuration", typeof(int), typeof(InfoControl),
                 new FrameworkPropertyMetadata(120000, OnShowDurationChanged));


        /// <summary>
        /// Duration of showing ToolTip in milliseconds. Default value is 120000.
        /// </summary>
        public int ShowDuration
        {
            get
            {
                return (int)GetValue(ShowDurationProperty);
            }
            set
            {
                SetValue(ShowDurationProperty, value);
            }
        }

        private static void OnShowDurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInfoControl = (InfoControl)d;
            ToolTipService.SetShowDuration(thisInfoControl, (int)e.NewValue);
        }


        /// <summary>
        /// Gets the Ellipse that is used to show background circle.
        /// </summary>
        public Ellipse BackGroundEllipse { get; private set; }

        /// <summary>
        /// Gets the TextBlock that is used to show the question character.
        /// </summary>
        public TextBlock QuestionTextBlock { get; private set; }


        /// <summary>
        /// Gets or sets the fill brush for the Ellipse shape. Default value is Gray.
        /// </summary>
        public Brush EllipseFillBrush
        {
            get { return BackGroundEllipse.Fill; }
            set { BackGroundEllipse.Fill = value; }
        }

        /// <summary>
        /// Gets or sets the foreground brush for the question character. Default value is White.
        /// </summary>
        public Brush QuestionCharacterForeground
        {
            get { return QuestionTextBlock.Foreground; }
            set { QuestionTextBlock.Foreground = value; }
        }

        /// <summary>
        /// Gets or sets the FontSize for the question character. Default value is 10.
        /// </summary>
        public double QuestionCharacterFontSize
        {
            get { return QuestionTextBlock.FontSize; }
            set { QuestionTextBlock.FontSize = value; }
        }




        //<Grid>
        //  <Ellipse Fill = "Gray" Width="12" Height="12" VerticalAlignment="Center" HorizontalAlignment="Center"/>
        //  <TextBlock Text = "?" Foreground="White" FontWeight="Bold" FontFamily="Tahoma" FontSize="10" VerticalAlignment="Center" HorizontalAlignment="Center" Margin="0 0 0 0" />
        //</Grid>

        public InfoControl()
        {
            Width = 12;
            Height = 12;
            VerticalAlignment = VerticalAlignment.Center;

            BackGroundEllipse = new Ellipse()
            {
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Fill = Brushes.Gray
            };

            QuestionTextBlock = new TextBlock()
            {
                Text = "?",
                FontFamily = new FontFamily("Tahoma"),
                FontWeight = FontWeights.Bold,
                FontSize = 10,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
            };

            Children.Add(BackGroundEllipse);
            Children.Add(QuestionTextBlock);


            _tooltipTextBlock = new TextBlock();
            _tooltipTextBlock.TextWrapping = TextWrapping.Wrap;

            Loaded += (sender, args) => ToolTipService.SetShowDuration(this, ShowDuration);
        }
    }
}
