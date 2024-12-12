using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Xml;
using System.Xml.Linq;
using System.Threading.Tasks;

namespace Uaa.Cbpp.DigitalMeasures
{

	public class ResponseObject
	{
		public string content { get; set; }
		public bool error { get; set; }
		public string errorMessage { get; set; }

		public ResponseObject() { error = false; }
	} // [/ResponseObject Class]

	public static class ChangeStringEncoding
	{

		public static string toUTF8(
			string _input
		)
		{
			// Encode the string in a UTF-8 byte array
			byte[] encodedString = Encoding.UTF8.GetBytes(_input);
			// Put the byte array into a stream and rewind it to the beginning
			MemoryStream ms = new MemoryStream(encodedString);
			ms.Flush();
			ms.Position = 0;

			StreamReader sr = new StreamReader(ms);
			string output = sr.ReadToEnd();
			return output;
		}

	} // [/ChangeStringEncoding Class]

	public static class XMLUtility
	{
		public static string RemoveAllNamespacesFromString(
			string xmlDocument
		)
		{
			XElement xmlDocumentWithoutNs = removeAllNamespaces(XElement.Parse(xmlDocument));
			return xmlDocumentWithoutNs.ToString();
		}

		private static XElement RemoveAllNamespaces(
			XElement xmlDocument
		)
		{
			XElement xmlDocumentWithoutNs = removeAllNamespaces(xmlDocument);
			return xmlDocumentWithoutNs;
		}

		private static XElement removeAllNamespaces(
			XElement xmlDocument
		)
		{
			var stripped = new XElement(xmlDocument.Name.LocalName);
			foreach (var attribute in
				xmlDocument.Attributes().Where(
					attribute =>
						!attribute.IsNamespaceDeclaration &&
						String.IsNullOrEmpty(attribute.Name.NamespaceName)
				)
			)
			{
				stripped.Add(new XAttribute(attribute.Name.LocalName, attribute.Value));
			}
			if (!xmlDocument.HasElements)
			{
				stripped.Value = xmlDocument.Value;
				return stripped;
			}
			stripped.Add(xmlDocument.Elements().Select(
				el =>
					RemoveAllNamespaces(el)
				)
			);
			return stripped;
		}

		public static XmlNodeList getNodeList(
			XmlDocument xmlDoc,
			string search
		)
		{
			XmlElement root = xmlDoc.DocumentElement;
			return root.SelectNodes("/Data/Record/" + search);
		}

		public static XmlNodeList getNodeList(
			XmlNode node,
			string search
		)
		{
			return node.SelectNodes(search);
		}

		public static XElement Sort(
			XElement _element
		)
		{
			return new XElement(
				_element.Name,
				_element.Attributes(),
				from child in _element.Nodes()
				where child.NodeType != XmlNodeType.Element
				select child,
				from child in _element.Elements()
				orderby child.Name.ToString()
				select Sort(child)
			);
		}

		public static XDocument Sort(
			XDocument file
		)
		{
			return new XDocument(
				file.Declaration,
				from child in file.Nodes()
				where child.NodeType != XmlNodeType.Element
				select child,
				Sort(file.Root)
			);
		}

	}  // [/XMLUtility Class]

	public static class DigitalMeasuresConnection
	{

		private static string baseAddress = "https://webservices.digitalmeasures.com";
		private static string loginUri = "/login/service/v4";
		private static string username = "user-name/web_service";
		private static string password = "########";
		private static string instrumentKey = "/INDIVIDUAL-ACTIVITIES-University";

		public static ResponseObject requestXmlData(
			string uri
		)
		{
			// specify protocol TLS 1.2 (includes server name indication (SNI))
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

			// create credentials
			CredentialCache credentialCache = new CredentialCache();
			credentialCache.Add(
				 new Uri(baseAddress),
				 "Basic",
				 new NetworkCredential(username, password)
			);

			// create client handler and client
			HttpClientHandler handler = new HttpClientHandler();
			handler.PreAuthenticate = true;
			handler.Credentials = credentialCache;
			handler.AllowAutoRedirect = true;
			handler.AutomaticDecompression = DecompressionMethods.GZip;

			HttpClient client = new HttpClient(handler);
			client.BaseAddress = new Uri(baseAddress);

			ResponseObject response = new ResponseObject();

			try
			{
				response.content = client.GetStringAsync(uri).Result;
				response.error = false;
			}
			catch (Exception e)
			{
				//HttpRequestException
				if (e is WebException && ((WebException)e).Status == WebExceptionStatus.ProtocolError)
				{
					WebResponse errResp = ((WebException)e).Response;
					using (Stream respStream = errResp.GetResponseStream())
					{
						// read the error response - to do
					}
				}
				response.error = true;
				response.errorMessage = e.Message;
			}

			return response;
		}

