using System;
using System.IO.Ports;
using System.Text.RegularExpressions;

namespace FreeKantar.Services
{
    public class SerialService : IDisposable
    {
        private SerialPort _serialPort;
        public event Action<double> OnWeightReceived;
        public event Action<string> OnError;

        public bool IsOpen => _serialPort?.IsOpen ?? false;

        public void Start(string portName, int baudRate)
        {
            try
            {
                Stop();
                _serialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
                _serialPort.DataReceived += SerialPort_DataReceived;
                _serialPort.Open();
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"COM Port Hatası: {ex.Message}");
            }
        }

        public void Stop()
        {
            if (_serialPort != null)
            {
                if (_serialPort.IsOpen) _serialPort.Close();
                _serialPort.DataReceived -= SerialPort_DataReceived;
                _serialPort.Dispose();
                _serialPort = null;
            }
        }

        private string _buffer = "";
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string data = _serialPort.ReadExisting();
                _buffer += data;

                // Prevent buffer overflow if no terminator is found
                if (_buffer.Length > 1024) _buffer = _buffer.Substring(_buffer.Length - 512);

                while (_buffer.Contains("\n") || _buffer.Contains("\r"))
                {
                    int index = _buffer.IndexOfAny(new char[] { '\n', '\r' });
                    string line = _buffer.Substring(0, index).Trim();
                    _buffer = _buffer.Substring(index + 1);

                    if (string.IsNullOrEmpty(line)) continue;

                    var match = Regex.Match(line, @"(\d+[\.,]?\d*)");
                    if (match.Success)
                    {
                        if (double.TryParse(match.Groups[1].Value.Replace(",", "."), out double weight))
                        {
                            OnWeightReceived?.Invoke(weight);
                        }
                    }
                }
            }
            catch { }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
