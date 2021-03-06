// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SvgExporter.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Provides functionality to export plots to scalable vector graphics using <see cref="Graphics" /> for text measuring.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Eto
{
    using System;
    using global::Eto.Drawing;

    /// <summary>
    /// Provides functionality to export plots to scalable vector graphics using <see cref="Graphics" /> for text measuring.
    /// </summary>
    public class SvgExporter : OxyPlot.SvgExporter, IDisposable
    {
        /// <summary>
        /// The graphics drawing surface.
        /// </summary>
        private readonly Graphics g;

        /// <summary>
        /// The render context.
        /// </summary>
        private readonly GraphicsRenderContext grc;

        /// <summary>
        /// Initializes a new instance of the <see cref="SvgExporter" /> class.
        /// </summary>
        public SvgExporter()
        {
            /* TODO Why is this needed at all? */
            this.g = new Graphics(new Bitmap(1, 1, PixelFormat.Format32bppRgba));
            this.TextMeasurer = this.grc = new GraphicsRenderContext(this.g);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.g.Dispose();
            this.grc.Dispose();
        }
    }
}
