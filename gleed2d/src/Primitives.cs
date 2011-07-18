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
    class Primitives
    {
        private static Primitives instance;
        public static Primitives Instance
        {
            get
            {
                if (instance == null) instance = new Primitives();
                return instance;
            }
        }

        Texture2D pixel;
        Texture2D circle;
        const int circleTextureRadius = 512;


        public Primitives()
        {
            pixel = new Texture2D(Game1.Instance.GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });
            circle = CreateCircleTexture(Game1.Instance.GraphicsDevice, circleTextureRadius, 0, 1, 1, Color.White, Color.White);
        }

        public Texture2D CreateCircleTexture(GraphicsDevice graphicsDevice, int radius, int borderWidth,
                                                    int borderInnerTransitionWidth, int borderOuterTransitionWidth,
                                                    Color color, Color borderColor)
        {
            int diameter = radius * 2;
            Vector2 center = new Vector2(radius, radius);
            
            
            Texture2D circle = new Texture2D(graphicsDevice, diameter, diameter, 1, TextureUsage.None, SurfaceFormat.Color);
            Color[] colors = new Color[diameter * diameter];
            int y = -1;
            for (int i = 0; i < colors.Length; i++)
            {
                int x = i % diameter;

                if (x == 0)
                {
                    y += 1;
                }

                Vector2 diff = new Vector2(x, y) - center;
                float length = diff.Length(); // distance.Length();

                if (length > radius)
                {
                    colors[i] = Color.TransparentBlack;
                }
                else if (length >= radius - borderOuterTransitionWidth)
                {
                    float transitionAmount = (length - (radius - borderOuterTransitionWidth)) / borderOuterTransitionWidth;
                    transitionAmount = 255 * (1 - transitionAmount);
                    colors[i] = new Color(borderColor.R, borderColor.G, borderColor.B, (byte)transitionAmount);
                }
                else if (length > radius - (borderWidth + borderOuterTransitionWidth))
                {
                    colors[i] = borderColor;
                }
                else if (length >= radius - (borderWidth + borderOuterTransitionWidth + borderInnerTransitionWidth))
                {
                    float transitionAmount = (length -
                                              (radius -
                                               (borderWidth + borderOuterTransitionWidth + borderInnerTransitionWidth))) /
                                             (borderInnerTransitionWidth + 1);
                    colors[i] = new Color((byte)MathHelper.Lerp(color.R, borderColor.R, transitionAmount),
                                          (byte)MathHelper.Lerp(color.G, borderColor.G, transitionAmount),
                                          (byte)MathHelper.Lerp(color.B, borderColor.B, transitionAmount));
                }
                else
                {
                    colors[i] = color;
                }
            }
            circle.SetData(colors);
            return circle;
        }




        public void drawPixel(SpriteBatch sb, int x, int y, Color c)
        {
            sb.Draw(pixel, new Vector2(x, y), c);
        }


        public void drawBox(SpriteBatch sb, Rectangle r, Color c, int linewidth)
        {
            drawLine(sb, r.Left, r.Top, r.Right, r.Top, c, linewidth);
            drawLine(sb, r.Right, r.Y, r.Right, r.Bottom, c, linewidth);
            drawLine(sb, r.Right, r.Bottom, r.Left, r.Bottom, c, linewidth);
            drawLine(sb, r.Left, r.Bottom, r.Left, r.Top, c, linewidth);
        }


        public void drawBoxFilled(SpriteBatch sb, float x, float y, float w, float h, Color c)
        {
            sb.Draw(pixel, new Rectangle((int)x, (int)y, (int)w, (int)h), c);
        }

        public void drawBoxFilled(SpriteBatch sb, Vector2 upperLeft, Vector2 lowerRight, Color c)
        {
            Rectangle r = Extensions.RectangleFromVectors(upperLeft, lowerRight);
            sb.Draw(pixel, r, c);
        }

        public void drawBoxFilled(SpriteBatch sb, Rectangle r, Color c)
        {
            sb.Draw(pixel, r, c);
        }

        public void drawCircle(SpriteBatch sb, Vector2 position, float radius, Color c, int linewidth)
        {
            drawPolygon(sb, makeCircle(position, radius, 32), c, linewidth);
        }

        public void drawCircleFilled(SpriteBatch sb, Vector2 position, float radius, Color c)
        {
            sb.Draw(circle, position, null, c, 0, new Vector2(circleTextureRadius, circleTextureRadius), radius / circleTextureRadius, SpriteEffects.None, 0);
        }


        public void drawLine(SpriteBatch sb, float x1, float y1, float x2, float y2, Color c, int linewidth)
        {
            Vector2 v = new Vector2(x2 - x1, y2 - y1);
            float rot = (float)Math.Atan2(y2 - y1, x2 - x1);
            sb.Draw(pixel, new Vector2(x1, y1), new Rectangle(1, 1, 1, linewidth), c, rot,
                new Vector2(0, linewidth / 2), new Vector2(v.Length(), 1), SpriteEffects.None, 0);
        }
        public void drawLine(SpriteBatch sb, Vector2 startpos, Vector2 endpos, Color c, int linewidth)
        {
            drawLine(sb, startpos.X, startpos.Y, endpos.X, endpos.Y, c, linewidth);
        }


        public void drawPath(SpriteBatch sb, Vector2[] points, Color c, int linewidth)
        {
            for (int i = 0; i < points.Length - 1; i++)
            {
                drawLine(sb, points[i], points[i + 1], c, linewidth);
            }
        }

        public void drawPolygon(SpriteBatch sb, Vector2[] points, Color c, int linewidth)
        {
            drawPath(sb, points, c, linewidth);
            drawLine(sb, points[points.Length-1], points[0], c, linewidth);
        }


        public Vector2[] makeCircle(Vector2 position, float radius, int numpoints)
        {
            Vector2[] polygon = new Vector2[numpoints];
            float angle = 0;
            for (int i = 0; i < numpoints; i++)
            {
                float x = (float)Math.Cos(angle) * radius;
                float y = (float)Math.Sin(angle) * radius;
                polygon[i] = position + new Vector2(x, y);
                angle += MathHelper.TwoPi / (float)numpoints;
            }
            return polygon;
        }



    }
}
