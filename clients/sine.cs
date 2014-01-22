/* 
 * draws a rotating sine around our sign
 * use keys cursor_left/right, R, G, B, D, C to control
 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace SomethingForLEDSign
{
    class Program
    {
        static byte brightness_sine(int x, int offset, int period)
        {
            double s = Math.Sin(2 * Math.PI / period * x + (double)2 * Math.PI * offset / period) * 127.0;

            s = Math.Abs(s);
            return (byte)s;
        }

        enum RGB
        {
            Red,
            Green,
            Blue
        };

        static void Main(string[] args)
        {
            const int LED_COUNT = 126;
            const int LEDS_USED = 126;

            byte[] data = new byte[LED_COUNT * 3];
            for (int i = 0; i < LED_COUNT * 3; i++)
                data[i] = 0x80;

            UdpClient client = new UdpClient();

            bool direction = true;
            int speed = 10;
            int X = 40;
            RGB rgb = RGB.Blue;

            while (true)
            {

                if (Console.KeyAvailable)
                {

                    switch (Console.ReadKey(true).Key)
                    {
                        case ConsoleKey.LeftArrow:
                            speed /= 2;
                            if (speed < 1)
                                speed = 1;
                            break;
                        case ConsoleKey.RightArrow:
                            speed *= 2;
                            if (speed > 500)
                                speed = 500;
                            break;
                        case ConsoleKey.C:
                            return;
                        case ConsoleKey.D:
                            direction ^= true;
                            break;
                        case ConsoleKey.R:
                            rgb = RGB.Red;
                            break;
                        case ConsoleKey.G:
                            rgb = RGB.Green;
                            break;
                        case ConsoleKey.B:
                            rgb = RGB.Blue;
                            break;

                    }
                    Console.WriteLine("speed: " + speed);

                    while (Console.KeyAvailable)
                        Console.ReadKey(true);

                }

                for (int i = 0; i < LEDS_USED; i++)
                {
                    byte brightness = brightness_sine(i, X, LEDS_USED);

                    data[i * 3 + 0] = (byte)(0x80 | (rgb == RGB.Blue ? brightness : 0));
                    data[i * 3 + 1] = (byte)(0x80 | (rgb == RGB.Red ? brightness : 0));
                    data[i * 3 + 2] = (byte)(0x80 | (rgb == RGB.Green ? brightness : 0));
                }

                if (direction)
                {
                    X++;
                    X %= LEDS_USED;
                }
                else
                {
                    X--;
                    if (X == -1)
                        X = LEDS_USED - 1;
                }



                client.Send(data, data.Length, "schild", 10002);

                System.Threading.Thread.Sleep(10 + speed);
            }
        }
    }
}
