
namespace OxyPlot.Eto
{
    using global::Eto.Drawing;
    public static class Util
    {
        public static Color ToEto(this OxyColor color) => Color.FromArgb(color.R, color.G, color.B, color.A);
        public static RectangleF ToRect(this OxyRect Rect)
            => new RectangleF((float)Rect.Left, (float)Rect.Top, (float)Rect.Width, (float)Rect.Height);
    }
}
