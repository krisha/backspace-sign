using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace SignSineWave2
{
    class Program
    {

        const string host = "schild";

        /// <summary>
        /// generates sine wave with micro steps (fading of leds)
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {

            const int LED_TOTAL = 126;
            const int LED_USED = 126;

            Random r = new Random();

            int offset = 0;

            UdpClient client = new UdpClient();

            byte[] ledData = new byte[LED_TOTAL * 3];
            double[] sinewave = new double[500];

            double currentRed = 1.0;
            double currentBlue = 1.0;
            double currentGreen = 1.0;

            double targetRed = r.NextDouble();
            double targetBlue = r.NextDouble();
            double targetGreen = r.NextDouble();

            double stepWidth = 0.005;


            /* prepare smooth sinewave */
            for (int i = 0; i < sinewave.Length; i++)
            {
                sinewave[i] = (double)Math.Abs(Math.Sin((double)i / sinewave.Length * 2 * Math.PI * 2) * 127);
            }

            while (true)
            {

                /* fit sinewave to led count */
                for (int i = 0; i < LED_USED; i++)
                {
                    double d = sinewave[(int)((double)sinewave.Length / LED_USED * i + offset) % sinewave.Length];
                    ledData[i * 3 + 0] = (byte)(d * currentRed);
                    ledData[i * 3 + 1] = (byte)(d * currentGreen);
                    ledData[i * 3 + 2] = (byte)(d * currentBlue);
                }


                client.Send(ledData, ledData.Length, host, 10002);
                System.Threading.Thread.Sleep(10);
                offset++;

                if (offset == sinewave.Length)
                    offset = 0;

                if (Math.Abs(currentRed - targetRed) <= 0.07 &&
                    Math.Abs(currentGreen - targetGreen) <= 0.07 &&
                    Math.Abs(currentBlue - targetBlue) <= 0.07)
                {
                    /* set new colors */
                    targetRed = r.NextDouble();
                    targetBlue = r.NextDouble();
                    targetGreen = r.NextDouble();
                }
                else
                {
                    /* adjust colors */
                    if (Math.Abs(currentRed - targetRed) > 0.07)
                    {
                        if (currentRed > targetRed)
                            currentRed -= stepWidth;
                        else
                            currentRed += stepWidth;
                    }

                    if (Math.Abs(currentGreen - targetGreen) > 0.07)
                    {
                        if (currentGreen > targetGreen)
                            currentGreen -= stepWidth;
                        else
                            currentGreen += stepWidth;
                    }

                    if (Math.Abs(currentBlue - targetBlue) > 0.07)
                    {
                        if (currentBlue > targetBlue)
                            currentBlue -= stepWidth;
                        else
                            currentBlue += stepWidth;
                    }
                }

                //if (Console.KeyAvailable)
                //  return;
            }


        }
    }
}
