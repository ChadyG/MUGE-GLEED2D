using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using GLEED2D.Properties;
using Forms = System.Windows.Forms;


namespace GLEED2D
{
    enum EditorState
    {
        idle,
        brush,          //"stamp mode": user double clicked on an item to add multiple instances of it
        cameramoving,   //user is moving the camera
        moving,         //user is moving an item
        rotating,       //user is rotating an item
        scaling,        //user is scaling an item
        selecting,      //user has opened a select box by dragging the mouse (windows style)
        brush_primitive //use is adding a primitive item
    }

    enum PrimitiveType
    {
        Rectangle, Circle, Path
    }

    class Editor
    {
        public static Editor Instance;
        EditorState state;
        Brush currentbrush;
        PrimitiveType currentprimitive;
        bool primitivestarted;
        List<Vector2> clickedPoints = new List<Vector2>();
        Layer selectedlayer;
        public Layer SelectedLayer
        {
            get { return selectedlayer; }
            set {
                selectedlayer = value;
                if (value==null) MainForm.Instance.toolStripStatusLabel2.Text = "Layer: (none)";
                    else MainForm.Instance.toolStripStatusLabel2.Text = "Layer: " + selectedlayer.Name;
            }
        }
        Item lastitem;
        public List<Item> SelectedItems;
        Rectangle selectionrectangle = new Rectangle();
        Vector2 mouseworldpos, grabbedpoint, initialcampos, newPosition;       
        List<Vector2> initialpos;                   //position before user interaction
        List<float> initialrot;                     //rotation before user interaction
        List<Vector2> initialscale;                 //scale before user interaction
        public Level level;
        public Camera camera;
        KeyboardState kstate, oldkstate;
        MouseState mstate, oldmstate;
        Forms.Cursor cursorRot, cursorScale, cursorDup;
        Stack<Command> undoBuffer = new Stack<Command>();
        Stack<Command> redoBuffer = new Stack<Command>();
        bool commandInProgress = false;
        public Texture2D dummytexture;
        bool drawSnappedPoint = false;
        Vector2 posSnappedPoint = Vector2.Zero;
        public string Version;


        public Editor()
        {
            Logger.Instance.log("Editor creation started.");

            Instance = this;
            state = EditorState.idle;

            SelectedItems = new List<Item>();
            initialpos = new List<Vector2>();
            initialrot = new List<float>();
            initialscale = new List<Vector2>();

            Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

            Logger.Instance.log("Loading Resources.");
            Stream resStream;
            resStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("GLEED2D.Resources.cursors.dragcopy.cur");
            cursorDup = new Forms.Cursor(resStream);
            resStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("GLEED2D.Resources.cursors.rotate.cur");
            cursorRot = new Forms.Cursor(resStream);
            resStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("GLEED2D.Resources.cursors.scale.cur");
            cursorScale = new Forms.Cursor(resStream);
            resStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("GLEED2D.Resources.circle.png");
            dummytexture = Texture2D.FromFile(Game1.Instance.GraphicsDevice, resStream);
            Logger.Instance.log("Resources loaded.");

            Logger.Instance.log("Loading Settings.");
            Constants.Instance.import("settings.xml");
            MainForm.Instance.ViewGrid.Checked = Constants.Instance.ShowGrid;
            MainForm.Instance.ViewSnapToGrid.Checked = Constants.Instance.SnapToGrid;
            MainForm.Instance.ViewWorldOrigin.Checked = Constants.Instance.ShowWorldOrigin;
            Logger.Instance.log("Settings loaded.");

            Logger.Instance.log("Creating new level.");
            MainForm.Instance.newLevel();
            Logger.Instance.log("New level created.");

            Logger.Instance.log("Editor creation ended.");
        }


        public void getSelectionFromLevel()
        {
            SelectedItems.Clear();
            SelectedLayer = null;
            string[] itemnames = level.selecteditems.Split(';');
            foreach (string itemname in itemnames)
            {
                if (itemname.Length>0) SelectedItems.Add(level.getItemByName(itemname));
            }
            SelectedLayer = level.getLayerByName(level.selectedlayers);
        }

        public void selectlevel()
        {
            MainForm.Instance.propertyGrid1.SelectedObject = level;
        }

        public void addLayer(Layer l)
        {
            l.level = level;
            if (!l.level.Layers.Contains(l)) l.level.Layers.Add(l);
        }

        public void deleteLayer(Layer l)
        {
            if (level.Layers.Count > 0)
            {
                Editor.Instance.beginCommand("Delete Layer \"" + l.Name + "\"");
                level.Layers.Remove(l);
                Editor.Instance.endCommand();
            }
            if (level.Layers.Count > 0) SelectedLayer = level.Layers.Last();
            else SelectedLayer = null;
            selectitem(null);
            updatetreeview();
        }

        public void moveLayerUp(Layer l)
        {
            int index = level.Layers.IndexOf(l);
            level.Layers[index] = level.Layers[index - 1];
            level.Layers[index - 1] = l;
            selectlayer(l);
        }

        public void moveLayerDown(Layer l)
        {
            int index = level.Layers.IndexOf(l);
            level.Layers[index] = level.Layers[index + 1];
            level.Layers[index + 1] = l;
            selectlayer(l);
        }

        public void selectlayer(Layer l)
        {
            if (SelectedItems.Count > 0) selectitem(null);
            SelectedLayer = l;
            updatetreeviewselection();
            MainForm.Instance.propertyGrid1.SelectedObject = l;
        }

        public void addItem(Item i)
        {
            if (!i.layer.Items.Contains(i)) i.layer.Items.Add(i);
        }

