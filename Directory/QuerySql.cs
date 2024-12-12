using System;

namespace Uaa.Cbpp.Directory
{

	public static class QuerySql
	{

		public static string Basic = @"
			SELECT
					p.id, p.first_name, p.last_name, p.name_prefix, p.name_suffix, p.role, p.phone, p.fax,
					p.email, p.dm_username, p.dm_exclude, p.linkedin, p.website_name, p.website, p.building, p.office, p.office_hours,
					p.cv_url, p.photo_url, p.details, p.courses, p.faculty_staff, p.focus, p.alt_profile_url,
					GROUP_CONCAT( DISTINCT CONCAT_WS('^',d.id,d.name,pd.role) ORDER BY d.id SEPARATOR ';' ) AS depts,
					GROUP_CONCAT( DISTINCT CONCAT_WS('^',c.id,c.name,pc.role) ORDER BY c.id SEPARATOR ';' ) AS committees
				FROM
					person AS p
					LEFT JOIN
						person__department AS pd
							ON p.id = pd.person_id
					LEFT JOIN
						person__committee AS pc
							ON p.id = pc.person_id 
					LEFT JOIN
						department d
							ON pd.department_id = d.id
					LEFT JOIN
						committee AS c
							ON pc.committee_id = c.id";

	}
}
