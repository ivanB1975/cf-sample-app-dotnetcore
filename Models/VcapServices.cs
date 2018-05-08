namespace CfSampleAppDotNetCore.Models
{
    public class VcapServices
    {
        public MongoDB[] mongodb { get; set; }
        public class MongoDB
        {
            public Credentials credentials { get; set; }

            public class Credentials
            {
                public string uri  { get; set; }
                public string database  { get; set; }
            }
        }


        public MariaDB[] mariadbent { get; set; }
        public class MariaDB
        {
            public Credentials credentials { get; set; }

            public class Credentials
            {
                public string database { get; set; }
                public string host { get; set; }
                public string username { get; set; }
                public string password { get; set; }
                public string port { get; set; }
            }
        }
    }
}