		public static XmlDocument getUserSchemaData(
			string username
		)
		{
			return (getUserSchemaData(username, ""));
		}

		public static XmlDocument getUserSchemaData(
			string username,
			string key
		)
		{
			XmlDocument xmlDoc = new XmlDocument();

			string key_ = (String.IsNullOrEmpty(key)) ? "" : "/" + key;

			string schemaUrl_ = loginUri + "/SchemaData" + instrumentKey + "/USERNAME:" + username + key_;

			ResponseObject result = requestXmlData(schemaUrl_);

			if (!result.error)
			{
				//Remove namespaces
				result.content = XMLUtility.RemoveAllNamespacesFromString(result.content);

				// Encode the XML string in a UTF-8 byte array
				result.content = ChangeStringEncoding.toUTF8(result.content);

				xmlDoc.LoadXml(result.content);
			}

			return xmlDoc;
		}

	} // [/DigitalMeasuresConnection Class]


	public class DigitalMeasuresUser
	{
		public string user { get; set; }
		public string linkClasses { get; set; }
		public XmlDocument data { get; set; }
		public List<string> excludeIDs = new List<string>();

		public DigitalMeasuresUser(
			string userid
		)
		{
			init(userid, "x dm-link");
		}
		public DigitalMeasuresUser(
			string userid,
			string links
		)
		{
			init(userid, links);
		}

		private void init(
			string userid,
			string links
		)
		{
			user = userid;
			linkClasses = links;
			data = DigitalMeasuresConnection.getUserSchemaData(userid);
		}


		// SPECIFIC NODELISTS
		public XmlNodeList intellContNodeList()
		{
			return data.SelectNodes("/Data/Record/INTELLCONT[STATUS[.='Published' or .='Accepted']]");
		}
		public XmlNodeList journalNodeList()
		{
			return data.SelectNodes("/Data/Record/INTELLCONT[CONTYPE = 'Journal Article' and STATUS = 'Published']");
			// to exclude attribute with value:  /INTELLCONT[not(@uid='id1' or @uid='id2')] or /INTELLCONT[CONTYPE='blah' and not(@id='124692285440')]
		}

		public XmlNodeList bookNodeList()
		{
			return data.SelectNodes("/Data/Record/INTELLCONT[CONTYPE[.='Book' or .='Textbook' or .='Book Chapter'] and STATUS = 'Published']");
		}

		public XmlNodeList workingPaperNodeList()
		{
			return data.SelectNodes("/Data/Record/INTELLCONT[CONTYPE = 'Working Paper' and STATUS[.='Published' or .='Accepted']]");
		}

		public XmlNodeList awardNodeList()
		{
			return data.SelectNodes("/Data/Record/AWARDHONOR");
		}

		public XmlNodeList presentationNodeList()
		{
			return data.SelectNodes("/Data/Record/PRESENT[MEETING_TYPE = 'Conference']");
		}

		public XmlNodeList affiliationNodeList()
		{
			return data.SelectNodes("/Data/Record/MEMBER");
		}

		public XmlNodeList educationNodeList()
		{
			return data.SelectNodes("/Data/Record/EDUCATION");
		}

		public XmlNodeList editorshipNodeList()
		{
			return data.SelectNodes("/Data/Record/SERVICE_PROFESSIONAL[contains(@ROLE,'Editor')]");
		}


		// RETRIEVE NODES AND RETURN FORMATTED OUTPUT
		// general intellectual contributions (journals, books, working papers, etc.)
		public List<IntellCont> intellContList()
		{
			return getIntellContList(intellContNodeList());
		}

