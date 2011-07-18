using System;
using System.IO;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Xml;
using System.Text;
using System.ComponentModel;


namespace GLEED2D
{
    public partial class Layer
    {

        [XmlIgnore()]
        [DisplayName("ScrollSpeed"), Category(" General")]
        [Description("The Scroll Speed relative to the main camera. The X and Y components are interpreted as factors, " +
        "so Vector2.One means same scrolling speed as the main camera. To be used for parallax scrolling.")]
        public Vector2 pScrollSpeed
        {
            get
            {
                return ScrollSpeed;
            }
            set
            {
                ScrollSpeed = value;
            }
        }


        [XmlIgnore]
        public Level level;

        [DisplayName("ZPos"), Category(" General")]
        [Description("Gosu ZPos")]
        public int ZPos { get; set; }

        public Layer(String name) : this()
        {
            this.Name = name;
            this.Visible = true;
        }

        public Layer clone()
        {
            Layer result = (Layer)this.MemberwiseClone();
            result.Items = new List<Item>(Items);
            for (int i = 0; i < result.Items.Count; i++)
            {
                result.Items[i] = result.Items[i].clone();
                result.Items[i].layer = result;
            }
            return result;
        }



        public Item getItemAtPos(Vector2 mouseworldpos)
        {
            for (int i = Items.Count - 1; i >= 0; i--)
            {
                if (Items[i].contains(mouseworldpos) && Items[i].Visible) return Items[i];
            }
            return null;
        }

        public void drawInEditor(SpriteBatch sb)
        {
            if (!Visible) return;
            foreach (Item item in Items) item.drawInEditor(sb);
        }



    }
}
