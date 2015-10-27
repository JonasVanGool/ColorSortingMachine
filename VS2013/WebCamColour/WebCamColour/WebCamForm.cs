using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using WebCamColour;
using System.IO.Ports;
using Emgu.CV;
using Emgu.CV.UI;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.VideoSurveillance;
using System.Xml;
using System.IO;


namespace WebCamColour
{
    public partial class WebCamColour : Form
    {
        private static MCvFont _font = new MCvFont(Emgu.CV.CvEnum.FONT.CV_FONT_HERSHEY_SIMPLEX, 1.0, 1.0);
        private static Capture _cameraCapture;
        private static Image<Gray, Byte> modelImage;
        private static IBGFGDetector<Bgr> _detector;
        private static Queue<Byte> _byteReceivedQueue;
        private static SerialPort _serialPort;
        private static bool _serialIsOpen = false;
        private static bool _webCamIsOpen = false;
        private static CaptureDeviceEnumerator x = new CaptureDeviceEnumerator();
        private byte _minR = 0, _maxR = 255;
        private byte _minG = 0, _maxG = 255;
        private byte _minB = 0, _maxB = 255;
        private int _GlobalColor = 0;
        private int _GlobalComboBoxIndex = 0;
        private int _CountWhite = 0;
        private bool _inputReady = false;
        private byte[] _inputMessage;
        private static int _numberOfFilters = 11;
        private string _savePath = "c:\\temp\\colorSorterFilters.xml";

        // filters
        private Filter[] _filters = new Filter[_numberOfFilters];


        public WebCamColour()
        {
            _inputMessage = new byte[4];
            _serialPort = new SerialPort();
            InitializeComponent();

            //check if xml file exist
            if (File.Exists(@_savePath))
            {
                System.Xml.Serialization.XmlSerializer reader = new System.Xml.Serialization.XmlSerializer(typeof(Filter[]));
                System.IO.StreamReader file = new System.IO.StreamReader(@_savePath);
                _filters= (Filter[]) reader.Deserialize(file);
                file.Close();
                Console.Write("xmlRead");
            }
            else
            {
                _filters[0] = new Filter("Grey",1000,new Bgr(0, 0, 0),new Bgr(0, 0, 0));
                _filters[1] = new Filter("Black",1000,new Bgr(0, 0, 0),new Bgr(50, 50, 50));
                _filters[2] = new Filter("Blue",1000,new Bgr(150, 30, 0),new Bgr(255, 100, 50));
                _filters[3] = new Filter("Green",1000,new Bgr(0, 50, 0),new Bgr(80, 200, 80));
                _filters[4] = new Filter("Red",1000,new Bgr(0, 0, 100),new Bgr(80, 80, 255));
                _filters[5] = new Filter("Yellow",1000,new Bgr(30, 100, 150),new Bgr(120, 200, 250));
                _filters[6] = new Filter("White",1000,new Bgr(225, 225, 225),new Bgr(255, 255, 255));
                _filters[7] = new Filter("Spare",1000,new Bgr(0, 0, 0),new Bgr(0, 0, 0));
                _filters[8] = new Filter("Spare",1000,new Bgr(0, 0, 0),new Bgr(0, 0, 0));
                _filters[9] = new Filter("Spare",1000,new Bgr(0, 0, 0),new Bgr(0, 0, 0));
                _filters[10] = new Filter("Spare",1000,new Bgr(0, 0, 0),new Bgr(0, 0, 0));
                Console.Write("xmlSave");      
                //save filters to xml file
                saveFilters(_filters);
            }

            // comboBox1 initialize
            foreach (string comPortName in SerialPort.GetPortNames())
            {
                this.comboBox1.Items.Add(comPortName);
            }
            this.comboBox1.SelectedIndex = SerialPort.GetPortNames().Count()-1;

            // comboBox2 initialize
            foreach (DirectShowLib.DsDevice d in x.AvailableVideoInputDevices)
            {
                this.comboBox2.Items.Add(d.Name);
            }
            this.comboBox2.SelectedIndex = 0;

            // comboBox2 initialize
            foreach (Filter filter in _filters)
            {
                this.comboBox3.Items.Add(filter._name);
            }
            this.comboBox3.SelectedIndex = 0;


        }

        void ProcessFrame(object sender, EventArgs e)
        {
            Image<Bgr, Byte> img = _cameraCapture.QueryFrame();
            Image<Gray, Byte> imgRange;
            img._SmoothGaussian(3);
            imgRange = img.InRange(new Bgr(_minB, _minG, _minR),new Bgr(_maxB, _maxG, _maxR) );
            imageBox1.Image = imgRange.Resize(320, 240, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);
            imageBox2.Image = img.Resize(320, 240, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);
            textBox8.Text = _filters[_GlobalColor]._name +" c:" + _CountWhite.ToString();
            textBox9.Text = GetCount(img, _filters[_GlobalComboBoxIndex]).ToString();
        }