		// journals
		public string journals()
		{
			return journals("li", "span");
		}
		public string journals(
			string blockEl
		)
		{
			return journals(blockEl, "span");
		}
		public string journals(
			string blockEl,
			string childEl
		)
		{
			return formatWrittenAsCitation(journalNodeList(), blockEl, childEl);
		}

		// books
		public string books()
		{
			return books("li", "span");
		}
		public string books(
			string blockEl
		)
		{
			return books(blockEl, "span");
		}
		public string books(
			string blockEl,
			string childEl
		)
		{
			return formatWrittenAsCitation(bookNodeList(), blockEl, childEl);
		}

		// working papers
		public string workingPapers()
		{
			return workingPapers("li", "span");
		}
		public string workingPapers(
			string blockEl
		)
		{
			return workingPapers(blockEl, "span");
		}
		public string workingPapers(
			string blockEl,
			string childEl
		)
		{
			return formatWrittenAsCitation(workingPaperNodeList(), blockEl, childEl);
		}

		// awards
		public string awards()
		{
			return awards("li", "span");
		}
		public string awards(
			string blockEl
		)
		{
			return awards(blockEl, "span");
		}
		public string awards(
			string blockEl,
			string childEl
		)
		{
			return formatAwards(awardNodeList(), blockEl, childEl);
		}

		// presentations
		public string presentations()
		{
			return presentations("li", "span");
		}
		public string presentations(
			string blockEl
		)
		{
			return presentations(blockEl, "span");
		}
		public string presentations(
			string blockEl,
			string childEl
		)
		{
			return formatPresentations(presentationNodeList(), blockEl, childEl);
		}
		public List<Presentation> presentationList()
		{
			return getPresentationList(presentationNodeList());
		}

		// affiliations
		public string affiliations()
		{
			return affiliations("li", "span");
		}
		public string affiliations(
			string blockEl
		)
		{
			return affiliations(blockEl, "span");
		}
		public string affiliations(
			string blockEl,
			string childEl
		)
		{
			return formatAffiliations(affiliationNodeList(), blockEl, childEl);
		}
		public List<Affiliation> affiliationList()
		{
			return getAffiliationList(affiliationNodeList());
		}

		// education
		public string education()
		{
			return education("li", "span");
		}
		public string education(
			string blockEl
		)
		{
			return education(blockEl, "span");
		}
		public string education(
			string blockEl,
			string childEl
		)
		{
			return formatEducation(educationNodeList(), blockEl, childEl);
		}
		public List<Education> educationList()
		{
			return getEducationList(educationNodeList());
		}