        public void deleteSelectedItems()
        {
            beginCommand("Delete Item(s)");
            List<Item> selecteditemscopy = new List<Item>(SelectedItems);

            List<Item> itemsaffected = new List<Item>();

            foreach (Item selitem in selecteditemscopy) {

                foreach (Layer l in level.Layers)
                    foreach (Item i in l.Items)
                        foreach (CustomProperty cp in i.CustomProperties.Values)
                        {
                            if (cp.type == typeof(Item) && cp.value == selitem)
                            {
                                cp.value = null;
                                itemsaffected.Add(i);
                            }
                        }

                selitem.layer.Items.Remove(selitem);
            }
            endCommand();
            selectitem(null);
            updatetreeview();

            if (itemsaffected.Count > 0)
            {
                string message = "";
                foreach (Item item in itemsaffected) message += item.Name + " (Layer: " + item.layer.Name + ")\n";
                Forms.MessageBox.Show("The following Items have Custom Properties of Type \"Item\" that refered to items that have just been deleted:\n\n"
                    + message + "\nThe corresponding Custom Properties have been set to NULL, since the Item referred to doesn't exist anymore.");
            }

        }

        public void moveItemUp(Item i)
        {
            int index = i.layer.Items.IndexOf(i);
            i.layer.Items[index] = i.layer.Items[index - 1];
            i.layer.Items[index - 1] = i;
            //updatetreeview();
        }

        public void moveItemDown(Item i)
        {
            int index = i.layer.Items.IndexOf(i);
            i.layer.Items[index] = i.layer.Items[index + 1];
            i.layer.Items[index + 1] = i;
            selectitem(i);
            //updatetreeview();
        }

        public void selectitem(Item i)
        {
            SelectedItems.Clear();
            if (i != null)
            {
                SelectedItems.Add(i);
                SelectedLayer = i.layer;
                updatetreeviewselection();
                MainForm.Instance.propertyGrid1.SelectedObject = i;
            }
            else
            {
                selectlayer(SelectedLayer);
            }
        }

        public void selectAll()
        {
            if (SelectedLayer == null) return;
            //if (SelectedLayer.Items.Count == 0) return;
            SelectedItems.Clear();
            foreach (Item i in SelectedLayer.Items)
            {
                SelectedItems.Add(i);
            }
            updatetreeviewselection();
        }

        public void moveItemToLayer(Item i1, Layer l2, Item i2)
        {
            int index2 = i2 == null ? 0 : l2.Items.IndexOf(i2);
            i1.layer.Items.Remove(i1);
            l2.Items.Insert(index2, i1);
            i1.layer = l2;
        }
        public void moveSelectedItemsToLayer(Layer chosenlayer)
        {
            if (chosenlayer == SelectedLayer) return;
            beginCommand("Move Item(s) To Layer \"" + chosenlayer.Name + "\"");
            List<Item> selecteditemscopy = new List<Item>(SelectedItems);
            foreach (Item i in selecteditemscopy)
            {
                moveItemToLayer(i, chosenlayer, null);
            }
            endCommand();
            SelectedItems.Clear();
            updatetreeview();
        }
        public void copySelectedItemsToLayer(Layer chosenlayer)
        {
            //if (chosenlayer == SelectedLayer) return;
            beginCommand("Copy Item(s) To Layer \"" + chosenlayer.Name + "\"");
            List<Item> selecteditemscopy = new List<Item>(SelectedItems);
            foreach (Item i in selecteditemscopy)
            {
                Item copy = i.clone();
                copy.layer = chosenlayer;
                copy.Name = MainForm.Instance.getUniqueNameBasedOn(copy.Name);
                addItem(copy);
            }
            endCommand();
            SelectedItems.Clear();
            updatetreeview();
        }
        public void createTextureBrush(string fullpath)
        {
            state = EditorState.brush;
            currentbrush = new Brush(fullpath);
        }
        
        public void destroyTextureBrush()
        {
            state = EditorState.idle;
            currentbrush = null;
        }

        public void paintTextureBrush(bool continueAfterPaint)
        {
            if (SelectedLayer == null)
            {
                System.Windows.Forms.MessageBox.Show(Resources.No_Layer);
                destroyTextureBrush();
                return;
            }
            Item i = new TextureItem(currentbrush.fullpath, new Vector2((int)mouseworldpos.X, (int)mouseworldpos.Y));
            i.Name = i.getNamePrefix() + level.getNextItemNumber();
            i.layer = SelectedLayer;
            beginCommand("Add Item \"" + i.Name + "\"");
            addItem(i);
            endCommand();
            updatetreeview();
            if (!continueAfterPaint) destroyTextureBrush();
        }

        public void createPrimitiveBrush(PrimitiveType primitiveType)
        {
            if (SelectedLayer == null)
            {
                System.Windows.Forms.MessageBox.Show(Resources.No_Layer);
                return;
            }

            state = EditorState.brush_primitive;
            primitivestarted = false;
            clickedPoints.Clear();
            currentprimitive = primitiveType;
            MainForm.Instance.pictureBox1.Cursor = Forms.Cursors.Cross;
            MainForm.Instance.listView2.Cursor = Forms.Cursors.Cross;
            switch (primitiveType)
            {
                case PrimitiveType.Rectangle:
                    MainForm.Instance.toolStripStatusLabel1.Text = Resources.Rectangle_Entered;
                    break;
                case PrimitiveType.Circle:
                    MainForm.Instance.toolStripStatusLabel1.Text = Resources.Circle_Entered;
                    break;
                case PrimitiveType.Path:
                    MainForm.Instance.toolStripStatusLabel1.Text = Resources.Path_Entered;
                    break;

            }
        }

        public void destroyPrimitiveBrush()
        {
            state = EditorState.idle;
            MainForm.Instance.pictureBox1.Cursor = Forms.Cursors.Default;
            MainForm.Instance.listView2.Cursor = Forms.Cursors.Default;
        }

        public void paintPrimitiveBrush()
        {
            switch (currentprimitive)
            {
                case PrimitiveType.Rectangle:
                    Item ri = new RectangleItem(Extensions.RectangleFromVectors(clickedPoints[0], clickedPoints[1]));
                    ri.Name = ri.getNamePrefix() + level.getNextItemNumber();
                    ri.layer = SelectedLayer;
                    beginCommand("Add Item \"" + ri.Name + "\"");
                    addItem(ri);
                    endCommand();
                    MainForm.Instance.toolStripStatusLabel1.Text = Resources.Rectangle_Entered;
                    break;
                case PrimitiveType.Circle:
                    Item ci = new CircleItem(clickedPoints[0], (mouseworldpos - clickedPoints[0]).Length());
                    ci.Name = ci.getNamePrefix() + level.getNextItemNumber();
                    ci.layer = SelectedLayer;
                    beginCommand("Add Item \"" + ci.Name + "\"");
                    addItem(ci);
                    endCommand();
                    MainForm.Instance.toolStripStatusLabel1.Text = Resources.Circle_Entered;
                    break;
                case PrimitiveType.Path:
                    Item pi = new PathItem(clickedPoints.ToArray());
                    pi.Name = pi.getNamePrefix() + level.getNextItemNumber();
                    pi.layer = SelectedLayer;
                    beginCommand("Add Item \"" + pi.Name + "\"");
                    addItem(pi);
                    endCommand();
                    MainForm.Instance.toolStripStatusLabel1.Text = Resources.Path_Entered;
                    break;
            }
            updatetreeview();
        }


