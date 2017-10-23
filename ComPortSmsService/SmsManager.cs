using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ComPortSmsService
{
    public class SmsManager
    {
        private ISerialPort _serialPort;
        public AutoResetEvent _receiveNow = new AutoResetEvent(false);

        public bool PortIsOpen => _serialPort.IsOpen;

        public SmsManager(ISerialPort serialPort/*, AutoResetEvent receiveNow*/)
        {
            _serialPort = serialPort;
            //_receiveNow = receiveNow;
        }
        public ISerialPort OpenPort(string portName, int baudRate,
            int dataBits, int readTimeout, int writeTimeout)
        {
            try
            {
                _serialPort.PortName = portName;          //COM1
                _serialPort.BaudRate = baudRate;          //9600
                _serialPort.DataBits = dataBits;          //8
                _serialPort.Parity = Parity.None;         //None
                _serialPort.StopBits = StopBits.One;      //1
                _serialPort.ReadTimeout = readTimeout;    //300
                _serialPort.WriteTimeout = writeTimeout;  //300
                _serialPort.Encoding = Encoding.GetEncoding("iso-8859-1");
                _serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceived);
                _serialPort.Open();
                _serialPort.DtrEnable = true;
                _serialPort.RtsEnable = true;
            }
            catch(Exception ex)
            {
                throw ex;
            }

            return _serialPort; // Why???
        }

        public void ClosePort()
        {
            try
            {
                _serialPort.Close();
                _serialPort.DataReceived -= new SerialDataReceivedEventHandler(DataReceived);
                //_serialPort = null;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public string ExecCommand(string command, int responseTimeout, string errorMessage)
        {
            try
            {
                _serialPort.DiscardOutBuffer();
                _serialPort.DiscardInBuffer();
                _receiveNow.Reset();
                _serialPort.Write(command + "\r");

                string input = ReadResponse(responseTimeout);
                if ((input.Length == 0) || ((!input.EndsWith("\r\n> ")) && (!input.EndsWith("\r\nOK\r\n"))))
                    throw new ApplicationException("No success message was received.");
                return input;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (e.EventType == SerialData.Chars)
                {
                    _receiveNow.Set();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public string ReadResponse(int timeout)
        {
            string buffer = string.Empty;
            try
            {
                do
                {
                    if (_receiveNow.WaitOne(timeout, false))
                    {
                        string t = _serialPort.ReadExisting();
                        buffer += t;
                    }
                    else
                    {
                        if (buffer.Length > 0)
                            throw new ApplicationException("Response received is incomplete.");
                        else
                            throw new ApplicationException("No data received from phone.");
                    }
                }
                while (!buffer.EndsWith("\r\nOK\r\n") && !buffer.EndsWith("\r\n> ") && !buffer.EndsWith("\r\nERROR\r\n"));
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return buffer;
        }

        public int CountMessages()
        {
            int totalMessagesCount = 0;
            try
            {
                // Execute Command
                string recievedData = ExecCommand("AT", 300, "No phone connected at ");
                recievedData = ExecCommand("AT+CMGF=1", 300, "Failed to set message format.");
                recievedData = ExecCommand("AT+CPMS?", 1000, "Failed to count SMS message");
                int receivedDataLength = recievedData.Length;

                // If command is executed successfully
                //if ((recievedData.Length >= 45) && (recievedData.StartsWith("AT+CPMS?")))
                if ((recievedData.Length >= 45) && (recievedData.StartsWith("+CPMS:")))
                {
                    // Parsing SMS
                    string[] strSplit = recievedData.Split(',');
                    string messageStorageArea1 = strSplit[0];     //SM
                    string messageExist1 = strSplit[1];           //Msgs exist in SM

                    // Count Total Number of SMS In SIM
                    totalMessagesCount = Convert.ToInt32(messageExist1);
                }

                // If command is not executed successfully
                else if (recievedData.Contains("ERROR"))
                {
                    // Error in Counting total number of SMS
                    string recievedError = recievedData;
                    recievedError = recievedError.Trim();
                    recievedData = "Following error occured while counting the message" + recievedError;

                }

                return totalMessagesCount;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public ShortMessageCollection ReadMessages(string command)
        {

            // Set up the phone and read the messages
            ShortMessageCollection messages = null;
            try
            {
                // Execute Command
                // Check connection
                ExecCommand("AT", 300, "No phone connected");
                // Use message format "Text mode"
                ExecCommand("AT+CMGF=1", 300, "Failed to set message format.");
                // Use character set "PCCP437"
                ExecCommand("AT+CSCS=\"PCCP437\"", 300, "Failed to set character set.");
                // Select SIM storage
                ExecCommand("AT+CPMS=\"SM\"", 300, "Failed to select message storage.");
                // Read the messages
                string input = ExecCommand(command, 5000, "Failed to read the messages.");

                // Parse messages
                messages = ParseMessages(input);

            }
            catch (Exception ex)
            {
                throw ex;
            }

            if (messages != null)
                return messages;
            else
                return null;

        }

        public ShortMessageCollection ParseMessages(string input)
        {
            ShortMessageCollection messages = new ShortMessageCollection();
            try
            {
                Regex r = new Regex(@"\+CMGL: (\d+),""(.+)"",""(.+)"",(.*),""(.+)""\r\n(.+)\r\n");
                Match m = r.Match(input);
                while (m.Success)
                {
                    ShortMessage msg = new ShortMessage();
                    //msg.Index = int.Parse(m.Groups[1].Value);
                    msg.Index = m.Groups[1].Value;
                    msg.Status = m.Groups[2].Value;
                    msg.Sender = m.Groups[3].Value;
                    msg.Alphabet = m.Groups[4].Value;
                    msg.Sent = m.Groups[5].Value;
                    msg.Message = m.Groups[6].Value;
                    messages.Add(msg);

                    m = m.NextMatch();
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            return messages;
        }


        public bool SendMessage(string phoneNo, string message)
        {
            bool isSend = false;

            try
            {

                string recievedData = ExecCommand("AT", 300, "No phone connected");
                recievedData = ExecCommand("AT+CMGF=1", 300, "Failed to set message format.");
                string command = "AT+CMGS=\"" + phoneNo + "\"";
                recievedData = ExecCommand(command, 300, "Failed to accept phoneNo");
                command = message + char.ConvertFromUtf32(26) + "\r";
                recievedData = ExecCommand(command, 3000, "Failed to send message"); //3 seconds
                if (recievedData.EndsWith("\r\nOK\r\n"))
                {
                    isSend = true;
                }
                else if (recievedData.Contains("ERROR"))
                {
                    isSend = false;
                }
                return isSend;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public bool DeleteMessage(string command)
        {
            bool isDeleted = false;
            try
            {

                // Execute Command
                string recievedData = ExecCommand("AT", 300, "No phone connected");
                recievedData = ExecCommand("AT+CMGF=1", 300, "Failed to set message format.");
                recievedData = ExecCommand(command, 300, "Failed to delete message");

                if (recievedData.EndsWith("\r\nOK\r\n"))
                {
                    isDeleted = true;
                }
                if (recievedData.Contains("ERROR"))
                {
                    isDeleted = false;
                }
                return isDeleted;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