		// FORMATTING
		// formatting for written contributions
		private string formatWrittenAsCitation(
			XmlNodeList nodeList
		)
		{
			return (formatWrittenAsCitation(nodeList, "li", "span", "cite"));
		}
		private string formatWrittenAsCitation(
			XmlNodeList nodeList,
			string blockEl
		)
		{
			return (formatWrittenAsCitation(nodeList, blockEl, "span", "cite"));
		}
		private string formatWrittenAsCitation(
			XmlNodeList nodeList,
			string blockEl,
			string childEl
		)
		{
			return (formatWrittenAsCitation(nodeList, blockEl, childEl, "cite"));
		}
		private string formatWrittenAsCitation(
			XmlNodeList nodeList,
			string blockEl,
			string childEl,
			string blockElClass
		)
		{
			string output = "";

			foreach (XmlNode node in nodeList)
			{
				string id = node.Attributes["id"].Value;
				output += "<!-- id: " + id + " -->";

				//excludeIDs
				if (excludeIDs.Contains(id)) { continue; }

				XmlNodeList authList = XMLUtility.getNodeList(node, "INTELLCONT_AUTH");

				output += "<" + blockEl + " class=\"" + blockElClass + "\">";
				output += "<" + childEl + " class=\"" + blockElClass + "-author\">";
				for (int i = 0; i < authList.Count; i++)
				{
					string author = authList[i].SelectSingleNode("FNAME").InnerText + " " + authList[i].SelectSingleNode("LNAME").InnerText.Trim();
					if (i == (authList.Count - 1))
					{
						output += (authList.Count > 1) ? "and " + author + ". " : author + ". ";
					}
					else if (authList.Count > 2)
					{
						output += author + ", ";
					}
					else
					{
						output += author + " ";
					}
				}
				output += "</" + childEl + ">";

				string title = "<" + childEl + " class=\"" + blockElClass + "-title\">" + node.SelectSingleNode("TITLE").InnerText.Trim() + "</" + childEl + ">";
				string publisher = "<" + childEl + " class=\"" + blockElClass + "-publisher\">" + node.SelectSingleNode("PUBLISHER").InnerText.Trim() + "</" + childEl + "> ";

				if (!String.IsNullOrEmpty(node.SelectSingleNode("WEB_ADDRESS").InnerText))
				{
					output += "<a class=\"" + linkClasses + "\" href=\"" + node.SelectSingleNode("WEB_ADDRESS").InnerText + "\" target=\"_blank\">" + title + "</a> ";
				}
				else
				{
					output += title + " ";
				}

				output += publisher;
				output += " <" + childEl + " class=\"" + blockElClass + "-year\">(" + node.SelectSingleNode("DTY_PUB").InnerText.Trim() + ")</" + childEl + "> ";
				output += "</" + blockEl + ">";
			}

			return output;
		}
		public List<IntellCont> getIntellContList(
			XmlNodeList nodeList
		)
		{
			List<IntellCont> output = new List<IntellCont>();

			foreach (XmlNode node in nodeList)
			{
				if (excludeIDs.Contains(node.Attributes["id"].Value)) { continue; }

				IntellCont cont = new IntellCont();

				XmlNodeList authList = XMLUtility.getNodeList(node, "INTELLCONT_AUTH");
				for (int i = 0; i < authList.Count; i++)
				{
					Author auth = new Author();

					auth.Id = authList[i].Attributes["id"].Value;
					auth.FirstName = authList[i].SelectSingleNode("FNAME").InnerText.Trim();
					auth.LastName = authList[i].SelectSingleNode("LNAME").InnerText.Trim();
					auth.StudentLevel = authList[i].SelectSingleNode("STUDENT_LEVEL").InnerText.Trim();

					cont.Authors.Add(auth);
				}

				cont.Id = node.Attributes["id"].Value;
				cont.Type = node.SelectSingleNode("CONTYPE").InnerText.Trim();
				cont.TypeOther = node.SelectSingleNode("CONTYPEOTHER").InnerText.Trim();
				cont.Classification = node.SelectSingleNode("CLASSIFICATION").InnerText.Trim();
				cont.Status = node.SelectSingleNode("STATUS").InnerText.Trim();
				cont.Title = node.SelectSingleNode("TITLE").InnerText.Trim();
				cont.TitleSecondary = node.SelectSingleNode("TITLE_SECONDARY").InnerText.Trim();
				cont.Publisher = node.SelectSingleNode("PUBLISHER").InnerText.Trim();
				cont.PublisherCityState = node.SelectSingleNode("PUBCTYST").InnerText.Trim();
				cont.PublisherCountry = node.SelectSingleNode("PUBCNTRY").InnerText.Trim();
				cont.PublishDay = node.SelectSingleNode("DTD_PUB").InnerText.Trim();
				cont.PublishMonth = node.SelectSingleNode("DTM_PUB").InnerText.Trim();
				cont.PublishYear = node.SelectSingleNode("DTY_PUB").InnerText.Trim();
				cont.Volume = node.SelectSingleNode("VOLUME").InnerText.Trim();
				cont.Issue = node.SelectSingleNode("ISSUE").InnerText.Trim();
				cont.PageNumber = node.SelectSingleNode("PAGENUM").InnerText.Trim();
				cont.WebAddress = node.SelectSingleNode("WEB_ADDRESS").InnerText.Trim();

				output.Add(cont);
			}

			return output;
		}

