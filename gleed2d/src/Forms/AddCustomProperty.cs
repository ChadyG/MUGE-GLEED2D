using System;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GLEED2D
{
    public partial class AddCustomProperty : Form
    {
        public CustomProperty cp = new CustomProperty();

        SerializableDictionary customproperties;


        public AddCustomProperty(SerializableDictionary currentproperties)
        {
            InitializeComponent();
            customproperties = currentproperties;
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {

            if (customproperties.ContainsKey(textBox1.Text))
            {
                MessageBox.Show("A Custom Property with that name already exists.");
                return;
            }

            if (textBox1.Text.Length==0)
            {
                MessageBox.Show("Please enter a Property Name.");
                return;
            }

            //CustomProperty cp = new CustomProperty();
            cp.name = textBox1.Text;
            cp.description = textBox2.Text;
            if (radioButton1.Checked)
            {
                cp.type = typeof(string);
                cp.value = "";
            }
            if (radioButton2.Checked)
            { 
                cp.type = typeof(bool);
                cp.value = false;
            }
            if (radioButton7.Checked)
            {
                cp.type = typeof(int);
                cp.value = 1;
            }
            if (radioButton6.Checked)
            {
                cp.type = typeof(double);
                cp.value = 1.0;
            }
            if (radioButton3.Checked)
            {
                cp.type = typeof(Vector2);
                cp.value = new Vector2(1, 1);
            }
            if (radioButton4.Checked)
            {
                cp.type = typeof(Color);
                cp.value = Color.White;
            }
            if (radioButton5.Checked)
            {
                cp.type = typeof(Item);
                cp.value = null;
            }
            
            customproperties[cp.name] = cp;
            this.Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void radioButton6_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void radioButton7_CheckedChanged(object sender, EventArgs e)
        {

        }


    }
}
