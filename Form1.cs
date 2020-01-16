using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace TheApplicationGUI
{

    public partial class Form1 : Form
    {
        TextWriter _writer = null; // required to override the Write and WriteLine methods to the textbox
        NewSystem myLuceneApp = new NewSystem(); // instantiates an object of the "The Application" class

        public Form1()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Overrides the Console Write and WriteLine methods to output to textbox
        /// </summary>
        /// <remarks>
        /// Clicking the begin button calls ButtonStart_Click, which runs the program
        /// </remarks>
        private void Form1_Load(object sender, EventArgs e)
        {
            TextBoxStreamWriter writer = new TextBoxStreamWriter(txtConsole);
            Console.SetOut(writer);
            MessageBox.Show("Click Begin to start loading");
        }

        private void TextBoxOutput_TextChanged(object sender, EventArgs e)
        {

        }

        public void ButtonEnter_Click(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// Initiates the bulk of the code (in Program.cs)
        /// </summary>
        /// <remarks>
        /// This button is disable after its first usage
        /// </remarks>
        private void ButtonStart_Click(object sender, EventArgs e)
        {
            this.Enabled = false;
            myLuceneApp.Run(); // the Run() method initiates all the functionality
        }
    }

    public class TextBoxStreamWriter : TextWriter
    {
        TextBox _output = null;

        public TextBoxStreamWriter(TextBox output)
        {
            _output = output;
        }

        /// <summary>
        /// Overrides the Console.Write method
        /// </summary>
        public override void Write(char value)
        {
            base.Write(value);
            _output.AppendText(value.ToString());

        }

        /// <summary>
        /// Overrides the Console.WriteLine method
        /// </summary>
        public override void WriteLine(char value)
        {
            base.Write(value);
            _output.AppendText(value.ToString());

        }

        /// <summary>
        /// Sets the character encoding for the Write and WriteLine methods
        /// </summary>
        public override Encoding Encoding
        {
            get { return System.Text.Encoding.UTF8; }
        }
    }


}
