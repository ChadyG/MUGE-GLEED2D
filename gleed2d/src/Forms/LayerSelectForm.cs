using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace GLEED2D
{
    public partial class LayerSelectForm : Form
    {
        public LayerSelectForm()
        {
            InitializeComponent();
        }

        private void LayerSelectForm_Load(object sender, EventArgs e)
        {
            treeView1.ImageList = MainForm.Instance.treeView1.ImageList;
            treeView1.ImageIndex = treeView1.SelectedImageIndex = 5;
            TreeNode rootnode = treeView1.Nodes.Add(Editor.Instance.level.Name);
            foreach (Layer l in Editor.Instance.level.Layers)
            {
                TreeNode node = rootnode.Nodes.Add(l.Name);
                node.Tag = l;
                node.ImageIndex = node.SelectedImageIndex = 0;
            }
            rootnode.Expand();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            Close();
            DialogResult = DialogResult.Cancel;
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            Close();
            DialogResult = DialogResult.OK;
        }

    
    
    
    }
}
