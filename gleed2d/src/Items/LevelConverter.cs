using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Web.UI.WebControls;
using System.Collections;
using System.Web.Script.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace GLEED2D
{
    public class LevelConverter : JavaScriptConverter
    {

        public override IEnumerable<Type> SupportedTypes
        {
            //Define the ListItemCollection as a supported type.
            get { return new ReadOnlyCollection<Type>(new List<Type>(new Type[] { typeof(Level) })); }
        }

        public override IDictionary<string, object> Serialize(object obj, JavaScriptSerializer serializer)
        {
            Level level = obj as Level;

            if (level != null)
            {
                // Create the representation.
                Dictionary<string, object> result = new Dictionary<string, object>();
                ArrayList layersList = new ArrayList();

                ArrayList canvasColor = new ArrayList();
                canvasColor.Add(level.CanvasColor.A);
                canvasColor.Add(level.CanvasColor.R);
                canvasColor.Add(level.CanvasColor.G);
                canvasColor.Add(level.CanvasColor.B);
                result["CanvasColor"] = canvasColor;
                result["Scale"] = level.Scale;
                ArrayList playerSpawn = new ArrayList();
                playerSpawn.Add(level.PlayerSpawn.X);
                playerSpawn.Add(level.PlayerSpawn.Y);
                result["PlayerSpawn"] = playerSpawn;
                result["Music"] = level.Music;
                result["ContentRootFolder"] = level.ContentRootFolder.Replace("\\", "/");

                int i = 0;
                ArrayList itemsList = new ArrayList();
                foreach (Layer layer in level.Layers)
                {
                    Dictionary<string, object> layerDict = new Dictionary<string, object>();
                    layerDict["ID"] = layer.Name;
                    layerDict["Scale"] = 1.0f/layer.ScrollSpeed.X;
                    layerDict["Layer"] = layer.ZPos;
                    layersList.Add(layerDict);
                    layer.Items.Reverse();
                    foreach (Item item in layer.Items)
                    {
                        //Add each entry to the dictionary.
                        Dictionary<string, object> listDict = new Dictionary<string, object>();
                        listDict.Add("Name", item.Name);

                        //Detect Components
                        Dictionary<string, object> transformDict = new Dictionary<string, object>();
                        ArrayList posList = new ArrayList();
                        posList.Add(item.Position.X);
                        posList.Add(item.Position.Y);
                        ArrayList position = posList;
                        position[0] = ((float)position[0]) / level.Scale;
                        position[1] = ((float)position[1]) / level.Scale;
                        transformDict["Position"] = posList;
                        transformDict["Rotation"] = 0.0;
                        //TextureItem => Renderable
                        if (item.GetType() == typeof(TextureItem))
                        {
                            TextureItem tex = item as TextureItem;
                            Dictionary<string, object> rendDict = new Dictionary<string, object>();
                            rendDict["Type"] = "Sprite";//support spritesheet later
                            rendDict["Layer"] = layerDict["Layer"];
                            rendDict["Image"] = tex.texture_filename.Replace("\\", "/");
                            ArrayList colorList = new ArrayList();
                            colorList.Add(tex.TintColor.A);
                            colorList.Add(tex.TintColor.R);
                            colorList.Add(tex.TintColor.G);
                            colorList.Add(tex.TintColor.B);
                            rendDict["ColorMod"] = colorList;
                            rendDict["xScale"] = tex.Scale.X;
                            rendDict["yScale"] = tex.Scale.Y;
                            transformDict["Rotation"] = tex.Rotation * 180.0f / MathHelper.Pi;
                            listDict.Add("Renderable", rendDict);
                        }
                        listDict.Add("Transform", transformDict);

                        //Game Specific junk (find a clean way to do this)
                        if (item.CustomProperties.ContainsKey("Gravity"))
                        {
                            Dictionary<string, object> gravDict = new Dictionary<string, object>();
                            gravDict["Gravity"] = (double)item.CustomProperties["Gravity"].value;
                            listDict.Add("Planet", gravDict);
                        }

                        itemsList.Add(listDict);
                    }
                    layer.Items.Reverse();
                }
                Dictionary<string, object> groupDict = new Dictionary<string, object>();
                ArrayList groupList = new ArrayList();
                groupDict["GroupName"] = "Default";
                groupDict["Children"] = itemsList;
                groupList.Add(groupDict);
                result["Layers"] = layersList;
                result["Objects"] = groupList;

                return result;
            }
            return new Dictionary<string, object>();
        }

        public override object Deserialize(IDictionary<string, object> dictionary, Type type, JavaScriptSerializer serializer)
        {
            if (dictionary == null)
                throw new ArgumentNullException("dictionary");

            if (type == typeof(Level))
            {
                // Create the instance to deserialize into.
                Level level = new Level();

                level.CanvasColor = new Color(
                    (byte)(int)(dictionary["CanvasColor"] as ArrayList)[1],
                    (byte)(int)(dictionary["CanvasColor"] as ArrayList)[2],
                    (byte)(int)(dictionary["CanvasColor"] as ArrayList)[3],
                    (byte)(int)(dictionary["CanvasColor"] as ArrayList)[0]);
                Constants.Instance.ColorBackground = level.CanvasColor;
                level.Scale = (int)dictionary["Scale"];

                ArrayList spawnList = dictionary["PlayerSpawn"] as ArrayList;
                float spawnX = 0.0f;
                if (spawnList[0] is decimal)
                    spawnX = (float)(decimal)spawnList[0];
                if (spawnList[0] is int)
                    spawnX = (float)(int)spawnList[0];

                float spawnY = 0.0f;
                if (spawnList[1] is decimal)
                    spawnY = (float)(decimal)spawnList[1];
                if (spawnList[1] is int)
                    spawnY = (float)(int)spawnList[1];
                level.PlayerSpawn = new Vector2(spawnX, spawnY);

                level.Music = dictionary["Music"] as String;
                level.ContentRootFolder = dictionary["ContentRootFolder"] as String;

                // Deserialize the ListItemCollection's items.
                Dictionary<int, object> layersDict = new Dictionary<int, object>();
                ArrayList itemsList = (ArrayList)dictionary["Layers"];
                foreach (Dictionary<string, object> layerDict in itemsList) 
                {

                    Layer layer = new Layer(layerDict["ID"] as String);
                    float scale = 1.0f;
                    if (layerDict["Scale"] is decimal)
                        scale = (float)(decimal)layerDict["Scale"];
                    if (layerDict["Scale"] is int)
                        scale = (float)(int)layerDict["Scale"];
                    layer.ScrollSpeed = new Microsoft.Xna.Framework.Vector2(1.0f/scale, 1.0f/scale);
                    layer.ZPos = (int)layerDict["Layer"];

                    level.Layers.Add(layer);

                    layersDict.Add((int)layerDict["Layer"], layer);
                //    level.Add(serializer.ConvertToType<ListItem>(itemsList[i]));
                }


                ArrayList objList = (ArrayList)dictionary["Objects"];
                foreach (Dictionary<string, object> groupDict in objList)
                {

                    ArrayList childList = (ArrayList)groupDict["Children"];
                    foreach (Dictionary<string, object> childDict in childList)
                    {

                        Vector2 position = new Vector2( 0.0f, 0.0f);
                        float rotation = 0.0f;

                        if (childDict.ContainsKey("Transform"))
                        {
                            Dictionary<string, object> transDict = childDict["Transform"] as Dictionary<string, object>;
                            ArrayList posList = transDict["Position"] as ArrayList;
                            float posX = 0.0f;
                            if (posList[0] is decimal)
                                posX = (float)(decimal)posList[0];
                            if (posList[0] is int)
                                posX = (float)(int)posList[0];

                            float posY = 0.0f;
                            if (posList[1] is decimal)
                                posY = (float)(decimal)posList[1];
                            if (posList[1] is int)
                                posY = (float)(int)posList[1];

                            position = new Vector2(posX * level.Scale, posY * level.Scale);
                            if (transDict["Rotation"] is decimal)
                                rotation = (float)(decimal)transDict["Rotation"];
                            if (transDict["Rotation"] is int)
                                rotation = (float)(int)transDict["Rotation"];
                        }

                        if (childDict.ContainsKey("Renderable"))
                        {
                            Dictionary<string, object> rendDict = childDict["Renderable"] as Dictionary<string, object>;
                            TextureItem item = new TextureItem(level.ContentRootFolder + '/' + rendDict["Image"] as String, position);
                            item.texture_filename = rendDict["Image"] as String;
                            item.Name = childDict["Name"] as String;
                            item.Rotation = rotation * MathHelper.Pi/180.0f;
                            item.TintColor = new Color(
                                (float)(int)(rendDict["ColorMod"] as ArrayList)[3],
                                (float)(int)(rendDict["ColorMod"] as ArrayList)[0],
                                (float)(int)(rendDict["ColorMod"] as ArrayList)[1],
                                (float)(int)(rendDict["ColorMod"] as ArrayList)[2]);

                            float xscale = 1.0f;
                            if (rendDict["xScale"] is decimal)
                                xscale = (float)(decimal)rendDict["xScale"];
                            if (rendDict["xScale"] is int)
                                xscale = (float)(int)rendDict["xScale"];
                            
                            float yscale = 1.0f;
                            if (rendDict["yScale"] is decimal)
                                yscale = (float)(decimal)rendDict["yScale"];
                            if (rendDict["yScale"] is int)
                                yscale = (float)(int)rendDict["yScale"];

                            item.setScale(new Vector2(xscale, yscale));

                            //Game Specific components
                            if (childDict.ContainsKey("Planet"))
                            {
                                Dictionary<string, object> gravDict = childDict["Planet"] as Dictionary<string, object>;
                                double gravity = 0.0f;
                                if (gravDict["Gravity"] is decimal)
                                    gravity = (double)(decimal)gravDict["Gravity"];
                                if (gravDict["Gravity"] is int)
                                    gravity = (double)(int)gravDict["Gravity"];
                                item.CustomProperties.Add("Gravity", new CustomProperty("Gravity", gravity, typeof(double), ""));
                            }

                            (layersDict[(int)rendDict["Layer"]] as Layer).Items.Add( item );
                        }

                        
                    }
                }

                //Engine draws things in reverse
                foreach (Layer layer in level.Layers)
                {
                    layer.Items.Reverse();
                }

                return level;
            }
            return null;
        }

    }
}