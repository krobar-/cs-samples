using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using MySql.Data;
using MySql.Data.MySqlClient;

using Uaa.Cbpp.DigitalMeasures;

namespace Uaa.Cbpp.Directory.Record
{
	public class Person
	{

		public int Id { get; set; }
		public Name Name { get; set; }
		public Work Work { get; set; }
		public Contact Contact { get; set; }
		public Bio Bio { get; set; }
		public DigitalMeasures DigitalMeasures { get; set; }
		public SocialMedia SocialMedia { get; set; }

		public List<Membership> Departments { get; set; }
		public List<Membership> Committees { get; set; }

		public List<Interest> Interests { get; set; }
		public List<Contribution> Contributions { get; set; }

		public string AltProfileUrl { get; set; }

		public string DepartmentsWithRole { get { return getDepartments(); } }

		public List<Affiliation> Affiliations { get { return getDmAffiliations(); } }
		public List<Education> Education { get { return getDmEducation(); } }
		public List<IntellCont> IntellectualContributions { get { return getDmIntellConts(); } }

		private DigitalMeasuresUser _dmUser { get; set; }
		public DigitalMeasuresUser dmUser { get { return autoLoadDmUser(); } }

		//constructor
		public Person()
		{
			Name = new Name();
			Work = new Work();
			Contact = new Contact();
			Bio = new Bio();
			DigitalMeasures = new DigitalMeasures();
			SocialMedia = new SocialMedia();

			Interests = new List<Interest>();
			Contributions = new List<Contribution>();

			Departments = new List<Membership>();
			Committees = new List<Membership>();
		}


		private List<Affiliation> getDmAffiliations()
		{
			if(!String.IsNullOrEmpty(DigitalMeasures.Username))
			{
				return dmUser.affiliationList();
			}
			return null;
		}

		private List<Education> getDmEducation()
		{
			if (!String.IsNullOrEmpty(DigitalMeasures.Username))
			{
				return dmUser.educationList();
			}
			return null;
		}

		private List<IntellCont> getDmIntellConts()
		{
			if (!String.IsNullOrEmpty(DigitalMeasures.Username))
			{
				List<IntellCont> output = dmUser.intellContList();
				output.Sort((x, y) => string.Compare(y.PublishYear, x.PublishYear));
				return output;
			}
			return null;
		}

		private DigitalMeasuresUser autoLoadDmUser()
		{
			if (!String.IsNullOrEmpty(DigitalMeasures.Username) && _dmUser == null )
			{
				_dmUser = new DigitalMeasuresUser(DigitalMeasures.Username);
				if (DigitalMeasures.Excludes.Any())
				{
					_dmUser.excludeIDs = DigitalMeasures.Excludes;
				}

			}
			return _dmUser;
		}


		public string getDepartments(params object[] args)
		{
			return membershipWithRole(Departments, args);
		}

		public string getCommittees(params object[] args)
		{
			return membershipWithRole(Committees, args);
		}

		private string membershipWithRole
		(
			List<Membership> Groups,
			params object[] args
		)
		{
			string delim = ", ";
			bool getRole = true;
			bool headOnly = true;
			string roleOpen = "";
			string roleClose = "";
			string openTag = "";
			string closeTag = "";
			string output = "";

			// variable number of parameters 
			for (int i = 0; i < args.Length; i++)
			{
				switch (i)
				{
					case 0:
						delim = (string)args[i];
						break;
					case 1:
						getRole = (bool)args[i];
						break;
					case 2:
						headOnly = (bool)args[i];
						break;
					case 3:
						roleOpen = (string)args[i];
						break;
					case 4:
						roleClose = (string)args[i];
						break;
					case 5:
						openTag = (string)args[i];
						break;
					case 6:
						closeTag = (string)args[i];
						break;
					default:
						break;
				}
			}

			for (int i = 0; i < Groups.Count; i++)
			{
				Membership group = Groups[i];

				output += openTag + group.Name;

				if (!string.IsNullOrEmpty(group.Role) && getRole)
				{
					if (headOnly)
					{
							output += (group.Role == "Chair") ? " " + roleOpen + "Chair" + roleClose : "";
							output += (group.Role == "Director") ? " " + roleOpen + "Director" + roleClose : "";
					}
					else
					{
						output += " " + roleOpen + group.Role + roleClose;
					}
				}

				if (Groups.Count > 1 && i < (Groups.Count - 1))
				{
					output += delim;
				}
				output += closeTag;
			}

			return output;
		}


		public string DepartmentBreadcrumb
		(
			string css_class,
			string listing_link
		)
		{
			string output = "";

			for (int i = 0; i < Departments.Count; i++)
			{
				var dept = Departments[i];

				output += "<a class=\""+ css_class + "\" href=\"" + listing_link + "?dept=" + dept.Id + "\" aria-label=\"department directory for " + dept.Name + "\">" + dept.Name + "</a>";

				if (Departments.Count > 1 && i < (Departments.Count - 1))
				{
					output += " / ";
				}
			}
			return output;
		}


		public override string ToString()
		{
			string output = string.Format("Name: {0} {1}\n", Name.First, Name.Last);
			output += string.Format("Faculty/Staff: {0}\n", Work.Category);
			output += string.Format("Phone: {0}  Fax: {1}  Email: {2}\n", Contact.Phone, Contact.Fax, Contact.Email);
			output += string.Format("Role: {0}  Focus: {1}", Work.Role, Work.Focus);

			return output;
		}

	}

