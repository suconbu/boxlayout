using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Suconbu.BoxLayouting
{
    public partial class FormTest : Form
    {
        BoxContainer boxContainer = new BoxContainer();

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

            string previous = null;
            foreach (var type in new[] { "byproperty", "byjson", "byxml" })
            {
                var swCreation = Stopwatch.StartNew();
                var container = new BoxContainer();
                try
                {
                    if (type == "byjson")
                    {
                        container.AddFromFile(@"..\..\test.json", (box, dataText) =>
                        //container.AddFromJson(File.ReadAllText(@"..\..\test.json"), (box, dataText) =>
                        {
                            box.Data = new Pen(Color.FromName(dataText), 1.0f);
                        });
                    }
                    else if(type == "byxml")
                    {
                        container.AddFromFile(@"..\..\test.xml", (box, dataText) =>
                        //container.AddFromXml(File.ReadAllText(@"..\..\test.xml"), (box, dataText) =>
                        {
                            box.Data = new Pen(Color.FromName(dataText), 1.0f);
                        });
                    }
                    else
                    {
                        var a = container.Add("a");
                        a.Data = new Pen(Color.Red, 1.0f);
                        //a.SetPosition("50px");
                        a.PositionTop = a.PositionLeft = a.PositionRight = a.PositionBottom = "50px";

                        var aa = a.Add("a.a");
                        aa.Data = new Pen(Color.Orange, 1.0f);
                        //aa.SetSize("50px", "50px");
                        aa.SizeWidth = aa.SizeHeight = "50px";

                        var ab = a.Add("a.b");
                        ab.Data = new Pen(Color.Orange, 1.0f);
                        //ab.SetSize("50px", "50px");
                        //ab.SetPosition("50px", "50px", null, null);
                        ab.SizeWidth = ab.SizeHeight = "50px";
                        ab.PositionTop = ab.PositionRight = "50px";

                        var ac = a.Add("a.c");
                        ac.Data = new Pen(Color.Orange, 1.0f);
                        //ac.SetSize("50px", "50px");
                        //ac.SetPosition(null, null, "50px", "50px");
                        ac.SizeWidth = ac.SizeHeight = "50px";
                        ac.PositionBottom = ac.PositionLeft = "50px";

                        var ad = a.Add("a.d");
                        ad.Data = new Pen(Color.Orange, 1.0f);
                        //ad.SetSize("50px", "50px");
                        //// Size指定ありかつPosition両端指定の場合はLeft/Topの方を採用
                        //ad.SetPosition("100px", "100px", "100px", "100px");
                        ad.SizeWidth = ad.SizeHeight = "50px";
                        ad.PositionTop = ad.PositionLeft = ad.PositionRight = ad.PositionBottom = "100px";

                        var ae = a.Add("a.e");
                        ae.Data = new Pen(Color.Orange, 1.0f);
                        //ae.SetSize("50px", "50px");
                        //// %は親要素のエリアに対する割合
                        //ae.SetCenter("50%", "50%");
                        //// Center指定ありの場合はPositionはすべて無視
                        //ae.SetPosition("100px", "100px", "100px", "100px");
                        ae.SizeWidth = ae.SizeHeight = "50px";
                        ae.CenterHorizontal = ae.CenterVertical = "50%";
                        ae.PositionTop = ae.PositionLeft = ae.PositionRight = ae.PositionBottom = "100px";

                        var b = container.Add("b");
                        b.Data = new Pen(Color.Blue, 1.0f);
                        //b.SetPosition("20vh", "20vw");
                        b.PositionTop = b.PositionBottom = "20vh";
                        b.PositionLeft = b.PositionRight = "20vw";

                        var ba = b.Add("b.a");
                        ba.Data = new Pen(Color.RoyalBlue, 1.0f);
                        //ba.SetSize("100px", "100px");
                        //ba.SetPosition("-50px", null, null, "+50px");
                        ba.SizeWidth = ba.SizeHeight = "100px";
                        ba.PositionTop = "-50px";
                        ba.PositionLeft = "+50px";
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.ToString());
                }
                swCreation.Stop();

                container.Width = this.ClientRectangle.Width;
                container.Height = this.ClientRectangle.Height;

                var swRecalculate = Stopwatch.StartNew();
                for (var i = 0; i < 1000; i++)
                {
                    container.Recalculate();
                }
                swRecalculate.Stop();

                var sb = new StringBuilder();
                container.Traverse(box =>
                {
                    sb.AppendLine($"{box.Name}:{box.Bounds.ToString()}");
                });
                var t = sb.ToString();
                File.WriteAllText($"test_bounds_{type}_{DateTime.Now.Ticks}.txt", t);
                Debug.Assert(previous == null || previous == t);
                previous = t;

                Trace.TraceInformation($"[{type}]");
                Trace.TraceInformation($"CreationTime: {swCreation.ElapsedMilliseconds / 1000.0f}ms");
                Trace.TraceInformation($"RecalculateTime: {swRecalculate.ElapsedMilliseconds / 1000.0f}ms");

                this.boxContainer = container;
            }

            this.Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            this.boxContainer.Width = this.ClientRectangle.Width;
            this.boxContainer.Height = this.ClientRectangle.Height;
            this.boxContainer.Recalculate();

            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            this.boxContainer.Traverse(box =>
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
