using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace Prtg.Sensor.RabbitMq
{
	public class Program
	{
		static void Main(string[] args)
		{
			if (args.Length != 4 && args.Length != 6)
			{
				Console.WriteLine("Missing Required arguments");
				Console.WriteLine("<Server:Port> <user> <password> <Type> <Host> <Name>");
				Console.WriteLine("Examples:");
				Console.WriteLine("LocalHost:15672 guest guest queues %2f MyQueue");
				Console.WriteLine("LocalHost:15672 guest guest exchanges %2f MyExchange");

				if (args.Length > 0 && args[0].Equals("protect"))
				{
					Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
					ConfigurationSection section = config.GetSection("connectionStrings");
					{
						if (!section.SectionInformation.IsProtected)
						{
							if (!section.ElementInformation.IsLocked)
							{
								section.SectionInformation.ProtectSection("DataProtectionConfigurationProvider");
								section.SectionInformation.ForceSave = true;
								config.Save(ConfigurationSaveMode.Full);

								Console.WriteLine();
								Console.WriteLine("Encrypted Configuration File");
							}
						}
					}
				}
				Environment.Exit(2);
			}

			try
			{
				RequestPrams requestPrams;

				if (args.Length == 4)
				{
					var connectionString = ConfigurationManager.ConnectionStrings[args[0]].ToString().Split(':');
					requestPrams = new RequestPrams
					{
						ServerAndPort = args[0],
						Type = args[1],
						Host = args[2],
						Name = args[3],
						User = connectionString[0],
						Password = connectionString[1]
					};
				}
				else
				{
					requestPrams = new RequestPrams
					{
						ServerAndPort = args[0],
						User = args[1],
						Password = args[2],
						Type = args[3],
						Host = args[4],
						Name = args[5]
					};
				}


				Execute(requestPrams).Wait();
			}
			catch (Exception e)
			{
				// Catch all
				var s = new XmlSerializer(typeof(Prtg));
				s.Serialize(Console.Out, new Prtg { Error = 1, Text = e.GetBaseException().Message });
				Environment.Exit(2);
			}

			// Return Codes
			// 0	OK
			// 1	WARNING
			// 2	System Error (e.g. a network/socket error)
			// 3	Protocol Error (e.g. web server returns a 404)
			// 4	Content Error (e.g. a web page does not contain a required word
		}

		public static async Task Execute(RequestPrams requestPrams)
		{
			var s = new XmlSerializer(typeof(Prtg));
			var result = await Read(requestPrams);
			s.Serialize(Console.Out, result);
		}

		public static async Task<Prtg> Read(RequestPrams request)
		{
			var client = new HttpClient
			{
				BaseAddress = new Uri("http://" + request.ServerAndPort + "/api/")
			};

			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(request.User + ":" + request.Password)));
			using (var result = await client.GetAsync(string.Join("/", request.Type, request.Host, request.Name)))
			{
				result.EnsureSuccessStatusCode();
				var serializer = new JsonSerializer();
				using (var s = await result.Content.ReadAsStreamAsync())
				{
					var reader = new JsonTextReader(new StreamReader(s));
					var data = serializer.Deserialize(reader);
					if (request.Type.Equals("exchanges", StringComparison.OrdinalIgnoreCase))
					{
						return ReadExchange(data);
					}
					else if (request.Type.Equals("queues", StringComparison.OrdinalIgnoreCase))
					{
						return ReadQueue(data);
					}

					throw new ArgumentOutOfRangeException("request", request.Type + " is an unsupported object type");
				}
			}
		}

		public static Prtg ReadQueue(dynamic data)
		{
			var result = new Prtg();


			result.Add(new PrtgResult
			{
				Channel = "Total",
				CustomUnit = "msgs",
				Value = data.messages
			});

			result.Add(new PrtgResult
			{
				Channel = "Unacknowledged",
				CustomUnit = "msgs",
				Value = data.messages_unacknowledged
			});

			result.Add(new PrtgResult
			{
				Channel = "Rate",
				CustomUnit = "msg/sec",
				Value = data.messages_details.rate
			});

			result.Add(new PrtgResult
			{
				Channel = "Publish Rate",
				CustomUnit = "msg/sec",
				Value = ReadDec(() => data.message_stats.publish_details.rate)
			});

			result.Add(new PrtgResult
			{
				Channel = "Consumers",
				ShowChart = 0,
				CustomUnit = "#",
				Value = data.consumers
			});

			result.Add(new PrtgResult
			{
				Channel = "Memory",
				ShowChart = 0,
				ShowTable = 0,
				Unit = ChannelUnit.BytesMemory,
				Value = data.memory
			});

			result.Add(new PrtgResult
			{
				Channel = "Acknowledge",
				CustomUnit = "msg/sec",
				Value = ReadDec(() => data.message_stats.ack_details.rate)
			});

			return result;
		}

		public static Prtg ReadExchange(dynamic data)
		{
			var result = new Prtg();

			result.Add(new PrtgResult
			{
				Channel = "Out",
				CustomUnit = "msg/sec",
				Value = ReadDec(() => data.message_stats.publish_out_details.rate)
			});

			result.Add(new PrtgResult
			{
				Channel = "In",
				CustomUnit = "msg/sec",
				Value = ReadDec(() => data.message_stats.publish_in_details.rate)
			});

			result.Add(new PrtgResult
			{
				Channel = "Destinations",
				CustomUnit = "#",
				ShowChart = 0,
				Value = ReadDec(() => data.outgoing.Count)
			});

			result.Add(new PrtgResult
			{
				Channel = "Total in",
				ShowChart = 0,
				ShowTable = 0,
				CustomUnit = "msgs",
				Value = ReadDec(() => data.message_stats.publish_in)
			});

			result.Add(new PrtgResult
			{
				Channel = "Total out",
				ShowChart = 0,
				ShowTable = 0,
				CustomUnit = "msgs",
				Value = ReadDec(() => data.message_stats.publish_out)
			});

			return result;
		}

		private static decimal ReadDec(Func<decimal> func)
		{
			try
			{
				return func();
			}
			catch (Exception)
			{
				return 0;
			}
		}
	}

	public class RequestPrams
	{
		public string ServerAndPort { get; set; }
		public string User { get; set; }
		public string Password { get; set; }
		public string Type { get; set; }
		public string Host { get; set; }
		public string Name { get; set; }
	}

	public class QueueData
	{
	}

	public class ExchangesData
	{
		public MessageStats message_stats { get; set; }
		public string name { get; set; }
		public string vhost { get; set; }
		public string type { get; set; }
		public bool durable { get; set; }
		public bool auto_delete { get; set; }
	}

	public class ExchangeOutGoing
	{
		public GoingStats stats { get; set; }
	}

	public class GoingStats
	{
		public int publish { get; set; }

		public MessageDetails publish_details { get; set; }
	}

	public class MessageStats
	{
		public int publish_in { get; set; }
		public MessageDetails publish_in_details { get; set; }

		public int publish_out { get; set; }
		public MessageDetails publish_out_details { get; set; }
	}

	public class MessageDetails
	{
		public int rate { get; set; }
	}
}
