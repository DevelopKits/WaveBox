using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using WaveBox.Model;

namespace WaveBox.Core.ApiResponse
{
	public class DatabaseResponse
	{
		[JsonProperty("error")]
		public string Error { get; set; }

		[JsonProperty("queries")]
		public IList<QueryLog> Queries { get; set; }

		public DatabaseResponse(string error, IList<QueryLog> queries)
		{
			Error = error;
			Queries = queries;
		}
	}
}

