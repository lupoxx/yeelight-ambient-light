using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Yeelight
{
    public class Bulb
    {
        public enum EnumModel { mono, color, stripe }
        public enum EnumColorMode { color = 1, temperature = 2, hsv = 3 }

        public IPEndPoint location { get; private set; }
        public string id { get; private set; }
        public EnumModel model { get; private set; }
        public string firmware { get; private set; }
        public List<string> support { get; private set; }
        public bool power { get; private set; }
        public int bright { get; private set; }
        public EnumColorMode color_mode { get; private set; }

        public string rgb { get; private set; }
        public int hue { get; private set; }
        public int sat { get; private set; }
        public string name { get; private set; }

        private TcpClient connection;

        public Bulb(string response)
        {
            var ip = Regex.Match(response, @"Location: yeelight:\/\/([0-9]+\.[0-9]+\.[0-9]+\.[0-9]+):([0-9]+)").Groups[1].Value;
            var port = Regex.Match(response, @"Location: yeelight:\/\/([0-9]+\.[0-9]+\.[0-9]+\.[0-9]+):([0-9]+)").Groups[2].Value;
            this.location = new IPEndPoint(IPAddress.Parse(ip), Convert.ToInt32(port));

            this.id = Regex.Match(response, "id: (0x[0-z]+)\\r\\n").Groups[1].Value;
            this.model = (EnumModel)Enum.Parse(typeof(EnumModel), Regex.Match(response, "model: ([A-z]+)\\r\\n").Groups[1].Value);

            this.firmware = Regex.Match(response, "fw_ver: ([0-9]+)\\r\\n").Groups[1].Value;

            this.support = Regex.Match(response, "support: ([^\\\r]+)").Groups[1].Value.Split(' ').ToList();

            this.power = Regex.Match(response, "power: ([^\\\r]+)").Groups[1].Value == "on" ? true : false;

            this.bright = Convert.ToInt32(Regex.Match(response, "bright: ([^\\\r]+)").Groups[1].Value);

            this.color_mode = (EnumColorMode)Enum.Parse(typeof(EnumColorMode), Regex.Match(response, "color_mode: ([^\\\r]+)").Groups[1].Value);

            this.rgb = Regex.Match(response, "rgb: ([^\\\r]+)").Groups[1].Value;

            this.hue = Convert.ToInt32(Regex.Match(response, "hue: ([^\\\r]+)").Groups[1].Value);

            this.sat = Convert.ToInt32(Regex.Match(response, "sat: ([^\\\r]+)").Groups[1].Value);

            this.name = Regex.Match(response, "name: ([^\\\r]+)").Groups[1].Value;
        }

        internal void initConnection()
        {
            this.connection = new TcpClient();
            this.connection.Connect(this.location);
        }

        internal void closeConnection()
        {
            this.connection.Close();
        }

        public void setBrightness(int value)
        {
            string command = "{\"id\":1,\"method\":\"set_bright\",\"params\":[" + value + ", \"smooth\", 500]}\r\n";
            executeRemoteCall(command);
        }

        public void setColor(int R, int G, int B)
        {
            int rgb = R * 65536 + G * 256 + B;
            string command = "{\"id\":1,\"method\":\"set_rgb\",\"params\":[" + rgb + ", \"smooth\", 500]}\r\n";
            executeRemoteCall(command);
        }


        private void executeRemoteCall(string command)
        {
            // Translate the passed message into ASCII and store it as a Byte array.
            Byte[] data = System.Text.Encoding.ASCII.GetBytes(command);

            // Get a client stream for reading and writing. 
            NetworkStream stream = this.connection.GetStream();

            // Send the message to the connected TcpServer. 
            stream.Write(data, 0, data.Length); //(**This is to send data using the byte method**) 

            // Buffer to store the response bytes.
            data = new Byte[256];

            // String to store the response ASCII representation.
            String responseData = String.Empty;

            // Read the first batch of the TcpServer response bytes.
            Int32 bytes = stream.Read(data, 0, data.Length); //(**This receives the data using the byte method**)
            responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes); //(**This converts it to string**)
        }
    }
}