        public void startMoving()
        {
            grabbedpoint = mouseworldpos;

            //save the distance to mouse for each item
            initialpos.Clear();
            foreach (Item selitem in SelectedItems)
            {
                initialpos.Add(selitem.pPosition);
            }
            
            state = EditorState.moving;
            //MainForm.Instance.pictureBox1.Cursor = Forms.Cursors.SizeAll;
        }

        public void setmousepos(int screenx, int screeny)
        {
            Vector2 maincameraposition = camera.Position;
            if (SelectedLayer != null) camera.Position *= SelectedLayer.ScrollSpeed;
            mouseworldpos = Vector2.Transform(new Vector2(screenx, screeny), Matrix.Invert(camera.matrix));
            if (Constants.Instance.SnapToGrid || kstate.IsKeyDown(Keys.G))
            {
                mouseworldpos = snapToGrid(mouseworldpos);
            }
            camera.Position = maincameraposition;
        }

        public Item getItemAtPos(Vector2 mouseworldpos)
        {
            if (SelectedLayer==null) return null;
            return SelectedLayer.getItemAtPos(mouseworldpos);
            /*if (level.Layers.Count == 0) return null;
            for (int i = level.Layers.Count - 1; i >= 0; i--)
            {
                Item item = level.Layers[i].getItemAtPos(mouseworldpos);
                if (item != null) return item;
            }
            return null;*/
        }

        public void loadLevel(Level l)
        {
            if (l.ContentRootFolder == null)
            {
                l.ContentRootFolder = Constants.Instance.DefaultContentRootFolder;
                if (!Directory.Exists(l.ContentRootFolder))
                {
                    Forms.DialogResult dr = Forms.MessageBox.Show(
                        "The DefaultContentRootFolder \"" + l.ContentRootFolder + "\" (as set in the Settings Dialog) doesn't exist!\n"
                        + "The ContentRootFolder of the new level will be set to the Editor's work directory (" + Forms.Application.StartupPath + ").\n"
                        + "Please adjust the DefaultContentRootFolder in the Settings Dialog.\n"
                        + "Do you want to open the Settings Dialog now?", "Error",
                        Forms.MessageBoxButtons.YesNo, Forms.MessageBoxIcon.Exclamation);
                    if (dr == Forms.DialogResult.Yes) new SettingsForm().ShowDialog();
                    l.ContentRootFolder = Forms.Application.StartupPath;
                }
            }
            else
            {
                if (!Directory.Exists(l.ContentRootFolder))
                {
                    Forms.MessageBox.Show("The directory \"" + l.ContentRootFolder + "\" doesn't exist! "
                        + "Please adjust the XML file before trying again.");
                    return;
                }
            }

            TextureLoader.Instance.Clear();

            foreach (Layer layer in l.Layers)
            {
                layer.level = l;
                foreach (Item item in layer.Items)
                {
                    item.layer = layer;
                    if (!item.loadIntoEditor()) return;
                }
            }

            level = l;
            MainForm.Instance.loadfolder(level.ContentRootFolder);
            if (level.Name == null) level.Name = "Level_01";


            SelectedLayer = null;
            if (level.Layers.Count > 0) SelectedLayer = level.Layers[0];
            SelectedItems.Clear();

            
            
            camera = new Camera(MainForm.Instance.pictureBox1.Width, MainForm.Instance.pictureBox1.Height);
            camera.Position = level.EditorRelated.CameraPosition;
            MainForm.Instance.zoomcombo.Text = "100%";
            undoBuffer.Clear();
            redoBuffer.Clear();
            MainForm.Instance.undoButton.DropDownItems.Clear();
            MainForm.Instance.redoButton.DropDownItems.Clear();
            MainForm.Instance.undoButton.Enabled = MainForm.Instance.undoMenuItem.Enabled = undoBuffer.Count > 0;
            MainForm.Instance.redoButton.Enabled = MainForm.Instance.redoMenuItem.Enabled = redoBuffer.Count > 0;
            commandInProgress = false;

            updatetreeview();
        }

        public void updatetreeview()
        {
            MainForm.Instance.treeView1.Nodes.Clear();
            level.treenode = MainForm.Instance.treeView1.Nodes.Add(level.Name);
            level.treenode.Tag = level;
            level.treenode.Checked = level.Visible;
            level.treenode.ContextMenuStrip = MainForm.Instance.LevelContextMenu;

            foreach (Layer layer in level.Layers)
            {
                Forms.TreeNode layernode = level.treenode.Nodes.Add(layer.Name, layer.Name);
                layernode.Tag = layer;
                layernode.Checked = layer.Visible;
                layernode.ContextMenuStrip = MainForm.Instance.LayerContextMenu;
                layernode.ImageIndex = layernode.SelectedImageIndex = 0;

                foreach (Item item in layer.Items)
                {
                    Forms.TreeNode itemnode = layernode.Nodes.Add(item.Name, item.Name);
                    itemnode.Tag = item;
                    itemnode.Checked = true;
                    itemnode.ContextMenuStrip = MainForm.Instance.ItemContextMenu;
                    int imageindex = 0;
                    if (item is TextureItem) imageindex = 1;
                    if (item is RectangleItem) imageindex = 2;
                    if (item is CircleItem) imageindex = 3;
                    if (item is PathItem) imageindex = 4;
                    itemnode.ImageIndex = itemnode.SelectedImageIndex = imageindex;
                }
                layernode.Expand();
            }
            level.treenode.Expand();

            updatetreeviewselection();
        }

