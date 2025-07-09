using System.Text.Json;
using System.Text.Json.Serialization;

namespace website.Users
{
	public class User
	{
		public User(string username, string password, bool deleted, DateTime createdAt, int id)
		{
			Username = username;
			Password = password;
			Id = id;
			Deleted = deleted;
			CreatedAt = createdAt;
		}

		[JsonPropertyName("u")]
		public string Username { get; set; }

		[JsonPropertyName("p")]
		public string Password { get; set; }

		public int Id { get; set; }

		[JsonPropertyName("d")]
		public bool Deleted { get; set; } = false;

		[JsonPropertyName("c")]
		public DateTime CreatedAt { get; set; }

		[JsonPropertyName("m")]
		public DateTime UpdatedAt { get; set; }
	}

	public class UserStore
	{
		private Dictionary<string, User> _users = new(StringComparer.OrdinalIgnoreCase);
		private readonly IConfiguration _configuration;
		private readonly ILogger<UserStore> _logger;

		public bool IsEmpty => _users.Count == 0;

		public UserStore(IConfiguration configuration, ILogger<UserStore> logger)
		{
			_configuration = configuration;
			_logger = logger;

			LoadUsers();
		}

		public User? TryLogin(string username, string password)
		{
			if (_users.TryGetValue(username, out var user) && password == user.Password)
			{
				return user;
			}
			return null;
		}

		public void LoadUsers()
		{
			var configRoot = _configuration["ConfigRoot"];
			if (string.IsNullOrEmpty(configRoot))
			{
				throw new InvalidOperationException("ConfigRoot is not configured.");
			}

			var usersFilePath = Path.Combine(configRoot, "users.json");
			if (File.Exists(usersFilePath))
			{
				var json = File.ReadAllText(usersFilePath);
				var users = System.Text.Json.JsonSerializer.Deserialize<List<User>>(json) ?? new List<User>();
				_users = users.ToDictionary(u => u.Username, u => u, StringComparer.OrdinalIgnoreCase);
			}
			else
			{
				_logger?.LogWarning($"Users file not found at {usersFilePath}. Initializing with an empty user store.");
			}
		}

		public void AddUser(string username, string password)
		{
			if (_users.ContainsKey(username))
			{
				throw new InvalidOperationException($"User '{username}' already exists.");
			}

			var newUser = new User(username, password, false, DateTime.UtcNow, 9999);
			_users[username] = newUser;
			SaveUsers();
		}

		public void RemoveUser(string username)
		{
			if (_users.Remove(username))
			{
				SaveUsers();
			}
		}

		public List<User> ListUsers()
		{
			return _users.Values.ToList();
		}

		void SaveUsers()
		{
			var configRoot = _configuration["ConfigRoot"];
			if (string.IsNullOrEmpty(configRoot))
			{
				throw new InvalidOperationException("ConfigRoot is not configured.");
			}
			var usersFilePath = Path.Combine(configRoot, "users.json");
			var json = JsonSerializer.Serialize(_users.Values.ToList(), new JsonSerializerOptions
			{
				DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
			});
			File.WriteAllText(usersFilePath, json);
		}
	}
}