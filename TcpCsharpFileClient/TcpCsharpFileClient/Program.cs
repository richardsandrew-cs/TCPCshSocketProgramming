using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;


    class Client
    {
        const String fileName = ".\\Data\\file.bin";


        //static IPAddress ipAdd = Dns.GetHostEntry(Dns.GetHostName()).AddressList[0];
        static IPAddress ipAdd = IPAddress.Parse("fe80::adc6:e715:a3f5:22b6");//example fe80::b8c9:4637:4f49:a1f4
        static Int32 portNum = 60612;

        static int total = 5;

        static Queue<double> outFileDownloadTime;
        static Queue<DateTime> outFileTime;

        static void ParseAndInitialize(string[] args)
        {
            //parse command line args
            //string after '-ip' is ipaddress in . seperated form for ipv4 or colon hex form for ipv6
            //string after -port is port number to connect on. no restriction on valid ranges.
            //without command line args, default ip is localhost, default port is 60612, a value in the 'dynamic port' range that is not registered by iana.org 
            //for more information about ports visit <https://www.iana.org/assignments/service-names-port-numbers/service-names-port-numbers.xhtml>

            if (args.Length != 0)
            {

                if (args.Length > 0)
                {
                    total = Int32.Parse(args[0]);
                }
                

                for (int i = 1; i < args.Length; i++)
                {
                    if (args.ElementAt(i) == "-ip")
                    {
                        ipAdd = IPAddress.Parse(args.ElementAt(++i));
                    }
                    else if (args.ElementAt(i) == "-port")
                    {
                        portNum = Int32.Parse(args.ElementAt(++i));
                    }
                }
            }
        }

        static void Main(string[] args)
        {
            ParseAndInitialize(args);
            SendAll();
        }

        static void SendAll()
        {
            int i;
            for (i = 0; i < total; i++)
            {
                ExecuteClient(fileName);
            }
        }

        static void ExecuteClient(String fileName)
        {
            BinaryReader fileToSend = new BinaryReader(File.OpenRead(fileName));

            IPEndPoint endPoint = new IPEndPoint(ipAdd, portNum);
            Socket sender = new Socket(ipAdd.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            Console.WriteLine("Attempting a connection");
            sender.Connect(endPoint);
#if DEBUG
            Console.WriteLine("DEBUG:Connection made");
#endif
            byte[] byteBuffer;
            while((byteBuffer = fileToSend.ReadBytes(sender.SendBufferSize)).Length > 0) {
                sender.Send(byteBuffer);
#if DEBUG 
                Console.WriteLine("DEBUG:" + byteBuffer.Length);
#endif
            }

            sender.Shutdown(SocketShutdown.Both);
            sender.Close();


            Console.WriteLine("Connection Closed");


            fileToSend.Close();
        }
    }