        public void updatetreeviewselection()
        {
            MainForm.Instance.propertyGrid1.SelectedObject = null;
            if (SelectedItems.Count > 0)
            {
                Forms.TreeNode[] nodes = MainForm.Instance.treeView1.Nodes.Find(SelectedItems[0].Name, true);
                if (nodes.Length > 0)
                {
                    List<Item> selecteditemscopy = new List<Item>(SelectedItems);
                    MainForm.Instance.propertyGrid1.SelectedObject = SelectedItems[0];
                    MainForm.Instance.treeView1.SelectedNode = nodes[0];
                    MainForm.Instance.treeView1.SelectedNode.EnsureVisible();
                    SelectedItems = selecteditemscopy;
                }
            }
            else if (SelectedLayer != null)
            {
                Forms.TreeNode[] nodes = MainForm.Instance.treeView1.Nodes[0].Nodes.Find(SelectedLayer.Name, false);
                if (nodes.Length > 0)
                {
                    MainForm.Instance.treeView1.SelectedNode = nodes[0];
                    MainForm.Instance.treeView1.SelectedNode.EnsureVisible();
                }
            }
        }

        public void saveLevel(string filename)
        {
            level.EditorRelated.CameraPosition = camera.Position;
            level.EditorRelated.Version = Version;
            level.export(filename);
        }

        public void alignHorizontally()
        {
            beginCommand("Align Horizontally");
            foreach (Item i in SelectedItems)
            {
                i.pPosition = new Vector2(i.pPosition.X, SelectedItems[0].pPosition.Y);
            }
            endCommand();
        }

        public void alignVertically()
        {
            beginCommand("Align Vertically");
            foreach (Item i in SelectedItems)
            {
                i.pPosition = new Vector2(SelectedItems[0].pPosition.X, i.pPosition.Y);
            }
            endCommand();
        }

        public void alignRotation()
        {
            beginCommand("Align Rotation");
            foreach (TextureItem i in SelectedItems)
            {
                i.pRotation = ((TextureItem)SelectedItems[0]).pRotation;
            }
            endCommand();
        }

        public void alignScale()
        {
            beginCommand("Align Scale");
            foreach (TextureItem i in SelectedItems)
            {
                i.pScale = ((TextureItem)SelectedItems[0]).pScale;
            }
            endCommand();
        }






        public void beginCommand(string description)
        {
            //System.Diagnostics.Debug.WriteLine(System.DateTime.Now.ToString() + ": beginCommand(" + description + ")");
            if (commandInProgress)
            {
                undoBuffer.Pop();
            }
            undoBuffer.Push(new Command(description));
            commandInProgress = true;
        }
        public void endCommand()
        {
            if (!commandInProgress) return;
            //System.Diagnostics.Debug.WriteLine(System.DateTime.Now.ToString() + ": endCommand()");
            undoBuffer.Peek().saveAfterState();
            redoBuffer.Clear();
            MainForm.Instance.redoButton.DropDownItems.Clear();
            MainForm.Instance.DirtyFlag = true;
            MainForm.Instance.undoButton.Enabled = MainForm.Instance.undoMenuItem.Enabled = undoBuffer.Count > 0;
            MainForm.Instance.redoButton.Enabled = MainForm.Instance.redoMenuItem.Enabled = redoBuffer.Count > 0;

            Forms.ToolStripMenuItem item = new Forms.ToolStripMenuItem(undoBuffer.Peek().Description);
            item.Tag = undoBuffer.Peek();
            MainForm.Instance.undoButton.DropDownItems.Insert(0, item);
            commandInProgress = false;
        }
        public void abortCommand()
        {
            if (!commandInProgress) return;
            undoBuffer.Pop();
            commandInProgress = false;
        }
        public void undo()
        {
            if (commandInProgress)
            {
                undoBuffer.Pop();
                commandInProgress = false;
            }
            if (undoBuffer.Count == 0) return;
            undoBuffer.Peek().Undo();
            redoBuffer.Push(undoBuffer.Pop());
            MainForm.Instance.propertyGrid1.Refresh();
            MainForm.Instance.DirtyFlag = true;
            MainForm.Instance.undoButton.Enabled = MainForm.Instance.undoMenuItem.Enabled = undoBuffer.Count > 0;
            MainForm.Instance.redoButton.Enabled = MainForm.Instance.redoMenuItem.Enabled = redoBuffer.Count > 0;
            MainForm.Instance.redoButton.DropDownItems.Insert(0, MainForm.Instance.undoButton.DropDownItems[0]);
        }
        public void undoMany(Command c)
        {
            while (redoBuffer.Count==0 || redoBuffer.Peek() != c) undo();
        }
        public void redo()
        {
            if (commandInProgress)
            {
                undoBuffer.Pop();
                commandInProgress = false;
            }
            if (redoBuffer.Count == 0) return;
            redoBuffer.Peek().Redo();
            undoBuffer.Push(redoBuffer.Pop());
            MainForm.Instance.propertyGrid1.Refresh();
            MainForm.Instance.DirtyFlag = true;
            MainForm.Instance.undoButton.Enabled = MainForm.Instance.undoMenuItem.Enabled = undoBuffer.Count > 0;
            MainForm.Instance.redoButton.Enabled = MainForm.Instance.redoMenuItem.Enabled = redoBuffer.Count > 0;
            MainForm.Instance.undoButton.DropDownItems.Insert(0, MainForm.Instance.redoButton.DropDownItems[0]);
        }
        public void redoMany(Command c)
        {
            while (undoBuffer.Count == 0 || undoBuffer.Peek() != c) redo();
        }


        public Vector2 snapToGrid(Vector2 input)
        {
            
            Vector2 result = input;
            result.X = Constants.Instance.GridSpacing.X * (int)Math.Round(result.X / Constants.Instance.GridSpacing.X);
            result.Y = Constants.Instance.GridSpacing.Y * (int)Math.Round(result.Y / Constants.Instance.GridSpacing.Y);
            posSnappedPoint = result;
            drawSnappedPoint = true;
            return result;
        }

