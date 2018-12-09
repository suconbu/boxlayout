using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

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

            try
            {
                var a = view.Add("a");
                a.Data = new Pen(Color.Red, 1.0f);
                a.SetPosition("50px");

                var aa = a.Add("a.a");
                aa.Data = new Pen(Color.Orange, 1.0f);
                aa.SetSize("50px", "50px");

                var ab = a.Add("a.b");
                ab.Data = new Pen(Color.Orange, 1.0f);
                ab.SetSize("50px", "50px");
                ab.SetPosition("50px", "50px", null, null);

                var ac = a.Add("a.c");
                ac.Data = new Pen(Color.Orange, 1.0f);
                ac.SetSize("50px", "50px");
                ac.SetPosition(null, null, "50px", "50px");

                var ad = a.Add("a.d");
                ad.Data = new Pen(Color.Orange, 1.0f);
                ad.SetSize("50px", "50px");
                // Size指定ありかつPosition両端指定の場合はLeft/Topの方を採用
                ad.SetPosition("100px", "100px", "100px", "100px");

                var ae = a.Add("a.e");
                ae.Data = new Pen(Color.Orange, 1.0f);
                ae.SetSize("50px", "50px");
                // %は親要素のエリアに対する割合
                ae.SetCenter("50%", "50%");
                // Center指定ありの場合はPositionはすべて無視
                ae.SetPosition("100px", "100px", "100px", "100px");

                var b = view.Add("b");
                b.Data = new Pen(Color.Blue, 1.0f);
                b.SetPosition("20vh", "20vw");

                var ba = b.Add("b.a");
                ba.Data = new Pen(Color.RoyalBlue, 1.0f);
                ba.SetSize("100px", "100px");
                ba.SetPosition("-50px", null, null, "+50px");
            }
            catch (Exception ex)
            {
                ;
            }

            this.view.Width = this.ClientRectangle.Width;
            this.view.Height = this.ClientRectangle.Height;
            var sw = Stopwatch.StartNew();
            for (var i = 0; i < 1000; i++)
            {
                this.view.Recalculate();
            }
            Console.WriteLine($"Recalculate: {sw.ElapsedMilliseconds / 1000.0f}ms");

            var sb = new StringBuilder();
            view.Traverse(box =>
            {
                sb.AppendLine($"{box.Name}:{box.Bounds.ToString()}");
            });
            File.WriteAllText($"test_bounds_{DateTime.Now.Ticks}.txt", sb.ToString());

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
