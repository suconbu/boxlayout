using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BoxLayouting
{
    public partial class FormTest : Form
    {
        View view = new View();
        //Brush textBrush = new SolidBrush(Color.Black);

        public FormTest()
        {
            InitializeComponent();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            var a = view.Add(".a");
            a.Data = new Pen(Color.Red, 1.0f);
            a.SetPosition("20vmin");

            var aa = a.Add(".a.a");
            aa.Data = new Pen(Color.Orange, 1.0f);
            aa.SetSize("100px", "50px");

            var ab = a.Add(".a.b");
            ab.Data = new Pen(Color.DarkGoldenrod, 1.0f); ;
            ab.SetPosition(null, "5vw", null, null);
            ab.SetSize("200px", "50px");
            ab.SetCenter(null, "80%");

            var b = view.Add(".b");
            b.Data = new Pen(Color.Blue, 1.0f); ;
            b.SetCenter("70%", "50%");
            b.SetSize("50%", "10vh");

            this.view.Width = this.ClientRectangle.Width;
            this.view.Height = this.ClientRectangle.Height;
            this.view.Recalculate();

            this.Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            this.view.Width = this.ClientRectangle.Width;
            this.view.Height = this.ClientRectangle.Height;
            this.view.Recalculate();

            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            this.view.Traverse(box =>
            {
                var pen = (Pen)box.Data;
                e.Graphics.DrawRectangle(pen, box.Bounds);
                e.Graphics.DrawLines(pen, new[]
                {
                    new Point(box.Bounds.Left, box.Bounds.Top), new Point(box.Bounds.Right, box.Bounds.Bottom),
                    new Point(box.Bounds.Left, box.Bounds.Bottom), new Point(box.Bounds.Right, box.Bounds.Top)
                });

                StringFormat sf = new StringFormat();
                sf.LineAlignment = StringAlignment.Center;
                sf.Alignment = StringAlignment.Center;
                e.Graphics.DrawString(box.Name, new Font(SystemFonts.MessageBoxFont.FontFamily, 20.0f), new SolidBrush(pen.Color),
                    new RectangleF(box.Bounds.X, box.Bounds.Y, box.Bounds.Width, box.Bounds.Height), sf);
            });
        }
    }
}