        public void update(GameTime gt)
        {
            if (level == null) return;

            oldkstate = kstate;
            oldmstate = mstate;
            kstate = Keyboard.GetState();
            mstate = Mouse.GetState();
            int mwheeldelta = mstate.ScrollWheelValue - oldmstate.ScrollWheelValue;
            if (mwheeldelta > 0 /* && kstate.IsKeyDown(Keys.LeftControl)*/)
            {
                float zoom = (float)Math.Round(camera.Scale * 10) * 10.0f + 10.0f;
                MainForm.Instance.zoomcombo.Text = zoom.ToString() + "%";
                camera.Scale = zoom / 100.0f;
            }
            if (mwheeldelta < 0 /* && kstate.IsKeyDown(Keys.LeftControl)*/)
            {
                float zoom = (float)Math.Round(camera.Scale * 10) * 10.0f - 10.0f;
                if (zoom <= 0.0f) return;
                MainForm.Instance.zoomcombo.Text = zoom.ToString() + "%";
                camera.Scale = zoom / 100.0f;
            }

            //Camera movement
            float delta;
            if (kstate.IsKeyDown(Keys.LeftShift)) delta = Constants.Instance.CameraFastSpeed * (float)gt.ElapsedGameTime.TotalSeconds;
                else delta = Constants.Instance.CameraSpeed * (float)gt.ElapsedGameTime.TotalSeconds;
            if (kstate.IsKeyDown(Keys.W) && kstate.IsKeyUp(Keys.LeftControl)) camera.Position += (new Vector2(0, -delta));
            if (kstate.IsKeyDown(Keys.S) && kstate.IsKeyUp(Keys.LeftControl)) camera.Position += (new Vector2(0, +delta));
            if (kstate.IsKeyDown(Keys.A) && kstate.IsKeyUp(Keys.LeftControl)) camera.Position += (new Vector2(-delta, 0));
            if (kstate.IsKeyDown(Keys.D) && kstate.IsKeyUp(Keys.LeftControl)) camera.Position += (new Vector2(+delta, 0));


            if (kstate.IsKeyDown(Keys.Subtract))
            {
                float zoom = (float)(camera.Scale * 0.995);
                MainForm.Instance.zoomcombo.Text = (zoom * 100).ToString("###0.0") + "%";
                camera.Scale = zoom;
            }
            if (kstate.IsKeyDown(Keys.Add))
            {
                float zoom = (float)(camera.Scale * 1.005);
                MainForm.Instance.zoomcombo.Text = (zoom * 100).ToString("###0.0") + "%";
                camera.Scale = zoom;
            }

            //get mouse world position considering the ScrollSpeed of the current layer
            Vector2 maincameraposition = camera.Position;
            if (SelectedLayer != null) camera.Position *= SelectedLayer.ScrollSpeed;
            mouseworldpos = Vector2.Transform(new Vector2(mstate.X, mstate.Y), Matrix.Invert(camera.matrix));
            mouseworldpos = mouseworldpos.Round();
            MainForm.Instance.toolStripStatusLabel3.Text = "Mouse: (" + mouseworldpos.X + ", " + mouseworldpos.Y + ")";
            camera.Position = maincameraposition;


            if (state == EditorState.idle)
            {
                //get item under mouse cursor
                Item item = getItemAtPos(mouseworldpos);
                if (item != null)
                {
                    MainForm.Instance.toolStripStatusLabel1.Text = item.Name;
                    item.onMouseOver(mouseworldpos);
                    if (kstate.IsKeyDown(Keys.LeftControl)) MainForm.Instance.pictureBox1.Cursor = cursorDup;
                }
                else
                {
                    MainForm.Instance.toolStripStatusLabel1.Text = "";
                }
                if (item != lastitem && lastitem != null) lastitem.onMouseOut();
                lastitem = item;

                //LEFT MOUSE BUTTON CLICK
                if ((mstate.LeftButton == ButtonState.Pressed && oldmstate.LeftButton == ButtonState.Released) ||
                    (kstate.IsKeyDown(Keys.D1) && oldkstate.IsKeyUp(Keys.D1)))
                {
                    if (item != null) item.onMouseButtonDown(mouseworldpos);
                    if (kstate.IsKeyDown(Keys.LeftControl) && item != null)
                    {
                        if (!SelectedItems.Contains(item)) selectitem(item);

                        beginCommand("Add Item(s)");

                        List<Item> selecteditemscopy = new List<Item>();
                        foreach (Item selitem in SelectedItems)
                        {
                            Item i2 = (Item)selitem.clone();
                            selecteditemscopy.Add(i2);
                        }
                        foreach (Item selitem in selecteditemscopy)
                        {
                            selitem.Name = selitem.getNamePrefix() + level.getNextItemNumber();
                            addItem(selitem);
                        }
                        selectitem(selecteditemscopy[0]);
                        updatetreeview();
                        for (int i = 1; i < selecteditemscopy.Count; i++) SelectedItems.Add(selecteditemscopy[i]);
                        startMoving();
                    }
                    else if (kstate.IsKeyDown(Keys.LeftShift) && item != null)
                    {
                        if (SelectedItems.Contains(item)) SelectedItems.Remove(item);
                        else SelectedItems.Add(item);
                    }
                    else if (SelectedItems.Contains(item))
                    {
                        beginCommand("Change Item(s)");
                        startMoving();
                    }
                    else if (!SelectedItems.Contains(item))
                    {
                        selectitem(item);
                        if (item != null)
                        {
                            beginCommand("Change Item(s)");
                            startMoving();
                        }
                        else
                        {
                            grabbedpoint = mouseworldpos;
                            selectionrectangle = Rectangle.Empty;
                            state = EditorState.selecting;
                        }

                    }
                }

                //MIDDLE MOUSE BUTTON CLICK
                if ((mstate.MiddleButton == ButtonState.Pressed && oldmstate.MiddleButton == ButtonState.Released) ||
                    (kstate.IsKeyDown(Keys.D2) && oldkstate.IsKeyUp(Keys.D2)))
                {
                    if (item != null) item.onMouseOut();
                    if (kstate.IsKeyDown(Keys.LeftControl))
                    {
                        grabbedpoint = new Vector2(mstate.X, mstate.Y);
                        initialcampos = camera.Position;
                        state = EditorState.cameramoving;
                        MainForm.Instance.pictureBox1.Cursor = Forms.Cursors.SizeAll;
                    }
                    else
                    {
                        if (SelectedItems.Count > 0)
                        {
                            grabbedpoint = mouseworldpos - SelectedItems[0].pPosition;

                            //save the initial rotation for each item
                            initialrot.Clear();
                            foreach (Item selitem in SelectedItems)
                            {
                                if (selitem.CanRotate())
                                {
                                    initialrot.Add(selitem.getRotation());
                                }
                            }

                            state = EditorState.rotating;
                            MainForm.Instance.pictureBox1.Cursor = cursorRot;

                            beginCommand("Rotate Item(s)");
                        }
                    }
                }

                //RIGHT MOUSE BUTTON CLICK
                if ((mstate.RightButton == ButtonState.Pressed && oldmstate.RightButton == ButtonState.Released) ||
                    (kstate.IsKeyDown(Keys.D3) && oldkstate.IsKeyUp(Keys.D3)))

                {
                    if (item != null) item.onMouseOut();
                    if (SelectedItems.Count > 0)
                    {
                        grabbedpoint = mouseworldpos - SelectedItems[0].pPosition;

                        //save the initial scale for each item
                        initialscale.Clear();
                        foreach (Item selitem in SelectedItems)
                        {
                            if (selitem.CanScale())
                            {
                                initialscale.Add(selitem.getScale());
                            }
                        }

                        state = EditorState.scaling;
                        MainForm.Instance.pictureBox1.Cursor = cursorScale;

                        beginCommand("Scale Item(s)");
                    }
                }

                if (kstate.IsKeyDown(Keys.H) && oldkstate.GetPressedKeys().Length == 0 && SelectedItems.Count > 0)
                {
                    beginCommand("Flip Item(s) Horizontally");
                    foreach (Item selitem in SelectedItems)
                    {
                        if (selitem is TextureItem)
                        {
                            TextureItem ti = (TextureItem)selitem;
                            ti.FlipHorizontally = !ti.FlipHorizontally;
                        }
                    } 
                    MainForm.Instance.propertyGrid1.Refresh();
                    endCommand();
                }
                if (kstate.IsKeyDown(Keys.V) && oldkstate.GetPressedKeys().Length == 0 && SelectedItems.Count > 0)
                {
                    beginCommand("Flip Item(s) Vertically");
                    foreach (Item selitem in SelectedItems)
                    {
                        if (selitem is TextureItem)
                        {
                            TextureItem ti = (TextureItem)selitem;
                            ti.FlipVertically = !ti.FlipVertically;
                        }
                    }
                    MainForm.Instance.propertyGrid1.Refresh();
                    endCommand();
                }
            }

            if (state == EditorState.moving)
            {
                int i = 0;
                foreach (Item selitem in SelectedItems)
                {
                    newPosition = initialpos[i] + mouseworldpos - grabbedpoint;
                    if (Constants.Instance.SnapToGrid || kstate.IsKeyDown(Keys.G)) newPosition = snapToGrid(newPosition);
                    drawSnappedPoint = false;
                    selitem.setPosition(newPosition);
                    i++;
                }
                MainForm.Instance.propertyGrid1.Refresh();
                if ((mstate.LeftButton == ButtonState.Released && oldmstate.LeftButton == ButtonState.Pressed) ||
                    (kstate.IsKeyUp(Keys.D1) && oldkstate.IsKeyDown(Keys.D1)))
                {

                    foreach (Item selitem in SelectedItems) selitem.onMouseButtonUp(mouseworldpos);

                    state = EditorState.idle;
                    MainForm.Instance.pictureBox1.Cursor = Forms.Cursors.Default;
                    if (mouseworldpos != grabbedpoint) endCommand(); else abortCommand();
                }
            }

            if (state == EditorState.rotating)
            {
                Vector2 newpos = mouseworldpos - SelectedItems[0].pPosition;
                float deltatheta = (float)Math.Atan2(grabbedpoint.Y, grabbedpoint.X) - (float)Math.Atan2(newpos.Y, newpos.X);
                int i = 0;
                foreach (Item selitem in SelectedItems)
                {
                    if (selitem.CanRotate())
                    {
                        selitem.setRotation(initialrot[i] - deltatheta);
                        if (kstate.IsKeyDown(Keys.LeftControl))
                        {
                            selitem.setRotation((float)Math.Round(selitem.getRotation() / MathHelper.PiOver4) * MathHelper.PiOver4);
                        }
                        i++;
                    }
                }
                MainForm.Instance.propertyGrid1.Refresh();
                if ((mstate.MiddleButton == ButtonState.Released && oldmstate.MiddleButton == ButtonState.Pressed) ||
                    (kstate.IsKeyUp(Keys.D2) && oldkstate.IsKeyDown(Keys.D2)))
                {
                    state = EditorState.idle;
                    MainForm.Instance.pictureBox1.Cursor = Forms.Cursors.Default;
                    endCommand();
                }
            }

            if (state == EditorState.scaling)
            {
                Vector2 newdistance = mouseworldpos - SelectedItems[0].pPosition;
                float factor = newdistance.Length() / grabbedpoint.Length();
                int i = 0;
                foreach (Item selitem in SelectedItems)
                {
                    if (selitem.CanScale())
                    {
                        if (selitem is TextureItem)
                        {
                            MainForm.Instance.toolStripStatusLabel1.Text = "Hold down [X] or [Y] to limit scaling to the according dimension.";
                        }

                        Vector2 newscale = initialscale[i];
                        if (!kstate.IsKeyDown(Keys.Y)) newscale.X = initialscale[i].X * (((factor - 1.0f) * 0.5f) + 1.0f);
                        if (!kstate.IsKeyDown(Keys.X)) newscale.Y = initialscale[i].Y * (((factor - 1.0f) * 0.5f) + 1.0f);
                        selitem.setScale(newscale);
                        
                        if (kstate.IsKeyDown(Keys.LeftControl))
                        {
                            Vector2 scale;
                            scale.X = (float)Math.Round(selitem.getScale().X * 10) / 10;
                            scale.Y = (float)Math.Round(selitem.getScale().Y * 10) / 10;
                            selitem.setScale(scale);
                        }
                        i++;
                    }
                }
                MainForm.Instance.propertyGrid1.Refresh();
                if ((mstate.RightButton == ButtonState.Released && oldmstate.RightButton == ButtonState.Pressed) ||
                    (kstate.IsKeyUp(Keys.D3) && oldkstate.IsKeyDown(Keys.D3)))
                {
                    state = EditorState.idle;
                    MainForm.Instance.pictureBox1.Cursor = Forms.Cursors.Default;
                    endCommand();
                }
            }

            if (state == EditorState.cameramoving)
            {
                Vector2 newpos = new Vector2(mstate.X, mstate.Y);
                Vector2 distance = (newpos - grabbedpoint) / camera.Scale;
                if (distance.Length() > 0)
                {
                    camera.Position = initialcampos - distance;
                }
                if (mstate.MiddleButton == ButtonState.Released)
                {
                    state = EditorState.idle;
                    MainForm.Instance.pictureBox1.Cursor = Forms.Cursors.Default;
                }
            }

            if (state == EditorState.selecting)
            {
                if (SelectedLayer == null) return;
                Vector2 distance = mouseworldpos - grabbedpoint;
                if (distance.Length() > 0)
                {
                    SelectedItems.Clear();
                    selectionrectangle = Extensions.RectangleFromVectors(grabbedpoint, mouseworldpos);
                    foreach (Item i in SelectedLayer.Items)
                    {
                        if (i.Visible && selectionrectangle.Contains((int)i.pPosition.X, (int)i.pPosition.Y)) SelectedItems.Add(i);
                    }
                    updatetreeviewselection();
                }
                if (mstate.LeftButton == ButtonState.Released)
                {
                    state = EditorState.idle;
                    MainForm.Instance.pictureBox1.Cursor = Forms.Cursors.Default;
                }
            }

            if (state == EditorState.brush)
            {
                if (Constants.Instance.SnapToGrid || kstate.IsKeyDown(Keys.G))
                {
                    mouseworldpos = snapToGrid(mouseworldpos);
                }
                if (mstate.RightButton == ButtonState.Pressed && oldmstate.RightButton == ButtonState.Released) state = EditorState.idle;
                if (mstate.LeftButton == ButtonState.Pressed && oldmstate.LeftButton == ButtonState.Released) paintTextureBrush(true);
            }


            if (state == EditorState.brush_primitive)
            {

                if (Constants.Instance.SnapToGrid || kstate.IsKeyDown(Keys.G)) mouseworldpos = snapToGrid(mouseworldpos);

                if (kstate.IsKeyDown(Keys.LeftControl) && primitivestarted && currentprimitive == PrimitiveType.Rectangle)
                {
                    Vector2 distance = mouseworldpos - clickedPoints[0];
                    float squareside = Math.Max(distance.X, distance.Y);
                    mouseworldpos = clickedPoints[0] + new Vector2(squareside, squareside);
                }
                if ((mstate.LeftButton == ButtonState.Pressed && oldmstate.LeftButton == ButtonState.Released) ||
                    (kstate.IsKeyDown(Keys.D1) && oldkstate.IsKeyUp(Keys.D1)))
                {
                    clickedPoints.Add(mouseworldpos);
                    if (primitivestarted == false)
                    {
                        primitivestarted = true;
                        switch (currentprimitive)
                        {
                            case PrimitiveType.Rectangle:
                                MainForm.Instance.toolStripStatusLabel1.Text = Resources.Rectangle_Started;
                                break;
                            case PrimitiveType.Circle:
                                MainForm.Instance.toolStripStatusLabel1.Text = Resources.Circle_Started;
                                break;
                            case PrimitiveType.Path:
                                MainForm.Instance.toolStripStatusLabel1.Text = Resources.Path_Started;
                                break;
                        }
                    }
                    else
                    {
                        if (currentprimitive != PrimitiveType.Path)
                        {
                            paintPrimitiveBrush();
                            clickedPoints.Clear();
                            primitivestarted = false;
                        }
                    }
                }
                if (kstate.IsKeyDown(Keys.Back) && oldkstate.IsKeyUp(Keys.Back))
                {
                    if (currentprimitive == PrimitiveType.Path && clickedPoints.Count > 1)
                    {
                        clickedPoints.RemoveAt(clickedPoints.Count-1);
                    }
                }

                if ((mstate.MiddleButton == ButtonState.Pressed && oldmstate.MiddleButton == ButtonState.Released) ||
                    (kstate.IsKeyDown(Keys.D2) && oldkstate.IsKeyUp(Keys.D2)))
                {
                    if (currentprimitive == PrimitiveType.Path && primitivestarted)
                    {
                        paintPrimitiveBrush();
                        clickedPoints.Clear();
                        primitivestarted = false;
                        MainForm.Instance.toolStripStatusLabel1.Text = Resources.Path_Entered;
                    }
                }
                if ((mstate.RightButton == ButtonState.Pressed && oldmstate.RightButton == ButtonState.Released) ||
                    (kstate.IsKeyDown(Keys.D3) && oldkstate.IsKeyUp(Keys.D3)))
                {
                    if (primitivestarted)
                    {
                        clickedPoints.Clear();
                        primitivestarted = false;
                        switch (currentprimitive)
                        {
                            case PrimitiveType.Rectangle:
                                MainForm.Instance.toolStripStatusLabel1.Text = Resources.Rectangle_Entered;
                                break;
                            case PrimitiveType.Circle:
                                MainForm.Instance.toolStripStatusLabel1.Text = Resources.Circle_Entered;
                                break;
                            case PrimitiveType.Path:
                                MainForm.Instance.toolStripStatusLabel1.Text = Resources.Path_Entered;
                                break;
                        }
                    }
                    else
                    {
                        destroyPrimitiveBrush();
                        clickedPoints.Clear();
                        primitivestarted = false;
                    }
                }
            }

        }

