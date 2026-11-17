using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Text;

namespace EklipseLib
{
    public class Scene
    {
        //in the future, this could include more stuff if i expand to things outside UI (a world with sprites, physics, etc)
        public static UIPage Page { get; protected set; }
        public Scene(UIPage startupPage)
        {
            ChangePage(startupPage);
        }
        public virtual void Update()
        {
            Page.Update();
        }
        public virtual void Draw()
        {
            Raylib.ClearBackground(Page.Background);
            Page.Draw();
        }
        public virtual void ChangePage(UIPage newPage)
        {
            Page?.Dispose();
            Page = newPage;
            while (Page.FinalSize.X > Raylib.GetScreenWidth() || Page.FinalSize.Y > Raylib.GetScreenHeight())
            {
                //ui page is too big for the screen, scale it down until it fits
                Page.Scale--;
            }
            Raylib.SetWindowSize((int)Page.OGSize.X, (int)Page.OGSize.Y);
            Page.PageResized();
        }
    }
}