using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;

using RazorEngine;
using RazorEngine.Templating;

using Uaa.Cbpp.Utility;

// Usage:
// using Uaa.Cbpp.Google.Calendar;
//var GCal = new Calendar("gfbmn4kqtplu5tbtkru48ppbjk@group.calendar.google.com");
//Calendar.EventsListOptionalParms options = new Calendar.EventsListOptionalParms
//{
//	TimeMin = DateTime.Parse("2017-08-01T08:00:00-09:00"),
//	TimeMax = DateTime.Parse("2018-08-01T07:59:59-09:00"),
//	SingleEvents = true,
//	SetOrderBy = "StartTime"
//};
//String result = GCal.SeminarEventList(options);

namespace Uaa.Cbpp.Google.Calendar
{
	public class Calendar
	{
		private CalendarService CalService;
		private string ApplicationName = "Redacted";
		private string serviceAccountEmail = "Redacted";
		public string CalendarID { get; set; }

		public Calendar(string calendarID = null)
		{

			// Load key file as bytes
			byte[] keyFile = ResourceHelpers.GetEmbeddedResourceAsBytes("Uaa.Cbpp.Assets.client_secret.p12");

			// Set scopes for credentials
			// CalendarService.Scope.Calendar 			- Manage your calendars
			// CalendarService.Scope.CalendarReadonly 	- View your Calendars
			string[] scopes = {
				CalendarService.Scope.Calendar
			};

			// Load certificate
			X509Certificate2 certificate = new X509Certificate2(keyFile, "notasecret", X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);

			// Create credentials
			ServiceAccountCredential Credential = new ServiceAccountCredential(
				new ServiceAccountCredential.Initializer(serviceAccountEmail) { Scopes = scopes }.FromCertificate(certificate)
			);

			// Create Google Calendar API service.
			CalService = new CalendarService(new BaseClientService.Initializer()
			{
				HttpClientInitializer = Credential,
				ApplicationName = ApplicationName,
			});

			// Set calendar id at construction if provided
			// ex. (Econ Seminar Calendar): gfbmn4kqtplu5tbtkru48ppbjk@group.calendar.google.com
			if (!String.IsNullOrEmpty(calendarID))
			{
				CalendarID = calendarID;
			}
		}

		public class EventsListOptionalParms
		{
			/// Whether to always include a value in the email field for the organizer, creator and attendees, even if no real email is available (i.e. a generated, non-working value will be provided). The use of this option is discouraged and should only be used by clients which cannot handle the absence of an email address value in the mentioned places. Optional. The default is False.
			public bool? AlwaysIncludeEmail { get; set; }
			/// Specifies event ID in the iCalendar format to be included in the response. Optional.
			public string ICalUID { get; set; }
			/// The maximum number of attendees to include in the response. If there are more than the specified number of attendees, only the participant is returned. Optional.
			public int? MaxAttendees { get; set; }
			/// Maximum number of events returned on one result page. The number of events in the resulting page may be less than this value, or none at all, even if there are more events matching the query. Incomplete pages can be detected by a non-empty nextPageToken field in the response. By default the value is 250 events. The page size can never be larger than 2500 events. Optional.
			public int? MaxResults { get; set; }
			/// The order of the events returned in the result. Optional. The default is an unspecified, stable order. 
			public EventsResource.ListRequest.OrderByEnum? OrderBy { get; set; }
			/// Utility property to set OrderBy.
			public string SetOrderBy
			{
				get
				{
					return null;
				}
				set
				{
					if (value == "StartTime")
					{
						OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;
						SingleEvents = true;
					}
					if (value == "Updated")
					{
						OrderBy = EventsResource.ListRequest.OrderByEnum.Updated;
					}
				}

			}
			/// Token specifying which result page to return. Optional.
			public string PageToken { get; set; }
			/// Extended properties constraint specified as propertyName=value. Matches only private properties. This parameter might be repeated multiple times to return events that match all given constraints.
			public string PrivateExtendedProperty { get; set; }
			/// Free text search terms to find events that match these terms in any field, except for extended properties. Optional.
			public string Q { get; set; }
			/// Extended properties constraint specified as propertyName=value. Matches only shared properties. This parameter might be repeated multiple times to return events that match all given constraints.
			public string SharedExtendedProperty { get; set; }
			/// Whether to include deleted events (with status equals "cancelled") in the result. Cancelled instances of recurring events (but not the underlying recurring event) will still be included if showDeleted and singleEvents are both False. If showDeleted and singleEvents are both True, only single instances of deleted events (but not the underlying recurring events) are returned. Optional. The default is False.
			public bool? ShowDeleted { get; set; }
			/// Whether to include hidden invitations in the result. Optional. The default is False.
			public bool? ShowHiddenInvitations { get; set; }
			/// Whether to expand recurring events into instances and only return single one-off events and instances of recurring events, but not the underlying recurring events themselves. Optional. The default is False.
			public bool? SingleEvents { get; set; }
			/// Token obtained from the nextSyncToken field returned on the last page of results from the previous list request. It makes the result of this list request contain only entries that have changed since then. All events deleted since the previous list request will always be in the result set and it is not allowed to set showDeleted to False.There are several query parameters that cannot be specified together with nextSyncToken to ensure consistency of the client state.These are: - iCalUID - orderBy - privateExtendedProperty - q - sharedExtendedProperty - timeMin - timeMax - updatedMin If the syncToken expires, the server will respond with a 410 GONE response code and the client should clear its storage and perform a full synchronization without any syncToken.Learn more about incremental synchronization.Optional. The default is to return all entries.
			public string SyncToken { get; set; }
			/// Upper bound (exclusive) for an event's start time to filter by. Optional. The default is not to filter by start time. Must be an RFC3339 timestamp with mandatory time zone offset, e.g., 2011-06-03T10:00:00-07:00, 2011-06-03T10:00:00Z. Milliseconds may be provided but will be ignored. If timeMin is set, timeMax must be greater than timeMin.
			public DateTime? TimeMax { get; set; }
			/// Lower bound (inclusive) for an event's end time to filter by. Optional. The default is not to filter by end time. Must be an RFC3339 timestamp with mandatory time zone offset, e.g., 2011-06-03T10:00:00-07:00, 2011-06-03T10:00:00Z. Milliseconds may be provided but will be ignored. If timeMax is set, timeMin must be smaller than timeMax.
			public DateTime? TimeMin { get; set; }
			/// Time zone used in the response. Optional. The default is the time zone of the calendar.
			public string TimeZone { get; set; }
			/// Lower bound for an event's last modification time (as a RFC3339 timestamp) to filter by. When specified, entries deleted since this time will always be included regardless of showDeleted. Optional. The default is not to filter by last modification time.
			public string UpdatedMin { get; set; }
		}

