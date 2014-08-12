using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Prtg.Sensor.RabbitMq.Tests
{
	[TestClass]
	public class MessageProcessingTest
	{
		[TestMethod]
		public async Task XmlSeralizer()
		{
			await Program.Execute(new RequestPrams
				{
					Host = "%2F",
					Name = "ConfMe_Messages_Voip_From_ServerHeartbeat",
					Password = "guest",
					User = "guest",
					ServerAndPort = "localhost:15672",
					Type = "exchanges"
				});
		}

		[TestMethod]
		public async Task ReadExchange2()
		{
			var result =
				await Program.Read(new RequestPrams
				{
					Host = "%2F",
					Name = "amq.direct",
					Password = "guest",
					User = "guest",
					ServerAndPort = "localhost:15672",
					Type = "exchanges"
				});

			Console.WriteLine(result);
		}		

		[TestMethod]
		public async Task ReadQueue()
		{
			var result =
				await Program.Read(new RequestPrams
				{
					Host = "%2F",
					Name = "GetMessage",
					Password = "guest",
					User = "guest",
					ServerAndPort = "localhost:15672",
					Type = "queues"
				});

			Console.WriteLine(result);
		}
	}
}
