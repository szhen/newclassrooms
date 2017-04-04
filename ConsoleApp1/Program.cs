using System;
using System.Text;
using System.Net;
using System.IO;
using System.Linq;
using System.Xml;
using Newtonsoft.Json;
using System.Threading;

namespace ConsoleApp1
{
	class Program
	{
		public static string threadExit;
		static void Main()
		{
			threadExit = "";
			// set up thread for httplisten
			ThreadStart hl = new ThreadStart(httplisten);
			Thread http = new Thread(hl);
			http.Start();

			// console app stuff here
			string input = "";
			Console.WriteLine("Type \"quit\" to exit the program");
			while (true)
			{
				Console.Write("Enter file name or path to file: ");
				input = Console.ReadLine();
				if (!input.Equals("quit"))
				{
					if (checkTypeFile(input).Equals("xml")) toJson(input);
					else if (checkTypeFile(input).Equals("json")) toXml(input);
					else Console.WriteLine("Error reading file or incorrect file type...");
				}
				else break;
			}

			// stop the thread
			threadExit = "exit";
			http.Join();
		}

		public static void httplisten()
		{
			// http listen server
			HttpListener listener = new HttpListener();
			// listen on localhost:4333
			listener.Prefixes.Add("http://localhost:4333/");

			listener.Start();
			while (!threadExit.Equals("exit"))
			{
				// using BeginGetContext because GetContext blocks and thread cannot terminate
				IAsyncResult result = listener.BeginGetContext(new AsyncCallback(listenCallBack), listener);
			}
			listener.Stop();
		}

		public static void listenCallBack(IAsyncResult result)
		{
			try
			{
				HttpListener listener = (HttpListener)result.AsyncState;
				// Call EndGetContext to complete the asynchronous operation.
				HttpListenerContext context = listener.EndGetContext(result);

				var data = new StreamReader(context.Request.InputStream).ReadToEnd();
				// figure out how to process data
				if (checkTypeString(data).Equals("xml"))
				{
					toJson(data, context);
				}
				else if (checkTypeString(data).Equals("json"))
				{
					toXml(data, context);
				}
				else
				{
					// send error message
					sendErrorMessage(context);
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
		}

		public static void toJson(string fileName)
		{
			try
			{
				XmlDocument xmlDoc = new XmlDocument();
				xmlDoc.Load(fileName);
				string json = JsonConvert.SerializeXmlNode(xmlDoc, Newtonsoft.Json.Formatting.Indented);
				File.Create("output.json").Close(); // close the file after creating
				File.WriteAllText("output.json", json);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
		}

		public static void toJson(string data, HttpListenerContext context)
		{
			try
			{
				// process xml to json
				// turn data string into xml object
				XmlDocument xmlData = new XmlDocument();
				xmlData.LoadXml(data);
				string json = JsonConvert.SerializeXmlNode(xmlData, Newtonsoft.Json.Formatting.Indented);
				// send the json data back to client
				byte[] dataByte = Encoding.UTF8.GetBytes(json);
				context.Response.ContentLength64 = dataByte.Length;
				var oStream = context.Response.OutputStream;
				oStream.Write(dataByte, 0, dataByte.Length);
				context.Response.Close();
			}
			catch (Exception e)
			{
				// error somewhere, send error message back to client
				sendErrorMessage(context);
			}
		}

		public static void toXml(string fileName)
		{
			try
			{
				string jsonString = File.ReadAllText(fileName);
				// DeserializeXmlNode needs the json data as a string
				XmlDocument converted = JsonConvert.DeserializeXmlNode(jsonString);
				converted.Save("output.xml");
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
		}

		public static void toXml(string data, HttpListenerContext context)
		{
			try
			{
				// process json to xml
				XmlDocument converted = JsonConvert.DeserializeXmlNode(data);
				// send xml data back to client
				byte[] dataByte = Encoding.UTF8.GetBytes(converted.OuterXml);
				context.Response.ContentLength64 = dataByte.Length;
				var oStream = context.Response.OutputStream;
				oStream.Write(dataByte, 0, dataByte.Length);
				context.Response.Close();
			}
			catch (Exception e)
			{
				// error somewhere, send error message back to client
				sendErrorMessage(context);
			}
		}

		public static string checkTypeFile(string fileName)
		{
			try
			{
				// quick check for xml, json, or neither: xml starts with < and json starts with { or [
				// files that start with <,{,or[ will pass this check, but will raise an exception when attempting to convert
				// to json or xml is not valid
				string line1 = File.ReadLines(fileName).First();
				if (line1[0].Equals('<')) return "xml";
				else if (line1[0].Equals('{') || line1[0].Equals('[')) return "json";
				else return "error";
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				return "error";
			}
		}

		public static string checkTypeString(string data)
		{
			// checks string to see if it is xml, json, or neither
			// same logic as checkTypeFile except instead of reading from the file, we have the actual data
			if (data[0].Equals('<')) return "xml";
			else if (data[0].Equals('{') || data[0].Equals('[')) return "json";
			else return "error";
		}

		public static void sendErrorMessage(HttpListenerContext context)
		{
			byte[] dataByte = Encoding.UTF8.GetBytes("Error: Bad data/file type");
			context.Response.ContentLength64 = dataByte.Length;
			var oStream = context.Response.OutputStream;
			oStream.Write(dataByte, 0, dataByte.Length);
			context.Response.Close();
		}
	}
}
