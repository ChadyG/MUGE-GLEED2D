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
    public partial class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponent();
            textBox1.Text = textBox1.Text.Replace("{v}", Editor.Instance.Version);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://gleed2d.codeplex.com");
        }
    }
}
