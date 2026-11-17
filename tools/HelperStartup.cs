using EklipseLib;
using Raylib_cs;
using System.IO;
using System.Numerics;
using System.Xml;
using System.Xml.Linq;

Console.WriteLine("Welcome to the EklipseLib Helper, type a command to start (or '?' for help)");
int state = 0; //for some reason i cant make an enum so this will do: 0 - START, 1 - SHEET, 2 - UI
string path = "";
while (true)
{
    string[] command = Console.ReadLine().Split();
    string[] c = command;
    try
    {
        command = new string[3];
        command[0] = c[0];
        command[1] = c[1];
        command[2] = c[2];
    }
    catch { }
    
    if (state == 0)
    {
        if (!string.IsNullOrEmpty(command[0]))
        {
            //add commands to startup the different helpers, do all that stuff
            if (command[0] == "?")
            {
                if (command[1] == "sheet")
                {
                    Console.WriteLine("sheet <folderPath>: Opens the given folder and finds all its xml and png files" +
                        "\nThe program will then prompt the user to type one of these commands:" +
                        "\n- new <fileName>: create a new xml spritesheet based on the provided texture" +
                        "\n- edit <fileName>: open the given xml spritesheet file" +
                        "\n- delete <fileName>: delete the given xml spritesheet file"
                        );
                }
                if (command[1] == "ui")
                {
                    Console.WriteLine("ui <folderPath>: Opens the given folder and finds all its xml files" +
                        "\nThe program will then prompt the user to type one of these commands:" +
                        "\n- new <fileName>: create a new xml ui file based on the provided spritesheet xml" +
                        "\n- edit <fileName>: open the given xml ui file" +
                        "\n- delete <fileName>: delete the given xml ui file"
                        );
                }
                else
                {
                    Console.WriteLine("List of available commands:" +
                    "\n? <command> : Gives information about the specified command or opens this screen if none given" +
                    "\n- sheet <folderPath> : Handle spritesheet xml files (create, modifiy or destroy one)" +
                    "\n- ui <folderPath> : Handle UI xml files (create, modify or destroy one)" +
                    "\n- quit : exit the helper"
                    );
                }
            }
            else if (command[0] == "quit") { break; }
            else if (command[0] == "sheet")
            {
                state = 1;
                path = command[1];
            }
            else if (command[0] == "ui")
            {
                state = 2;
                path = command[1];
            }
            else
            {
                Console.WriteLine("ERROR: Command not found");
            }
        }
        else
        {
            Console.WriteLine("ERROR: Command not found");
        }
    }
    if (state == 1 || state == 2)
    {
        if (command[0] == "cancel") { state = 0; }
        else
        {
            DirectoryInfo folder = new(path);
            List<FileInfo> filesXml = new(folder.EnumerateFiles("*.xml"));
            List<FileInfo> filesPng = new(folder.EnumerateFiles("*.png"));
            if (command[1] == "new")
            {
                if (state == 1)
                    StartSheet(folder, path + @"\" + command[0], false);
                else
                    StartUI(folder, path + @"\" + command[0], false);
            }
            if (command[1] == "edit")
            {
                if (state == 1)
                    StartSheet(folder, path + @"\" + command[0], true);
                else
                    StartUI(folder, path + @"\" + command[0], true);
            }
            if (command[1] == "delete")
            {
                var file = filesXml.Find((FileInfo f) => { return f.Name == command[0]; } );
                if (file != null) { file.Delete();
                    Console.WriteLine($"Deleted {file.Name}"); }
                else { Console.WriteLine("ERROR: File not found, write its full name including extension"); }
            }
            Console.WriteLine("Found XML files:");
            foreach (var file in filesXml)
            {
                Console.WriteLine(file.Name);
            }
            Console.WriteLine("\nAnd PNG files:");
            foreach (var file in filesPng)
            {
                Console.WriteLine(file.Name);
            }
            Console.WriteLine("Input <fileName> <action> where valid actions are new, edit, and delete (type cancel to go back)");
        }
    }
}
void StartSheet(DirectoryInfo folder, string path, bool pathIsXml)
{
    #region PREPARATION
    Raylib.InitWindow(500, 500, "Texture Atlas Creator Helper");
    Raylib.SetTargetFPS(60);
    Raylib.SetExitKey(KeyboardKey.Null);

    //make sure old boots of the program dont leave anything behind (this is just in case the program exited with errors)
    DirectoryInfo dir = new(Raylib.GetApplicationDirectoryAsString() + "temp");
    dir?.Delete(true);

    DirectoryInfo temp = Directory.CreateDirectory(Raylib.GetApplicationDirectoryAsString() + "/temp"); //create a local temp folder

    TextureAtlas atlas = new();
    if (pathIsXml)
    {
        FileInfo file = new(path); //file has our xml
        FileInfo local = file.CopyTo(temp.FullName + "/opened.xml"); //we copy our xml to a local folder
        using (XmlReader reader = XmlReader.Create(temp.FullName + @"\opened.xml")) //we open the local file with an xml reader
        {
            atlas = TextureAtlas.ReadFromFile(reader); //doing this will fuck up the texture, we need to get that too and reassign
        }
        XDocument doc = XDocument.Load(temp.FullName + @"\opened.xml"); XElement root = doc.Root;
        string[] pathN = root.Element("Texture").Value.Split('/'); string textPath = pathN[pathN.Length - 1];
        FileInfo texture = file.Directory.GetFiles(textPath)[0]; //get the png
        FileInfo localPng = texture.CopyTo(temp.FullName + "/" + texture.Name); //copy png to local

        atlas.Texture = Raylib.LoadTexture("temp/" + localPng.Name);
    }
    else
    {
        FileInfo file = new(path); //this is the png file
        FileInfo local = file.CopyTo(temp.FullName + "/opened.png"); //we copy our png to a local folder
        atlas.Texture = Raylib.LoadTexture("temp/opened.png");
    }
    Vector2 windowSize = atlas.Texture.Dimensions + Vector2.One * 500;
    Raylib.SetWindowSize((int)windowSize.X, (int)windowSize.Y);
    Raylib.SetWindowPosition(200, 200);
    #endregion
    bool holding = false; Vector2 selectionStart = Vector2.NaN; Rectangle finalSelect = new(); RegionContextMenu menu = new();
    int changing = -1; string newValue = ""; bool autoRegions = false; string autoRegionSize = "";
    while (!Raylib.WindowShouldClose())
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.White);
        Raylib.DrawTexture(atlas.Texture, 1, 1, Color.White);

        foreach (TextureRegion reg in atlas.Regions.Values)
        {
            Raylib.DrawRectangleLinesEx(reg.Rect, 1, new Color(255, 0, 0, 100));
        }

        Vector2 mousePos = Raylib.GetMousePosition();
        if (Raylib.IsMouseButtonDown(MouseButton.Left)) //mouse held down
        {
            if (!holding) { holding = true; selectionStart = mousePos; }
            else
            {
                Vector2 start = selectionStart;
                Vector2 rectSize = mousePos - selectionStart;
                if (rectSize.X * rectSize.Y >= 0) { }
                if (rectSize.Y < 0)
                {
                    start += Vector2.UnitY * rectSize.Y; //put the start corner up, make the size go downwards (positive)
                    rectSize.Y = -rectSize.Y;
                }
                if (rectSize.X < 0)
                {
                    start += Vector2.UnitX * rectSize.X; //put the start corner left, make the size go right (positive)
                    rectSize.X = -rectSize.X;
                }
                Raylib.DrawRectangleV(start, rectSize, new Color(0, 150, 150, 100));
                finalSelect = new(start.X, start.Y, rectSize.X, rectSize.Y);
            }
        }
        else //this is made so that the info boxes of existing regions dont appear while selecting a new 
        {
            foreach (var reg in atlas.Regions)
            {
                if (Raylib.CheckCollisionPointRec(mousePos, reg.Value.Rect))
                {
                    Raylib.DrawRectangleV(mousePos, new Vector2(300, 60), new Color(100, 100, 100, 150));
                    Raylib.DrawText(reg.Key, (int)mousePos.X + 25, (int)mousePos.Y + 8, 25, Color.White);
                    Raylib.DrawText("Space: open menu", (int)mousePos.X + 25, (int)mousePos.Y + 35, 18, Color.White);

                    if (Raylib.IsMouseButtonDown(MouseButton.Right)) //delete region
                    {
                        atlas.RemoveRegion(reg.Key);
                    }
                    if (Raylib.IsKeyPressed(KeyboardKey.Space))
                    {
                        menu = new() { active = true, regName = reg.Key, rect = reg.Value.Rect };
                    }
                }
            }
        }
        if (Raylib.IsMouseButtonReleased(MouseButton.Left))
        {
            holding = false;
            if (finalSelect.Width > 2 && finalSelect.Height > 2)
            {
                atlas.Regions.Add($"NewRegion{selectionStart.X}{selectionStart.Y}", new(atlas.Texture, finalSelect)); selectionStart = Vector2.NaN;
            }
        }

        Raylib.DrawText("Press , to clear regions", 20, ((int)windowSize.Y) - 80, 20, Color.Black);
        if (Raylib.IsKeyPressed(KeyboardKey.Comma)) atlas.Clear(); 

        //auto regions
        Raylib.DrawText("Press . for auto regions", 20, ((int)windowSize.Y) - 50, 20, Color.Black);
        if (Raylib.IsKeyPressed(KeyboardKey.Period)) { autoRegions = !autoRegions; autoRegionSize = ""; }
        if (autoRegions)
        {
            char newChar = (char)Raylib.GetCharPressed();
            if ((char.IsDigit(newChar) || newChar == ',' ) && autoRegionSize.Length <= 8) { autoRegionSize += newChar; }
            Raylib.DrawText($"Regions Size (X,Y): {autoRegionSize}", 20, ((int)windowSize.Y) - 30, 20, Color.Black);
            if (Raylib.IsKeyPressed(KeyboardKey.Backspace) && autoRegionSize.Length > 0) { autoRegionSize = autoRegionSize.Remove(autoRegionSize.Length - 1); }
            if (Raylib.IsKeyPressed(KeyboardKey.Enter))
            {
                string[] size = autoRegionSize.Split(',');
                if (size.Length == 2)
                {
                    atlas.Clear();
                    int x = int.Parse(size[0]); int y = int.Parse(size[1]);
                    for (int i = 0; i < atlas.Texture.Width; i += x)
                    {
                        for (int j = 0; j < atlas.Texture.Height; j += y)
                        {
                            Rectangle rect = new(i, j, x, y);
                            if (rect.X + rect.Width > atlas.Texture.Width) rect.Width = atlas.Texture.Width - rect.X;
                            if (rect.Y + rect.Height > atlas.Texture.Height) rect.Height = atlas.Texture.Height - rect.Y;
                            if (rect.Width > 0 && rect.Height > 0)
                                atlas.AddRegion($"NewRegion{i}{j}", rect);
                        }
                    }
                }
                autoRegions = false; autoRegionSize = "";
            }
        }

        //draw region context menu if needed
        if (menu.active)
        {
            int[] menuPos = [(int)windowSize.X - 400, (int)windowSize.Y - 200];
            Raylib.DrawRectangle(menuPos[0], menuPos[1], 400, 200, new Color(100, 100, 100, 150));
            Color[] textColors = [Color.Black, Color.Black, Color.Black, Color.Black, Color.Black];

            //change values
            if (Raylib.IsKeyPressed(KeyboardKey.Tab))
            {
                changing++; //changing starts at -1
                newValue = "";
                if (changing == 5) changing = -1; //after editing all, go back to -1
            }
            if (changing != -1)
            {
                textColors[changing] = Color.Red;
                char newChar = (char)Raylib.GetCharPressed();
                if (changing == 0)
                {
                    if (char.IsAsciiLetterOrDigit(newChar) && newValue.Length <= 15) newValue += newChar;
                }
                else
                    if (char.IsDigit(newChar) && newValue.Length <= 5) newValue += newChar;
                if (Raylib.IsKeyPressed(KeyboardKey.Backspace) && newValue.Length > 0) { newValue = newValue.Remove(newValue.Length - 1); }
                if (Raylib.IsKeyPressed(KeyboardKey.Enter))  //asign the new value to the corresponding place
                {
                    TextureRegion reg = new(atlas.Regions[menu.regName]);
                    string newName = menu.regName;
                    switch (changing)
                    {
                        case 0:
                            newName = newValue;
                            atlas.RemoveRegion(menu.regName);
                            atlas.AddRegion(newValue, (int)reg.Rect.X, (int)reg.Rect.Y, (int)reg.Rect.Width, (int)reg.Rect.Height);
                            break;
                        case 1:
                            atlas.Regions[menu.regName].Rect.X = int.Parse(newValue);
                            break;
                        case 2:
                            atlas.Regions[menu.regName].Rect.Y = int.Parse(newValue);
                            break;
                        case 3:
                            atlas.Regions[menu.regName].Rect.Width = int.Parse(newValue);
                            break;
                        case 4:
                            atlas.Regions[menu.regName].Rect.Height = int.Parse(newValue);
                            break;
                    }
                    newValue = "";
                    menu = new() { active = true, regName = newName, rect = atlas.Regions[newName].Rect };
                }
                Raylib.DrawText($"NEW: {newValue}", menuPos[0] + 10, menuPos[1] + 145, 25, Color.Red);
            }

            Raylib.DrawText($"Name: {menu.regName}", menuPos[0] + 10, menuPos[1] + 10, 25, textColors[0]);
            Raylib.DrawText($"X: {menu.rect.X}", menuPos[0] + 10, menuPos[1] + 40, 25, textColors[1]);
            Raylib.DrawText($"Y: {menu.rect.Y}", menuPos[0] + 10, menuPos[1] + 65, 25, textColors[2]);
            Raylib.DrawText($"W: {menu.rect.Width}", menuPos[0] + 10, menuPos[1] + 90, 25, textColors[3]);
            Raylib.DrawText($"H: {menu.rect.Height}", menuPos[0] + 10, menuPos[1] + 115, 25, textColors[4]);
            Raylib.DrawText("Tab: edit", menuPos[0] + 10, menuPos[1] + 175, 18, Color.Black);

            if (Raylib.IsKeyPressed(KeyboardKey.Escape)) { menu = new(); changing = -1; newValue = ""; }
        }

        Raylib.EndDrawing();
    }
    Raylib.CloseWindow();
    Console.WriteLine("Do you wish to save the changes made? y/n: ");
    string save = Console.ReadLine();
    if (save == "y")
    {
        Console.WriteLine("Give the file a name: ");
        string name = Console.ReadLine();
        if (string.IsNullOrEmpty(name)) name = "OUTPUT.xml";
        name = name.EndsWith(".xml") ? name : name + ".xml";

        DirectoryInfo output = Directory.CreateDirectory(Raylib.GetApplicationDirectoryAsString() + "/OUT");
        Console.WriteLine($"Saving to {output.FullName + @"\" + name}," +
            $"\nplease remember to put it and its texture in a Assets/Sprites structure" +
            $"in the program root so that the xml can find its texture");
        TextureAtlas.WriteToFile(atlas, output.FullName + @"\" + name);
    }
    else
    {
        Console.WriteLine("Changes discarded.");
    }
}
void StartUI(DirectoryInfo folder, string path, bool xmlIsUi)
{
    #region PREPARATION
    Raylib.InitWindow(500, 500, "UI Page Creator Helper");
    Raylib.SetTargetFPS(60);
    Raylib.SetExitKey(KeyboardKey.Null);

    //make sure old boots of the program dont leave anything behind (this is just in case the program exited with errors)
    DirectoryInfo dir = new(Raylib.GetApplicationDirectoryAsString() + "temp");
    if (dir.Exists) dir?.Delete(true);
    DirectoryInfo otherDir = new(Raylib.GetApplicationDirectoryAsString() + "Assets");
    if (otherDir.Exists) otherDir?.Delete(true);

    DirectoryInfo temp = Directory.CreateDirectory(Raylib.GetApplicationDirectoryAsString() + "temp"); //create a local temp folder

    UIPage page = new();
    TextureAtlas atlas = new();
    string ATLASNAME = ""; //important later (go to the end of the file)
    if (xmlIsUi)
    {
        FileInfo file = new(path); //file has our xml
        FileInfo local = file.CopyTo(temp.FullName + "/opened.xml"); //we copy our xml to a local folder
        /*brief explanation here: the ui xml needs access to the atlas to be built, otherwise all panel elements will fail to be read
        //so what we do is first copy over the atlas xml in a directory matching the one asked by the ui xml, that way the atlas loads
        //and the panels can load too, so yeah, first we copy over the atlas xml, which also requires us to copy over its png, then
        //we can proceed to read the ui xml */
        DirectoryInfo atlasDir = Directory.CreateDirectory(Raylib.GetApplicationDirectoryAsString() + @"Assets\Sprites");
        using (XmlReader reader = XmlReader.Create(temp.FullName + "/opened.xml")) //we open the local file with an xml reader
        {
            XDocument atlasDoc = XDocument.Load(reader); XElement atlasRoot = atlasDoc.Root;
            string[] atlasPathN = atlasRoot.Element("Atlas").Value.Split('/'); string atlasTextPath = atlasPathN[atlasPathN.Length - 1];
            ATLASNAME = atlasTextPath;
            FileInfo atlasXml = file.Directory.Parent.GetDirectories("Sprites")[0].GetFiles(atlasTextPath)[0]; //get the atlas xml
            FileInfo localAtlas = atlasXml.CopyTo(atlasDir.FullName + "/" + atlasXml.Name); //copy atlas to local
            //now, same for the atlas' png, so we open the atlas xml
            using (XmlReader atlasReader = XmlReader.Create(atlasDir.FullName + "/" + atlasXml.Name))
            {
                XDocument textDoc = XDocument.Load(atlasReader); XElement textRoot = textDoc.Root;
                string[] textPathN = textRoot.Element("Texture").Value.Split('/'); string textTextPath = textPathN[textPathN.Length - 1];
                FileInfo texture = file.Directory.Parent.GetDirectories("Sprites")[0].GetFiles(textTextPath)[0]; //get the png
                FileInfo localPng = texture.CopyTo(atlasDir.FullName + "/" + texture.Name); //copy png to local
            }
            atlas = TextureAtlas.ReadFromFile(atlasDir.FullName + "/" + atlasXml.Name); //doesnt fuck up the texture bc its copied correctly
        }
        //finally, we can load the ui xml
        page = UIPage.ReadFromFile(temp.FullName + @"\opened.xml");
    }
    else
    {
        FileInfo file = new(path); //this is the atlas xml file
        ATLASNAME = file.Name;
        FileInfo local = file.CopyTo(temp.FullName + " /opened.xml"); //we copy our atlas xml to a local folder
        using (XmlReader reader = XmlReader.Create(temp.FullName + @"\opened.xml")) //we open the local file with an xml reader
        {
            atlas = TextureAtlas.ReadFromFile(reader); //doing this will fuck up the texture, we need to get that too and reassign
        }
        XDocument doc = XDocument.Load(temp.FullName + @"\opened.xml"); XElement root = doc.Root;
        string[] pathN = root.Element("Texture").Value.Split('/'); string textPath = pathN[pathN.Length - 1];
        FileInfo texture = file.Directory.GetFiles(textPath)[0]; //get the png
        FileInfo localPng = texture.CopyTo(temp.FullName + "/" + texture.Name); //copy png to local

        atlas.Texture = Raylib.LoadTexture("temp/" + localPng.Name);
    }
    //so, right now we have an atlas that holds the sprites for the ui, and a ui page (with or without content)
    Raylib.SetWindowSize((int)page.OGSize.X + 200, (int)page.OGSize.Y);
    Raylib.SetWindowPosition(200, 200);

    //BUILD THE ENTIRE UI OF THE PROGRAM
    Button labelBtt = new Button(new(page.OGSize.X + 10, 20), Color.White, new(120, 45), new Label(new(), Color.Black, "New label") { TextSize = 22 }) { BorderColor = Color.Black };
    labelBtt.Click += () => { page.uiElements.Add($"NewLabel{page.uiElements.Count}", new Label(new(300,300), Color.DarkPurple, "NEW LABEL")); };
    Button buttonBtt = new Button(new(page.OGSize.X + 10, 80), Color.White, new(140, 45), new Label(new(), Color.Black, "New Button") { TextSize = 22 }) { BorderColor = Color.Black };
    buttonBtt.Click += () => { page.uiElements.Add($"NewButton{page.uiElements.Count}", new Button(new(300, 300), Color.Orange, new(120, 50))); };

    UIPage programUI = new() { OGSize = page.OGSize + Vector2.One * 200, uiElements = new()
    {
        {"Label btt",  labelBtt},
        {"Button btt", buttonBtt }
    } };

    #endregion

    int changingWindowSize = 0; string newSize = ""; string openElement = ""; bool creatingPanel = false; string textureName = "";
    int changing = -1; string newValue = "";
    while (!Raylib.WindowShouldClose())
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.White);
        
        programUI.uiElements["Label btt"].Position = new(page.OGSize.X + 10, 20);
        programUI.uiElements["Button btt"].Position = new(page.OGSize.X + 10, 80);
        (programUI.uiElements["Label btt"] as Button).ButtonText.Position = new Vector2(page.OGSize.X + 10, 20) + Vector2.One * 10;
        (programUI.uiElements["Button btt"] as Button).ButtonText.Position = new Vector2(page.OGSize.X + 10, 80) + Vector2.One * 10;

        Dictionary<string, Rectangle> rects = new();
        page.Draw(); //draw ui into screen, add them to our rects and give them red outlines
        foreach (var elm in page.uiElements)
        {
            if (elm.Value.IsChild) continue;
            if (elm.Value is Button btt) { Raylib.DrawRectangleLinesEx(new(elm.Value.Position, btt.Size), 2, Color.Red);
                rects.Add(elm.Key, new(elm.Value.Position, btt.Size)); }
            if (elm.Value is Label) { Raylib.DrawRectangleLinesEx(new(elm.Value.Position, new(100, 50)), 2, Color.Red);
                rects.Add(elm.Key, new(elm.Value.Position, new(100, 50))); }
            if (elm.Value is Panel panel) { Raylib.DrawRectangleLinesEx(new(elm.Value.Position - panel.Sprite.Size / 2, panel.Sprite.Size), 2, Color.Red);
                rects.Add(elm.Key, new(elm.Value.Position - panel.Sprite.Size / 2, panel.Sprite.Size)); }
        }

        //THIS PART IS FOR INTERACTING WITH UI ELEMENTS
        Vector2 mousePos = Raylib.GetMousePosition();
        foreach (var rect in rects)
        {
            if (Raylib.CheckCollisionPointRec(mousePos, rect.Value))
            {
                Raylib.DrawRectangleV(mousePos, new Vector2(300, 60), new Color(100, 100, 100, 150));
                Raylib.DrawText(rect.Key, (int)mousePos.X + 25, (int)mousePos.Y + 8, 25, Color.White);
                Raylib.DrawText("Space: open menu", (int)mousePos.X + 25, (int)mousePos.Y + 35, 18, Color.White);

                if (Raylib.IsMouseButtonDown(MouseButton.Left)) //drag/assign element
                {
                    if (Raylib.IsKeyDown(KeyboardKey.LeftAlt)) //assign panel/label to inspected button
                    {
                        if (!string.IsNullOrEmpty(openElement))
                        {
                            if (page.uiElements[openElement] is Button BTT)
                            {
                                if (page.uiElements[rect.Key] is Panel panel)
                                {
                                    panel.Sprite.Scale = new Vector2(BTT.Size.X / panel.Sprite.Size.X, BTT.Size.Y / panel.Sprite.Size.Y);
                                    panel.Sprite.Rotation = 0; panel.IsChild = true; panel.Position = BTT.Position + panel.Sprite.Size / 2;
                                    BTT.Image = panel;
                                }
                                else if (page.uiElements[rect.Key] is Label label)
                                {
                                    label.IsChild = true; label.Position = BTT.Position + Vector2.One * 10;
                                    BTT.ButtonText = label;
                                }
                            }
                        }
                    }
                    else //when assigning the elm has to go to the button, not putting an else here doesnt let that happen
                    {
                        page.uiElements[rect.Key].Position = mousePos - Vector2.One * 30;
                        if (page.uiElements[rect.Key] is Button btt)
                        {
                            if (btt.ButtonText != null) { btt.ButtonText.Position = mousePos - Vector2.One * 30 + Vector2.One * 10; }
                            if (btt.Image != null) { btt.Image.Position = mousePos - Vector2.One * 30 + btt.Image.Sprite.Size / 2; }
                        }
                        else if (page.uiElements[rect.Key] is Panel) page.uiElements[rect.Key].Position = mousePos;
                    }
                }
                if (Raylib.IsMouseButtonDown(MouseButton.Right)) //delete region
                {
                    page.uiElements.Remove(rect.Key);
                    if (openElement == rect.Key) openElement = "";
                }
                if (Raylib.IsKeyPressed(KeyboardKey.Space))
                {
                    openElement = rect.Key;
                }
            }
        }
           
        //THIS PART IS FOR CHANGING THE SIZE OF WINDOW
        if (Raylib.IsKeyPressed(KeyboardKey.Period)) { newSize = ""; changingWindowSize = changingWindowSize == 2 ? 0 : changingWindowSize + 1; }
        if (changingWindowSize != 0)
        {
            char newChar = (char)Raylib.GetCharPressed();
            if (char.IsDigit(newChar) && newSize.Length <= 4) newSize += newChar;
            if (Raylib.IsKeyPressed(KeyboardKey.Backspace) && newSize.Length > 0) newSize = newSize.Remove(newSize.Length - 1);
            if (Raylib.IsKeyPressed(KeyboardKey.Enter))
            {
                int[] currentSize = [((int)page.OGSize.X), ((int)page.OGSize.Y)];
                if (changingWindowSize == 1) { Raylib.SetWindowSize(int.Parse(newSize) + 200, currentSize[1]);
                    page.OGSize = new(int.Parse(newSize), currentSize[1]); }
                else { Raylib.SetWindowSize(currentSize[0] + 200, int.Parse(newSize));
                    page.OGSize = new(currentSize[0], int.Parse(newSize)); }
            }
            Raylib.DrawText($"NEW WINDOW SIZE {(changingWindowSize == 1 ? "X" : "Y")}: {newSize}", 0, 0, 25, Color.Black);
        }

        //THIS PART IS FOR THE LEFT TOOLBAR
        Raylib.DrawRectangle((int)page.OGSize.X, 0, 200, (int)page.OGSize.Y, Color.Gray);
        Raylib.DrawText(", to create panel", ((int)page.OGSize.X) + 10, ((int)page.OGSize.Y) - 60, 20, Color.Black);
        Raylib.DrawText(". to change size", ((int)page.OGSize.X) + 10, ((int)page.OGSize.Y) - 40, 20, Color.Black);
        Raylib.DrawText("R Alt to reload ui", ((int)page.OGSize.X) + 10, ((int)page.OGSize.Y) - 20, 20, Color.Black);
        programUI.Update();
        programUI.Draw();

        //THIS PART IS FOR CREATING IMAGE PANELS (doesnt work if we are editing an element)
        if (Raylib.IsKeyPressed(KeyboardKey.Comma) && string.IsNullOrEmpty(openElement)) { creatingPanel = !creatingPanel; }
        if (creatingPanel)
        {
            char newChar = (char)Raylib.GetCharPressed();
            if (char.IsAsciiLetterOrDigit(newChar) && newSize.Length <= 4) textureName += newChar;
            if (Raylib.IsKeyPressed(KeyboardKey.Backspace) && textureName.Length > 0) textureName = textureName.Remove(textureName.Length - 1);
            if (Raylib.IsKeyPressed(KeyboardKey.Enter))
            {
                if (atlas.Regions.TryGetValue(textureName, out TextureRegion reg))
                {
                    page.uiElements.Add($"NewPanel{page.uiElements.Count}", new Panel(new(300, 300), Color.White, new(reg)));
                    creatingPanel = false; textureName = "";
                }
            }
            Raylib.DrawText($"Sprite name:\n{textureName}", ((int)page.OGSize.X) + 10, 150, 22, Color.Black);
        }

        //THIS PART IS FOR THE UI ELEMENT INSPECTOR
        if (!string.IsNullOrEmpty(openElement))
        {
            int[] menuPos = [(int)page.OGSize.X - 400, (int)page.OGSize.Y - 200];
            Raylib.DrawRectangle(menuPos[0], menuPos[1], 400, 200, new Color(100, 100, 100, 150));
            Color[] textColors = [Color.Black, Color.Black, Color.Black, Color.Black, Color.Black, Color.Black];
            if (changing != -1) textColors[changing] = Color.Red; //over here bc textColors overrides the number otherwise

            UIElement elm = page.uiElements[openElement];
            Raylib.DrawText($"Name: {openElement}", menuPos[0] + 5, menuPos[1] + 10, 22, textColors[0]);
            Raylib.DrawText($"X, Y: {elm.Position}", menuPos[0] + 5, menuPos[1] + 35, 22, textColors[1]);
            Raylib.DrawText($"RGB: <{elm.Color.R}, {elm.Color.G}, {elm.Color.B}>", menuPos[0] + 5, menuPos[1] + 60, 22, textColors[2]);

            int options = 0; //how many things of elm can you change
            if (elm is Label lab)
            {
                Raylib.DrawText($"Text: {lab.Text}", menuPos[0] + 5, menuPos[1] + 85, 22, textColors[3]);
                Raylib.DrawText($"Size: {lab.TextSize}", menuPos[0] + 5, menuPos[1] + 110, 22, textColors[4]);
                Raylib.DrawText($"Spacing: {lab.Spacing}", menuPos[0] + 5, menuPos[1] + 135, 22, textColors[5]);
                options = 6;
            }
            if (elm is Panel pan)
            {
                Raylib.DrawText($"Sprite: {atlas.GetRegionName(pan.Sprite)}", menuPos[0] + 5, menuPos[1] + 85, 22, textColors[3]);
                options = 4;
            }
            if (elm is Button btt)
            {
                Raylib.DrawText($"Size: <{btt.Size.X},{btt.Size.Y}>", menuPos[0] + 5, menuPos[1] + 85, 22, textColors[3]);
                Raylib.DrawText($"Border RGB: <{btt.BorderColor.R}, {btt.BorderColor.G}, {btt.BorderColor.B}>", menuPos[0] + 5, menuPos[1] + 110, 22, textColors[4]);
                Raylib.DrawText($"Highlight: {btt.HoveringHighlights}", menuPos[0] + 5, menuPos[1] + 135, 22, textColors[5]);
                Raylib.DrawText("ALT + CLICK A LABEL/IMAGE\nTO ASSIGN IT", menuPos[0] + 5, menuPos[1] + 160, 22, Color.Red);
                options = 6;
            }

            if (Raylib.IsKeyPressed(KeyboardKey.Escape)) { openElement = ""; changing = -1; newValue = ""; }
            //change values
            if (Raylib.IsKeyPressed(KeyboardKey.Tab))
            {
                changing++; //changing starts at -1
                newValue = "";
                if (changing == options) changing = -1;
            }
            if (changing != -1)
            {
                char newChar = (char)Raylib.GetCharPressed();
                if (changing == 0 || (changing == 3 && elm is not Button))
                {
                    if ((char.IsAsciiLetterOrDigit(newChar) || newChar == ',') && newValue.Length <= 15) newValue += newChar;
                }
                else
                    if ((char.IsDigit(newChar) || newChar == ',') && newValue.Length <= 12) newValue += newChar;
                if (Raylib.IsKeyPressed(KeyboardKey.Backspace) && newValue.Length > 0) newValue = newValue.Remove(newValue.Length - 1);
                Raylib.DrawText($"NEW: {newValue}", menuPos[0], menuPos[1] - 25, 25, Color.Red);
                if (Raylib.IsKeyPressed(KeyboardKey.Enter))
                {
                    if (changing == 0) //name (always)
                    {
                        UIElement prevELm = page.uiElements[openElement];
                        page.uiElements.Remove(openElement);
                        page.uiElements.Add(newValue, prevELm);
                        openElement = newValue;
                    }
                    else if (changing == 1) //X,Y (always)
                    {
                        string[] pos = newValue.Split(',');
                        page.uiElements[openElement].Position = new(int.Parse(pos[0]), int.Parse(pos[1]));
                    }
                    else if (changing == 2) //R,G,B (always)
                    {
                        string[] col = newValue.Split(',');
                        page.uiElements[openElement].Color = new Color(int.Parse(col[0]), int.Parse(col[1]), int.Parse(col[2]));
                    }
                    else if (changing == 3)
                    {
                        if (elm is Label)
                            (page.uiElements[openElement] as Label).Text = newValue;
                        else if (elm is Panel)
                        {
                            if (atlas.Regions.TryGetValue(newValue, out TextureRegion newRegion))
                            {
                                (page.uiElements[openElement] as Panel).Sprite = new(newRegion);
                            }
                        }
                        else if (elm is Button)
                        {
                            string[] size = newValue.Split(',');
                            (page.uiElements[openElement] as Button).Size = new(int.Parse(size[0]), int.Parse(size[1]));
                        }
                    }
                    else if (changing == 4)
                    {
                        if (elm is Label)
                            (page.uiElements[openElement] as Label).TextSize = int.Parse(newValue);
                        else
                        {
                            string[] col = newValue.Split(',');
                            (page.uiElements[openElement] as Button).BorderColor = new Color(int.Parse(col[0]), int.Parse(col[1]), int.Parse(col[2]));
                        }
                    }
                    else if (changing == 5)
                    {
                        if (elm is Label)
                            (page.uiElements[openElement] as Label).Spacing = int.Parse(newValue);
                        else
                        {
                            bool state = (page.uiElements[openElement] as Button).HoveringHighlights;
                            (page.uiElements[openElement] as Button).HoveringHighlights = !state;
                        }
                    }
                }
            }
        }

        //RELOADING UI
        if (Raylib.IsKeyPressed(KeyboardKey.RightAlt))
        {
            page.Dispose(); //otherwise copies of the same texture get store in memory and that is bad
            page = UIPage.ReadFromFile(temp.FullName + @"\opened.xml");
        }
        Raylib.EndDrawing();
    }
    Raylib.CloseWindow();
    //god i hope i can keep this file below 800 lines
    Console.WriteLine("Do you wish to save the changes made? y/n: ");
    string save = Console.ReadLine();
    if (save == "y")
    {
        Console.WriteLine("Give the file a name: ");
        string name = Console.ReadLine();
        if (string.IsNullOrEmpty(name)) name = "OUTPUT.xml";
        name = name.EndsWith(".xml") ? name : name + ".xml";

        DirectoryInfo output = Directory.CreateDirectory(Raylib.GetApplicationDirectoryAsString() + "/OUT");
        Console.WriteLine($"Saving to {output.FullName + @"\" + name}," +
            $"\nplease remember to put this in the proper file structure of the" +
            $"program root so that this xml can find its atlas and texture");
        UIPage.WriteToFile(page, output.FullName + @"\" + name, ATLASNAME, atlas);
    }
    else
    {
        Console.WriteLine("Changes discarded.");
    }
}
struct RegionContextMenu
{
    public bool active = false; public string regName = ""; public Rectangle rect = new(); public RegionContextMenu() { }
}