        public void draw(SpriteBatch sb)
        {
            Game1.Instance.GraphicsDevice.Clear(Constants.Instance.ColorBackground);
            if (level == null || !level.Visible) return;
            foreach (Layer l in level.Layers)
            {
                Vector2 maincameraposition = camera.Position;
                camera.Position *= l.ScrollSpeed;
                sb.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Deferred, SaveStateMode.None, camera.matrix);

                l.drawInEditor(sb);
                if (l == SelectedLayer && state == EditorState.selecting)
                {
                    Primitives.Instance.drawBoxFilled(sb, selectionrectangle, Constants.Instance.ColorSelectionBox);
                }
                if (l == SelectedLayer && state == EditorState.brush)
                {
                    sb.Draw(currentbrush.texture, new Vector2(mouseworldpos.X, mouseworldpos.Y), null, new Color(1f, 1f, 1f, 0.7f),
                        0, new Vector2(currentbrush.texture.Width / 2, currentbrush.texture.Height / 2), 1, SpriteEffects.None, 0);
                }
                if (l == SelectedLayer && state == EditorState.brush_primitive && primitivestarted)
                {
                    switch (currentprimitive)
                    {
                        case PrimitiveType.Rectangle:
                            Rectangle rect = Extensions.RectangleFromVectors(clickedPoints[0], mouseworldpos);
                            Primitives.Instance.drawBoxFilled(sb, rect, Constants.Instance.ColorPrimitives);
                            break;
                        case PrimitiveType.Circle:
                            Primitives.Instance.drawCircleFilled(sb, clickedPoints[0], (mouseworldpos - clickedPoints[0]).Length(), Constants.Instance.ColorPrimitives);
                            break;
                        case PrimitiveType.Path:
                            Primitives.Instance.drawPath(sb, clickedPoints.ToArray(), Constants.Instance.ColorPrimitives, Constants.Instance.DefaultPathItemLineWidth);
                            Primitives.Instance.drawLine(sb, clickedPoints.Last(), mouseworldpos, Constants.Instance.ColorPrimitives, Constants.Instance.DefaultPathItemLineWidth);
                            break;

                    }
                }
                sb.End();
                //restore main camera position
                camera.Position = maincameraposition;
            }
            

