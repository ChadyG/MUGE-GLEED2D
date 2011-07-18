using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;

namespace GLEED2D
{

    class Brush
    {
        public String fullpath;
        public Texture2D texture;

        public Brush(String fullpath)
        {
            this.fullpath = fullpath;
            this.texture = TextureLoader.Instance.FromFile(Game1.Instance.GraphicsDevice, fullpath);
        }
    }



}
