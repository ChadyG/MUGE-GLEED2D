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
    public partial class LinkItemsForm : Form
    {
        public LinkItemsForm()
        {
            InitializeComponent();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            label2.Enabled = checkBox2.Enabled = checkBox1.Checked;
            
            textBox2.Text = "";
            updateSecondItem();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            updateSecondItem();
        }

        private void updateSecondItem()
        {
            textBox2.Enabled = checkBox1.Checked && !checkBox2.Checked;
            if (!checkBox1.Checked) return;
            if (checkBox2.Checked) textBox2.Text = textBox1.Text;
            
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            if (textBox1.Text=="")
            {
                MessageBox.Show("Please specify a name for the Custom Property that is to be added to the first Item!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (checkBox1.Checked && textBox2.Text=="")
            {
                MessageBox.Show("Please specify a name for the Custom Property that is to be added to the second Item!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (Editor.Instance.SelectedItems[0].CustomProperties.ContainsKey(textBox1.Text))
            {
                MessageBox.Show("The first Item (" + Editor.Instance.SelectedItems[0].Name + ") "+
                    "already has a Custom Property named \"" + textBox1.Text + "\". Please use another name!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            CustomProperty cp = new CustomProperty();
            cp.name = textBox1.Text;
            cp.type = typeof(Item);
            cp.value = Editor.Instance.SelectedItems[1];
            Editor.Instance.SelectedItems[0].CustomProperties.Add(cp.name, cp);

            if (checkBox1.Checked)
            {
                if (Editor.Instance.SelectedItems[1].CustomProperties.ContainsKey(textBox2.Text))
                {
                    MessageBox.Show("The second Item (" + Editor.Instance.SelectedItems[1].Name + ") " +
                        "already has a Custom Property named \"" + textBox2.Text + "\". Please use another name!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Editor.Instance.SelectedItems[0].CustomProperties.Remove(cp.name);
                    return;
                }
                cp = new CustomProperty();
                cp.name = textBox2.Text;
                cp.type = typeof(Item);
                cp.value = Editor.Instance.SelectedItems[0];
                Editor.Instance.SelectedItems[1].CustomProperties.Add(cp.name, cp);
            }
            
            MainForm.Instance.propertyGrid1.Refresh();
            this.Hide();
        }

        private void LinkItemsForm_VisibleChanged(object sender, EventArgs e)
        {
            groupBox1.Text = groupBox1.Text.Replace("$f", Editor.Instance.SelectedItems[0].Name);
            groupBox2.Text = groupBox2.Text.Replace("$s", Editor.Instance.SelectedItems[1].Name);
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            updateSecondItem();
        }



    }
}
