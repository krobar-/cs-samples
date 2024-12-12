using System;
using System.Data;
using System.Collections.Generic;
using MySql.Data;
using MySql.Data.MySqlClient;

using Uaa.Cbpp.Database;

namespace Uaa.Cbpp.Directory
{
	public class Utility
	{
		public Utility()
		{
		}

		public List<string> GetEnumOptions(
			string table,
			string column
		)
		{

			List<string> results = new List<string>();
			string result = "";

			string sql = @"
				SELECT SUBSTR(COLUMN_TYPE, 6, LENGTH(COLUMN_TYPE)-6)
				FROM information_schema.COLUMNS
				WHERE TABLE_NAME = @t_name AND COLUMN_NAME = @c_name;";

			
			var db = new Connection();
			db.Init();

			if (db.IsConnected())
			{
				MySqlCommand cmd = new MySqlCommand(sql, db.DBConnection);
				cmd.Parameters.AddWithValue("t_name", table); 
				cmd.Parameters.AddWithValue("c_name", column);

				MySqlDataReader reader = cmd.ExecuteReader();
				if (reader.HasRows)
				{
					if (reader.Read())
					{
						result = reader.GetString(0);
					}
				}
				reader.Close();
				db.Close();
			}


			if (!String.IsNullOrEmpty(result))
			{
				string[] options = result.Split(',');

				foreach(string option in options)
				{
					results.Add(option.Substring(1, option.Length - 2));
				}
			}

			return results;
		}
	}
}