		// formatting for affiliations
		private string formatAffiliations(
			XmlNodeList nodeList
		)
		{
			return (formatAffiliations(nodeList, "li", "span", "affiliation"));
		}
		private string formatAffiliations(
			XmlNodeList nodeList,
			string blockEl
		)
		{
			return (formatAffiliations(nodeList, blockEl, "span", "affiliation"));
		}
		private string formatAffiliations(
			XmlNodeList nodeList,
			string blockEl,
			string childEl
		)
		{
			return (formatAffiliations(nodeList, blockEl, childEl, "affiliation"));
		}
		private string formatAffiliations(
			XmlNodeList nodeList,
			string blockEl,
			string childEl,
			string blockElClass
		)
		{
			string output = "";
			//ex: Canadian Transportation Research Forum (CTRF)

			foreach (XmlNode node in nodeList)
			{

				output += "<" + blockEl + " class=\"" + blockElClass + "\">";
				output += "<" + childEl + " class=\"" + blockElClass + "-organization\">" + node.SelectSingleNode("ORG").InnerText.Trim() + "</" + childEl + "> ";
				output += "<" + childEl + " class=\"" + blockElClass + "-abbreviation\">(" + node.SelectSingleNode("ORGABBR").InnerText.Trim() + ")</" + childEl + ">";
				output += "</" + blockEl + ">";
			}

			return output;
		}
		public List<Affiliation> getAffiliationList(
			XmlNodeList nodeList
		)
		{
			List<Affiliation> output = new List<Affiliation>();

			foreach (XmlNode node in nodeList)
			{
				if (excludeIDs.Contains(node.Attributes["id"].Value)) { continue; }

				Affiliation affiliation = new Affiliation();

				affiliation.Id = node.Attributes["id"].Value;
				affiliation.Organization = node.SelectSingleNode("ORG").InnerText.Trim();
				affiliation.Abbreviation = node.SelectSingleNode("ORGABBR").InnerText.Trim();
				affiliation.Scope = node.SelectSingleNode("SCOPE").InnerText.Trim();
				affiliation.Description = node.SelectSingleNode("DESC").InnerText.Trim();
				affiliation.StartDay = node.SelectSingleNode("DTD_START").InnerText.Trim();
				affiliation.StartMonth = node.SelectSingleNode("DTM_START").InnerText.Trim();
				affiliation.StartYear = node.SelectSingleNode("DTY_START").InnerText.Trim();
				affiliation.EndDay = node.SelectSingleNode("DTD_END").InnerText.Trim();
				affiliation.EndMonth = node.SelectSingleNode("DTM_END").InnerText.Trim();
				affiliation.EndYear = node.SelectSingleNode("DTY_END").InnerText.Trim();

				output.Add(affiliation);
			}

			return output;
		}

		// foramtting for awards
		private string formatAwards(
			XmlNodeList nodeList
		)
		{
			return (formatAwards(nodeList, "li", "span", "award"));
		}
		private string formatAwards(
			XmlNodeList nodeList,
			string blockEl
		)
		{
			return (formatAwards(nodeList, blockEl, "span", "award"));
		}
		private string formatAwards(
			XmlNodeList nodeList,
			string blockEl,
			string childEl
		)
		{
			return (formatAwards(nodeList, blockEl, childEl, "award"));
		}
		private string formatAwards(
			XmlNodeList nodeList,
			string blockEl,
			string childEl,
			string blockElClass
		)
		{
			string output = "";

			foreach (XmlNode node in nodeList)
			{

				output += "<" + blockEl + " class=\"" + blockElClass + "\">";
				output += "<" + childEl + " class=\"" + blockElClass + "-year\">" + node.SelectSingleNode("DTY_DATE").InnerText.Trim() + "</" + childEl + "> ";
				output += "<" + childEl + " class=\"" + blockElClass + "-name\">" + node.SelectSingleNode("NAME").InnerText.Trim() + "</" + childEl + "> ";
				output += "<" + childEl + " class=\"" + blockElClass + "-organization\">" + node.SelectSingleNode("ORG").InnerText.Trim() + "</" + childEl + "> ";

				if (!String.IsNullOrEmpty(node.SelectSingleNode("DESC").InnerText))
				{
					output += "<" + childEl + " class=\"" + blockElClass + "-description\">" + node.SelectSingleNode("DESC").InnerText.Trim() + "</" + childEl + "> ";
				}
				output += "</" + blockEl + ">";
			}

			return output;
		}

