using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

/*
 * SignMultiplexer selects one UDP input stream and forwards it to one destination
 * 
 * 2 UDP servers are started.
 * The PowerServer accepts '0' and '1' to disable and enable the forwarding globally
 * The DataServer accepts any UDP stream and may forward it to the destination
 * 
 * One input stream is selected to be forwarded to the destination; following rules apply:
 * 1. a new stream will be immediately forwarded and cuts of other streams
 * 2. if there's only 1 stream it will be forwarded forever
 * 3. if there are multiple streams each stream will be forwarded upto slotTime
 * 4. streams are forwarded in a round robin behaviour
 * 5. if there's no continous stream another stream might take over after slotTime/2
 * 6. in rare cases (new streams disappearing) a stream might be forwarded for slotTime * 2
 * 
 * Use one stream only for a sequence. Do not open a new connection for each frame.
 * Only do that if you need unlimited access ;-)
 */
namespace SignMultiplexer
{
    class Program
    {
        const int dataServerPort = 10001;
        const int powerServerPort = 10002;
        const string host = "10.1.20.236";
        const int ledCount = 156;
        const int slotTime = 90;

        static void Main(string[] args)
        {
            Console.WriteLine("SignMultiplexer started");

            UdpClient dataServer = new UdpClient(new IPEndPoint(IPAddress.Any, dataServerPort));
            UdpClient powerServer = new UdpClient(new IPEndPoint(IPAddress.Any, powerServerPort));

            dataServer.BeginReceive(new AsyncCallback ( DataReceive ), dataServer );
            powerServer.BeginReceive(new AsyncCallback(PowerReceive), powerServer);

            while (true)
            {
                PenaltyAdjust();
                System.Threading.Thread.Sleep(10000);
            }

        }

        static bool EndpointEquals(IPEndPoint endpoint1, IPEndPoint endpoint2)
        {
            /* in case both are null or equal */
            if (endpoint1 == endpoint2)
                return true;

            if (endpoint1 == null || endpoint2 == null)
                return false;

            if (endpoint1.Address.Equals(endpoint2.Address) && endpoint1.Port == endpoint2.Port)
                return true;

            return false;
        }

        #region Penalty

        /* endpoint eco system :) */

        class Penalty
        {
            internal IPEndPoint endpoint;
            internal DateTime lastActive;
            internal int penalty;
        }

        static List<Penalty> penalties = new List<Penalty>();

        static int PenaltyGet(IPEndPoint endpoint)
        {
            lock (penalties)
            {
                Penalty p = penalties.FirstOrDefault(x => EndpointEquals(x.endpoint, endpoint));

                if (p == null)
                    return 0;

                return p.penalty;
            }

        }

        static void PenaltyAdd(IPEndPoint endpoint)
        {
            lock (penalties)
            {
                Penalty p = penalties.FirstOrDefault(x => EndpointEquals(x.endpoint, endpoint));

                if (p == null)
                {
                    p = new Penalty() { endpoint = endpoint, penalty = 0 };
                    penalties.Add(p);
                }

                p.lastActive = DateTime.Now;
                p.penalty += slotTime;
            }
        }

        static void PenaltyActive(IPEndPoint endpoint)
        {
            lock (penalties)
            {
                Penalty p = penalties.FirstOrDefault(x => EndpointEquals(x.endpoint, endpoint));

                if (p == null)
                    return;

                p.lastActive = DateTime.Now;
            }
        }

        static void PenaltyAdjust()
        {
            lock (penalties)
            {
                /* remove dead endpoints */
                int rem = penalties.RemoveAll(x => DateTime.Now.Subtract(x.lastActive).TotalSeconds >= 4 * slotTime);
                if ( rem != 0 )
                    Console.WriteLine("[{0}] removed {1} dead endpoint(s)", DateTime.Now, rem);

                if (penalties.Count == 0)
                    return;

                /* adjust minimum time */
                int min = penalties.Min(x => x.penalty);

                if (min > slotTime)
                {
                    penalties.ForEach(x => x.penalty -= min - slotTime);
                }
            }
        }

        #endregion

        static IPEndPoint lastEndpoint = null;
        static DateTime lastEndpointChange = DateTime.MinValue;
        static DateTime lastActivity = DateTime.MinValue;

        static bool EndpointAllowedToSend(IPEndPoint endpoint)
        {
            string newEndpoint =
                String.Format("[{0}] dataServer: new client {1}, penalty {2}", DateTime.Now, endpoint, PenaltyGet ( endpoint ) );

            if (endpoint == null)
                return false;

            if (lastEndpoint == null)
            {
                lastEndpoint = endpoint;
                lastEndpointChange = DateTime.Now;
                lastActivity = DateTime.Now;

                PenaltyAdd(endpoint);
                Console.WriteLine(newEndpoint );

                return true;
            }

            PenaltyActive(endpoint);

            if (DateTime.Now.Subtract(lastActivity).TotalSeconds >= /*30 +*/ PenaltyGet ( endpoint )/2 )
            {
                /* no activity for >= 30 seconds */

                if (!EndpointEquals(lastEndpoint, endpoint))
                {
                    /* new endpoint */
                    lastEndpoint = endpoint;
                    lastEndpointChange = DateTime.Now;

                    PenaltyAdd(endpoint);
                    Console.WriteLine(newEndpoint);
                }

                lastActivity = DateTime.Now;
                return true;
            }

            if (!EndpointEquals(endpoint, lastEndpoint))
            {
                /* new endpoint */

                if (DateTime.Now.Subtract(lastEndpointChange).TotalSeconds >= /*2 * 60 +*/ PenaltyGet ( endpoint ))
                {
                    /* old one active for more than 2 minutes */

                    lastEndpoint = endpoint;
                    lastActivity = DateTime.Now;
                    lastEndpointChange = DateTime.Now;

                    PenaltyAdd(endpoint);
                    Console.WriteLine(newEndpoint);

                    return true;
                }
                else
                    return false;
            }

            lastActivity = DateTime.Now;
            return true;
        }

        static void DataReceive(IAsyncResult result)
        {
            UdpClient dataServer = (UdpClient)result.AsyncState;

            IPEndPoint remote = new IPEndPoint ( IPAddress.Any, 0 );

            byte[] data = dataServer.EndReceive(result, ref remote);

            if (EndpointAllowedToSend(remote) && ClientSend ( data ) )
                dataServer.Send(new byte[1] { (byte)'1' }, 1, remote);
            else
                dataServer.Send(new byte[1] { (byte)'0' }, 1, remote);


            dataServer.BeginReceive(new AsyncCallback(DataReceive), dataServer);
        }

        static bool on = true;

        static void PowerReceive(IAsyncResult result)
        {

            const string invalidData = "powerServer: received invalid data";

            UdpClient powerServer = (UdpClient)result.AsyncState;

            IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);

            byte[] data = powerServer.EndReceive(result, ref remote);

            if (data != null && data.Length == 1)
            {
                if (data[0] == '1')
                {
                    on = true;
                    Console.WriteLine("powerServer: toggled to on");
                }
                else if (data[0] == '0')
                {
                    on = true;
                    ClientSend(new byte[ledCount * 3]);
                    on = false;
                    Console.WriteLine("powerServer: toggled to off");
                }
                else
                    Console.WriteLine(invalidData);
            }
            else
                Console.WriteLine(invalidData);

            powerServer.BeginReceive(new AsyncCallback(PowerReceive), powerServer);
        }

        static bool ClientSend(byte[] data)
        {
            if (!on || data == null)
                return false;

            new UdpClient().Send(data, data.Length, host, dataServerPort);

            return true;
        }
    }
}
