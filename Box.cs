using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;

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

        public void AddFrom(string definitionJson, Action<Box, string> createHandler = null)
        {
            try
            {
                var jboxes = JArray.Parse(definitionJson);
                foreach (var jbox in jboxes.Cast<JObject>())
                {
                    this.rootBox.Add(new Box(jbox, createHandler));
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
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
            this.rootBox.RecalculateBoundsRecursive(new Size(this.Width, this.Height));
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

        BoxPosition boxPosition;

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

        internal Box(JObject jbox, Action<Box, string> createHandler)
        {
            foreach (var prop in jbox)
            {
                var match = Regex.Match(prop.Key, @"(?<key1>\w+)(-(?<key2>\w+))?");
                var key = prop.Key;
                var key1 = match.Groups["key1"].Value ?? string.Empty;
                var key2 = match.Groups["key2"].Value ?? string.Empty;
                var value = prop.Value.Type == JTokenType.String ? (string)prop.Value : string.Empty;
                var values = value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (key == "name")
                {
                    this.Name = value;
                }
                else if(key1 == "position")
                {
                    if(string.IsNullOrEmpty(key2))
                    {
                        if (values.Length == 1) this.SetPosition(values[0]);
                        else if (values.Length == 2) this.SetPosition(values[0], values[1]);
                        else if (values.Length == 3) this.SetPosition(values[0], values[1], values[2]);
                        else if (values.Length == 4) this.SetPosition(values[0], values[1], values[2], values[3]);
                        else throw new FormatException($"Too meny value '{prop.Value}'.");
                    }
                    else
                    {
                        if (key2 == "top") this.Top = value;
                        else if (key2 == "left") this.Left = value;
                        else if (key2 == "right") this.Right = value;
                        else if (key2 == "bottom") this.Bottom = value;
                        else throw new FormatException($"Unknown key '{prop.Key}'.");
                    }
                }
                else if (key1 == "size")
                {
                    if (string.IsNullOrEmpty(key2))
                    {
                        if (values.Length == 1) this.SetSize(values[0]);
                        else if (values.Length == 2) this.SetSize(values[0], values[1]);
                        else throw new FormatException($"Too meny value '{prop.Value}'.");
                    }
                    else
                    {
                        if (key2 == "width") this.Width = value;
                        else if (key2 == "height") this.Height = value;
                        else throw new FormatException($"Unknown key '{prop.Key}'.");
                    }
                }
                else if (key1 == "center")
                {
                    if (string.IsNullOrEmpty(key2))
                    {
                        if (values.Length == 1) this.SetCenter(values[0]);
                        else if (values.Length == 2) this.SetCenter(values[0], values[1]);
                        else throw new FormatException($"Too meny value '{prop.Value}'.");
                    }
                    else
                    {
                        if (key2 == "horizontal") this.HorizontalCenter = value;
                        else if (key2 == "vertical") this.VerticalCenter = value;
                        else throw new FormatException($"Unknown key '{prop.Key}'.");
                    }
                }
                else if(key == "children")
                {
                    foreach (var jboxChild in prop.Value.Values<JObject>())
                    {
                        this.Add(new Box(jboxChild, createHandler));
                    }
                }
            }
            createHandler?.Invoke(this, jbox["data"]?.Value<string>());
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

        public Box Add(Box box)
        {
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

        // Position
        // 親Boxの上下左右からの距離を指定
        public void SetPosition(string all)
        {
            this.boxPosition = new BoxPosition(all, all, all, all);
        }
        public void SetPosition(string topBottom, string leftRight)
        {
            this.boxPosition = new BoxPosition(topBottom, leftRight, topBottom, leftRight);
        }
        public void SetPosition(string top, string leftRight, string bottom)
        {
            this.boxPosition = new BoxPosition(top, leftRight, bottom, leftRight);
        }
        public void SetPosition(string top, string right, string bottom, string left)
        {
            this.boxPosition = new BoxPosition(top, right, bottom, left);
        }
        public string Top
        {
            get { return this.boxPosition.Top.OriginalValue; }
            set { this.boxPosition.Top = new BoxValue(value); }
        }
        public string Left
        {
            get { return this.boxPosition.Left.OriginalValue; }
            set { this.boxPosition.Left = new BoxValue(value); }
        }
        public string Right
        {
            get { return this.boxPosition.Right.OriginalValue; }
            set { this.boxPosition.Right = new BoxValue(value); }
        }
        public string Bottom
        {
            get { return this.boxPosition.Bottom.OriginalValue; }
            set { this.boxPosition.Bottom = new BoxValue(value); }
        }

        // Size
        // Boxの幅高さを指定
        // Size指定ありかつPositionで両端指定(LeftとRight/TopとBottom)されてた場合はLeft/TopとSizeを採用
        public void SetSize(string widthHeight)
        {
            this.boxWidth = new BoxValue(widthHeight);
            this.boxHeight = new BoxValue(widthHeight);
        }
        public void SetSize(string width, string height)
        {
            this.boxWidth = new BoxValue(width);
            this.boxHeight = new BoxValue(height);
        }
        public string Width
        {
            get { return this.boxWidth.OriginalValue; }
            set { this.boxWidth = new BoxValue(value); }
        }
        public string Height
        {
            get { return this.boxHeight.OriginalValue; }
            set { this.boxHeight = new BoxValue(value); }
        }

        // Center
        // Boxの中心を親要素のどこに持ってくるかを指定
        // Centerを指定したらPositionは無視
        public void SetCenter(string horizontalVertical)
        {
            this.boxCenterX = new BoxValue(horizontalVertical);
            this.boxCenterY = new BoxValue(horizontalVertical);
        }
        public void SetCenter(string horizontal, string vertical)
        {
            this.boxCenterX = new BoxValue(horizontal);
            this.boxCenterY = new BoxValue(vertical);
        }
        public string HorizontalCenter
        {
            get { return this.boxCenterX.OriginalValue; }
            set { this.boxCenterX = new BoxValue(value); }
        }
        public string VerticalCenter
        {
            get { return this.boxCenterY.OriginalValue; }
            set { this.boxCenterY = new BoxValue(value); }
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

        internal void RecalculateBoundsRecursive(Size viewSize)
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
                // Size指定あり
                if (!float.IsNaN(centerX))
                {
                    // Center指定あり
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
                // Size指定あり
                if (!float.IsNaN(centerY))
                {
                    // Center指定あり
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
                box.RecalculateBoundsRecursive(viewSize);
            }
        }

        bool IsRoot() { return this.parent == null; }
    }

    // 単位
    enum BoxValueUnit { Null, Pixel, Parcent, Vw, Vh, Vmax, Vmin }

    struct BoxValue
    {
        public float Length;
        public BoxValueUnit Unit;
        public string OriginalValue;

        public BoxValue(string value)
        {
            this.Length = 0.0f;
            this.Unit = BoxValueUnit.Null;
            this.OriginalValue = value;

            if (value != null)
            {
                var match = Regex.Match(value, @"^(?<length>[+-]?\d+(\.\d+)?)(?<unit>(\w+|%))?$");
                if (match.Success && float.TryParse(match.Groups["length"].Value, out this.Length))
                {
                    if (match.Groups["unit"].Success)
                    {
                        var unit = match.Groups["unit"].Value;
                        if (unit == "px") this.Unit = BoxValueUnit.Pixel;
                        else if (unit == "%") this.Unit = BoxValueUnit.Parcent;
                        else if (unit == "vw") this.Unit = BoxValueUnit.Vw;
                        else if (unit == "vh") this.Unit = BoxValueUnit.Vh;
                        else if (unit == "vmax") this.Unit = BoxValueUnit.Vmax;
                        else if (unit == "vmin") this.Unit = BoxValueUnit.Vmin;
                        else throw new ArgumentException($"Unknown value unit '{unit}'.");
                    }
                    else
                    {
                        // 0だけは単位省略が許される
                        this.Unit = (this.Length == 0.0f) ? BoxValueUnit.Pixel : throw new ArgumentException("Unit of value is missing.");
                    }
                }
                else
                {
                    throw new ArgumentException("Invalid value format.");
                }
            }
        }

        public bool IsNull() { return this.Unit == BoxValueUnit.Null; }

        public float GetCalculated(Size viewSize, float baseLength)
        {
            float value;
            if (this.Unit == BoxValueUnit.Pixel) value = this.Length;
            else if (this.Unit == BoxValueUnit.Vw) value = this.Length / 100.0f * viewSize.Width;
            else if (this.Unit == BoxValueUnit.Vh) value = this.Length / 100.0f * viewSize.Height;
            else if (this.Unit == BoxValueUnit.Vmax) value = this.Length / 100.0f * Math.Max(viewSize.Width, viewSize.Height);
            else if (this.Unit == BoxValueUnit.Vmin) value = this.Length / 100.0f * Math.Min(viewSize.Width, viewSize.Height);
            else if (this.Unit == BoxValueUnit.Parcent) value = this.Length / 100.0f * baseLength;
            else value = float.NaN;
            return value;
        }

        public override string ToString()
        {
            return this.IsNull() ? "null" : this.OriginalValue;
        }
    }

    struct PositionF
    {
        public float Top;
        public float Left;
        public float Right;
        public float Bottom;

        public PositionF(float left, float top, float right, float bottom)
        {
            this.Top = top;
            this.Left = left;
            this.Right = right;
            this.Bottom = bottom;
        }
    }

    struct BoxPosition
    {
        public BoxValue Top;
        public BoxValue Left;
        public BoxValue Right;
        public BoxValue Bottom;

        public BoxPosition(string top, string right, string bottom, string left)
        {
            this.Top = new BoxValue(top);
            this.Left = new BoxValue(left);
            this.Right = new BoxValue(right);
            this.Bottom = new BoxValue(bottom);
        }

        public PositionF GetCalculated(Size viewSize, int baseWidth, int baseHeight)
        {
            return new PositionF(
                this.Left.GetCalculated(viewSize, baseWidth),
                this.Top.GetCalculated(viewSize, baseHeight),
                this.Right.GetCalculated(viewSize, baseWidth),
                this.Bottom.GetCalculated(viewSize, baseHeight));
        }

        public override string ToString()
        {
            return $"Top:{this.Top}, Right:{this.Right}, Bottom:{this.Bottom}, Left:{this.Left}";
        }
    }
}
