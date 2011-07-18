using System.ComponentModel;
using System.Drawing.Design;
using System.Xml.Serialization;
using CustomUITypeEditors;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Windows.Forms;

namespace GLEED2D
{
    public partial class CircleItem
    {

        [DisplayName("Radius"), Category(" General")]
        [XmlIgnore()]
        public float pRadius { get { return Radius; } set { Radius = value; } }

        [DisplayName("FillColor"), Category(" General")]
        [Editor(typeof(XNAColorUITypeEditor), typeof(UITypeEditor))]
        [XmlIgnore()]
        public Color pFillColor { get { return FillColor; } set { FillColor = value; } }


        public CircleItem(Vector2 startpos, float radius)
            : base()
        {
            this.Position = startpos;
            this.Radius = radius;
            this.FillColor = Constants.Instance.ColorPrimitives;
        }

        public override Item clone()
        {
            CircleItem result = (CircleItem)this.MemberwiseClone();
            result.CustomProperties = new SerializableDictionary(CustomProperties);
            result.hovering = false;
            return result;
        }

        public override string getNamePrefix()
        {
            return "Circle_";
        }

        public override bool contains(Vector2 worldpos)
        {
            return (worldpos - Position).Length() <= Radius;
        }


        public override void OnTransformed()
        {
        }


        public override void onMouseButtonDown(Vector2 mouseworldpos)
        {
            hovering = false;
            MainForm.Instance.pictureBox1.Cursor = Cursors.SizeAll;
            base.onMouseButtonDown(mouseworldpos);
        }


        public override bool CanScale()
        {
            return true;
        }

        public override Vector2 getScale()
        {
            return new Vector2(pRadius, pRadius);
        }

        public override void setScale(Vector2 scale)
        {
            pRadius = (float)Math.Round(scale.X);
        }

        public override void drawInEditor(SpriteBatch sb)
        {
            if (!Visible) return;
            Color c = FillColor;
            if (hovering && Constants.Instance.EnableHighlightOnMouseOver) c = Constants.Instance.ColorHighlight;
            Primitives.Instance.drawCircleFilled(sb, Position, Radius, c);
        }


        public override void drawSelectionFrame(SpriteBatch sb, Matrix matrix, Color color)
        {

            Vector2 transformedPosition = Vector2.Transform(Position, matrix);
            Vector2 transformedRadius = Vector2.TransformNormal(Vector2.UnitX * Radius, matrix);
            Primitives.Instance.drawCircle(sb, transformedPosition, transformedRadius.Length(), color, 2);

            Vector2[] extents = new Vector2[4];
            extents[0] = transformedPosition + Vector2.UnitX * transformedRadius.Length();
            extents[1] = transformedPosition + Vector2.UnitY * transformedRadius.Length();
            extents[2] = transformedPosition - Vector2.UnitX * transformedRadius.Length();
            extents[3] = transformedPosition - Vector2.UnitY * transformedRadius.Length();

            foreach (Vector2 p in extents)
            {
                Primitives.Instance.drawCircleFilled(sb, p, 4, color);
            }

            Vector2 origin = Vector2.Transform(pPosition, matrix);
            Primitives.Instance.drawBoxFilled(sb, origin.X - 5, origin.Y - 5, 10, 10, color);

        }

    }
}
