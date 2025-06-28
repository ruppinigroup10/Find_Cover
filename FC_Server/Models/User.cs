namespace FC_Server.Models
{
    public class User
    {
        private int user_id;
        private string username;
        private string password_hash;
        private string email;
        private string phone_number;
        private DateTime created_at;
        private bool is_active;
        private bool is_provider;
        private DateTime? birthday;


        public int UserId { get => user_id; set => user_id = value; }
        public string Username { get => username; set => username = value; }
        public string PasswordHash { get => password_hash; set => password_hash = value; }
        public string Email { get => email; set => email = value; }
        public string PhoneNumber { get => phone_number; set => phone_number = value; }
        public DateTime CreatedAt { get => created_at; set => created_at = value; }
        public bool IsActive { get => is_active; set => is_active = value; }
        public bool IsProvider { get => is_provider; set => is_provider = value; }
        public DateTime? Birthday { get => birthday; set => birthday = value; }

        // Constructor without parameters
        public User()
        {
            this.user_id = 0;
            this.username = "";
            this.password_hash = "";
            this.email = "";
            this.phone_number = "";
            this.created_at = DateTime.Now;
            this.is_active = false;
            this.is_provider = false;
            this.birthday = null;
        }

        // Constructor with parameters
        public User(int userId, string username, string passwordHash, string email, string phoneNumber, bool isActive, bool isProvider)
        {
            this.user_id = userId;
            this.username = username;
            this.password_hash = passwordHash;
            this.email = email;
            this.phone_number = phoneNumber;
            this.created_at = DateTime.Now;
            this.is_active = isActive;
            this.is_provider = isProvider;
        }

        public User(int userId, string username, string passwordHash, string email, string phoneNumber, bool isActive, bool isProvider, DateTime birthday)
        {
            this.user_id = userId;
            this.username = username;
            this.password_hash = passwordHash;
            this.email = email;
            this.phone_number = phoneNumber;
            this.created_at = DateTime.Now;
            this.is_active = isActive;
            this.is_provider = isProvider;
            this.birthday = birthday;
        }

        public static User? Register(string username, string password_hash, string email, string phone_number, DateTime birthday)
        {
            DBservices dbs = new DBservices();
            return dbs.RegisterUser(username, password_hash, email, phone_number, birthday);
        }

        public static User? UpdateUser(int user_id, string username, string password_hash, string email, string phone_number)
        {
            DBservices dbs = new DBservices();
            return dbs.UpdateUser(user_id, username, password_hash, email, phone_number);
        }

        public static User? Login(string email, string password_hash)
        {
            DBservices dbs = new DBservices();
            return dbs.LoginUser(email, password_hash);
        }

        public static User? getUser(int user_id)
        {
            DBservices dbs = new DBservices();
            return dbs.getUser(user_id);
        }
    }
}
