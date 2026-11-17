using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Xml;
using System.Xml.Linq;

namespace EklipseLib
{
    public class UIPage
    {
        public TextureAtlas? uiAtlas;
        public Vector2 OGSize =  Vector2.One * 500;
        public int Scale = 1; //scale of the ui page, positive numbers multiply and negatives divide
        public Vector2 FinalSize => OGSize * (Scale > 0 ? Scale : 1f / Math.Abs(Scale));
        public Color Background = Color.White;
        public Dictionary<string, UIElement> uiElements;
        public UIPage() { uiElements = new(); }
        public UIPage(Dictionary<string, UIElement> elements) { uiElements = elements; }
        public virtual void Draw()
        {
            foreach (var element in uiElements.Values)
            {
                element.Draw();
            }
        }
        public void PageResized()
        {
            foreach (var element in uiElements.Values)
            {
                element.Position *= (Scale > 0 ? Scale : 1f / Math.Abs(Scale));
                if (element is Button btt)
                    btt.Size *= (Scale > 0 ? Scale : 1f / Math.Abs(Scale));
                else if (element is Panel panel)
                    panel.Sprite.Scale *= (Scale > 0 ? Scale : 1f / Math.Abs(Scale));
                else if (element is Label label)
                    label.TextSize = (int)(label.TextSize * (Scale > 0 ? Scale : 1f / Math.Abs(Scale)));
            }
        }
        public virtual void Update()
        {
            foreach (var element in uiElements.Values)
            {
                if (element is Button)
                {
                    Button btt = element as Button;
                    Rectangle buttonRect = new Rectangle(btt.Position, btt.Size);
                    if (Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), buttonRect))
                    {
                        btt.Hovered();
                        if (Raylib.IsMouseButtonPressed(MouseButton.Left))
                        {
                            btt.Clicked();
                        }
                        if (Raylib.IsMouseButtonDown(MouseButton.Left))
                        {
                            btt.Held();
                        }
                    }
                }
                //other update logic
            }
        }
        public static UIPage ReadFromFile(string filePath)
        {
            using (XmlReader reader = XmlReader.Create(filePath))
            {
                return UIPage.ReadFromFile(reader);
            }
        }
        public static UIPage ReadFromFile(XmlReader reader)
        {
            UIPage ui = new();
            XDocument doc = XDocument.Load(reader);
            XElement root = doc.Root;

            string[] str = root.Element("Size")?.Value.Split(',') ?? ["500", "500"];
            Vector2 dimensions = new Vector2(int.Parse(str[0]), int.Parse(str[1]));
            ui.OGSize = dimensions;

            var bgColor = root.Element("BgColor");
            if (bgColor != null)
            {
                int r = int.Parse(bgColor.Attribute("R")?.Value ?? "255");
                int g = int.Parse(bgColor.Attribute("G")?.Value ?? "255");
                int b = int.Parse(bgColor.Attribute("B")?.Value ?? "255");
                ui.Background = new Color(r, g, b);
            }
            var labels = root.Element("Labels")?.Elements("Label");
            if (labels != null)
            {
                foreach (var label in labels)
                {
                    int x = int.Parse(label.Attribute("x")?.Value ?? "0");
                    int y = int.Parse(label.Attribute("y")?.Value ?? "0");
                    string name = label.Attribute("name")?.Value ?? $"Label {x} {y}";
                    string[] color = label.Element("Color")?.Value.Split(',') ?? ["255", "255", "255"];
                    Color col = new Color(int.Parse(color[0]), int.Parse(color[1]), int.Parse(color[2]));

                    XElement? settings = label.Element("Settings");
                    int size = 10, spacing = 1;
                    if (settings != null)
                    {
                        size = int.Parse(settings.Attribute("size")?.Value ?? "25");
                        spacing = int.Parse(settings.Attribute("spacing")?.Value ?? "5");
                    }
                    string font = label.Attribute("font")?.Value ?? "Default";

                    string text = label.Attribute("text")?.Value ?? "Lorem ipsum";

                    ui.uiElements.Add(name, new Label(new Vector2(x, y), col, text)
                    { TextSize = size, Spacing = spacing, TextFont = font == "Default" ? Raylib.GetFontDefault() : Raylib.LoadFont(font) });
                }
            }
            string AtlasPath = root.Element("Atlas").Value;
            TextureAtlas atlas = TextureAtlas.ReadFromFile(AtlasPath);
            ui.uiAtlas = atlas;
            var sprites = root.Element("Panels")?.Elements("Panel");
            if (sprites != null)
            {
                foreach (var sprite in sprites)
                {
                    int x = int.Parse(sprite.Attribute("x")?.Value ?? "0");
                    int y = int.Parse(sprite.Attribute("y")?.Value ?? "0");
                    string name = sprite.Attribute("name")?.Value ?? $"Panel {x} {y}";
                    string texture = sprite.Attribute("texture").Value;
                    TextureRegion reg = atlas.GetRegion(texture);
                    string[] color = sprite.Element("Color")?.Value.Split(',') ?? ["255", "255", "255"];
                    Color col = new Color(int.Parse(color[0]), int.Parse(color[1]), int.Parse(color[2]));

                    XElement? settings = sprite.Element("Settings");
                    Vector2 scale = Vector2.One; float rot = 0;
                    if (settings != null)
                    {
                        string[] scaleStr = settings.Attribute("scale")?.Value.Split(',') ?? ["1", "1"];
                        scale = new Vector2(int.Parse(scaleStr[0]), int.Parse(scaleStr[1]));
                        rot = float.Parse(settings.Attribute("rotation")?.Value ?? "0");
                    }
                    Sprite spr = new(reg) { Rotation = rot, Scale = scale };
                    Panel p = new(new Vector2(x, y), col, spr);
                    ui.uiElements.Add(name, p);
                }
            }
            var buttons = root.Element("Buttons")?.Elements("Button");
            if (buttons != null)
            {
                foreach (var btt in buttons)
                {
                    int x = int.Parse(btt.Attribute("x")?.Value ?? "0");
                    int y = int.Parse(btt.Attribute("y")?.Value ?? "0");
                    int w = int.Parse(btt.Attribute("w")?.Value ?? "0");
                    int h = int.Parse(btt.Attribute("h")?.Value ?? "0");
                    string name = btt.Attribute("name")?.Value ?? $"Button {x} {y}";
                    string[] color = btt.Element("Color")?.Value.Split(',') ?? ["255", "255", "255"];
                    Color col = new Color(int.Parse(color[0]), int.Parse(color[1]), int.Parse(color[2]));

                    bool highlight = true; Color borderCol = Color.Black;
                    XElement? settings = btt.Element("Settings");
                    if (settings != null)
                    {
                        highlight = bool.Parse(settings.Attribute("highlight")?.Value ?? "true");
                        string[] borderColor = btt.Element("Color")?.Value.Split(',') ?? ["0", "0", "0"];
                        borderCol = new Color(int.Parse(borderColor[0]), int.Parse(borderColor[1]), int.Parse(borderColor[2]));
                    }
                    Label? label = null;
                    XElement? labelElm = btt.Element("Label");
                    if (labelElm != null) { label = ui.uiElements[labelElm.Value] as Label; }
                    Panel? sprite = null;
                    XElement? panelElm = btt.Element("Sprite");
                    if (panelElm != null) { sprite = ui.uiElements[panelElm.Value] as Panel; }

                    Button button = new Button(new Vector2(x, y), col, new Vector2(w, h), label, sprite)
                    {
                        HoveringHighlights = highlight,
                        BorderColor = borderCol
                    };
                    ui.uiElements.Add(name, button);
                }
            }
            return ui;
        }
        public static void WriteToFile(UIPage page, string path, string atlasName, TextureAtlas uiAtlas)
        {
            XmlWriterSettings setts = new() { Indent = true, IndentChars = "    " };
            using (XmlWriter writer = XmlWriter.Create(path, setts))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("UIPage");
                writer.WriteElementString("Size", $"{page.OGSize.X}, {page.OGSize.Y}");
                writer.WriteElementString("Atlas", $"Assets/Sprites/{atlasName}");
                writer.WriteStartElement("BgColor");
                writer.WriteAttributeString("R", page.Background.R.ToString());
                writer.WriteAttributeString("G", page.Background.G.ToString());
                writer.WriteAttributeString("B", page.Background.B.ToString());
                writer.WriteEndElement(); //close bg color
                Dictionary<string, Label> labels = new(); Dictionary<string, Button> btts = new(); Dictionary<string, Panel> panels = new();
                foreach (string elmKey in page.uiElements.Keys) //sort elements by type
                {
                    if ((page.uiElements[elmKey] as Label) != null) labels.Add(elmKey, page.uiElements[elmKey] as Label);
                    if ((page.uiElements[elmKey] as Button) != null) btts.Add(elmKey, page.uiElements[elmKey] as Button);
                    if ((page.uiElements[elmKey] as Panel) != null) panels.Add(elmKey, page.uiElements[elmKey] as Panel);
                }
                if (labels.Count != 0)
                {
                    writer.WriteStartElement("Labels");
                    foreach (string labelKey in labels.Keys)
                    {
                        writer.WriteStartElement("Label");
                        writer.WriteAttributeString("name", labelKey);
                        Label label = labels[labelKey];
                        writer.WriteAttributeString("x", ((int)label.Position.X).ToString());
                        writer.WriteAttributeString("y", ((int)label.Position.Y).ToString());
                        writer.WriteAttributeString("text", label.Text);
                        writer.WriteElementString("Color", $"{label.Color.R},{label.Color.G},{label.Color.B}");
                        writer.WriteStartElement("Settings");
                        writer.WriteAttributeString("size", label.TextSize.ToString());
                        writer.WriteAttributeString("spacing", label.Spacing.ToString());
                        writer.WriteAttributeString("font", label.TextFont.Equals(Raylib.GetFontDefault()) ? "Default" : "INSERT_FONT_DIR");
                        writer.WriteEndElement(); //closes settings
                        writer.WriteEndElement(); //closes label
                    }
                    writer.WriteEndElement(); //close labels
                }
                if (panels.Count != 0)
                {
                    writer.WriteStartElement("Panels");
                    foreach (string panelKey in panels.Keys)
                    {
                        writer.WriteStartElement("Panel");
                        writer.WriteAttributeString("name", panelKey);
                        Panel panel = panels[panelKey];
                        writer.WriteAttributeString("x", ((int)panel.Position.X).ToString());
                        writer.WriteAttributeString("y", ((int)panel.Position.Y).ToString());
                        string name = uiAtlas.Regions.FirstOrDefault(x => x.Value.Equivalent(new TextureRegion(panel.Sprite))).Key;
                        writer.WriteAttributeString("texture", name);
                        writer.WriteElementString("Color", $"{panel.Color.R},{panel.Color.G},{panel.Color.B}");
                        writer.WriteStartElement("Settings");
                        writer.WriteAttributeString("scale", $"{panel.Sprite.Scale.X},{panel.Sprite.Scale.Y}");
                        writer.WriteAttributeString("rotation", $"{panel.Sprite.Rotation}");

                        writer.WriteEndElement(); //close settings
                        writer.WriteEndElement(); //close panel
                    }
                    writer.WriteEndElement(); //close panels
                }
                if (btts.Count != 0)
                {
                    writer.WriteStartElement("Buttons");
                    foreach (string bttKey in btts.Keys)
                    {
                        writer.WriteStartElement("Button");
                        writer.WriteAttributeString("name", bttKey);
                        Button btt = btts[bttKey];
                        writer.WriteAttributeString("x", ((int)btt.Position.X).ToString());
                        writer.WriteAttributeString("y", ((int)btt.Position.Y).ToString());
                        writer.WriteAttributeString("w", ((int)btt.Size.X).ToString());
                        writer.WriteAttributeString("h", ((int)btt.Size.Y).ToString());
                        writer.WriteElementString("Color", $"{btt.Color.R},{btt.Color.G},{btt.Color.B}");
                        writer.WriteStartElement("Settings");
                        writer.WriteAttributeString("highlight", btt.HoveringHighlights.ToString());
                        writer.WriteAttributeString("borderColor", $"{btt.BorderColor.R},{btt.BorderColor.G},{btt.BorderColor.B}");
                        writer.WriteEndElement(); //close settings
                        if (btt.ButtonText != null)
                        {
                            string name = page.uiElements.FirstOrDefault(x => x.Value == (btt.ButtonText as UIElement)).Key;
                            writer.WriteElementString("Label", name);
                        }
                        if (btt.Image != null)
                        {
                            string name = page.uiElements.FirstOrDefault(x => x.Value == (btt.Image as UIElement)).Key;
                            writer.WriteElementString("Sprite", name);
                        }
                        writer.WriteEndElement(); //close button
                    }
                    writer.WriteEndElement(); //close buttons
                }
                writer.WriteEndElement(); //close uiPage
                writer.WriteEndDocument(); //close doc
                writer.Flush();
            }
        }
        public void Dispose()
        {
            uiAtlas?.Dispose();
            foreach (var element in uiElements.Values)
            {
                if (element is Label label) { Raylib.UnloadFont(label.TextFont); }
                if (element is Panel panel) { Raylib.UnloadTexture(panel.Sprite.Texture); }
            }
            uiElements.Clear();
            //there is still something thats not being disposed of, its a really small thing but still something
        }
    }
    public class UIElement
    {
        public Vector2 Position;
        public Color Color;
        public bool IsChild = false;
        public UIElement(Vector2 pos, Color col) { Position = pos; Color = col; }
        public virtual void Draw() { }
    }
    public class Button : UIElement
    {
        public bool Enabled = true;
        public Vector2 Size;
        public Label? ButtonText;
        public Panel? Image;
        Color _color, _imgColor;
        public Color BorderColor = Color.White;
        public Color DisabledColor = Color.Gray;
        public bool HoveringHighlights = true;
        public Button(Vector2 pos, Color col, Vector2 size, Label? label = null, Panel? sprite = null) : base(pos, col)
        {
            Size = size; ButtonText = label; Image = sprite;
            if (label != null) { ButtonText.IsChild = true; ButtonText.Position = pos + Vector2.One * 10; }
            if (sprite != null)
            {
                Image.Sprite.Scale = new Vector2(Size.X / Image.Sprite.Size.X, Size.Y / Image.Sprite.Size.Y);
                Image.Sprite.Rotation = 0; Image.IsChild = true; Image.Position = pos + Image.Sprite.Size / 2;
            }
            SetEvents();
        }
        private void SetEvents()
        {
            _color = Color;
            _imgColor = Image?.Color ?? Color.White;
            Click = () => { };
            Hover = () => { if (HoveringHighlights)
                {
                    _color = Color.Lerp(Color, Color.Black, 0.1f);
                    Image?.Color = Color.Lerp(Image?.Color ?? Color.White, Color.Black, 0.1f);
                }
                else { _color = Color; Image?.Color = _imgColor; }
            };
            Hold = () =>
            {
                if (HoveringHighlights)
                {
                    _color = Color.Lerp(Color, Color.Black, 0.3f);
                    Image?.Color = Color.Lerp(Image?.Color ?? Color.White, Color.Black, 0.3f);
                }
                else { _color = Color; Image?.Color = _imgColor; }
            };
        }
        public override void Draw()
        {
            if (IsChild) return;
            if (!Enabled) _color = DisabledColor;
            Raylib.DrawRectangle((int)Position.X, (int)Position.Y, (int)Size.X, (int)Size.Y, _color);
            Raylib.DrawRectangleLines((int)Position.X, (int)Position.Y, (int)Size.X, (int)Size.Y, BorderColor);
            ButtonText?.IsChild = false; Image?.IsChild = false;
            ButtonText?.Draw(); Image?.Draw();
            ButtonText?.IsChild = true; Image?.IsChild = true;
            _color = Color; //reset color (used when stopping hovering)
            Image?.Color = _imgColor; //reset sprite color
        }
        public void Held() { if (Enabled) Hold(); }
        public void Clicked() { if (Enabled) Click(); }
        public void Hovered() { if (Enabled) Hover(); }
        public delegate void ClickHandler();
        public event ClickHandler Hold;
        public event ClickHandler Click;
        public event ClickHandler Hover;
    }
    public class Label : UIElement
    {
        public string Text;
        public int TextSize = 25, Spacing = 1;
        public Font TextFont = Raylib.GetFontDefault();
        public Label(Vector2 pos, Color col, string text) : base(pos, col) { Text = text; }
        public override void Draw()
        {
            if (IsChild) return;
            Raylib.DrawTextEx(TextFont, Text, Position, TextSize, Spacing, Color);
        }
    }
    public class Panel : UIElement
    {
        public Sprite Sprite;
        public Panel(Vector2 pos, Color col, Sprite tx) : base(pos, col) { Sprite = tx; }
        public override void Draw()
        {
            if (IsChild) return;
            Rectangle destRect = new(Position.X, Position.Y, Sprite.Size.X, Sprite.Size.Y);
            Raylib.DrawTexturePro(Sprite.Texture, Sprite.Rect, destRect, Sprite.Origin, Sprite.Rotation, Color);
        }
    }
}