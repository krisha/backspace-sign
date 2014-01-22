using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;

/*
 * quick and dirty solution to let the sign blink
 * when there is a kill in warsow / deathmatch mode
 */
namespace Warsow2Sign
{
    class Program
    {

        class Player
        {
            internal string name;
            internal int score;
        }

        /* sign blink */
        static void Indicate(string name, int score)
        {
            const int LED_COUNT = 126;

            UdpClient client = new UdpClient();

            /* blue */
            byte[] data = new byte[LED_COUNT * 3];
            for (int i = 0; i < LED_COUNT; i++)
            {
                data[i * 3 + 0] = 0x80 + 30;
                data[i * 3 + 1] = 0x80;
                data[i * 3 + 2] = 0x80;
            }

            client.Send(data, data.Length, "schild", 10002);

            System.Threading.Thread.Sleep(50);

            /* blank */
            for (int i = 0; i < LED_COUNT; i++)
            {
                data[i * 3 + 0] = 0x80;
                data[i * 3 + 1] = 0x80;
                data[i * 3 + 2] = 0x80;
            }

            client.Send(data, data.Length, "schild", 10002);

        }

        static void Main(string[] args)
        {
            List<Player> players = new List<Player>();

            IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, 0);

            UdpClient client = new UdpClient();

            /* connect to warsow server */
            client.Connect("localhost", 44400);

            /* build warsow server request */
            byte[] request = Encoding.ASCII.GetBytes("getstatus test");
            byte[] fullrequest = new byte[request.Length + 4];

            for (int i = 0; i < 4; i++)
                fullrequest[i] = 0xFF;

            Array.Copy(request, 0, fullrequest, 4, request.Length);

            while (true)
            {
                /* get current data from warsow server */
                client.Send(fullrequest, fullrequest.Length);
                byte[] response = client.Receive(ref endpoint);

                /* cut header */
                string responseAscii = Encoding.ASCII.GetString(response, 4, response.Length - 4);

                /* throw away name and variables */
                IEnumerable<string> splitted = responseAscii.Split('\n').Skip(2);

                /* loop through player list in server response */
                foreach (string playerEntry in splitted)
                {
                    int firstQuote = playerEntry.IndexOf('"');
                    int lastQuote = playerEntry.LastIndexOf('"');

                    if (firstQuote != -1 && lastQuote != -1)
                    {
                        /* name is in quotes, remove it */
                        string name = playerEntry.Substring(firstQuote + 1, lastQuote - firstQuote - 1);
                        string remaining = playerEntry.Remove(firstQuote, lastQuote - firstQuote + 1);

                        IEnumerable<string> splittedPlayerEntry = remaining.Split(' ');

                        /* score is on first substring */
                        int score = Int32.Parse(splittedPlayerEntry.ElementAt(0));

                        /* try to get saved player name */
                        IEnumerable<Player> ePlayer = players.Where(p => p.name.Equals(name));

                        if (ePlayer.Count() == 0)
                        {
                            players.Add(new Player() { name = name, score = score });
                            Console.WriteLine("new player detected: " + name);
                        }
                        else
                        {
                            int orgScore = ePlayer.ElementAt(0).score;

                            /* score changed */
                            if (score != orgScore)
                            {
                                /* ignore death time score */
                                if (score != 0 && orgScore != 0)
                                    Indicate(name, score);
                                Console.WriteLine("bam 4 " + name + ", score: " + score);
                                Player p = ePlayer.ElementAt(0);

                                /* update player score */
                                p.score = score;
                            }
                        }
                    }
                }

                System.Threading.Thread.Sleep(10);
            }
        }
    }
}
