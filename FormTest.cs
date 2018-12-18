using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Suconbu.BoxLayouting
{
    public partial class FormTest : Form
    {
        BoxContainer boxContainer = new BoxContainer();
        FileSystemWatcher watcher = new FileSystemWatcher();

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

            this.SetupBox();

            //this.watcher = new FileSystemWatcher(@".");
            //this.watcher.NotifyFilter = NotifyFilters.LastWrite;
            //this.watcher.Filter = "test.*";
            //this.watcher.Changed += (ss, ee) =>
            //{
            //    this.SetupBox();
            //    this.Invalidate();
            //};
            //this.watcher.EnableRaisingEvents = true;
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
                object userData = null;
                box.UserData.TryGetValue("pen", out userData);
                var pen = userData as Pen;
                box.UserData.TryGetValue("brush", out userData);
                var brush = userData as Brush;
                box.UserData.TryGetValue("image", out userData);
                var image = userData as Image;

                var draw = box["draw"];
                if (draw == "line")
                {
                    if (pen != null)
                    {
                        e.Graphics.DrawRectangle(pen, box.Bounds);
                    }
                }
                else if (draw == "fill")
                {
                    if (brush != null)
                    {
                        e.Graphics.FillRectangle(brush, box.Bounds);
                    }
                }
                else if (draw?.StartsWith("image") ?? false)
                {
                    if (image != null)
                    {
                        Rectangle sourceRect;
                        Rectangle destRect;
                        if (draw == "image-cover")
                        {
                            sourceRect = Box.GetContainRectangle(box.Bounds.Size, image.Size);
                            destRect = box.Bounds;
                        }
                        else if (draw == "image-contain")
                        {
                            sourceRect = new Rectangle(new Point(), image.Size);
                            destRect = Box.GetContainRectangle(image.Size, box.Bounds.Size);
                            destRect.Offset(box.Bounds.Location);
                        }
                        else // if (draw == "image-stretch")
                        {
                            sourceRect = new Rectangle(new Point(), image.Size);
                            destRect = box.Bounds;
                        }
                        e.Graphics.DrawImage(image, destRect, sourceRect, GraphicsUnit.Pixel);
                    }
                }
                else
                {
                    ;
                }
                var penpen = SystemPens.ControlText;
                e.Graphics.DrawRectangle(penpen, box.Bounds);
                e.Graphics.DrawLines(penpen, new[]
                {
                    new Point(box.Bounds.Left, box.Bounds.Top), new Point(box.Bounds.Right, box.Bounds.Bottom),
                    new Point(box.Bounds.Left, box.Bounds.Bottom), new Point(box.Bounds.Right, box.Bounds.Top)
                });

                StringFormat sf = new StringFormat();
                sf.LineAlignment = StringAlignment.Center;
                sf.Alignment = StringAlignment.Center;
                e.Graphics.DrawString(box.Name, new Font(SystemFonts.MessageBoxFont.FontFamily, 20.0f), SystemBrushes.ControlText,
                    new RectangleF(box.Bounds.X, box.Bounds.Y, box.Bounds.Width, box.Bounds.Height), sf);
            });
        }

        void SetupBox()
        {
            string previous = null;
            foreach (var type in new[] { "byproperty", "byjson", "byxml" })
            //foreach (var type in new[] { "byxml" })
            {
                var swCreation = Stopwatch.StartNew();
                var container = new BoxContainer();
                try
                {
                    if (type == "byjson")
                    {
                        container.AddBoxFromFile(@"test.json");
                    }
                    else if (type == "byxml")
                    {
                        container.AddBoxFromFile(@"test.xml");
                    }
                    else
                    {
                        var a = container.Add("a");
                        //a.SetPosition("50px");
                        a.PositionTop = a.PositionLeft = a.PositionRight = a.PositionBottom = "50px";

                        var aa = a.Add("a.a");
                        //aa.SetSize("50px", "50px");
                        aa.SizeWidth = aa.SizeHeight = "50px";

                        var ab = a.Add("a.b");
                        //ab.SetSize("50px", "50px");
                        //ab.SetPosition("50px", "50px", null, null);
                        ab.SizeWidth = ab.SizeHeight = "50px";
                        ab.PositionTop = ab.PositionRight = "50px";

                        var ac = a.Add("a.c");
                        //ac.SetSize("50px", "50px");
                        //ac.SetPosition(null, null, "50px", "50px");
                        ac.SizeWidth = ac.SizeHeight = "50px";
                        ac.PositionBottom = ac.PositionLeft = "50px";

                        var ad = a.Add("a.d");
                        //ad.SetSize("50px", "50px");
                        //// Size指定ありかつPosition両端指定の場合はLeft/Topの方を採用
                        //ad.SetPosition("100px", "100px", "100px", "100px");
                        ad.SizeWidth = ad.SizeHeight = "50px";
                        ad.PositionTop = ad.PositionLeft = ad.PositionRight = ad.PositionBottom = "100px";

                        var ae = a.Add("a.e");
                        //ae.SetSize("50px", "50px");
                        //// %は親要素のエリアに対する割合
                        //ae.SetCenter("50%", "50%");
                        //// Center指定ありの場合はPositionはすべて無視
                        //ae.SetPosition("100px", "100px", "100px", "100px");
                        ae.SizeWidth = ae.SizeHeight = "50px";
                        ae.CenterHorizontal = ae.CenterVertical = "50%";
                        ae.PositionTop = ae.PositionLeft = ae.PositionRight = ae.PositionBottom = "100px";

                        var b = container.Add("b");
                        //b.SetPosition("20vh", "20vw");
                        b.PositionTop = b.PositionBottom = "20vh";
                        b.PositionLeft = b.PositionRight = "20vw";

                        var ba = b.Add("b.a");
                        //ba.SetSize("100px", "100px");
                        //ba.SetPosition("-50px", null, null, "+50px");
                        ba.SizeWidth = ba.SizeHeight = "100px";
                        ba.PositionTop = "-50px";
                        ba.PositionLeft = "+50px";

                        var bb = b.Add("b.b");
                        bb.PositionTop = "60%";
                        bb.PositionRight = "30%";
                        bb.PositionBottom = "10%";
                        bb.PositionLeft = "20%";

                        var bc = b.Add("b.c");
                        bc.SizeWidth = "20vmax";
                        bc.SizeHeight = "20vmin";
                        bc.CenterHorizontal = "80%";
                        bc.CenterVertical = "20%";
                    }

                    container.Traverse(box =>
                    {
                        var color = Color.Black;
                        var width = 1;
                        var draw = box["draw"];
                        if (draw == "line" || draw == "fill")
                        {
                            if (box["color"] != null)
                            {
                                var s = box["color"];
                                var r = int.Parse(s.Substring(1, 2), NumberStyles.HexNumber);
                                var g = int.Parse(s.Substring(3, 2), NumberStyles.HexNumber);
                                var b = int.Parse(s.Substring(5, 2), NumberStyles.HexNumber);
                                var a = int.Parse(s.Substring(7, 2), NumberStyles.HexNumber);
                                color = Color.FromArgb(a, r, g, b);
                            }

                            if (draw == "line")
                            {
                                if (box["width"] != null)
                                {
                                    width = int.Parse(box["width"]);
                                }
                                box.UserData["pen"] = new Pen(color, width);
                            }
                            else if (draw == "fill")
                            {
                                box.UserData["brush"] = new SolidBrush(color);
                            }
                        }
                        else if (draw?.StartsWith("image") ?? false)
                        {
                            if (box["file"] != null && File.Exists(box["file"]))
                            {
                                box.UserData["image"] = Bitmap.FromFile(box["file"]);
                            }
                        }
                        else
                        {
                            ;
                        }
                    });
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
                //File.WriteAllText($"test_bounds_{type}_{DateTime.Now.Ticks}.txt", t);
                Debug.Assert(previous == null || previous == t);
                previous = t;

                Trace.TraceInformation($"[{type}]");
                Trace.TraceInformation($"CreationTime: {swCreation.ElapsedMilliseconds / 1000.0f}ms");
                Trace.TraceInformation($"RecalculateTime: {swRecalculate.ElapsedMilliseconds / 1000.0f}ms");

                this.boxContainer = container;
            }
        }
    }
}
