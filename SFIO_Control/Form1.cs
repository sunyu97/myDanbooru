using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Sunfish.IO;

namespace SFIO_Control
{
    public partial class Form1 : Form
    {
        //public ServerFactory sf = new ServerFactory();

        public Form1()
        {
            InitializeComponent();
        }

        public SFIO big = new SFIO();

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void button1_Click(object sender, EventArgs e)
        {
            big.loadSFIO(System.IO.Path.GetDirectoryName(Application.ExecutablePath) + "\\bigfile");
            label1.Text = big.maxSeq.ToString();
            label2.Text = big.maxOffset.ToString();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            big.initSFIO(System.IO.Path.GetDirectoryName(Application.ExecutablePath) + "\\bigfile");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string[] po = System.IO.File.ReadAllLines(openFileDialog1.FileName);
                big.putFile_(po);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            System.IO.MemoryStream m = big.getFile(textBox1.Text);
            pictureBox1.Image = Image.FromStream(m);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            big.writeIndex();
        }

        private void button7_Click(object sender, EventArgs e)
        {
        }

        private void button8_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string[] po = System.IO.File.ReadAllLines(openFileDialog1.FileName);
                big.putFile_2(po);
            }
        }
    }
}