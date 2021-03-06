// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlotView.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a control that displays a <see cref="PlotModel" />.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Eto.Skia
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using OxyPlot.SkiaSharp;
    using global::Eto.SkiaDraw;
    using global::Eto.Drawing;
    using global::Eto.Forms;

    /// <summary>
    /// Represents a control that displays a <see cref="PlotModel" />.
    /// </summary>
    [Serializable]
    public class PlotView : SkiaDrawable, IPlotView
    {
        /// <summary>
        /// The model lock.
        /// </summary>
        private readonly object modelLock = new object();

        /// <summary>
        /// The render context.
        /// </summary>
        private readonly SkiaRenderContext renderContext = new SkiaRenderContext();

        /// <summary>
        /// The current model (holding a reference to this plot view).
        /// </summary>
        [NonSerialized]
        private PlotModel currentModel;

        /// <summary>
        /// The is model invalidated.
        /// </summary>
        private bool isModelInvalidated;

        /// <summary>
        /// The model.
        /// </summary>
        private PlotModel model;

        /// <summary>
        /// The default controller.
        /// </summary>
        private IPlotController defaultController = new PlotController();

        /// <summary>
        /// The update data flag.
        /// </summary>
        private bool updateDataFlag = true;

        /// <summary>
        /// The zoom rectangle.
        /// </summary>
        private global::SkiaSharp.SKRect zoomRectangle;

        private TrackerHitResult trackerHitResult;

        global::SkiaSharp.SKPaint fillPaint = new global::SkiaSharp.SKPaint()
        {
            Style = global::SkiaSharp.SKPaintStyle.Fill,
        };
        global::SkiaSharp.SKPaint linePaint = new global::SkiaSharp.SKPaint()
        {
            Style = global::SkiaSharp.SKPaintStyle.Stroke,
        };
        global::SkiaSharp.SKPathEffect dashEffect = global::SkiaSharp.SKPathEffect.CreateDash(new float[] { 5, 2 }, 0);

        /// <summary>
        /// Initializes a new instance of the <see cref="PlotView" /> class.
        /// </summary>
        public PlotView()
        {
            this.CanFocus = true;

            this.PanCursor = Cursors.Move;
            this.ZoomRectangleCursor = Cursors.Pointer;
            this.ZoomHorizontalCursor = Cursors.HorizontalSplit;
            this.ZoomVerticalCursor = Cursors.VerticalSplit;
            var doCopy = new DelegatePlotCommand<OxyKeyEventArgs>((view, controller, args) => this.DoCopy());
            (this as IView).ActualController.BindKeyDown(OxyKey.C, OxyModifierKeys.Control, doCopy);
        }

        /// <summary>
        /// Gets the actual model in the view.
        /// </summary>
        /// <value>
        /// The actual model.
        /// </value>
        Model IView.ActualModel
        {
            get
            {
                return this.Model;
            }
        }

        /// <summary>
        /// Gets the actual model.
        /// </summary>
        /// <value>The actual model.</value>
        PlotModel IPlotView.ActualModel
        {
            get
            {
                return this.Model;
            }
        }

        /// <summary>
        /// Gets the actual controller.
        /// </summary>
        /// <value>
        /// The actual <see cref="IController" />.
        /// </value>
        IController IView.ActualController
        {
            get
            {
                return this.Controller ?? this.defaultController;
            }
        }

        /// <summary>
        /// Gets the coordinates of the client area of the view.
        /// </summary>
        OxyRect IView.ClientArea
        {
            get
            {
                return new OxyRect(0, 0, this.ClientSize.Width, this.ClientSize.Height);
            }
        }

        /// <summary>
        /// Gets or sets the model.
        /// </summary>
        [DefaultValue(null)]
        public PlotModel Model
        {
            get
            {
                return this.model;
            }

            set
            {
                if (this.model != value)
                {
                    this.model = value;
                    this.OnModelChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the plot controller.
        /// </summary>
        /// <value>The controller.</value>
        [DefaultValue(null)]
        public IPlotController Controller { get; set; }

        /// <summary>
        /// Gets or sets the pan cursor.
        /// </summary>
        public Cursor PanCursor { get; set; }

        /// <summary>
        /// Gets or sets the horizontal zoom cursor.
        /// </summary>
        public Cursor ZoomHorizontalCursor { get; set; }

        /// <summary>
        /// Gets or sets the rectangle zoom cursor.
        /// </summary>
        public Cursor ZoomRectangleCursor { get; set; }

        /// <summary>
        /// Gets or sets the vertical zoom cursor.
        /// </summary>
        public Cursor ZoomVerticalCursor { get; set; }

        /// <summary>
        /// Shows the tracker.
        /// </summary>
        /// <param name="trackerHitResult">The data.</param>
        void IPlotView.ShowTracker(TrackerHitResult trackerHitResult)
        {
            this.trackerHitResult = trackerHitResult;

            this.Invalidate();
        }

        /// <summary>
        /// Hides the tracker.
        /// </summary>
        void IPlotView.HideTracker()
        {
            this.trackerHitResult = null;

            this.Invalidate();
        }

        /// <summary>
        /// Shows the zoom rectangle.
        /// </summary>
        /// <param name="rectangle">The rectangle.</param>
        void IView.ShowZoomRectangle(OxyRect rectangle)
        {
            this.zoomRectangle = new global::SkiaSharp.SKRect(
                (float)rectangle.Left,
                (float)rectangle.Top,
                (float)(rectangle.Left + rectangle.Width),
                (float)(rectangle.Top + rectangle.Height));
            this.Invalidate();
        }

        /// <summary>
        /// Hides the zoom rectangle.
        /// </summary>
        void IView.HideZoomRectangle()
        {
            this.zoomRectangle = global::SkiaSharp.SKRect.Empty;

            this.Invalidate();
        }

        /// <summary>
        /// Invalidates the plot (not blocking the UI thread)
        /// </summary>
        /// <param name="updateData">if set to <c>true</c>, all data collections will be updated.</param>
        public void InvalidatePlot(bool updateData = true)
        {
            if (updateData)
                this.updateDataFlag = true;

            this.isModelInvalidated = true;

            this.Invalidate();
        }

        /// <summary>
        /// Called when the Model property has been changed.
        /// </summary>
        private void OnModelChanged()
        {
            lock (this.modelLock)
            {
                if (this.currentModel != null)
                {
                    ((IPlotModel)this.currentModel).AttachPlotView(null);
                    this.currentModel = null;
                }

                if (this.Model != null)
                {
                    ((IPlotModel)this.Model).AttachPlotView(this);
                    this.currentModel = this.Model;
                }
            }

            this.InvalidatePlot(true);
        }

        /// <summary>
        /// Sets the cursor type.
        /// </summary>
        /// <param name="cursorType">The cursor type.</param>
        void IView.SetCursorType(OxyPlot.CursorType cursorType)
        {
            switch (cursorType)
            {
                case OxyPlot.CursorType.Pan:
                    this.Cursor = this.PanCursor;
                    break;
                case OxyPlot.CursorType.ZoomRectangle:
                    this.Cursor = this.ZoomRectangleCursor;
                    break;
                case OxyPlot.CursorType.ZoomHorizontal:
                    this.Cursor = this.ZoomHorizontalCursor;
                    break;
                case OxyPlot.CursorType.ZoomVertical:
                    this.Cursor = this.ZoomVerticalCursor;
                    break;
                default:
                    this.Cursor = Cursors.Arrow;
                    break;
            }
        }

        /// <summary>
        /// Sets the clipboard text.
        /// </summary>
        /// <param name="text">The text.</param>
        void IPlotView.SetClipboardText(string text)
        {
            Clipboard.Instance.Text = text;
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Forms.Control.MouseDown" /> event.
        /// </summary>
        /// <param name="e">A <see cref="T:System.Windows.Forms.MouseEventArgs" /> that contains the event data.</param>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            (this as IView).ActualController.HandleMouseDown(this, e.ToMouseDownEventArgs(this));
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Forms.Control.MouseMove" /> event.
        /// </summary>
        /// <param name="e">A <see cref="T:System.Windows.Forms.MouseEventArgs" /> that contains the event data.</param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            (this as IView).ActualController.HandleMouseMove(this, e.ToMouseEventArgs(this));
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Forms.Control.MouseUp" /> event.
        /// </summary>
        /// <param name="e">A <see cref="T:System.Windows.Forms.MouseEventArgs" /> that contains the event data.</param>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            (this as IView).ActualController.HandleMouseUp(this, e.ToMouseUpEventArgs(this));
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);

            (this as IView).ActualController.HandleMouseEnter(this, e.ToMouseEventArgs(this));
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Forms.Control.MouseLeave" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> that contains the event data.</param>
        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);

            (this as IView).ActualController.HandleMouseLeave(this, e.ToMouseEventArgs(this));
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Forms.Control.MouseWheel" /> event.
        /// </summary>
        /// <param name="e">A <see cref="T:System.Windows.Forms.MouseEventArgs" /> that contains the event data.</param>
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            (this as IView).ActualController.HandleMouseWheel(this, e.ToMouseWheelEventArgs(this));
        }

        protected override void OnPaint(SKPaintEventArgs e)
        {
            base.OnPaint(e);

            e.Surface.Canvas.Clear();

            var plot_model = this.Model;

            if (plot_model is null)
                return;

            if (this.isModelInvalidated)
            {
                (plot_model as IPlotModel).Update(this.updateDataFlag);

                this.isModelInvalidated = this.updateDataFlag = false;
            }

            lock (plot_model.SyncRoot)
            {
                this.renderContext.SkCanvas = e.Surface.Canvas;

                (plot_model as IPlotModel).Render(this.renderContext, new OxyRect(0, 0, Width, Height));

                this.renderContext.SkCanvas = null;
            }

            if (this.zoomRectangle != global::SkiaSharp.SKRect.Empty)
            {
                fillPaint.Color = global::SkiaSharp.SKColors.Yellow.WithAlpha(0x40);
                linePaint.Color = global::SkiaSharp.SKColors.Black;
                linePaint.PathEffect = this.dashEffect;

                e.Surface.Canvas.DrawRect(zoomRectangle, fillPaint);
                e.Surface.Canvas.DrawRect(zoomRectangle, linePaint);

                linePaint.PathEffect = null;
            }

            if (this.trackerHitResult != null)
            {
                DrawTracker(e.Surface.Canvas);
            }
        }

        private void DrawTracker(global::SkiaSharp.SKCanvas canvas)
        {
            linePaint.Color = global::SkiaSharp.SKColors.Black.WithAlpha(128);

            canvas.DrawLine(
                (float)trackerHitResult.XAxis.ScreenMin.X,
                (float)trackerHitResult.Position.Y,
                (float)trackerHitResult.XAxis.ScreenMax.X,
                (float)trackerHitResult.Position.Y,
                linePaint);
            canvas.DrawLine(
                (float)trackerHitResult.Position.X,
                (float)trackerHitResult.YAxis.ScreenMin.Y,
                (float)trackerHitResult.Position.X,
                (float)trackerHitResult.YAxis.ScreenMax.Y,
                linePaint);

            var lines = trackerHitResult.Text.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            float width = 0, height = fillPaint.FontSpacing * (lines.Length + 0.5f);
            var char_width = fillPaint.MeasureText("X");

            foreach (var line in lines)
                width = Math.Max(width, fillPaint.MeasureText(line) + char_width * 2);

            var rect = new global::SkiaSharp.SKRect(
                (float)trackerHitResult.Position.X,
                (float)trackerHitResult.Position.Y,
                (float)trackerHitResult.Position.X + width,
                (float)trackerHitResult.Position.Y + height);

            var xoff = rect.Location.X > this.Width / 2 ? -rect.Width : 0;
            var yoff = rect.Location.Y > this.Height / 2 ? -rect.Height : 0;
            rect.Offset(xoff, yoff);

            fillPaint.Color = global::SkiaSharp.SKColors.LightSkyBlue;
            linePaint.Color = global::SkiaSharp.SKColors.Black;

            canvas.DrawRect(rect, fillPaint);
            canvas.DrawRect(rect, linePaint);

            fillPaint.Color = global::SkiaSharp.SKColors.Black;
            var location = rect.Location;
            location.X += char_width;
            foreach (var line in lines)
            {
                location.Y += fillPaint.FontSpacing;
                canvas.DrawText(line, location, fillPaint);
            }
        }
 
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            (this as IView).ActualController.HandleKeyDown(this, e.ToOxyKeyEventArgs());
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            this.InvalidatePlot(false);
        }

        /// <summary>
        /// Disposes the PlotView.
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources or not.</param>
        protected override void Dispose(bool disposing)
        {
            this.Model = null;
            this.Controller = null;

            bool disposed = this.IsDisposed;

            base.Dispose(disposing);

            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                this.renderContext.Dispose();
            }
        }

        /// <summary>
        /// Performs the copy operation.
        /// </summary>
        private void DoCopy()
        {
            var stream = new System.IO.MemoryStream();

            SkiaSharp.PngExporter.Export(this.Model, stream, this.ClientSize.Width, this.ClientSize.Height);

            Clipboard.Instance.Image = new Bitmap(stream);
        }
    }
}
