#define BOX_SUPPORT_JSON

#if BOX_SUPPORT_JSON
using Newtonsoft.Json.Linq;
#endif//BOX_SUPPORT_JSON
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Suconbu.BoxLayouting
{
    //## 
    public class BoxContainer
    {
        public int Width { get { return this.Size.Width; } }
        public int Height { get { return this.Size.Height; } }
        public Size Size { get; private set; }

        readonly Box rootBox = new Box(string.Empty);

        public BoxContainer() { }

        public IEnumerable<Box> AddFromFile(string path)
        {
            var ext = Path.GetExtension(path).ToLower();
#if BOX_SUPPORT_JSON
            if (ext == ".json")
            {
                return this.AddFromJsonString(File.ReadAllText(path));
            }
#endif//BOX_SUPPORT_JSON
            if (ext == ".xml")
            {
                return this.AddFromXmlString(File.ReadAllText(path));
            }
            throw new NotSupportedException($"Not supported extension '{ext}'.");
        }

#if BOX_SUPPORT_JSON
        public IEnumerable<Box> AddFromJsonString(string definitionJson)
        {
            var jboxes = JArray.Parse(definitionJson);
            BoxFactory.AddFromJson(this.rootBox, jboxes);
            return this.rootBox.Children.Values;
        }
#endif//BOX_SUPPORT_JSON

        public IEnumerable<Box> AddFromXmlString(string definitionXml)
        {
            var setting = new XmlReaderSettings();
            setting.IgnoreComments = true;
            setting.IgnoreWhitespace = true;
            using (var sr = new StringReader(definitionXml))
            using (var xr = XmlReader.Create(sr, setting))
            {
                BoxFactory.AddFromXml(this.rootBox, xr);
            }
            return this.rootBox.Children.Values;
        }

        public Box Add(string name)
        {
            return this.rootBox.AddChild(name);
        }

        public void Remove(string name)
        {
            this.rootBox.RemoveChild(name);
        }

        public void Clear()
        {
            this.rootBox.ClearChildren();
        }

        public void Resize(Size size)
        {
            if (this.Size != size)
            {
                this.Size = size;
                this.rootBox.RecalculateBoundsRecursive(size);
            }
        }

        public void Resize(int width, int height)
        {
            this.Resize(new Size(width, height));
        }

        public void TraverseDown(Action<Box> handler)
        {
            this.rootBox.TraverseDown(handler);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            this.TraverseDown(box =>
            {
                sb.AppendLine(box.ToString());
            });
            return sb.ToString();
        }

        //##
        static class BoxFactory
        {
#if BOX_SUPPORT_JSON
            public static void AddFromJson(Box parentBox, JToken jboxes)
            {
                foreach (var jbox in jboxes.Values<JObject>())
                {
                    var box = new Box();
                    foreach (var prop in jbox)
                    {
                        if (prop.Key == "children")
                        {
                            AddFromJson(box, prop.Value);
                        }
                        else
                        {
                            SetProperty(box, prop.Key, prop.Value.Value<string>());
                        }
                    }
                    parentBox.AddChild(box);
                }
            }
#endif//BOX_SUPPORT_JSON

            public static void AddFromXml(Box parentBox, XmlReader reader)
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "box")
                    {
                        var box = new Box();
                        while (reader.MoveToNextAttribute())
                        {
                            SetProperty(box, reader.LocalName, reader.Value);
                        }
                        reader.MoveToElement();
                        if (!reader.IsEmptyElement)
                        {
                            AddFromXml(box, reader);
                        }
                        parentBox.AddChild(box);
                    }
                    else if (reader.NodeType == XmlNodeType.EndElement)
                    {
                        break;
                    }
                }
            }

            static void SetProperty(Box box, string key, string value)
            {
                var keys = key.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                var values = value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                box[key] = value;

                if (key == "name")
                {
                    box.Name = value;
                }
                else if (keys[0] == "position")
                {
                    if (key.Length == keys[0].Length)
                    {
                        if (values.Length == 1) box.SetPosition(values[0]);
                        else if (values.Length == 2) box.SetPosition(values[0], values[1]);
                        else if (values.Length == 3) box.SetPosition(values[0], values[1], values[2]);
                        else if (values.Length == 4) box.SetPosition(values[0], values[1], values[2], values[3]);
                        else throw new FormatException($"Too meny value '{value}'.");
                    }
                    else
                    {
                        if (keys[1] == "top") box.PositionTop = value;
                        else if (keys[1] == "left") box.PositionLeft = value;
                        else if (keys[1] == "right") box.PositionRight = value;
                        else if (keys[1] == "bottom") box.PositionBottom = value;
                        else throw new FormatException($"Unknown key '{key}'.");
                    }
                }
                else if (keys[0] == "size")
                {
                    if (key.Length == keys[0].Length)
                    {
                        if (values.Length == 1) box.SetSize(values[0]);
                        else if (values.Length == 2) box.SetSize(values[0], values[1]);
                        else throw new FormatException($"Too meny value '{value}'.");
                    }
                    else
                    {
                        if (keys[1] == "width") box.SizeWidth = value;
                        else if (keys[1] == "height") box.SizeHeight = value;
                        else throw new FormatException($"Unknown key '{key}'.");
                    }
                }
                else if (keys[0] == "center")
                {
                    if (key.Length == keys[0].Length)
                    {
                        if (values.Length == 1) box.SetCenter(values[0]);
                        else if (values.Length == 2) box.SetCenter(values[0], values[1]);
                        else throw new FormatException($"Too meny value '{value}'.");
                    }
                    else
                    {
                        if (keys[1] == "horizontal") box.CenterHorizontal = value;
                        else if (keys[1] == "vertical") box.CenterVertical = value;
                        else throw new FormatException($"Unknown key '{key}'.");
                    }
                }
                else
                {
                    ;
                }
            }
        }
    }

    //##
    public class Box
    {
        public static Box Empty { get { return empty; } }
        static Box empty = new Box();

        public string Name { get; internal set; }
        public Rectangle Bounds { get; private set; }
        public Dictionary<string, object> UserData { get; private set; } = new Dictionary<string, object>();

        public string this[string name]
        {
            get { return this.properties.TryGetValue(name, out var s) ? s : null; }
            set { this.properties[name] = value; }
        }
        public IReadOnlyDictionary<string, Box> Children { get { return this.children; } }

        BoxPosition boxPosition;

        BoxValue boxWidth;
        BoxValue boxHeight;

        BoxValue boxCenterX;
        BoxValue boxCenterY;

        Box parent;
        Dictionary<string, Box> children = new Dictionary<string, Box>();
        Dictionary<string, string> properties = new Dictionary<string, string>();

        internal Box() { }

        internal Box(string name = null)
        {
            this.Name = name ?? throw new ArgumentNullException();
        }

        public bool IsEmtpy() { return this.Name == null; }

        public Box AddChild(string name)
        {
            if (this.IsEmtpy()) return Empty;
            var box = new Box(name);
            box.parent = this;
            this.children.Add(box.Name, box);
            return box;
        }

        public Box AddChild(Box box)
        {
            box.parent = this;
            this.children.Add(box.Name, box);
            return box;
        }

        public void RemoveChild(string name)
        {
            if(this.children.TryGetValue(name, out var box))
            {
                box.parent = null;
                this.children.Remove(name);
            }
        }

        public void ClearChildren()
        {
            this.children.Clear();
        }

        // Position
        // Set the distance from the bounds of parent Box.
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
        public string PositionTop
        {
            get { return this.boxPosition.Top.ValueString; }
            set { this.boxPosition.Top = new BoxValue(value); }
        }
        public string PositionLeft
        {
            get { return this.boxPosition.Left.ValueString; }
            set { this.boxPosition.Left = new BoxValue(value); }
        }
        public string PositionRight
        {
            get { return this.boxPosition.Right.ValueString; }
            set { this.boxPosition.Right = new BoxValue(value); }
        }
        public string PositionBottom
        {
            get { return this.boxPosition.Bottom.ValueString; }
            set { this.boxPosition.Bottom = new BoxValue(value); }
        }

        // Size
        // Set the width / height.
        // If both sides are specified by Position, will adopt the Left / Top.
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
        public string SizeWidth
        {
            get { return this.boxWidth.ValueString; }
            set { this.boxWidth = new BoxValue(value); }
        }
        public string SizeHeight
        {
            get { return this.boxHeight.ValueString; }
            set { this.boxHeight = new BoxValue(value); }
        }

        // Center
        // Specify the position of the center of Box relative of the parent Box.
        // Position is ignored when this parameter is set.
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
        public string CenterHorizontal
        {
            get { return this.boxCenterX.ValueString; }
            set { this.boxCenterX = new BoxValue(value); }
        }
        public string CenterVertical
        {
            get { return this.boxCenterY.ValueString; }
            set { this.boxCenterY = new BoxValue(value); }
        }

        public void TraverseDown(Action<Box> handler)
        {
            this.Traverse(true, handler);
        }

        public void TraverseUp(Action<Box> handler)
        {
            this.Traverse(false, handler);
        }

        void Traverse(bool topToBottom, Action<Box> handler)
        {
            foreach (var box in this.children.Values)
            {
                if (topToBottom) handler(box);
                if (box.children.Count > 0)
                {
                    box.Traverse(topToBottom, handler);
                }
                if (!topToBottom) handler(box);
            }
        }

        public static Rectangle GetContainRectangle(Size source, Size dest, float horizontalCenter = 0.5f, float verticalCenter = 0.5f)
        {
            var contain = new Rectangle();
            float sourceAspect = (float)source.Width / source.Height;
            float destAspect = (float)dest.Width / dest.Height;
            if (sourceAspect < destAspect)
            {
                contain.Width = source.Width * dest.Height / source.Height;
                contain.Height = dest.Height;
            }
            else
            {
                contain.Width = dest.Width;
                contain.Height = source.Height * dest.Width / source.Width;
            }
            contain.X = (int)((dest.Width - contain.Width) * horizontalCenter);
            contain.Y = (int)((dest.Height - contain.Height) * verticalCenter);
            return contain;
        }

        public override string ToString()
        {
            return $"{this.Name}:{this.Bounds.ToString()}";
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
                bounds.X = !float.IsNaN(position.Left) ? (int)(parentBounds.Left + position.Left) : parentBounds.Left;
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
                bounds.Y = !float.IsNaN(position.Top) ? (int)(parentBounds.Top + position.Top) : parentBounds.Top;
                bounds.Height = (!float.IsNaN(position.Top) && !float.IsNaN(position.Bottom)) ? (int)(parentBounds.Height - position.Top - position.Bottom) : 0;
            }

            this.Bounds = bounds;

            foreach (var box in this.children.Values)
            {
                box.RecalculateBoundsRecursive(viewSize);
            }
        }

        bool IsRoot() { return this.parent == null; }

        enum BoxValueUnit { Null, Pixel, Parcent, Vw, Vh, Vmax, Vmin }

        //##
        struct BoxValue
        {
            public float Length;
            public BoxValueUnit Unit;
            public string ValueString;

            public BoxValue(string valueString)
            {
                this.Length = 0.0f;
                this.Unit = BoxValueUnit.Null;
                this.ValueString = valueString;

                if (valueString != null)
                {
                    var match = Regex.Match(valueString, @"^(?<length>[+-]?\d+(\.\d+)?)(?<unit>(\w+|%))?$");
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
                            // Unit can be omitted in '0'.
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
                return this.IsNull() ? "null" : this.ValueString;
            }
        }

        //##
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

            public override string ToString()
            {
                return $"Top:{this.Top}, Right:{this.Right}, Bottom:{this.Bottom}, Left:{this.Left}";
            }
        }

        //##
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
}
