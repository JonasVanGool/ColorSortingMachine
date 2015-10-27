using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Threading;
using System.Windows.Forms;

namespace WebCamColour
{
    class CommController
    {
        private SerialPort _serialPort;
        private static Queue<Byte> _byteReceivedQueue;
        private Thread readThread;
        private int isReading;

        public CommController(String portName)
        {
            // Create a new SerialPort object with default settings.
            _serialPort = new SerialPort(portName,9600);
            _serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
            _byteReceivedQueue = new Queue<Byte>();
            isReading = -1;
        }

        public bool Begin(int baudRate){
            _serialPort.BaudRate = baudRate;
                try
                {
                    if(!_serialPort.IsOpen)
                        _serialPort.Open();
                    return true;
                }
                catch
                {
                    MessageBox.Show("There was an error. Please make sure that the correct port was selected, and the device, plugged in.");
                    return false;
                }
                
        }

        public bool IsAvailable()
        {
            return (_byteReceivedQueue.Count != 0) ? true : false;
        }

        public byte Read()
        {
            return _byteReceivedQueue.Dequeue();
        }

        public bool Stop()
        {
            try
            {
                _serialPort.Close();
            }
            catch
            {
                return false;
            }
            return true;
        }
        public bool Write(byte inputByte)
        {
            Byte[] input = new Byte[1];
            input[0] = inputByte;
            try
            {
                _serialPort.Write(input,0,1);
                return true;
            }
            catch
            {
                MessageBox.Show("There was an error. Please make sure that the correct port was selected, and the device, plugged in.");
                return false;
            }    
        }

        private static void DataReceivedHandler(
                    object sender,
                    SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            byte indata = (byte)sp.ReadByte();
            _byteReceivedQueue.Enqueue(indata);
            Console.WriteLine("Data Received:");
            Console.Write(indata);
        }
    }
}
