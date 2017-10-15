using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComPortSmsService
{
    [ExcludeFromCodeCoverage]
    public class SystemSerialPort : ISerialPort
    {
        private SerialPort _serialPort;

        public SystemSerialPort()
        {
            _serialPort = new SerialPort();
        }

        public SystemSerialPort(IContainer container)
        {
            _serialPort = new SerialPort(container);
        }

        public SystemSerialPort(string portName)
        {
            _serialPort = new SerialPort(portName);
        }

        public SystemSerialPort(string portName, int baudRate)
        {
            _serialPort = new SerialPort(portName, baudRate);
        }

        public SystemSerialPort(string portName, int baudRate, Parity parity)
        {
            _serialPort = new SerialPort(portName, baudRate, parity);
        }

        public SystemSerialPort(string portName, int baudRate, Parity parity, int dataBits)
        {
            _serialPort = new SerialPort(portName, baudRate, parity, dataBits);
        }

        public SystemSerialPort(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            _serialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
        }



        public Stream BaseStream => _serialPort.BaseStream;
        public int BaudRate
        {
            get { return _serialPort.BaudRate; }
            set { _serialPort.BaudRate = value; }
        }
        public bool BreakState
        {
            get { return _serialPort.BreakState; }
            set { _serialPort.BreakState = value; }
        }
        public int BytesToRead => _serialPort.BytesToRead;
        public int BytesToWrite => _serialPort.BytesToWrite;
        public bool CDHolding => _serialPort.CDHolding;
        public bool CtsHolding => _serialPort.CtsHolding;
        public int DataBits
        {
            get { return _serialPort.DataBits; }
            set { _serialPort.DataBits = value; }
        }
        public bool DiscardNull
        {
            get { return _serialPort.DiscardNull; }
            set { _serialPort.DiscardNull = value; }
        }
        public bool DsrHolding => _serialPort.DsrHolding;
        public bool DtrEnable
        {
            get { return _serialPort.DtrEnable; }
            set { _serialPort.DtrEnable = value; }
        }
        public Encoding Encoding
        {
            get { return _serialPort.Encoding; }
            set { _serialPort.Encoding = value; }
        }
        public Handshake Handshake
        {
            get { return _serialPort.Handshake; }
            set { _serialPort.Handshake = value; }
        }
        public bool IsOpen => _serialPort.IsOpen;
        public string NewLine
        {
            get { return _serialPort.NewLine; }
            set { _serialPort.NewLine = value; }
        }
        public Parity Parity
        {
            get { return _serialPort.Parity; }
            set { _serialPort.Parity = value; }
        }
        public byte ParityReplace
        {
            get { return _serialPort.ParityReplace; }
            set { _serialPort.ParityReplace = value; }
        }
        public string PortName
        {
            get { return _serialPort.PortName; }
            set { _serialPort.PortName = value; }
        }
        public int ReadBufferSize
        {
            get { return _serialPort.ReadBufferSize; }
            set { _serialPort.ReadBufferSize = value; }
        }
        public int ReadTimeout
        {
            get { return _serialPort.ReadTimeout; }
            set { _serialPort.ReadTimeout = value; }
        }
        public int ReceivedBytesThreshold
        {
            get { return _serialPort.ReceivedBytesThreshold; }
            set { _serialPort.ReceivedBytesThreshold = value; }
        }
        public bool RtsEnable
        {
            get { return _serialPort.RtsEnable; }
            set { _serialPort.RtsEnable = value; }
        }
        public StopBits StopBits
        {
            get { return _serialPort.StopBits; }
            set { _serialPort.StopBits = value; }
        }
        public int WriteBufferSize
        {
            get { return _serialPort.WriteBufferSize; }
            set { _serialPort.WriteBufferSize = value; }
        }
        public int WriteTimeout
        {
            get { return _serialPort.WriteTimeout; }
            set { _serialPort.WriteTimeout = value; }
        }

        // https://timdams.com/2011/05/23/writing-an-event-wrapper/
        // https://webcache.googleusercontent.com/search?q=cache:Irhd9qKSFo8J:https://timdams.com/2011/05/23/writing-an-event-wrapper/+&cd=7&hl=sv&ct=clnk&gl=se&client=firefox-b
        // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/add
        public event SerialDataReceivedEventHandler DataReceived
        {
            add { _serialPort.DataReceived += value; }
            remove { _serialPort.DataReceived -= value; }
        }
        public event SerialErrorReceivedEventHandler ErrorReceived
        {
            add { _serialPort.ErrorReceived += value; }
            remove { _serialPort.ErrorReceived -= value;  }
        }
        public event SerialPinChangedEventHandler PinChanged
        {
            add { _serialPort.PinChanged += value; }
            remove { _serialPort.PinChanged += value; }
        }

        public void Close() => _serialPort.Close();
        public void DiscardInBuffer() => _serialPort.DiscardInBuffer();
        public void DiscardOutBuffer() => _serialPort.DiscardOutBuffer();
        public static string[] GetPortNames() => SerialPort.GetPortNames();
        string[] ISerialPort.GetPortNames() => GetPortNames();
        public void Open() => _serialPort.Open();
        public int Read(byte[] buffer, int offset, int count) => _serialPort.Read(buffer, offset, count);
        public int Read(char[] buffer, int offset, int count) => _serialPort.Read(buffer, offset, count);
        public int ReadByte() => _serialPort.ReadByte();
        public int ReadChar() => _serialPort.ReadChar();
        public string ReadExisting() => _serialPort.ReadExisting();
        public string ReadLine() => _serialPort.ReadLine();
        public string ReadTo(string value) => _serialPort.ReadTo(value);
        public void Write(string text) => _serialPort.Write(text);
        public void Write(char[] buffer, int offset, int count) => _serialPort.Write(buffer, offset, count);
        public void Write(byte[] buffer, int offset, int count) => _serialPort.Write(buffer, offset, count);
        public void WriteLine(string text) => _serialPort.WriteLine(text);

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~SystemSerialPort() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
