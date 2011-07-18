using System.ComponentModel;
using System.Drawing.Design;
using System.Xml.Serialization;
using CustomUITypeEditors;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Windows.Forms;
using System;

namespace GLEED2D
{
    public partial class PathItem
    {
        [DisplayName("IsPolygon"), Category(" General")]
        [Description("If true, the Path will be drawn as a polygon (last and first point will be connected).")]
        [XmlIgnore()]
        public bool pIsPolygon { get { return IsPolygon; } set { IsPolygon = value; } }

        [DisplayName("LineWidth"), Category(" General")]
        [Description("The line width of this path. Can be used for rendering.")]
        [XmlIgnore()]
        public int pLineWidth { get { return LineWidth; } set { LineWidth = value; } }

        [DisplayName("LineColor"), Category(" General")]
        [Description("The line color of this path. Can be used for rendering.")]
        [Editor(typeof(XNAColorUITypeEditor), typeof(UITypeEditor))]
        [XmlIgnore()]
        public Color pLineColor { get { return LineColor; } set { LineColor = value; } }



        int pointundermouse = -1;
        int pointgrabbed = -1;
        Vector2 initialpos;

        public PathItem(Vector2[] points)
            : base()
        {
            Position = points[0];
            WorldPoints = points;
            LocalPoints = (Vector2[])points.Clone();
            for (int i = 0; i < LocalPoints.Length; i++) LocalPoints[i] -= Position;
            LineWidth = Constants.Instance.DefaultPathItemLineWidth;
            LineColor = Constants.Instance.ColorPrimitives;
        }

        public override Item clone()
        {
            PathItem result = (PathItem)this.MemberwiseClone();
            result.CustomProperties = new SerializableDictionary(CustomProperties);
            result.LocalPoints = (Vector2[])this.LocalPoints.Clone();
            result.WorldPoints = (Vector2[])this.WorldPoints.Clone();
            result.hovering = false;
            return result;
        }

        public override string getNamePrefix()
        {
            return "Path_";
        }

        public override bool contains(Vector2 worldpos)
        {
            for (int i = 1; i < WorldPoints.Length; i++)
            {
                if (worldpos.DistanceToLineSegment(WorldPoints[i], WorldPoints[i-1]) <= LineWidth) return true;
            }
            if (IsPolygon)
                if (worldpos.DistanceToLineSegment(WorldPoints[0], WorldPoints[WorldPoints.Length-1]) <= LineWidth) return true;
            return false;
        }

        /// <summary>
        /// Calculates the WorldPoints based on Position and LocalPoints
        /// </summary>
        public override void OnTransformed()
        {
            for (int i = 0; i < WorldPoints.Length; i++) WorldPoints[i] = LocalPoints[i] + Position;
        }


        public override void onMouseOver(Vector2 mouseworldpos)
        {
            pointundermouse = -1;
            for (int i = 0; i < WorldPoints.Length; i++)
            {
                if (mouseworldpos.DistanceTo(WorldPoints[i]) <= 5)
                {
                    pointundermouse = i;
                    MainForm.Instance.pictureBox1.Cursor = Cursors.Hand;
                    MainForm.Instance.toolStripStatusLabel1.Text = Name + " (Point " + i.ToString() + ": " + WorldPoints[i].ToString() + ")";
                }
            }
            if (pointundermouse == -1) MainForm.Instance.pictureBox1.Cursor = Cursors.Default;
            base.onMouseOver(mouseworldpos);
        }

        public override void onMouseOut()
        {
            base.onMouseOut();
        }

        public override void onMouseButtonDown(Vector2 mouseworldpos)
        {
            hovering = false;
            if (pointundermouse >= 0)
            {
                pointgrabbed = pointundermouse;
                initialpos = WorldPoints[pointundermouse];
            }
            else MainForm.Instance.pictureBox1.Cursor = Cursors.SizeAll;
            base.onMouseButtonDown(mouseworldpos);
        }

        public override void onMouseButtonUp(Vector2 mouseworldpos)
        {
            if (pointgrabbed == 0)
            {
                LocalPoints[0] = Vector2.Zero;
                for (int i = 1; i < LocalPoints.Length; i++)
                {
                    LocalPoints[i] = WorldPoints[i] - WorldPoints[0];
                }
                Position = WorldPoints[0];
                OnTransformed();
                MainForm.Instance.propertyGrid1.Refresh();
            }

            pointgrabbed = -1;
            base.onMouseButtonUp(mouseworldpos);
        }

        public override void setPosition(Vector2 pos)
        {
            if (pointgrabbed >= 0)
            {
                LocalPoints[pointgrabbed] = initialpos + pos - Position * 2;
                OnTransformed();
                MainForm.Instance.toolStripStatusLabel1.Text = Name + " (Point " + pointgrabbed.ToString() + ": " + WorldPoints[pointgrabbed].ToString() + ")";
            }
            else base.setPosition(pos);
        }


        public override bool CanRotate()
        {
            return true;
        }

        public override float getRotation()
        {
            return (float)Math.Atan2(LocalPoints[1].Y, LocalPoints[1].X);
        }

        public override void setRotation(float rotation)
        {
            float current = (float)Math.Atan2(LocalPoints[1].Y, LocalPoints[1].X);
            float delta = rotation - current;

            Matrix matrix = Matrix.CreateRotationZ(delta);

            for (int i = 1; i < LocalPoints.Length; i++)
            {
                LocalPoints[i] = Vector2.Transform(LocalPoints[i], matrix);
            }
            OnTransformed();
        }


        public override bool CanScale()
        {
            return true;
        }

        public override Vector2 getScale()
        {
            float length = (LocalPoints[1] - LocalPoints[0]).Length();
            return new Vector2(length, length);
        }

        public override void setScale(Vector2 scale)
        {
            float factor = scale.X / (LocalPoints[1] - LocalPoints[0]).Length();
            for (int i = 1; i < LocalPoints.Length; i++)
            {
                Vector2 olddistance = LocalPoints[i] - LocalPoints[0];
                LocalPoints[i] = LocalPoints[0] + olddistance * factor;
            }
            OnTransformed();
        }

        public override void drawInEditor(SpriteBatch sb)
        {
            if (!Visible) return;
            Color c = LineColor;
            if (hovering && Constants.Instance.EnableHighlightOnMouseOver) c = Constants.Instance.ColorHighlight;

            if (IsPolygon)
                Primitives.Instance.drawPolygon(sb, WorldPoints, c, LineWidth);
            else
                Primitives.Instance.drawPath(sb, WorldPoints, c, LineWidth);

        }

        public override void drawSelectionFrame(SpriteBatch sb, Matrix matrix, Color color)
        {
            Vector2[] transformedPoints = new Vector2[WorldPoints.Length];
            Vector2.Transform(WorldPoints, ref matrix, transformedPoints);

            if (IsPolygon)
                Primitives.Instance.drawPolygon(sb, transformedPoints, color, 2);
            else
                Primitives.Instance.drawPath(sb, transformedPoints, color, 2);

            foreach (Vector2 p in transformedPoints)
            {
                Primitives.Instance.drawCircleFilled(sb, p, 4, color);
            }

            Primitives.Instance.drawBoxFilled(sb, transformedPoints[0].X - 5, transformedPoints[0].Y - 5, 10, 10, color);


        }

    }
}
