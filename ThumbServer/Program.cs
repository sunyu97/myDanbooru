using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Sunfish.IO;

namespace ThumbServer
{
    internal class Program
    {
        private static int maxRequestHandlers = 4;

        private static int requestHandlerID = 0;

        private static HttpListener listener;

        private static void log(string info)
        {
            Console.WriteLine(string.Format("{0} - {1}", DateTime.Now.ToShortTimeString(), info));
        }

        private static void RequestHandler(IAsyncResult result)
        {
            try
            {
                HttpListenerContext context = listener.EndGetContext(result);
                string k = context.Request.Url.AbsolutePath;
                // log(k.Length.ToString() + k.Substring(0, 7));
                if (k.Length == 43 && k.Substring(0, 7) == "/thumb/")
                {
                    //StreamWriter sw = new StreamWriter(context.Response.OutputStream);

                    //byte[] kx = big.getFileB(k.Substring(7, 32));

                    MemoryStream ms = big.getFile(k.Substring(7, 32));
                    if (ms != null)
                    {
                        context.Response.ContentType = "image/jpeg";
                        //Console.Write(context.Response.Headers.AllKeys);
                        context.Response.Headers.Add(HttpResponseHeader.CacheControl, "max-age=31536000");

                        //log("Success: " + k.Substring(7, 32));
                        ms.WriteTo(context.Response.OutputStream);
                        ms.Flush();
                        //context.Response.OutputStream.Write(kx, 0, kx.Length);
                        //context.Response.OutputStream.Flush();
                        //    context.Response.AddHeader("Cache-Control", "max-age=31536000");
                        // context.Response.AddHeader("Expires", "Thu，16 May 2013 12：00：00 GMT");
                    }
                    else
                    {
                        log("Error getting " + k.Substring(7, 32));
                        StreamWriter sw = new StreamWriter(context.Response.OutputStream, Encoding.UTF8);
                        sw.WriteLine("Error getting file" + k.Substring(7, 32));
                        sw.Flush();
                        context.Response.ContentType = "text/html";

                        context.Response.ContentEncoding = Encoding.UTF8;
                    }
                }
                else
                {
                    StreamWriter sw = new StreamWriter(context.Response.OutputStream, Encoding.UTF8);
                    sw.WriteLine("Error getting file");
                    sw.Flush();
                    log("Unknown " + context.Request.Url);
                    context.Response.ContentType = "text/html";
                    context.Response.ContentEncoding = Encoding.UTF8;
                }
                //StreamWriter sw = new StreamWriter(context.Response.OutputStream);

                /*

                StreamWriter sw = new StreamWriter(context.Response.OutputStream, Encoding.UTF8);

                sw.WriteLine("<html><head><title>C# </title>");
                log(context.Request.Url.ToString());
                sw.WriteLine("</head><body>" + "");

                sw.WriteLine("</body></html>");

                sw.Flush();

                context.Response.ContentType = "text/html";

                context.Response.ContentEncoding = Encoding.UTF8;
                */
                context.Response.Close();
            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine(result.AsyncState);
            }
            finally
            {
                if (listener.IsListening)
                {
                    listener.BeginGetContext(RequestHandler,

                    "RequestHandler_" + Interlocked.Increment(ref requestHandlerID));
                }
            }
        }

        public static SFIO big = new SFIO();

        private static void Main(string[] args)
        {
            Console.WriteLine("Begin Loading ");
            big.loadSFIO(System.IO.Path.GetDirectoryName(Application.ExecutablePath) + "\\bigfile");
            Console.WriteLine("LOADED maxFile = " + big.maxSeq);
            using (listener = new HttpListener())
            {
                listener.Prefixes.Add("http://*:8080/");

                listener.Start();

                for (int count = 0; count < maxRequestHandlers; count++)
                {
                    listener.BeginGetContext(RequestHandler, "RequestHandler_" +

                        Interlocked.Increment(ref requestHandlerID));
                }
                Console.WriteLine("HTTP Server running on 8080 port, press enter to abort");
                Console.ReadLine();

                listener.Stop();
                listener.Abort();
            }
        }
    }
}