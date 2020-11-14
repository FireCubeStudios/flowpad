using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace flowpad.UserControls
{
    public sealed partial class GraphPaperUserControl : UserControl
    {
        public static DependencyProperty GridSizeProperty =
      DependencyProperty.Register(
        "GridSize",
        typeof(int),
        typeof(GraphPaperUserControl),
        new PropertyMetadata(DEFAULT_GRID_SIZE, OnGridSizeChanged));

        static readonly int DEFAULT_GRID_SIZE = 20;
        public GraphPaperUserControl()
        {
            this.InitializeComponent();
            this.GridSize = DEFAULT_GRID_SIZE;
        }
        public int GridSize
        {
            get
            {
                return ((int)base.GetValue(GridSizeProperty));
            }
            set
            {
                base.SetValue(GridSizeProperty, value);
            }
        }
        void OnCanvasSizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.canvasSize = new Rect(
              0, 0, e.NewSize.Width, e.NewSize.Height);
        }
        void OnDraw(
          CanvasControl sender,
          CanvasDrawEventArgs args)
        {
            // We've already done our drawing by the time we get here and
            // we just fill the space with the tiled brush we've made.
            args.DrawingSession.FillRectangle(
              this.canvasSize,
              this.tiledBrush);
        }
        void OnCreateResources(
          CanvasControl sender,
          CanvasCreateResourcesEventArgs args)
        {
            this.lineBrush = new CanvasSolidColorBrush(sender.Device,
              Colors.LightBlue);

            this.RecreateTiledBrush();

            this.initialised = true;
        }
        void RecreateTiledBrush()
        {
            using (var target = new CanvasRenderTarget(
              this.canvasControl,
              new Size(this.GridSize, this.GridSize)))
            {
                using (var session = target.CreateDrawingSession())
                {
                    var boxSize = this.GridSize - 1;

                    session.Clear(Colors.White);

                    session.DrawLine(
                      new Vector2(0, boxSize),
                      new Vector2(boxSize, boxSize),
                      this.lineBrush);

                    session.DrawLine(
                      new Vector2(boxSize, 0),
                      new Vector2(boxSize, boxSize),
                      this.lineBrush);
                }
                this.tiledBrush = new CanvasImageBrush(this.canvasControl, target)
                {
                    ExtendX = CanvasEdgeBehavior.Wrap,
                    ExtendY = CanvasEdgeBehavior.Wrap
                };
            }
        }
        static void OnGridSizeChanged(
          DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            GraphPaperUserControl control = (GraphPaperUserControl)sender;

            if (control.initialised)
            {
                control.RecreateTiledBrush();
                control.canvasControl.Invalidate();
            }
        }
        bool initialised;
        Rect canvasSize;
        CanvasImageBrush tiledBrush;
        CanvasSolidColorBrush lineBrush;

    }
}
