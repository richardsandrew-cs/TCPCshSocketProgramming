using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Security.Cryptography;
namespace TcpCsharpFileReciever
{
    class Program
    {
        const String receivedSuffix = ".\\Data\\Received Files\\"; //default: ".\\Data\\Received Files\\"

        const String fileName = ".\\Data\\Standard Files\\file.bin"; //default: ".\\Data\\Standard Files\\file.bin"

        const String resultsFilePath = ".\\Data\\results.txt"; //default: ".\\Data\\results.txt"

        static readonly byte[] sourceHash = MD5.Create().ComputeHash(File.OpenRead(fileName));

        static int total = 10;

        static int count = 1;

        static readonly IPAddress ipAddr = Dns.GetHostEntry(Dns.GetHostName()).AddressList[0];
        static Int32 portNum = 60612;


        //queues are allocated in ParseAndInitialize
        static Queue<String> outFileNames;
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
                total = 0;
                
                if (args.Length > 0)
                {
                    total = Int32.Parse(args[0]);
                }
                for (int i = 1; i < args.Length; i++)
                {
                    if (args.ElementAt(i) == "-port")
                    {
                        i++;
                        portNum = Int32.Parse(args.ElementAt(i));
                    }
                }
            }
            outFileNames = new Queue<String>(total);
            outFileDownloadTime = new Queue<double>(total);
            outFileTime = new Queue<DateTime>(total);
        }

        static void Main(string[] args)
        {
            ParseAndInitialize(args);

            Console.WriteLine(ipAddr.ToString() + " " + portNum);

            ExecuteServer();
            AnalyzeReceivedFiles();

        }


        static void ExecuteServer()
        {

            IPEndPoint localEndPoint = new IPEndPoint(ipAddr, portNum);
            Socket listener = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(localEndPoint);
            listener.Listen(10);
            for (int i = 0; i < total; i++)
            {
                //initialize necessary variables
                DateTime currTime = DateTime.Now;

                String fileName= receivedSuffix + currTime.ToString("yyyyMMddHHmmss") + (i+1) + ".bin";
                count++;

                BinaryWriter fileToWrite = new BinaryWriter(File.OpenWrite(fileName));
                byte[] byteBuffer;
                Stopwatch stopwatch = new Stopwatch();



                Socket clientSocket = listener.Accept();//blocks until sender requests a connection
                stopwatch.Start();//after accepting, the sender stars transmitting, begin here to measure propogation time of first transmission
                byteBuffer = new byte[clientSocket.ReceiveBufferSize];
                int numBytes;
                //stopwatch.Start();//starting here will not measure propogation time of the first transmission
                while ((numBytes = clientSocket.Receive(byteBuffer)) != 0)// block until recieved info, if closed, end loop
                {
                    fileToWrite.Write(byteBuffer,0,numBytes);//write the recieved bytes
                }
                stopwatch.Stop();//stop measuring time, socket has been closed



                //safely close the socket on this end
                clientSocket.Shutdown(SocketShutdown.Both);
                clientSocket.Close();
                //close the output file
                fileToWrite.Close();


                //add file details to queues for analysis
                outFileNames.Enqueue(fileName);
                outFileDownloadTime.Enqueue(stopwatch.Elapsed.TotalMilliseconds);
                outFileTime.Enqueue(currTime);

            }
            listener.Dispose();
        }


        static void AnalyzeReceivedFiles()
        {
            StreamWriter resultsFile = File.AppendText(resultsFilePath);
            String fileName;
            double fileDownloadTime;
            String fileTime;

            while (outFileNames.Count != 0)
            {
                fileName = outFileNames.Dequeue();
                fileDownloadTime = outFileDownloadTime.Dequeue();
                fileTime = outFileTime.Dequeue().ToString("yyyyMMddHHmmss");

                byte[] resultHash = MD5.Create().ComputeHash(File.OpenRead(fileName));

                bool isMatch;
                int i = 0;
                if (sourceHash.Length != resultHash.Length)//MD5 hashes always produce 128 bits, but couldnt hurt to check
                {
                    isMatch = false;
                }
                else
                {
                    isMatch = true;//is true until mismatch found
                    for (i = 0; i < sourceHash.Length; i++)
                    {
                        if (sourceHash[i] != resultHash[i])
                        {
                            isMatch = false;//mismatch found, hashes not equal, break now
                            break;
                        }
                    }
                }

                if (i == sourceHash.Length)//if reached the end without finding a mismatch, return i to index 0 to write the first byte
                {
                    i = 0;
                    
                }
                resultsFile.WriteLine(String.Format("{0,-15}{1,-2:X}{2,-2:X}  {3,-6}{4,12:N4}ms", fileTime, resultHash[i], sourceHash[i],(isMatch ? "Match" : "Error"), fileDownloadTime));
                //[yyyyMMddHHmmss of download][mismatch byte of results][mismatch byte of source][match/error][download elapsed time
            }
            resultsFile.Flush();
            resultsFile.Close();
        }
    }
}