		public List<Award> getAwardList(
			XmlNodeList nodeList
		)
		{
			List<Award> output = new List<Award>();

			foreach (XmlNode node in nodeList)
			{
				Award award = new Award();

				award.Id = node.Attributes["id"].Value;
				award.Name = node.SelectSingleNode("NAME").InnerText.Trim();
				award.Organization = node.SelectSingleNode("ORG").InnerText.Trim();
				award.Scope = node.SelectSingleNode("SCOPE").InnerText.Trim();
				award.scopeLocale = node.SelectSingleNode("SCOPE_LOCALE").InnerText.Trim();
				award.Description = node.SelectSingleNode("DESC").InnerText.Trim();
				award.DateDay = node.SelectSingleNode("DTD_DATE").InnerText.Trim();
				award.DateMonth = node.SelectSingleNode("DTM_DATE").InnerText.Trim();
				award.DateYear = node.SelectSingleNode("DTY_DATE").InnerText.Trim();

				output.Add(award);
			}
			return output;
		}

		// formatting for education
		private string formatEducation(
			XmlNodeList nodeList
		)
		{
			return (formatEducation(nodeList, "li", "span", "education"));
		}
		private string formatEducation(
			XmlNodeList nodeList,
			string blockEl
		)
		{
			return (formatEducation(nodeList, blockEl, "span", "education"));
		}
		private string formatEducation(
			XmlNodeList nodeList,
			string blockEl,
			string childEl
		)
		{
			return (formatEducation(nodeList, blockEl, childEl, "education"));
		}
		private string formatEducation(
			XmlNodeList nodeList,
			string blockEl,
			string childEl,
			string blockElClass
		)
		{
			string output = "";
			//ex: The University of Winnipeg, B.A. in Economics

			foreach (XmlNode node in nodeList)
			{

				output += "<" + blockEl + " class=\"" + blockElClass + "\">";
				output += "<" + childEl + " class=\"" + blockElClass + "-school\">" + node.SelectSingleNode("SCHOOL").InnerText.Trim() + "</" + childEl + ">, ";
				output += "<" + childEl + " class=\"" + blockElClass + "-degree\">" + node.SelectSingleNode("DEG").InnerText.Trim() + "</" + childEl + "> in ";
				output += "<" + childEl + " class=\"" + blockElClass + "-major\">" + node.SelectSingleNode("MAJOR").InnerText.Trim() + "</" + childEl + ">";
				output += "</" + blockEl + ">";
			}

			return output;
		}
		public List<Education> getEducationList(
			XmlNodeList nodeList
		)
		{
			List<Education> output = new List<Education>();

			foreach (XmlNode node in nodeList)
			{
				if (excludeIDs.Contains(node.Attributes["id"].Value)) { continue; }

				Education edu = new Education();

				edu.Id = node.Attributes["id"].Value;
				edu.Degree = node.SelectSingleNode("DEG").InnerText.Trim();
				edu.DegOther = node.SelectSingleNode("DEGOTHER").InnerText.Trim();
				edu.School = node.SelectSingleNode("SCHOOL").InnerText.Trim();
				edu.Location = node.SelectSingleNode("LOCATION").InnerText.Trim();
				edu.Major = node.SelectSingleNode("MAJOR").InnerText.Trim();
				edu.DissertationTitle = node.SelectSingleNode("DISSTITLE").InnerText.Trim();
				edu.Distinction = node.SelectSingleNode("DISTINCTION").InnerText.Trim();
				edu.Highest = node.SelectSingleNode("HIGHEST").InnerText.Trim();
				edu.GraduationYear = node.SelectSingleNode("YR_COMP").InnerText.Trim();

				output.Add(edu);
			}

			return output;
		}

