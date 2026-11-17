using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Xml;
using System.Xml.Linq;

namespace EklipseLib
{
    public class TextureRegion
    {
        public Texture2D Texture;
        public Rectangle Rect;
        public virtual Vector2 Size => new Vector2(Rect.Width, Rect.Height);
        public TextureRegion(Texture2D tx, Rectangle? rect = null) { Texture = tx; Rect = rect ?? new Rectangle(0, 0, Texture.Dimensions); }
        public TextureRegion(Texture2D tx, int x, int y, int w, int h) { Texture = tx; Rect = new Rectangle(x, y, w, h); }
        public TextureRegion(TextureRegion reg) { Texture = reg.Texture; Rect = reg.Rect; }
        public override bool Equals(object? obj)
        {
            TextureRegion other = obj as TextureRegion;
            if (other == null) return false;
            return Texture.Id == other.Texture.Id && Rect == other.Rect;
        }
        public bool Equivalent(TextureRegion other)
        {
            //in many cases, using equals fails bc the texture id is not the same, since there is no way to compare the paths
            //or something else to check if its the same texture, we just ignore the texture and compare the rects instead
            if (other == null) return false;
            return Rect == other.Rect;
        }
    }
    public class Sprite : TextureRegion
    {
        public Vector2 Scale = Vector2.One;
        public override Vector2 Size => new Vector2(Rect.Width, Rect.Height) * Scale;
        public Vector2 Origin => Size / 2; //center of sprite
        public float Rotation = 0;
        public Sprite(Texture2D tx, Rectangle? rect = null) : base(tx, rect) { }
        public Sprite(Texture2D tx, int x, int y, int w, int h) : base(tx, x, y, w, h) { }
        public Sprite(TextureRegion reg) : base(reg) { }
        public override bool Equals(object? obj)
        {
            Sprite other = obj as Sprite;
            if (other == null) return false;
            return (this as TextureRegion).Equals(other as TextureRegion) && Scale == other.Scale && Origin == other.Origin && Rotation == other.Rotation;
        }
        public bool Equivalent(Sprite other)
        {
            if (other == null) return false;
            return (this as TextureRegion).Equivalent(other as TextureRegion) && Scale == other.Scale && Origin == other.Origin && Rotation == other.Rotation;
        }
    }
    public class TextureAtlas
    {
        public Texture2D Texture;
        public Dictionary<string, TextureRegion> Regions;
        public TextureAtlas() { Regions = new(); }
        public TextureAtlas(Texture2D texture, Dictionary<string, Rectangle> rects)
        {
            Texture = texture;
            Regions = new();
            foreach (var rect in rects)
            {
                Regions.Add(rect.Key, new(Texture, rect.Value));
            }
        }
        public void AddRegion(string name, int x, int y, int w, int h)
        {
            TextureRegion reg = new(Texture, x, y, w, h);
            Regions.Add(name, reg);
        }
        public void AddRegion(string name, Rectangle rect)
        {
            TextureRegion reg = new(Texture, rect);
            Regions.Add(name, reg);
        } 
        public TextureRegion GetRegion(string name) { return Regions[name]; }
        public string GetRegionName(TextureRegion region)
        {
            TextureRegion value = new(region); //ensures we dont compare a sprite to a region
            foreach (var pair in Regions)
            {
                //comparing them directly doesnt work, since we are in the same atlas, ill just compare their rects
                if (pair.Value.Rect.Position == value.Rect.Position && pair.Value.Size == value.Size) { return pair.Key; }
            }
            return null;
        }
        public bool RemoveRegion(string name) { return Regions.Remove(name); }
        public void Clear() { Regions.Clear(); }
        public static TextureAtlas ReadFromFile(string filePath)
        {
            using (XmlReader reader = XmlReader.Create(filePath))
            {
                return TextureAtlas.ReadFromFile(reader);
            }
        }
        public static TextureAtlas ReadFromFile(XmlReader reader)
        {
            TextureAtlas atlas = new();
            XDocument doc = XDocument.Load(reader);
            XElement root = doc.Root;
            /* XML Example:
                * <TextureAtlas>
                * <Texture>Assets/Sprites/test.png</Texture>
                * 
                * <Regions>
                *      <Region name="spriteOne" x="0" y="0" width="32" height="32" />
                *      <Region name="spriteTwo" x="32" y="0" width="32" height="32" />
                * </Regions
                * </TextureAtlas>
                */
            string TexturePath = root.Element("Texture").Value;
            atlas.Texture = Raylib.LoadTexture(TexturePath);
            var regions = root.Element("Regions")?.Elements("Region");
            if (regions != null)
            {
                foreach (var region in regions)
                {
                    string name = region.Attribute("name")?.Value;
                    int x = int.Parse(region.Attribute("x")?.Value ?? "0");
                    int y = int.Parse(region.Attribute("y")?.Value ?? "0");
                    int width = int.Parse(region.Attribute("w")?.Value ?? "0");
                    int height = int.Parse(region.Attribute("h")?.Value ?? "0");

                    if (!string.IsNullOrEmpty(name)) { atlas.AddRegion(name, x, y, width, height); }
                }
            }
            return atlas;
        }
        public static void WriteToFile(TextureAtlas atlas, string path)
        {
            XmlWriterSettings setts = new() { Indent = true, IndentChars = "    " };
            using (XmlWriter writer = XmlWriter.Create(path, setts))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("TextureAtlas");
                string[] parts = path.Split(@"\");
                writer.WriteElementString("Texture", $"Assets/Sprites/{parts[parts.Length - 1]}"); //gets only name
                if (atlas.Regions.Count > 0)
                {
                    writer.WriteStartElement("Regions");
                    foreach (string regName in atlas.Regions.Keys)
                    {
                        writer.WriteStartElement("Region");
                        writer.WriteAttributeString("name", regName);
                        writer.WriteAttributeString("x", atlas.Regions[regName].Rect.X.ToString());
                        writer.WriteAttributeString("y", atlas.Regions[regName].Rect.Y.ToString());
                        writer.WriteAttributeString("w", atlas.Regions[regName].Rect.Width.ToString());
                        writer.WriteAttributeString("h", atlas.Regions[regName].Rect.Height.ToString());
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement(); //close regions
                }
                writer.WriteEndElement(); //close root
                writer.WriteEndDocument();
                writer.Flush();
            }
        }
        public void Dispose()
        {
            Regions.Clear();
            Raylib.UnloadTexture(Texture);
        }
    }
}