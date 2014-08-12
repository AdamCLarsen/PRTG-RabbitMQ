using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Prtg.Sensor.RabbitMq
{
	// ReSharper disable InconsistentNaming
	[XmlRoot("prtg")]
	public class Prtg
	{
		private List<PrtgResult> _results;

		[XmlElement("result")]
		public List<PrtgResult> Results
		{
			get { return _results ?? (_results = new List<PrtgResult>()); }
			set { _results = value; }
		}

		[XmlElement("Error")]
		public int Error { get; set; }

		public bool ShouldSerializeError()
		{
			return Error != 0; 
		}

		public string Text { get; set; }

		public void Add(PrtgResult prtgResult)
		{
			Results.Add(prtgResult);
		}

		public override string ToString()
		{
			return string.Join("\r\n", Results);
		}
	}

	public class PrtgResult
	{
		private string _customUnit;
		private int _float = 1;
		private int _showChart = 1;
		private int _showTable = 1;
		public string Channel { get; set; }
		public decimal Value { get; set; }

		public ChannelUnit Unit { get; set; }

		public string CustomUnit
		{
			get { return _customUnit; }
			set
			{
				if (value != null)
				{
					Unit = ChannelUnit.Custom;
				}

				_customUnit = value;
			}
		}

		public int Float
		{
			get { return _float; }
			set { _float = value; }
		}

		public int ShowChart
		{
			get { return _showChart; }
			set { _showChart = value; }
		}

		public int ShowTable
		{
			get { return _showTable; }
			set { _showTable = value; }
		}

		public override string ToString()
		{
			return string.Format("{0} : {1} {2}", Channel ?? "N/A", Value, CustomUnit ?? Unit.ToString());
		}
	}

	public enum ChannelUnit
	{
		Custom,
		BytesBandwidth,
		BytesMemory,
		BytesDisk,
		Temperature,
		Percent,
		TimeResponse,
		TimeSeconds,
		Count,
		[XmlEnum("CPU (*)")]
		Cpu,
		BytesFile,
		SpeedDisk,
		SpeedNet,
		TimeHours,
	}
}