		// formatting for presentations
		private string formatPresentations(
			XmlNodeList nodeList
		)
		{
			return (formatPresentations(nodeList, "li", "span", "presentation", false));
		}
		private string formatPresentations(
			XmlNodeList nodeList,
			string blockEl
		)
		{
			return (formatPresentations(nodeList, blockEl, "span", "presentation", false));
		}
		private string formatPresentations(
			XmlNodeList nodeList,
			string blockEl,
			string childEl
		)
		{
			return (formatPresentations(nodeList, blockEl, childEl, "presentation", false));
		}
		private string formatPresentations(
			XmlNodeList nodeList,
			string blockEl,
			string childEl,
			string blockElClass
		)
		{
			return (formatPresentations(nodeList, blockEl, childEl, blockElClass, false));
		}
		private string formatPresentations(
			XmlNodeList nodeList,
			string blockEl,
			string childEl,
			string blockElClass,
			bool academicOnly
		)
		{
			string output = "";

			foreach (XmlNode node in nodeList)
			{

				if (academicOnly && (node.SelectSingleNode("ACADEMIC").InnerText.Trim() != "Academic")) continue;

				// PRESENTATION_TYPE, NAME, ORG, LOCATION, TITLE, MEETING_TYPE, DTM_DATE, DTD_DATE, DTY_DATE
				string date_ = "<" + childEl + " class=\"" + blockElClass + "-date\">" + node.SelectSingleNode("DTM_DATE").InnerText.Trim();
				if (!String.IsNullOrEmpty(node.SelectSingleNode("DTD_DATE").InnerText))
					date_ += " " + node.SelectSingleNode("DTD_DATE").InnerText.Trim() + ",";
				date_ += " " + node.SelectSingleNode("DTY_DATE").InnerText.Trim() + "</" + childEl + "> ";

				string title_ = "<" + childEl + " class=\"" + blockElClass + "-title\">" + node.SelectSingleNode("TITLE").InnerText.Trim() + "</" + childEl + "> ";

				bool fullLocation_ = (!String.IsNullOrEmpty(node.SelectSingleNode("ORG").InnerText) && !String.IsNullOrEmpty(node.SelectSingleNode("LOCATION").InnerText));
				bool haveLocation_ = (!String.IsNullOrEmpty(node.SelectSingleNode("ORG").InnerText) || !String.IsNullOrEmpty(node.SelectSingleNode("LOCATION").InnerText));

				string location_ = "";
				if (!String.IsNullOrEmpty(node.SelectSingleNode("ORG").InnerText))
					location_ += node.SelectSingleNode("ORG").InnerText.Trim();

				if (fullLocation_)
					location_ += " - ";

				if (!String.IsNullOrEmpty(node.SelectSingleNode("LOCATION").InnerText))
					location_ += node.SelectSingleNode("LOCATION").InnerText.Trim();

				if (haveLocation_)
					location_ = "<" + childEl + " class=\"" + blockElClass + "-location\">" + location_ + "</" + childEl + "> ";


				// July( 28,) 2010
				// "title"
				// Organization[ - ]Location
				output += "<" + blockEl + " class=\"" + blockElClass + "\">";
				output += date_;
				if (!String.IsNullOrEmpty(node.SelectSingleNode("TITLE").InnerText))
					output += title_;
				output += location_;
				output += "</" + blockEl + ">";
			}

			return output;
		}

