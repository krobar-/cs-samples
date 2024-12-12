using System;
using System.Data;
using System.Collections.Generic;
using MySql.Data;
using MySql.Data.MySqlClient;

using Uaa.Cbpp.Database;
using Uaa.Cbpp.Directory.Record;

namespace Uaa.Cbpp.Directory
{
	public class Listing
	{
		public Listing()
		{
		}

		public Group All()
		{
			List<Person> People = new List<Person>();
			Group Everyone = new Group();

			string sql = QuerySql.Basic + @"
				WHERE p.faculty_staff IN ('faculty','staff','both') 
				GROUP BY
					p.id
				ORDER BY
					p.last_name ASC;";

			var db = new Connection();
			db.Init();

			if (db.IsConnected())
			{
				MySqlCommand cmd = new MySqlCommand(sql, db.DBConnection);

				MySqlDataReader reader = cmd.ExecuteReader();

				if (reader.HasRows)
				{
					while (reader.Read())
					{
						Person person = Load.Person(reader);
						Everyone.Members.Add(person);
					}
				}
				db.Close();
			}
			return Everyone;
		}

		public Group ListByDepartment(int deptId)
		{
			return ListByDepartment(deptId, "current");
		}

		public Group ListByDepartment(int deptId, string cat)
		{
			Group Department = new Group();

			string categories = GetCategoryString(cat);

			string sql = QuerySql.Basic + @"
				WHERE
					p.id IN (SELECT person_id FROM person__department WHERE department_id = @department_id)
				AND
					p.faculty_staff IN (" + categories + @") 
				GROUP BY
					p.id
				ORDER BY
					p.last_name ASC;";

			var db = new Connection();
			db.Init();

			if (db.IsConnected())
			{
				MySqlCommand cmd = new MySqlCommand(sql, db.DBConnection);
				cmd.Parameters.Add("@department_id", MySqlDbType.Int32).Value = deptId;

				MySqlDataReader reader = cmd.ExecuteReader();
				if (reader.HasRows)
				{
					while (reader.Read())
					{
						Person person = Load.Person(reader);
						Department.Members.Add(person);
					}
				}
				reader.Close();
				db.Close();
			}
			return Department;
		}

		public Group ListByCategory(string cat)
		{
			Group Category = new Group();

			// use Parameters.Add?
			string categories = GetCategoryString(cat);

			string sql = QuerySql.Basic + @"
				WHERE p.faculty_staff IN (" + categories + @") 
				GROUP BY
					p.id
				ORDER BY
					p.last_name ASC;";

			var db = new Connection();
			db.Init();

			if (db.IsConnected())
			{
				MySqlCommand cmd = new MySqlCommand(sql, db.DBConnection);

				MySqlDataReader reader = cmd.ExecuteReader();
				if (reader.HasRows)
				{
					while (reader.Read())
					{
						Person person = Load.Person(reader);
						Category.Members.Add(person);
					}
				}
				reader.Close();
				db.Close();
			}
			return Category;
		}

		private string GetCategoryString(string cat)
		{
			Dictionary<string, string> map = new Dictionary<string, string>()
			{
				{"all", "'faculty', 'staff', 'administration', 'both', 'retired'"},
				{"current", "'faculty', 'staff','administration', 'both'"},
				{"faculty", "'faculty', 'both'"},
				{"staff", "'staff','administration', 'both'"},
				{"both", "'both'"},
				{"neither", "'neither'"},
				{"retired", "'retired'"},
				{"other", "'other'"},
				{"admin", "'administration'"}
			};

			return map.TryGetValue(cat.ToLower(), out string categories) ? categories : "'faculty', 'staff', 'administration', 'both'";
		}

		public List<Department> GetDepartments()
		{
			List<Department> Departments = new List<Department>();

			string sql = @"SELECT * FROM department ORDER BY department.id ASC;";

			var db = new Connection();
			db.Init();

			if (db.IsConnected())
			{
				MySqlCommand cmd = new MySqlCommand(sql, db.DBConnection);

				MySqlDataReader reader = cmd.ExecuteReader();
				if (reader.HasRows)
				{
					while (reader.Read())
					{
						Department dept = new Department();

						dept.Id = reader.GetInt32(reader.GetOrdinal("id"));
						dept.Name = reader.GetString(reader.GetOrdinal("name"));
						dept.HeadTitle = reader["head_title"].ToString();
						if (!reader.IsDBNull(reader.GetOrdinal("head_id")))
						{
							dept.HeadId = reader.GetInt32(reader.GetOrdinal("head_id"));
						}

						Departments.Add(dept);
					}
				}
				reader.Close();
				db.Close();
			}
			return Departments;
		}

	}

	public class Department
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string HeadTitle { get; set; }
		public int? HeadId { get; set; }
	}
	public class Committee
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string Link { get; set; }
	}

}
