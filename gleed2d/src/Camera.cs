using Microsoft.Xna.Framework;

namespace GLEED2D
{
    public class Camera
    {
        Vector2 position;
        public Vector2 Position
        {
            get { 
                return position; 
            }
            set { 
                position = value;
                updatematrix();
            }
        }

        float rotation;
        public float Rotation
        {
            get
            {
                return rotation;
            }
            set
            {
                rotation = value;
                updatematrix();
            }
        }

        float scale;
        public float Scale
        {
            get
            {
                return scale;
            }
            set
            {
                scale = value;
                updatematrix();
            }
        }

        public Matrix matrix;
        Vector2 viewport;                //width and height of the viewport


        public Camera(float width, float height)
        {
            position = Vector2.Zero;
            rotation = 0;
            scale = 1.0f;
            viewport = new Vector2(width, height);
            updatematrix();
        }

        void updatematrix()
        {
            matrix = Matrix.CreateTranslation(-position.X, -position.Y, 0.0f) *
                     Matrix.CreateRotationZ(rotation) *
                     Matrix.CreateScale(scale) *
                     Matrix.CreateTranslation(viewport.X / 2, viewport.Y / 2, 0.0f);
        }

        public void updateviewport(float width, float height)
        {
            viewport.X = width;
            viewport.Y = height;
            updatematrix();
        }

    }
}