		public List<Presentation> getPresentationList(
			XmlNodeList nodeList
		)
		{
			List<Presentation> output = new List<Presentation>();

			foreach (XmlNode node in nodeList)
			{
				Presentation cont = new Presentation();

				XmlNodeList authList = XMLUtility.getNodeList(node, "PRESENT_AUTH");
				for (int i = 0; i < authList.Count; i++)
				{
					Author auth = new Author();

					auth.Id = authList[i].Attributes["id"].Value;
					auth.FacultyName = authList[i].SelectSingleNode("FACULTY_NAME").InnerText.Trim();
					auth.FirstName = authList[i].SelectSingleNode("FNAME").InnerText.Trim();
					auth.MiddleName = authList[i].SelectSingleNode("MNAME").InnerText.Trim();
					auth.LastName = authList[i].SelectSingleNode("LNAME").InnerText.Trim();
					auth.Role = authList[i].SelectSingleNode("ROLE").InnerText.Trim();
					auth.StudentLevel = authList[i].SelectSingleNode("STUDENT_LEVEL").InnerText.Trim();

					cont.Authors.Add(auth);
				}

				cont.Id = node.Attributes["id"].Value;
				cont.Type = node.SelectSingleNode("PRESENTATION_TYPE").InnerText.Trim();
				cont.TypeOther = node.SelectSingleNode("PRESENTATION_TYPE_OTHER").InnerText.Trim();
				cont.Name = node.SelectSingleNode("PRESENTATION_TYPE_OTHER").InnerText.Trim();
				cont.Organization = node.SelectSingleNode("PRESENTATION_TYPE_OTHER").InnerText.Trim();
				cont.Location = node.SelectSingleNode("PRESENTATION_TYPE_OTHER").InnerText.Trim();
				cont.Title = node.SelectSingleNode("TITLE").InnerText.Trim();
				cont.MeetingType = node.SelectSingleNode("MEETING_TYPE").InnerText.Trim();
				cont.MeetingTypeOther = node.SelectSingleNode("MEETING_TYPE_OTHER").InnerText.Trim();
				cont.Academic = node.SelectSingleNode("ACADEMIC").InnerText.Trim();
				cont.Scope = node.SelectSingleNode("SCOPE").InnerText.Trim();
				cont.Refereed = node.SelectSingleNode("REFEREED").InnerText.Trim();
				cont.Classification = node.SelectSingleNode("CLASSIFICATION").InnerText.Trim();
				cont.Abstract = node.SelectSingleNode("ABSTRACT").InnerText.Trim();
				cont.DateDay = node.SelectSingleNode("DTD_DATE").InnerText.Trim();
				cont.DateMonth = node.SelectSingleNode("DTM_DATE").InnerText.Trim();
				cont.DateYear = node.SelectSingleNode("DTY_DATE").InnerText.Trim();

				output.Add(cont);
			}

			return output;
		}

	} // [/DigitalMeasuresUser]

public class Affiliation
	{
		public string Id { get; set; }
		public string Organization { get; set; }
		public string Abbreviation { get; set; }
		public string Leadership { get; set; }
		public string Scope { get; set; }
		public string Description { get; set; }
		public string StartDay { get; set; }
		public string StartMonth { get; set; }
		public string StartYear { get; set; }
		public string EndDay { get; set; }
		public string EndMonth { get; set; }
		public string EndYear { get; set; }
	}

	public class Education
	{
		public string Id { get; set; }
		public string Degree { get; set; }
		public string DegOther { get; set; }
		public string School { get; set; }
		public string Location { get; set; }
		public string Major { get; set; }
		public string DissertationTitle { get; set; }
		public string Distinction { get; set; }
		public string Highest { get; set; }
		public string GraduationYear { get; set; }
	}

	public class IntellCont
	{
		public string Id { get; set; }
		public string Type { get; set; }
		public string TypeOther { get; set; }
		public string Classification { get; set; }
		public string Status { get; set; }
		public string Title { get; set; }
		public string TitleSecondary { get; set; }
		public string Publisher { get; set; }
		public string PublisherCityState { get; set; }
		public string PublisherCountry { get; set; }
		public string PublishDay { get; set; }
		public string PublishMonth { get; set; }
		public string PublishYear { get; set; }
		public string WebAddress { get; set; }
		public string Volume { get; set; }
		public string Issue { get; set; }
		public string PageNumber { get; set; }
		public List<Author> Authors { get; set; }

		public IntellCont()
		{
			Authors = new List<Author>();
		}
	}

	public class Author
	{
		public string Id { get; set; }
		public string FacultyName { get; set; }
		public string FirstName { get; set; }
		public string MiddleName { get; set; }
		public string LastName { get; set; }
		public string Role { get; set; }
		public string StudentLevel { get; set; }
	}

	public class Award
	{

		public string Id { get; set; }
		public string Name { get; set; }
		public string Organization { get; set; }
		public string Scope { get; set; }
		public string scopeLocale { get; set; }
		public string Description { get; set; }
		public string DateDay { get; set; }
		public string DateMonth { get; set; }
		public string DateYear { get; set; }


	}

	public class Presentation
	{
		public string Id { get; set; }
		public string Type { get; set; }
		public string TypeOther { get; set; }
		public string Name { get; set; }
		public string Organization { get; set; }
		public string Location { get; set; }
		public string Title { get; set; }
		//public Author Author { get; set; }
		public string MeetingType { get; set; }
		public string MeetingTypeOther { get; set; }
		public string Academic { get; set; }
		public string Scope { get; set; }
		public string Refereed { get; set; }
		public string Classification { get; set; }
		public string Abstract { get; set; }
		public string DateDay { get; set; }
		public string DateMonth { get; set; }
		public string DateYear { get; set; }
		public List<Author> Authors { get; set; }

		public Presentation()
		{
			Authors = new List<Author>();
		}

	}
}