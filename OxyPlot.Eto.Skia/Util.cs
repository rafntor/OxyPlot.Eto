// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Util.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Provides utility functionality
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Eto.Skia
{
    using global::Eto.Drawing;

    public static class Util
    {
        public static Color ToEto(this OxyColor color) => Color.FromArgb(color.R, color.G, color.B, color.A);

        public static RectangleF ToRect(this OxyRect rect)
            => new RectangleF((float)rect.Left, (float)rect.Top, (float)rect.Width, (float)rect.Height);
    }
}
