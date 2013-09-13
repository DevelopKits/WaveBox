using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Ninject;
using WaveBox.Core;
using WaveBox.Core.ApiResponse;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model;
using WaveBox.Core.Model.Repository;
using WaveBox.Core.Static;
using WaveBox.Service.Services.Http;
using WaveBox.Static;

namespace WaveBox.ApiHandler.Handlers
{
	public class UsersApiHandler : IApiHandler
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public string Name { get { return "users"; } }

		/// <summary>
		/// Process allows the modification of users and their properties
		/// </summary>
		public void Process(UriWrapper uri, IHttpProcessor processor, User user)
		{
			// Return list of users to be passed as JSON
			IList<User> listOfUsers = new List<User>();

			// See if we need to make a test user
			if (uri.Parameters.ContainsKey("testUser") && uri.Parameters["testUser"].IsTrue())
			{
				bool success = false;
				int durationSeconds = 0;
				if (uri.Parameters.ContainsKey("durationSeconds"))
				{
					success = Int32.TryParse(uri.Parameters["durationSeconds"], out durationSeconds);
				}

				// Create a test user and reply with the account info
				User testUser = Injection.Kernel.Get<IUserRepository>().CreateTestUser(success ? (int?)durationSeconds : null);
				if (testUser == null)
				{
					processor.WriteJson(new UsersResponse("Couldn't create user", listOfUsers));
					return;
				}

				listOfUsers.Add(testUser);
				processor.WriteJson(new UsersResponse(null, listOfUsers));
				return;
			}

			// Check for no action, or read action
			if (uri.Action == null || uri.Action == "read")
			{
				// On valid key, return a specific user, and their attributes
				if (uri.Id != null)
				{
					User oneUser = Injection.Kernel.Get<IUserRepository>().UserForId((int)uri.Id);
					listOfUsers.Add(oneUser);
				}
				else
				{
					// On invalid key, return all users
					listOfUsers = Injection.Kernel.Get<IUserRepository>().AllUsers();
				}

				processor.WriteJson(new UsersResponse(null, listOfUsers));
				return;
			}

			// Perform actions
			// killSession - remove a session by rowId
			if (uri.Action == "killSession")
			{
				// Try to pull rowId from parameters for session management
				int rowId = 0;
				if (!uri.Parameters.ContainsKey("rowId"))
				{
					processor.WriteJson(new UsersResponse("Missing parameter 'rowId' for action 'killSession'", null));
					return;
				}

				// Try to parse rowId integer
				if (!Int32.TryParse(uri.Parameters["rowId"], out rowId))
				{
					processor.WriteJson(new UsersResponse("Invalid integer for 'rowId' for action 'killSession'", null));
					return;
				}

				// After all checks pass, delete this session, return user associated with it
				var session = Injection.Kernel.Get<ISessionRepository>().SessionForRowId(rowId);
				if (session != null)
				{
					session.DeleteSession();
					listOfUsers.Add(Injection.Kernel.Get<IUserRepository>().UserForId(Convert.ToInt32(session.UserId)));
				}

				processor.WriteJson(new UsersResponse(null, listOfUsers));
				return;
			}

			// create - create a new user
			if (uri.Action == "create")
			{
				// Check for required username and password parameters
				if (!uri.Parameters.ContainsKey("username") || !uri.Parameters.ContainsKey("password"))
				{
					processor.WriteJson(new UsersResponse("Parameters 'username' and 'password' required for action 'create'", null));
					return;
				}

				string username = uri.Parameters["username"];
				string password = uri.Parameters["password"];

				// Attempt to create the user
				User newUser = Injection.Kernel.Get<IUserRepository>().CreateUser(username, password, null);
				if (newUser == null)
				{
					processor.WriteJson(new UsersResponse("Action 'create' failed to create new user", null));
					return;
				}

				// Verify user didn't already exist
				if (newUser.UserId == null)
				{
					processor.WriteJson(new UsersResponse("User '" + username + "' already exists!", null));
					return;
				}

				// Return newly created user
				logger.IfInfo(String.Format("Successfully created new user [id: {0}, username: {1}]", newUser.UserId, newUser.UserName));
				listOfUsers.Add(newUser);

				processor.WriteJson(new UsersResponse(null, listOfUsers));
				return;
			}

			// Verify ID present
			if (uri.Id == null)
			{
				processor.WriteJson(new UsersResponse("Missing parameter ID", null));
				return;
			}

			// delete - remove a user
			if (uri.Action == "delete")
			{
				// Attempt to fetch and delete user
				User deleteUser = Injection.Kernel.Get<IUserRepository>().UserForId((int)uri.Id);
				if (deleteUser.UserName == null || !deleteUser.Delete())
				{
					processor.WriteJson(new UsersResponse("Action 'delete' failed to delete user", null));
					return;
				}

				// Return deleted user
				logger.IfInfo(String.Format("Successfully deleted user [id: {0}, username: {1}]", deleteUser.UserId, deleteUser.UserName));
				listOfUsers.Add(deleteUser);
			}

			// Invalid action
			processor.WriteJson(new UsersResponse("Invalid action specified", null));
			return;
		}
	}
}
