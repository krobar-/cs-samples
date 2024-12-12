using System;
using System.Collections.Generic;

namespace Uaa.Cbpp.Directory.Record
{
	public class Group
	{
		public List<Person> Members { get; set; }

		//constructor
		public Group()
		{
			Members = new List<Person>();
		}

	}

	public static partial class Load
	{
		public static void Group(string record)
		{

		}
	}

	public static partial class Save
	{
		public static void Group(string record)
		{

		}
	}
}