		public Events GetEventList()
		{
			EventsListOptionalParms optional = new EventsListOptionalParms{
				// set common options
				SingleEvents = true
			};

			return GetEventList(optional);
		}

		public Events GetEventList(EventsListOptionalParms optional)
		{
			string serv = "service";
			
			try
			{
				// Initial validation.
				if (CalService == null)
					throw new ArgumentNullException(serv);
				if (CalendarID == null)
					throw new ArgumentNullException(CalendarID);

				// Building the initial request.
				var request = CalService.Events.List(CalendarID);

				// Applying optional parameters to the request.                
				request = (EventsResource.ListRequest)ObjectHelpers.ApplyOptionalParms(request, optional);

				// Requesting data.
				return request.Execute();
			}
			catch (Exception ex)
			{
				throw new Exception("Request Events.List failed.", ex);
			}
		}

		public String SeminarEventList(EventsListOptionalParms optional = null)
		{

			string output = String.Empty;
			//CultureInfo enUS = new CultureInfo("en-US");
			int lastMonth = 0;
			int lastYear = 0;

			// set common options
			if (optional == null)
			{
				optional.SingleEvents = true;
			}

			// Retrieve events
			Events events = GetEventList(optional);

			// List events.
			if (events.Items != null && events.Items.Count > 0)
			{

				foreach ( Event eventItem in events.Items)
				{

					bool isAllDay = false;

					DateTime? startTime = eventItem.Start.DateTime;
					if(startTime == null)
					{
						startTime = DateTime.Parse(eventItem.Start.Date);
						isAllDay = true;
					}

					// format time block
					string timeBlock = String.Empty;

					if(isAllDay)
					{
						timeBlock = "All Day";
					} else {
						timeBlock = ((DateTime)startTime).ToString("h:mm tt");

						if(eventItem.EndTimeUnspecified != true)
						{
							timeBlock += " - " + ((DateTime)eventItem.End.DateTime).ToString("h:mm tt");
						}
					}

					int thisYear = ((DateTime)startTime).Year;
					int thisMonth = ((DateTime)startTime).Month;

					// check for month / year headers
					if(thisMonth != lastMonth)
					{
						string colspan = (thisYear > lastYear) ? "2" : "4";
						string yearText = "";

						if(thisYear > lastYear)
						{
							yearText = "<th colspan=\"2\">" + ((DateTime)startTime).ToString("yyyy") + "</th>\n";
							lastYear = thisYear;
						}
							
						output += "<tr class=\"month-header\">\n";
						output += "<th colspan=\"" + colspan + "\">" + ((DateTime)startTime).ToString("MMMM").ToUpper() + "</th>\n" + yearText;
						output += "</tr>\n";

						lastMonth = thisMonth;
					}

					string[] extDataKeys = {
						"name", "link", "organization", "department"
					};

					Dictionary<string, string> extInfo = new Dictionary<string, string>();

					// get additional info from notes/description field
					if(!String.IsNullOrEmpty(eventItem.Description))
					{
						// regex patterns
						string brPattern = @"<br\s*/?>";
						string htmlPattern = @"</?[\w-]+((\s+\w+(\s*=\s*(?:"".*?""|'.*?'|[^'"">\s]+))?)+\s*|\s*)/?>";
						string kvPattern = @"(?<=name|link|organization|department)\s*[:=]\s*";

						// handle html formatted content
						eventItem.Description = Regex.Replace(eventItem.Description, brPattern, "\n", RegexOptions.IgnoreCase); // replace <br> with line feeds
						eventItem.Description = Regex.Replace(eventItem.Description, htmlPattern, "", RegexOptions.Singleline); // remove all remaining tags

						// split entries by line or delimiter
						string[] dParts = eventItem.Description.Split(
							new[] { "\r\n", "\r", "\n", "|" },
							StringSplitOptions.RemoveEmptyEntries
						);

						// add to dictionary by key/value splits
						foreach (var dPart in dParts)
						{
							string[] itemArr = Regex.Split(dPart, kvPattern, RegexOptions.IgnoreCase);
							if (itemArr.Length > 1)
							{
								extInfo.Add(itemArr[0].ToLower().Trim(), itemArr[1].Trim());
							}
						}
					} 

					// format additional info
					// name, organization, department, link
					string addInfo = String.Empty;
					string name = (extInfo.ContainsKey("name") && !String.IsNullOrEmpty(extInfo["name"])) ? extInfo["name"] : "";
					addInfo += "<td>";
					addInfo += (extInfo.ContainsKey("link") && !String.IsNullOrEmpty(extInfo["link"])) 
						? "<a href=\"" +  Regex.Replace(extInfo["link"], @"<[^>]+>|&nbsp;", "").Trim() +  "\" target=\"_blank\">" + name + "</a><br/>"
						: name + "<br/>";
					addInfo += (extInfo.ContainsKey("organization") && !String.IsNullOrEmpty(extInfo["organization"])) ? extInfo["organization"] + "<br/>" : "";
					addInfo += (extInfo.ContainsKey("department") && !String.IsNullOrEmpty(extInfo["department"])) ? extInfo["department"] : "";
					addInfo += "</td>\n";

					// format event listing
					output += "<tr>\n";
					output += "<td>" + ((DateTime)startTime).ToString("MMMM d, yyyy") + "<br/>(" + ((DateTime)startTime).ToString("dddd") + ")</td>\n";
					output += addInfo;
					output += "<td>&ldquo;" + eventItem.Summary + "&rdquo;<br/>";

					if (eventItem.Attachments != null && eventItem.Attachments.Count > 0)
					{
						foreach (EventAttachment att in eventItem.Attachments)
						{
							output += "<a href=\"" + att.FileUrl + "\" target=\"_blank\" aria-label=\"Read abstract about " + eventItem.Summary + "\">Read Abstract</a><br/>";
						}
					}
					output += "</td>\n";

					output += "<td>" + timeBlock + "<br/>" + eventItem.Location + "</td>\n";
					output += "</tr>\n";
				}
			}
			else
			{
				output += "<tr><td colspan=\"4\">No events found.</td></tr>";
			}

			return (output);
		}

		public void ExtendedProperties()
		{
			//// Future Implementation
			//// Read any current extended properties

			//// Parse the notes/description field

			//Dictionary<string, string> extInfo = new Dictionary<string, string>();
			//// get additional info from notes/description field
			//if (!String.IsNullOrEmpty(eventItem.Description))
			//{
			//	string[] dParts = eventItem.Description.Split('|');
			//	foreach (var dPart in dParts)
			//	{
			//		string[] itemArr = dPart.Split('=');
			//		if (itemArr.Length > 1)
			//		{
			//			extInfo.Add(itemArr[0].ToLower(), itemArr[1]);
			//		}
			//	}
			//}

			//// Add/Update/Delete extended properties

			//var EP = new Event.ExtendedPropertiesData();
		
			//EP.Shared = new Dictionary<String, String>();
			//EP.Shared.Add("MyKey", "MyValue");
			//myevent.ExtendedProperties = EP;

			//Event.ExtendedPropertiesData exp = new Event.ExtendedPropertiesData();
			//exp.Shared = new Dictionary<string, string>();
			//exp.Shared.Add(ExpKey, ExpVal);
			//ev.ExtendedProperties = exp;
		}
	}
}