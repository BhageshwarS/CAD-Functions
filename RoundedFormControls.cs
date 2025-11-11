using System;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MYCOLLECTION
{
    class RoundedFormControls
    {
        public static void Apply(Control c, int radius, Color? borderColor = null, int borderWidth = 1)
        {
            PaintEventHandler paint = null;
            EventHandler invalidate = (s, e) => c.Invalidate();
            EventHandler parentBackChanged = (s, e) => c.Invalidate();

            paint = (s, e) =>
            {
                if (c.ClientSize.Width <= 0 || c.ClientSize.Height <= 0) return;

                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                float w = c.ClientSize.Width - 1f;
                float h = c.ClientSize.Height - 1f;
                float r = radius;
                float maxR = (float)Math.Floor(Math.Min(w, h) / 2f);
                if (r > maxR) r = maxR;
                if (r < 0f) r = 0f;

                RectangleF rect = new RectangleF(0.5f, 0.5f, w, h);

                using (GraphicsPath path = CreateRoundRect(rect, r))
                {
                    // "Erase" outside the rounded path with the parent's background (smooth edges)
                    Color outside = c.Parent != null ? c.Parent.BackColor : c.BackColor;
                    using (Region outsideRegion = new Region(e.ClipRectangle))
                    using (SolidBrush outsideBrush = new SolidBrush(outside))
                    {
                        outsideRegion.Exclude(path);
                        g.FillRegion(outsideBrush, outsideRegion);
                    }

                    // Optional border
                    if (borderWidth > 0)
                    {
                        Color bc = borderColor.HasValue ? borderColor.Value : ControlPaint.Dark(c.BackColor);
                        using (Pen p = new Pen(bc, borderWidth))
                        {
                            p.Alignment = PenAlignment.Inset;
                            g.DrawPath(p, path);
                        }
                    }
                }
            };

            c.Paint += paint;
            c.Resize += invalidate;
            c.ParentChanged += (s, e) =>
            {
                if (c.Parent != null) c.Parent.BackColorChanged += parentBackChanged;
                c.Invalidate();
            };
            if (c.Parent != null) c.Parent.BackColorChanged += parentBackChanged;

            c.Disposed += (s, e) =>
            {
                c.Paint -= paint;
                c.Resize -= invalidate;
                if (c.Parent != null) c.Parent.BackColorChanged -= parentBackChanged;
            };
        }

        private static GraphicsPath CreateRoundRect(RectangleF r, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            if (radius <= 0f)
            {
                path.AddRectangle(r);
                return path;
            }
            float d = radius * 2f;
            path.StartFigure();
            path.AddArc(r.X, r.Y, d, d, 180, 90); // TL
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90); // TR
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); // BR
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90); // BL
            path.CloseFigure();
            return path;
        }
    }
}