            //draw selection frames around selected items
            if (SelectedItems.Count > 0)
            {
                Vector2 maincameraposition = camera.Position;
                camera.Position *= SelectedItems[0].layer.ScrollSpeed;
                sb.Begin();
                int i = 0;
                foreach (Item item in SelectedItems)
                {
                    if (item.Visible && item.layer.Visible && kstate.IsKeyUp(Keys.Space))
                    {
                        Color color = i == 0 ? Constants.Instance.ColorSelectionFirst : Constants.Instance.ColorSelectionRest;
                        item.drawSelectionFrame(sb, camera.matrix, color);
                        if (i == 0 && (state == EditorState.rotating || state == EditorState.scaling))
                        {
                            Vector2 center = Vector2.Transform(item.Position, camera.matrix);
                            Vector2 mouse = Vector2.Transform(mouseworldpos, camera.matrix);
                            Primitives.Instance.drawLine(sb, center, mouse, Constants.Instance.ColorSelectionFirst, 1);
                        }
                    }
                    i++;
                }
                sb.End();
                //restore main camera position
                camera.Position = maincameraposition;

            }

            if (Constants.Instance.ShowGrid)
            {
                sb.Begin();
                int max = Constants.Instance.GridNumberOfGridLines/2;
                for(int x=0; x<=max; x++)
                {
                    Vector2 start = Vector2.Transform(new Vector2(x, -max) * Constants.Instance.GridSpacing.X, camera.matrix);
                    Vector2 end = Vector2.Transform(new Vector2(x, max) * Constants.Instance.GridSpacing.X, camera.matrix);
                    Primitives.Instance.drawLine(sb, start, end, Constants.Instance.GridColor, Constants.Instance.GridLineThickness);
                    start = Vector2.Transform(new Vector2(-x, -max) * Constants.Instance.GridSpacing.X, camera.matrix);
                    end = Vector2.Transform(new Vector2(-x, max) * Constants.Instance.GridSpacing.X, camera.matrix);
                    Primitives.Instance.drawLine(sb, start, end, Constants.Instance.GridColor, Constants.Instance.GridLineThickness);
                }
                for (int y = 0; y <= max; y++)
                {
                    Vector2 start = Vector2.Transform(new Vector2(-max, y) * Constants.Instance.GridSpacing.Y, camera.matrix);
                    Vector2 end = Vector2.Transform(new Vector2(max, y) * Constants.Instance.GridSpacing.Y, camera.matrix);
                    Primitives.Instance.drawLine(sb, start, end, Constants.Instance.GridColor, Constants.Instance.GridLineThickness);
                    start = Vector2.Transform(new Vector2(-max, -y) * Constants.Instance.GridSpacing.Y, camera.matrix);
                    end = Vector2.Transform(new Vector2(max, -y) * Constants.Instance.GridSpacing.Y, camera.matrix);
                    Primitives.Instance.drawLine(sb, start, end, Constants.Instance.GridColor, Constants.Instance.GridLineThickness);
                }        
                sb.End();
            }
            if (Constants.Instance.ShowWorldOrigin)
            {
                sb.Begin();
                Vector2 worldOrigin = Vector2.Transform(Vector2.Zero, camera.matrix);
                Primitives.Instance.drawLine(sb, worldOrigin + new Vector2(-20, 0), worldOrigin + new Vector2(+20, 0), Constants.Instance.WorldOriginColor, Constants.Instance.WorldOriginLineThickness);
                Primitives.Instance.drawLine(sb, worldOrigin + new Vector2(0, -20), worldOrigin + new Vector2(0, 20), Constants.Instance.WorldOriginColor, Constants.Instance.WorldOriginLineThickness);
                sb.End();
            }

            if (drawSnappedPoint)
            {
                sb.Begin();
                posSnappedPoint = Vector2.Transform(posSnappedPoint, camera.matrix);
                Primitives.Instance.drawBoxFilled(sb, posSnappedPoint.X - 5, posSnappedPoint.Y - 5, 10, 10, Constants.Instance.ColorSelectionFirst);
                sb.End();

            }

            drawSnappedPoint = false;

        }

















    }
}
