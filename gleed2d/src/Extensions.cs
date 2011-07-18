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
using System.Drawing.Design;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
using System.Windows.Forms.Design;
using System.Reflection;


namespace GLEED2D
{
    static class Extensions
    {


        public static Vector2 Round(this Vector2 v)
        {
            //v.X = (float)Math.Round(v.X);
            //v.Y = (float)Math.Round(v.Y);
            //return v;
            return new Vector2((float)Math.Round(v.X), (float)Math.Round(v.Y));
        }

        public static Point ToPoint(this Vector2 v)
        {
            return new Point((int)Math.Round(v.X), (int)Math.Round(v.Y));
        }

        public static Vector2 ToVector2(this Point p)
        {
            return new Vector2(p.X, p.Y);
        }

        public static float DistanceTo(this Vector2 v0, Vector2 v)
        {
            return (v - v0).Length();
        }

        public static float DistanceToLineSegment(this Vector2 v, Vector2 a, Vector2 b)
        {
            Vector2 x = b - a;
            x.Normalize();
            float t = Vector2.Dot(x, v - a);
            if (t < 0) return (a - v).Length();
            float d = (b - a).Length();
            if (t > d) return (b - v).Length();
            return (a + x * t - v).Length();

        }

        public static Rectangle Transform(this Rectangle r, Matrix m)
        {
            Vector2[] poly = new Vector2[2];
            poly[0] = new Vector2(r.Left, r.Top);
            poly[1] = new Vector2(r.Right, r.Bottom);
            Vector2[] newpoly = new Vector2[2];
            Vector2.Transform(poly, ref m, newpoly);

            Rectangle result = new Rectangle();
            result.Location = newpoly[0].ToPoint();
            result.Width = (int)(newpoly[1].X - newpoly[0].X);
            result.Height = (int)(newpoly[1].Y - newpoly[0].Y);
            return result;
        }

        /// <summary>
        /// Convert the Rectangle to an array of Vector2 containing its 4 corners.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="m"></param>
        /// <returns>An array of Vector2 representing the rectangle's corners starting from top/left and going clockwise.</returns>
        public static Vector2[] ToPolygon(this Rectangle r)
        {
            Vector2[] poly = new Vector2[4];
            poly[0] = new Vector2(r.Left, r.Top);
            poly[1] = new Vector2(r.Right, r.Top);
            poly[2] = new Vector2(r.Right, r.Bottom);
            poly[3] = new Vector2(r.Left, r.Bottom);
            return poly;
        }

        public static Rectangle RectangleFromVectors(Vector2 v1, Vector2 v2)
        {
            Vector2 distance = v2 - v1;
            Rectangle result = new Rectangle();
            result.X = (int)Math.Min(v1.X, v2.X);
            result.Y = (int)Math.Min(v1.Y, v2.Y);
            result.Width = (int)Math.Abs(distance.X);
            result.Height = (int)Math.Abs(distance.Y);
            return result;
        }

    }




}
