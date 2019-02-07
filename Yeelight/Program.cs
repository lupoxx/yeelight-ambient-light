using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Yeelight
{
    class Program
    {
        public const int THREASHOLD = 50;
        public const int BASE_BRIGHTNESS = 20;

        static void Main(string[] args)
        {
            string message = "M-SEARCH * HTTP/1.1\r\nHOST: 239.255.255.250:1982\r\nMAN: \"ssdp:discover\"\r\nST: wifi_bulb";

            var data = Encoding.ASCII.GetBytes(message);
            using (var udpClient = new UdpClient(5252))
            {
                var address = IPAddress.Parse("239.255.255.250");
                var ipEndPoint = new IPEndPoint(address, 1982);
                udpClient.JoinMulticastGroup(address);
                udpClient.Send(data, data.Length, ipEndPoint);
                udpClient.Close();
            }

            try
            {
                using (var udpReceiver = new UdpClient(5252))
                {
                    while (true)
                    {
                        udpReceiver.Client.ReceiveTimeout = 5000;
                        IPEndPoint remoteIPEndPoint = new IPEndPoint(IPAddress.Any, 5252);
                        byte[] content = udpReceiver.Receive(ref remoteIPEndPoint);

                        if (content.Length > 0)
                        {
                            string response = Encoding.UTF8.GetString(content);
                            var bulb = new Bulb(response);
                            BulbManager.instance.addBulb(bulb);
                        }
                    }
                }
            }
            catch (Exception ex) {; }

            //NON FUNZIONA
            //Servirebbe per ricevere le notifiche di quando una lampada entra in rete

            //using (var udpReceiver = new UdpClient(1982))
            //{
            //    //udpReceiver.Client.ReceiveTimeout = 15000;
            //    IPEndPoint remoteIPEndPoint = new IPEndPoint(IPAddress.Any, 1982);
            //    byte[] content = udpReceiver.Receive(ref remoteIPEndPoint);

            //    if (content.Length > 0)
            //    {
            //        string response = Encoding.UTF8.GetString(content);
            //    }
            //}

            //     private static void UDPReceiver(IAsyncResult res)
            //{
            //    IPEndPoint remoteIPEndPoint = new IPEndPoint(IPAddress.Any, 5252);
            //    byte[] content = udpReceiver.EndReceive(res, ref remoteIPEndPoint);

            //    if (content.Length > 0)
            //    {
            //        string response = Encoding.UTF8.GetString(content);
            //        var bulb = new Bulb(response);
            //        //BulbManager.instance.addBulb(bulb);
            //    }
            //    udpReceiver.BeginReceive(new AsyncCallback(UDPReceiver), null);
            //}

            //BulbManager.instance.bulbs.First().setBrightness(100);
            //BulbManager.instance.bulbs.First().setBrightness(50);
            //BulbManager.instance.bulbs.First().setBrightness(1);

            //BulbManager.instance.bulbs.First().setColor(255, 0, 0);
            //BulbManager.instance.bulbs.First().setColor(0, 255, 0);
            //BulbManager.instance.bulbs.First().setColor(0, 0, 255);

            int prevR = 0;
            int prevG = 0;
            int prevB = 0;
            foreach (var bulb in BulbManager.instance.bulbs.Where(p => p.model == Bulb.EnumModel.color))
                bulb.setBrightness(BASE_BRIGHTNESS);

            while (true)
            {
                try
                {
                    var screenshot = takeScreenShot();
                    int meanR = 0;
                    int meanG = 0;
                    int meanB = 0;

                    for (int x = 0; x < screenshot.Width; x++)
                    {
                        for (int y = 0; y < screenshot.Height; y++)
                        {
                            var threasholdGray = 5;

                            bool isGray = Math.Abs((screenshot.GetPixel(x, y).R - screenshot.GetPixel(x, y).G)) <= threasholdGray
                                && Math.Abs((screenshot.GetPixel(x, y).R - screenshot.GetPixel(x, y).B)) <= threasholdGray
                                && Math.Abs((screenshot.GetPixel(x, y).G - screenshot.GetPixel(x, y).B)) <= threasholdGray;

                            //if (isGray)
                            //    continue;

                            meanR += Convert.ToInt16(screenshot.GetPixel(x, y).R);
                            meanG += Convert.ToInt16(screenshot.GetPixel(x, y).G);
                            meanB += Convert.ToInt16(screenshot.GetPixel(x, y).B);
                        }
                    }

                    meanR = meanR / (screenshot.Width * screenshot.Height);
                    meanG = meanG / (screenshot.Width * screenshot.Height);
                    meanB = meanB / (screenshot.Width * screenshot.Height);

                    if (meanR == 0)
                        meanR = 1;
                    if (meanG == 0)
                        meanG = 1;
                    if (meanB == 0)
                        meanB = 1;

                    var brightness = (meanR + meanG + meanB) / 3 * BASE_BRIGHTNESS / 255;
                    if (brightness == 0)
                        brightness = 1;

                    if (hueChanged(prevR, prevG, prevB, meanR, meanG, meanB))
                    {

                        foreach (var bulb in BulbManager.instance.bulbs.Where(p => p.model == Bulb.EnumModel.color))
                        {
                            bulb.setColor(meanR, meanG, meanB);
                            bulb.setBrightness(brightness);
                        }
                        prevR = meanR;
                        prevG = meanG;
                        prevB = meanB;
                    }
                    screenshot.Dispose();
                    System.GC.Collect();
                    System.GC.WaitForPendingFinalizers();
                    Thread.Sleep(200);
                }
                catch (Exception ex) {; }
            }
        }

        public static Bitmap takeScreenShot()
        {
            //Create a new bitmap.
            var bmpScreenshot = new Bitmap(Screen.PrimaryScreen.Bounds.Width,
                                           Screen.PrimaryScreen.Bounds.Height,
                                           PixelFormat.Format32bppArgb);

            // Create a graphics object from the bitmap.
            var gfxScreenshot = Graphics.FromImage(bmpScreenshot);

            // Take the screenshot from the upper left corner to the right bottom corner.
            gfxScreenshot.CopyFromScreen(Screen.PrimaryScreen.Bounds.X,
                                        Screen.PrimaryScreen.Bounds.Y,
                                        0,
                                        0,
                                        Screen.PrimaryScreen.Bounds.Size,
                                        CopyPixelOperation.SourceCopy);

            bmpScreenshot = CropBitmap(bmpScreenshot, Screen.PrimaryScreen.Bounds.Width / 3, Screen.PrimaryScreen.Bounds.Height / 3, Screen.PrimaryScreen.Bounds.Width / 3, Screen.PrimaryScreen.Bounds.Height / 3);

            // Save the screenshot to the specified path that the user has chosen.
            //bmpScreenshot.Save("Screenshot.png", ImageFormat.Png);
            return bmpScreenshot;
        }

        public static Bitmap CropBitmap(Bitmap bitmap, int cropX, int cropY, int cropWidth, int cropHeight)
        {
            Rectangle rect = new Rectangle(cropX, cropY, cropWidth, cropHeight);
            Bitmap cropped = bitmap.Clone(rect, bitmap.PixelFormat);
            return cropped;
        }

        public static bool hueChanged(int prevR, int prevG, int prevB, int R, int G, int B)
        {

            return (Math.Abs(prevR - R) >= THREASHOLD
                || Math.Abs(prevG - G) >= THREASHOLD
                || Math.Abs(prevB - B) >= THREASHOLD);
        }
    }


}


