using System;
using System.Data;
using System.Collections.Generic;
using MySql.Data;
using MySql.Data.MySqlClient;

using Uaa.Cbpp.Database;
using Uaa.Cbpp.Directory.Record;

namespace Uaa.Cbpp.Directory
{

	public class Profile
	{

		public Profile()
		{
		}

		public bool Exists(int recordId)
		{

			string sql = @"
				SELECT EXISTS(
					SELECT 1
					FROM
						person
					WHERE
						id = @person_id
					LIMIT 1)";

			bool exists = false;

			var db = new Connection();
			db.Init();

			if (db.IsConnected())
			{
				MySqlCommand cmd = new MySqlCommand(sql, db.DBConnection);
				cmd.Parameters.Add("@person_id", MySqlDbType.Int32).Value = recordId;

				// check that record exists
				var personExists = cmd.ExecuteScalar();
				if (Convert.ToInt32(personExists) > 0)
				{
					exists = true;
				}

				db.Close();
			}

			return exists;
		}


		public Person GetPerson(int recordId)
		{
			Person person = new Person();

			string sql = QuerySql.Basic + @"
				WHERE
					p.id = @person_id

				LIMIT 0, 1";

			string existsSql = @"
				SELECT EXISTS(
					SELECT 1
					FROM
						person
					WHERE
						id = @person_id
					LIMIT 1)";

			string interestSql = @"
				SELECT
					id, type, photo_url, alt_text, link, description
				FROM interest 
				WHERE
					person_id = @person_id
				ORDER BY
					id ASC;";

			string contributionSql = @"
				SELECT
					id, type, photo_url, alt_text, link, description
				FROM contribution 
				WHERE
					person_id = @person_id
				ORDER BY
					id ASC;";
			string excludeSql = @"
				SELECT
					id, dm_id
				FROM dm_exclude 
				WHERE
					person_id = @person_id";

			var db = new Connection();
			db.Init();

			if (db.IsConnected())
			{
				MySqlCommand cmd = new MySqlCommand(existsSql, db.DBConnection);
				cmd.Parameters.Add("@person_id", MySqlDbType.Int32).Value = recordId;


				// check that record exists
				var personExists = cmd.ExecuteScalar();
				if (Convert.ToInt32(personExists) > 0)
				{
					cmd.CommandText = sql;
					MySqlDataReader reader = cmd.ExecuteReader();
					if (reader.Read())
					{
						person = Load.Person(reader);
					}
					reader.Close();


					cmd.CommandText = contributionSql;
					reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						Contribution cont = new Contribution();
						cont.Id = reader.GetInt32(reader.GetOrdinal("id"));
						cont.Type = reader["type"].ToString();
						cont.Photo = reader["photo_url"].ToString();
						cont.AltText = reader["alt_text"].ToString();
						cont.Link = reader["link"].ToString();
						cont.Description = reader["description"].ToString();
						person.Contributions.Add(cont);
					}
					reader.Close();

					cmd.CommandText = interestSql;
					reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						Interest interest = new Interest();
						interest.Id = reader.GetInt32(reader.GetOrdinal("id"));
						interest.Type = reader["type"].ToString();
						interest.Photo = reader["photo_url"].ToString();
						interest.AltText = reader["alt_text"].ToString();
						interest.Link = reader["link"].ToString();
						interest.Description = reader["description"].ToString();
						person.Interests.Add(interest);
					}
					reader.Close();

					cmd.CommandText = excludeSql;
					reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						person.DigitalMeasures.Excludes.Add(reader["dm_id"].ToString());
					}
					reader.Close();
				}

				db.Close();
			}
			return person;
		}
	}


}