using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.Diagnostics;
using System.Windows.Forms;


namespace GLEED2D
{
    [DebuggerDisplay("Name: {Name}, Address: {&Name}")]
    public partial class Level : IUndoable
    {

        public void add()
        {
        }

        public void remove()
        {
        }

        public IUndoable cloneforundo()
        {
            selecteditems = "";
            foreach (Item i in Editor.Instance.SelectedItems) selecteditems += i.Name + ";";
            if (Editor.Instance.SelectedLayer != null) selectedlayers = Editor.Instance.SelectedLayer.Name;


            Level result = (Level)this.MemberwiseClone();
            result.Layers = new List<Layer>(Layers);
            for (int i = 0; i < result.Layers.Count; i++)
            {
                result.Layers[i] = result.Layers[i].clone();
                result.Layers[i].level = result;
            }
            return (IUndoable)result;
        }

        public void makelike(IUndoable other)
        {
            /*Layer l2 = (Layer)other;
            Items = l2.Items;
            treenode.Nodes.Clear();
            foreach (Item i in Items)
            {
                Editor.Instance.addItem(i);
            }*/


            Level l2 = (Level)other;
            Layers = l2.Layers;
            treenode.Nodes.Clear();
            foreach (Layer l in Layers)
            {
                Editor.Instance.addLayer(l);
                //TODO add all items
            }
        }

        public string getName()
        {
            return Name;
        }

        public void setName(string name)
        {
            Name = name;
            treenode.Text = name;
        }

    }
}