        void handelImage()
        {
            if (_cameraCapture == null)
            {
                byte[] message = new byte[4];
                message[0] = 3;
                message[1] = 0;
                message[2] = 0;
                message[3] = 101;
                _serialPort.Write(message, 0, 4);
                return;
            }
            Image<Bgr, Byte> img = _cameraCapture.QueryFrame();
            int colour = GetExpectedColour(img,_filters);
            _GlobalColor = colour;
            Image<Gray, Byte> imgRange;
            img._SmoothGaussian(3);
            imgRange = img.InRange(_filters[_GlobalColor]._minBgr, _filters[_GlobalColor]._maxBgr);
            imageBox1.Image = imgRange.Resize(320, 240, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);
            imageBox2.Image = img.Resize(320, 240, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);
        }
        private void WebCamColour_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!_serialIsOpen)
            {
                try
                {
                    _serialPort = new SerialPort(comboBox1.SelectedItem.ToString(), int.Parse(textBox1.Text),Parity.None, 8, StopBits.One);
                    _serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
                    // Set the read/write timeouts
                    _serialPort.ReadTimeout = 500;
                    _serialPort.WriteTimeout = 500;

                    _serialPort.Open();
                }
                catch (Exception e1)
                {
                    MessageBox.Show(e1.Message);
                    return;
                }
                _serialIsOpen = true;
            }
            else
            {
                _serialPort = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
                try
                {
                    _serialPort = new SerialPort(comboBox1.SelectedItem.ToString(), int.Parse(textBox1.Text),Parity.None, 8, StopBits.One);
                    _serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
                    // Set the read/write timeouts
                    _serialPort.ReadTimeout = 500;
                    _serialPort.WriteTimeout = 500;

                    _serialPort.Open();
                }
                catch (Exception e1)
                {
                    MessageBox.Show(e1.Message);
                    return;
                }
            }
        }

        private void DataReceivedHandler(
                    object sender,
                    SerialDataReceivedEventArgs e)
        {
            
            while (_serialPort.BytesToRead == 0) { }
            _inputMessage[0] = (byte)_serialPort.ReadByte();
            while (_serialPort.BytesToRead == 0) { }
            _inputMessage[1] = (byte)_serialPort.ReadByte();
            while (_serialPort.BytesToRead == 0) { }
            _inputMessage[2] = (byte)_serialPort.ReadByte();
            while (_serialPort.BytesToRead == 0) { }
            _inputMessage[3] = (byte)_serialPort.ReadByte();

            _inputReady = true;

            handelImage();

            byte[] message = new byte[4];
            message[0] = 1;
            message[1] = (byte)_GlobalColor;
            message[2] = 0;
            message[3] = 101;
            _serialPort.Write(message, 0, 4);

            _inputReady = false;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (!_webCamIsOpen)
            {
                _webCamIsOpen = true;
                try
                {
                    _cameraCapture = new Capture(comboBox2.SelectedIndex);
                }
                catch (Exception e1)
                {
                    MessageBox.Show(e1.Message);
                    return;
                }
                Application.Idle += ProcessFrame;
            }
            else
            {
                _webCamIsOpen = true;
                try
                {
                    _cameraCapture = null;
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    _cameraCapture = new Capture(comboBox2.SelectedIndex);
                }
                catch (Exception e2)
                {
                    MessageBox.Show(e2.Message);
                    return;
                }
            }
        }

        private void textBox6_TextChanged(object sender, EventArgs e)
        {
            if (textBox6.Text != "")
            {
                try
                {
                    _maxB = byte.Parse(textBox6.Text);
                }
                catch (FormatException)
                {
                    Console.WriteLine("Unable to parse '{0}'.", textBox6.Text);
                }
                catch (OverflowException)
                {
                    Console.WriteLine("'{0}' is greater than {1} or less than {2}.",
                                      textBox6.Text, Byte.MaxValue, Byte.MinValue);
                }
            }
        }

        private void textBox7_TextChanged(object sender, EventArgs e)
        {
            if (textBox7.Text != "")
            {
                try
                {
                    _minB = byte.Parse(textBox7.Text);
                }
                catch (FormatException)
                {
                    Console.WriteLine("Unable to parse '{0}'.", textBox7.Text);
                }
                catch (OverflowException)
                {
                    Console.WriteLine("'{0}' is greater than {1} or less than {2}.",
                                      textBox7.Text, Byte.MaxValue, Byte.MinValue);
                }
            }
        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        { 
            if (textBox5.Text != "")
            {
                try
                {
                    _minG = byte.Parse(textBox5.Text);
                }
                catch (FormatException)
                {
                    Console.WriteLine("Unable to parse '{0}'.", textBox5.Text);
                }
                catch (OverflowException)
                {
                    Console.WriteLine("'{0}' is greater than {1} or less than {2}.",
                                      textBox5.Text, Byte.MaxValue, Byte.MinValue);
                }
            }
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            if (textBox4.Text != "")
            {
                try
                {
                    _maxG = byte.Parse(textBox4.Text);
                }
                catch (FormatException)
                {
                    Console.WriteLine("Unable to parse '{0}'.", textBox4.Text);
                }
                catch (OverflowException)
                {
                    Console.WriteLine("'{0}' is greater than {1} or less than {2}.",
                                      textBox4.Text, Byte.MaxValue, Byte.MinValue);
                }
            }
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            if (textBox3.Text != "")
            {
                try
                {
                    _maxR = byte.Parse(textBox3.Text);
                }
                catch (FormatException)
                {
                    Console.WriteLine("Unable to parse '{0}'.", textBox3.Text);
                }
                catch (OverflowException)
                {
                    Console.WriteLine("'{0}' is greater than {1} or less than {2}.",
                                      textBox3.Text, Byte.MaxValue, Byte.MinValue);
                }
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            if (textBox2.Text != "")
            {
                try
                {
                    _minR = byte.Parse(textBox2.Text);
                }
                catch (FormatException)
                {
                    Console.WriteLine("Unable to parse '{0}'.", textBox2.Text);
                }
                catch (OverflowException)
                {
                    Console.WriteLine("'{0}' is greater than {1} or less than {2}.",
                                      textBox2.Text, Byte.MaxValue, Byte.MinValue);
                }
            }
        }

        private int GetExpectedColour(Image<Bgr, Byte> img, Filter[] filters)
        {
            int[] filtertImgsCounts = new int[filters.Count()];
            for (int i = 0; i < filters.Count(); i++)
            {
                Image<Gray, Byte> Filtert = img.InRange(filters[i]._minBgr, _filters[i]._maxBgr);
                filtertImgsCounts[i] = (Filtert.CountNonzero()[0] > filters[i]._minCount) ? Filtert.CountNonzero()[0] : 0;
            }
            int maxValue = filtertImgsCounts.Max();
            _CountWhite = maxValue;
            return filtertImgsCounts.ToList().IndexOf(maxValue);
        }

        private int GetCount(Image<Bgr, Byte> img, Filter filter)
        {
            Image<Gray, Byte> Filtert = img.InRange(filter._minBgr, filter._maxBgr);
            return Filtert.CountNonzero()[0];
        }

        private void button3_Click(object sender, EventArgs e)
        {
            _filters[comboBox3.SelectedIndex]._minBgr = new Bgr(byte.Parse(textBox7.Text), byte.Parse(textBox5.Text), byte.Parse(textBox2.Text));
            _filters[comboBox3.SelectedIndex]._maxBgr = new Bgr(byte.Parse(textBox6.Text), byte.Parse(textBox4.Text), byte.Parse(textBox3.Text));
            saveFilters(_filters);
        }

        T[] InitializeArray<T>(int length) where T : new()
        {
            T[] array = new T[length];
            for (int i = 0; i < length; ++i)
            {
                array[i] = new T();
            }
            return array;
        }

        public void saveFilters(Filter[] filters)
        {
            System.Xml.Serialization.XmlSerializer writer = new System.Xml.Serialization.XmlSerializer(typeof(Filter[]));
            System.IO.StreamWriter file = new System.IO.StreamWriter(
                @_savePath);
            writer.Serialize(file,filters);
            file.Close();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            byte[] message = new byte[4];
            message[0] = 2;
            message[1] = 210;
            message[2] = 0;
            message[3] = 101;
            _serialPort.Write(message,0,4);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            byte[] message = new byte[4];
            message[0] = 3;
            message[1] = 0;
            message[2] = 0;
            message[3] = 101;
            _serialPort.Write(message, 0, 4);
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            textBox2.Text = _filters[comboBox3.SelectedIndex]._minBgr.Red.ToString();
            textBox5.Text = _filters[comboBox3.SelectedIndex]._minBgr.Green.ToString();
            textBox7.Text = _filters[comboBox3.SelectedIndex]._minBgr.Blue.ToString();
            textBox3.Text = _filters[comboBox3.SelectedIndex]._maxBgr.Red.ToString();
            textBox4.Text = _filters[comboBox3.SelectedIndex]._maxBgr.Green.ToString();
            textBox6.Text = _filters[comboBox3.SelectedIndex]._maxBgr.Blue.ToString();
            _GlobalComboBoxIndex = comboBox3.SelectedIndex;
        }
    }
}