	public class Name
	{
		public string First { get; set; }
		public string Last { get; set; }
		public string Prefix { get; set; }
		public string Suffix { get; set; }

		public override string ToString()
		{
			string p = string.IsNullOrEmpty(Prefix) ? "" : Prefix + " ";
			string s = string.IsNullOrEmpty(Suffix) ? "" : " " + Suffix;
			return string.Format("{0}{1} {2}{3}", p, First, Last, s);
		}
	}

	public class Work
	{
		public string Role { get; set; }
		public string Category { get; set; }
		public string Focus { get; set; }
		public string Courses { get; set; }
	}

	public class Contact
	{
		public string Phone { get; set; }
		public string Fax { get; set; }
		public string Email { get; set; }
		public Website Website { get; set; }
		public Office Office { get; set; }

		public Contact()
		{
			Office = new Office();
			Website = new Website();
		}
	}

	public class Office
	{
		public string Building { get; set; }
		public string Room { get; set; }
		public string Hours { get; set; }
	}

	public class Website
	{
		public string Url { get; set; }
		public string Name { get; set; }

		public bool IsValid { get { return (!String.IsNullOrEmpty(Url) && !String.IsNullOrEmpty(Name)); } }
	}

	public class Bio
	{
		public string CV { get; set; }
		public string Photo { get; set; }
		public string Story { get; set; }
		public string ExtendedStory { get; set; }
	}

	public class DigitalMeasures
	{
		public string Username { get; set; }
		public List<string> Excludes { get; set; }

		public DigitalMeasures()
		{
			Excludes = new List<string>();
		}

	}

	public class Membership
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string Role { get; set; }

		public override string ToString()
		{
			return "Name: " + Name + " Role: " + Role;
		}
	}

	public class SocialMedia
	{
		public string Facebook { get; set; }
		public string Linkedin { get; set; }
		public string Twitter { get; set; }
		public string Instagram { get; set; }

		public bool IsValid
		{ 	
			get 
			{ 
				return (
					   !String.IsNullOrEmpty(Facebook)
					|| !String.IsNullOrEmpty(Linkedin)
					|| !String.IsNullOrEmpty(Twitter)
					|| !String.IsNullOrEmpty(Instagram)
				); 
			}
		}
	}

	public class Interest
	{
		public int Id { get; set; }
		public string Type { get; set; }
		public string Photo { get; set; }
		public string AltText { get; set; }
		public string Link { get; set; }
		public string Description { get; set; }
	}

	public class Contribution
	{
		public int Id { get; set; }
		public string Type { get; set; }
		public string Photo { get; set; }
		public string AltText { get; set; }
		public string Link { get; set; }
		public string Description { get; set; }
	}

	public static partial class Load
	{
		public static Person Person(IDataRecord record)
		{
			Person person = new Person();

			person.Id = 						record.GetInt32(record.GetOrdinal("id"));

			person.Name.First = 				record["first_name"].ToString();
			person.Name.Last = 					record["last_name"].ToString();
			person.Name.Prefix = 				record["name_prefix"].ToString();
			person.Name.Suffix = 				record["name_suffix"].ToString();

			person.Work.Role =					record["role"].ToString();
			person.Work.Category = 				record["faculty_staff"].ToString();
			person.Work.Focus = 				record["focus"].ToString();
			person.Work.Courses = 				record["courses"].ToString();

			person.Contact.Phone = 				record["phone"].ToString();
			person.Contact.Fax = 				record["fax"].ToString();
			person.Contact.Email = 				record["email"].ToString();
			person.Contact.Website.Name = 		record["website_name"].ToString();
			person.Contact.Website.Url = 		record["website"].ToString();
			person.Contact.Office.Building = 	record["building"].ToString();
			person.Contact.Office.Room = 		record["office"].ToString();
			person.Contact.Office.Hours = 		record["office_hours"].ToString();

			person.Bio.CV = 					record["cv_url"].ToString();
			person.Bio.Photo = 					record["photo_url"].ToString();
			person.Bio.Story = 					record["details"].ToString();
			person.Bio.ExtendedStory =			record["details"].ToString();

			person.DigitalMeasures.Username =	record["dm_username"].ToString();
			//person.DigitalMeasures.Exclude =	record["dm_exclude"].ToString();

			person.AltProfileUrl = 				record["alt_profile_url"].ToString();

			person.SocialMedia.Linkedin = 		record["linkedin"].ToString();

			foreach (var group in record["depts"].ToString().Split(';'))
			{
				if (!string.IsNullOrEmpty(group))
				{
					string[] x = collectionStringToArray(group, 3, '^');
					person.Departments.Add(new Membership() { Id = Int32.Parse(x[0]), Name = x[1], Role = x[2] });
				}
			}

			foreach (var group in record["committees"].ToString().Split(';'))
			{
				if (!string.IsNullOrEmpty(group))
				{
					string[] x = collectionStringToArray(group, 3, '^');
					person.Committees.Add(new Membership() { Id = Int32.Parse(x[0]), Name = x[1], Role = x[2] });
				}
			}

			return person;
		}

		private static string[] collectionStringToArray(string str, int len, char delim)
		{
			string[] x = new string[len];
			string[] y = str.Split(delim);

			if (y.Length >= x.Length)
			{
				x = y;
			}
			else
			{
				for (int i = 0; i < y.Length; i++)
					x[i] = y[i];
			}

			return x;
		}

	}

	public static partial class Save
	{
		public static void Person(Person person)
		{

		}
	}


}
