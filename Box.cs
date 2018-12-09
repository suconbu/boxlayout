using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BoxLayouting
{
    class View
    {
        public int Width { get; set; }
        public int Height { get; set; }

        Box rootBox = new Box(string.Empty);

        public View() { }
        public View(int width, int height)
        {
            this.Width = width;
            this.Height = height;
        }

        public Box Add(string name)
        {
            return this.rootBox.Add(name);
        }

        public void Remove(string name)
        {
            this.rootBox.Remove(name);
        }

        public void Recalculate()
        {
            this.rootBox.RecalculateBounds(new Size(this.Width, this.Height));
        }

        public void Traverse(Action<Box> handler)
        {
            this.rootBox.TraverseUp(handler);
        }
    }

    class Box
    {
        public static Box Empty { get { return empty; } }
        static Box empty = new Box();

        public string Name { get; private set; }
        public object Data { get; set; }
        public Rectangle Bounds { get; private set; }

        public Box this[string name] { get { return this.boxes[name]; } }

        BoxDimension boxPosition;

        BoxValue boxWidth;
        BoxValue boxHeight;

        BoxValue boxCenterX;
        BoxValue boxCenterY;

        Box parent;
        readonly Dictionary<string, Box> boxes = new Dictionary<string, Box>();

        internal Box(string name)
        {
            this.Name = name ?? throw new ArgumentNullException();
        }

        Box() { }

        public bool IsEmtpy() { return this.Name == null; }

        public Box Add(string name)
        {
            if (this.IsEmtpy()) return Empty;
            var box = new Box(name);
            box.parent = this;
            this.boxes.Add(box.Name, box);
            return box;
        }

        public void Remove(string name)
        {
            if(this.boxes.TryGetValue(name, out var box))
            {
                box.parent = null;
                this.boxes.Remove(name);
            }
        }

        public void SetPosition(string all) { this.boxPosition = new BoxDimension(all, all, all, all); }
        public void SetPosition(string topBottom, string leftRight) { this.boxPosition = new BoxDimension(topBottom, leftRight, topBottom, leftRight); }
        public void SetPosition(string top, string leftRight, string bottom) { this.boxPosition = new BoxDimension(top, leftRight, bottom, leftRight); }
        public void SetPosition(string top, string right, string bottom, string left) { this.boxPosition = new BoxDimension(top, right, bottom, left); }

        // Boxの中心を親要素のどこに持ってくるかを指定
        // Centerを指定したらPositionは無視
        public void SetCenter(string x, string y)
        {
            this.boxCenterX = new BoxValue(x);
            this.boxCenterY = new BoxValue(y);
        }

        // Positionで両端が指定されていてもSize指定を優先(その時の位置は左/上を採用)
        public void SetSize(string width, string height)
        {
            this.boxWidth = new BoxValue(width);
            this.boxHeight = new BoxValue(height);
        }

        public void TraverseUp(Action<Box> handler)
        {
            foreach(var box in this.boxes.Values)
            {
                if(box.boxes.Count > 0)
                {
                    box.TraverseUp(handler);
                }
                handler(box);
            }
        }

        internal void RecalculateBounds(Size viewSize)
        {
            Rectangle parentBounds = this.IsRoot() ? new Rectangle(0, 0, viewSize.Width, viewSize.Height) : this.parent.Bounds;
            float width = this.IsRoot() ? viewSize.Width : this.boxWidth.GetCalculated(viewSize, this.parent.Bounds.Width);
            float height = this.IsRoot() ? viewSize.Height : this.boxHeight.GetCalculated(viewSize, this.parent.Bounds.Height);
            float centerX = this.IsRoot() ? float.NaN : this.boxCenterX.GetCalculated(viewSize, this.parent.Bounds.Width);
            float centerY = this.IsRoot() ? float.NaN : this.boxCenterY.GetCalculated(viewSize, this.parent.Bounds.Height);
            var position = this.boxPosition.GetCalculated(viewSize, parentBounds.Width, parentBounds.Height);

            var bounds = parentBounds;

            if(!float.IsNaN(width))
            {
                if (!float.IsNaN(centerX))
                {
                    bounds.X = (int)(parentBounds.Left + centerX - (width / 2.0f));
                }
                else
                {
                    bounds.X =
                        (float.IsNaN(position.Left) && float.IsNaN(position.Right)) ? parentBounds.Left :
                        (float.IsNaN(position.Left) && !float.IsNaN(position.Right)) ? (int)(parentBounds.Right - position.Right - width) :
                        (int)(parentBounds.Left + position.Left);
                }
                bounds.Width = (int)width;
            }
            else
            {
                bounds.X = !float.IsNaN(position.Left) ? (int)position.Left : 0;
                bounds.Width = !float.IsNaN(position.Left) && !float.IsNaN(position.Right) ? (int)(parentBounds.Width - position.Left - position.Right) : 0;
            }

            if (!float.IsNaN(height))
            {
                if (!float.IsNaN(centerY))
                {
                    bounds.Y = (int)(parentBounds.Top + centerY - (height / 2.0f));
                }
                else
                {
                    bounds.Y =
                        (float.IsNaN(position.Top) && float.IsNaN(position.Bottom)) ? parentBounds.Top :
                        (float.IsNaN(position.Top) && !float.IsNaN(position.Bottom)) ? (int)(parentBounds.Bottom - position.Bottom - height) :
                        (int)(parentBounds.Top + position.Top);
                }
                bounds.Height = (int)height;
            }
            else
            {
                bounds.Y = !float.IsNaN(position.Top) ? (int)position.Top : 0;
                bounds.Height = (!float.IsNaN(position.Top) && !float.IsNaN(position.Bottom)) ? (int)(parentBounds.Height - position.Top - position.Bottom) : 0;
            }

            this.Bounds = bounds;

            foreach (var box in this.boxes.Values)
            {
                box.RecalculateBounds(viewSize);
            }
        }

        bool IsRoot() { return this.parent == null; }
    }


    enum BoxValueUnit { Null, Pixel, Vw, Vh, Vmax, Vmin, Parcent }
    struct BoxValue
    {
        public float Length;
        public BoxValueUnit Unit;

        public BoxValue(string value)
        {
            this.Length = 0.0f;
            this.Unit = BoxValueUnit.Null;

            var match = value != null ? Regex.Match(value, @"(?<length>\d+(\.\d+)?)(?<unit>px|vw|vh|vmax|vmin|%)?") : Match.Empty;
            if (match.Success)
            {
                if (float.TryParse(match.Groups["length"].Value, out this.Length))
                {
                    if (match.Groups["unit"].Success)
                    {
                        var unit = match.Groups["unit"].Value;
                        if (unit == "px") this.Unit = BoxValueUnit.Pixel;
                        else if (unit == "vw") this.Unit = BoxValueUnit.Vw;
                        else if (unit == "vh") this.Unit = BoxValueUnit.Vh;
                        else if (unit == "vmax") this.Unit = BoxValueUnit.Vmax;
                        else if (unit == "vmin") this.Unit = BoxValueUnit.Vmin;
                        else if (unit == "%") this.Unit = BoxValueUnit.Parcent;
                    }
                    else
                    {
                        if(this.Length == 0.0f)
                        {
                            this.Unit = BoxValueUnit.Pixel;
                        }
                    }
                }
            }
        }

        public bool IsNull() { return this.Unit == BoxValueUnit.Null; }

        public float GetCalculated(Size viewSize, float baseLength)
        {
            float value = float.NaN;
            if (this.Unit == BoxValueUnit.Pixel) value = this.Length;
            else if (this.Unit == BoxValueUnit.Vw) value = this.Length / 100.0f * viewSize.Width;
            else if (this.Unit == BoxValueUnit.Vh) value = this.Length / 100.0f * viewSize.Height;
            else if (this.Unit == BoxValueUnit.Vmax) value = this.Length / 100.0f * Math.Max(viewSize.Width, viewSize.Height);
            else if (this.Unit == BoxValueUnit.Vmin) value = this.Length / 100.0f * Math.Min(viewSize.Width, viewSize.Height);
            else if (this.Unit == BoxValueUnit.Parcent) value = this.Length / 100.0f * baseLength;
            return value;
        }
    }

    struct PaddingF
    {
        public float Left;
        public float Top;
        public float Right;
        public float Bottom;

        public PaddingF(float left, float top, float right, float bottom)
        {
            this.Left = left;
            this.Top = top;
            this.Right = right;
            this.Bottom = bottom;
        }
    }

    struct BoxDimension
    {
        public BoxValue Left;
        public BoxValue Top;
        public BoxValue Right;
        public BoxValue Bottom;

        public BoxDimension(string top, string right, string bottom, string left)
        {
            this.Left = new BoxValue(left);
            this.Top = new BoxValue(top);
            this.Right = new BoxValue(right);
            this.Bottom = new BoxValue(bottom);
        }

        public PaddingF GetCalculated(Size viewSize, int baseWidth, int baseHeight)
        {
            return new PaddingF(
                this.Left.GetCalculated(viewSize, baseWidth),
                this.Top.GetCalculated(viewSize, baseHeight),
                this.Right.GetCalculated(viewSize, baseWidth),
                this.Bottom.GetCalculated(viewSize, baseHeight));
        }
    }
}
