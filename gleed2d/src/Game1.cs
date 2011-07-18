using System;
using System.Collections.Generic;
using System.Linq;
using Forms = System.Windows.Forms;
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
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        public SpriteBatch spriteBatch;
        GamePadState gamepadstate, oldgamepadstate;
        KeyboardState keyboardstate, oldkeyboardstate;
        public Forms.Form winform;
        private IntPtr drawSurface;


        public static Game1 Instance;

        public Game1(IntPtr drawSurface)
        {
            Logger.Instance.log("Game1 creation started.");

            Instance = this;

            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = 800;
            graphics.PreferredBackBufferHeight = 600;

            Content.RootDirectory = "Content";

            Logger.Instance.log("Creating Winform.");
            this.drawSurface = drawSurface;
            graphics.PreparingDeviceSettings += new EventHandler<PreparingDeviceSettingsEventArgs>(graphics_PreparingDeviceSettings);
            winform = (Forms.Form)Forms.Form.FromHandle(Window.Handle);
            winform.VisibleChanged += new EventHandler(Game1_VisibleChanged);
            winform.Size = new System.Drawing.Size(10, 10);
            Mouse.WindowHandle = drawSurface;
            resizebackbuffer(MainForm.Instance.pictureBox1.Width, MainForm.Instance.pictureBox1.Height);
            winform.Hide();
            Logger.Instance.log("Winform created.");

            Logger.Instance.log("Game1 creation ended.");
        }

        void graphics_PreparingDeviceSettings(object sender, PreparingDeviceSettingsEventArgs e)
        {
            e.GraphicsDeviceInformation.PresentationParameters.DeviceWindowHandle = drawSurface;
        }
        private void Game1_VisibleChanged(object sender, EventArgs e)
        {
            winform.Hide();
            winform.Size = new System.Drawing.Size(10, 10);
            winform.Visible = false;
        }


        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            Logger.Instance.log("Creating Editor object.");

            new Editor();

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        public void resizebackbuffer(int width, int height)
        {
            graphics.PreferredBackBufferWidth = width;
            graphics.PreferredBackBufferHeight = height;
            graphics.ApplyChanges();
        }


        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {

        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (!MainForm.Instance.pictureBox1.ContainsFocus) return;

            gamepadstate = GamePad.GetState(PlayerIndex.One);
            if (gamepadstate.IsConnected)
            {
                if (gamepadstate.Buttons.Back == ButtonState.Pressed) this.Exit();

            }
            keyboardstate = Keyboard.GetState();

           
            Editor.Instance.update(gameTime);

            oldgamepadstate = gamepadstate;
            oldkeyboardstate = keyboardstate;
            base.Update(gameTime);
        }


        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            float fps = 1 / (float)gameTime.ElapsedGameTime.TotalSeconds;
            MainForm.Instance.toolStripStatusLabel4.Text = "FPS: " + fps.ToString("#0.00");


            Editor.Instance.draw(spriteBatch);

            base.Draw(gameTime);
            
        }

    }
}
