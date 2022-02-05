// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlotView.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a control that displays a <see cref="PlotModel" />.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Eto
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using global::Eto.Drawing;
    using global::Eto.Forms;

    /// <summary>
    /// Represents a control that displays a <see cref="PlotModel" />.
    /// </summary>
    [Serializable]
    public class PlotView : Drawable, IPlotView
    {
        /// <summary>
        /// The model lock.
        /// </summary>
        private readonly object modelLock = new object();

        /// <summary>
        /// The render context.
        /// </summary>
        private readonly GraphicsRenderContext renderContext;

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
        private Rectangle zoomRectangle;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlotView" /> class.
        /// </summary>
        public PlotView()
        {
            this.renderContext = new GraphicsRenderContext();
            this.CanFocus = true;

            this.PanCursor = Cursors.Move;
            this.ZoomRectangleCursor = Cursors.Pointer; // WindowsForms use Cursors.SizeNWSE;
            this.ZoomHorizontalCursor = Cursors.HorizontalSplit;
            this.ZoomVerticalCursor = Cursors.VerticalSplit;
            var doCopy = new DelegatePlotCommand<OxyKeyEventArgs>((view, controller, args) => this.DoCopy(view, args));
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
        /// Hides the tracker.
        /// </summary>
        void IPlotView.HideTracker()
        {
            this.ToolTip = null;
        }

        /// <summary>
        /// Hides the zoom rectangle.
        /// </summary>
        void IView.HideZoomRectangle()
        {
            this.zoomRectangle = Rectangle.Empty;
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
        /// Shows the tracker.
        /// </summary>
        /// <param name="trackerHitResult">The data.</param>
        void IPlotView.ShowTracker(TrackerHitResult trackerHitResult)
        {
            this.ToolTip = trackerHitResult.ToString();
        }

        /// <summary>
        /// Shows the zoom rectangle.
        /// </summary>
        /// <param name="rectangle">The rectangle.</param>
        void IView.ShowZoomRectangle(OxyRect rectangle)
        {
            this.zoomRectangle = new Rectangle((int)rectangle.Left, (int)rectangle.Top, (int)rectangle.Width, (int)rectangle.Height);
            this.Invalidate();
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

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Forms.Control.Paint" /> event.
        /// </summary>
        /// <param name="e">A <see cref="T:System.Windows.Forms.PaintEventArgs" /> that contains the event data.</param>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            try
            {
                if (this.isModelInvalidated)
                {
                    if (this.model != null)
                    {
                        ((IPlotModel)this.model).Update(this.updateDataFlag);
                        this.updateDataFlag = false;
                    }

                    this.isModelInvalidated = false;
                }

                this.renderContext.SetGraphicsTarget(e.Graphics);

                if (this.model != null)
                {
                    if (!this.model.Background.IsUndefined())
                    {
                        using (var brush = new SolidBrush(this.model.Background.ToEto()))
                        {
                            e.Graphics.FillRectangle(brush, e.ClipRectangle);
                        }
                    }

                    ((IPlotModel)this.model).Render(this.renderContext, new OxyRect(0, 0, this.Width, this.Height));
                }

                if (this.zoomRectangle != Rectangle.Empty)
                {
                    using (var zoomBrush = new SolidBrush(Color.FromArgb(0xFF, 0xFF, 0x00, 0x40)))
                    using (var zoomPen = new Pen(Colors.Black))
                    {
                        zoomPen.DashStyle = new DashStyle(0f, 3f, 1f);

                        e.Graphics.FillRectangle(zoomBrush, this.zoomRectangle);
                        e.Graphics.DrawRectangle(zoomPen, this.zoomRectangle);
                    }
                }
            }
            catch (Exception paintException)
            {
                var trace = new StackTrace(paintException);
                Debug.WriteLine(paintException);
                Debug.WriteLine(trace);
                var font = Fonts.Monospace(10);

                // e.Graphics.RestoreTransform();
                e.Graphics.DrawText(font, Brushes.Red, this.Width * 0.5f, this.Height * 0.5f, "OxyPlot paint exception: " + paintException.Message);

                // e.Graphics.DrawString("OxyPlot paint exception: " + paintException.Message, font, Brushes.Red, this.Width * 0.5f, this.Height * 0.5f, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
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
        private void DoCopy(IPlotView view, OxyInputEventArgs args)
        {
            var exporter = new PngExporter
            {
                Width = this.ClientSize.Width,
                Height = this.ClientSize.Height,
            };

            var bitmap = exporter.ExportToBitmap(this.Model);

            Clipboard.Instance.Image = bitmap;
        }
    }
}
