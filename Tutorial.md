---
title: Bind a Service to Your App
owner: Tobias Fuhrimann
modified: Ivan Baldinotti
---

# Bind a service to your app

The <a href="../service-offerings/index.html" target="_blank">service marketplace</a> has a large number of data stores, from Redis and MongoDB, to MariaDB (fork of MySQL) and RabbitMQ. You can run `cf marketplace` to get an overview. In this step you will add a MariaDB enterprise database to your app.

In this tutorial we'll create a REST API as an MVC app to create and store kittens.

First, create the database:

<pre class="terminal">
$ cf create-service mariadbent usage dotnet-test
Creating service instance dotnet-test in org MyOrg / space MySpace as user@mydomain.com...
OK

Create in progress. Use 'cf services' or 'cf service dotnet-test' to check operation status.

Attention: The plan `usage` of service `mariadbent` is not free. The instance `dotnet-test` will incur a cost. Contact your administrator if you think this is in error.
</pre>

This creates a enterprise MariaDB database for you which we now have to bind to our application. Binding means that the credentials and URL of the service will be written dynamically into the environment variables of the app as `VCAP_SERVICES` and can hence be used directly from there.

Let's bind the new service to our existing application:

<pre class="terminal">
$ cf bind-service my-dotnetcore-app dotnet-test
Binding service dotnet-test to app my-dotnetcore-app in org MyOrg / space MySpace as user@mydomain.com...
OK
TIP: Use 'cf restage my-dotnetcore-app' to ensure your env variable changes take effect
</pre>

<p class="note">
  <strong>Note</strong>: If you are getting <code>Server error, status code: 409</code>, please try again after a couple of minutes.
</p>

After that we restage the application as suggested so that it includes the new credentials in its environment variables:

<pre class="terminal">
$ cf restage my-dotnetcore-app
Restaging app my-app in org MyOrg / space MySpace as user@mydomain.com...
Creating container
Successfully created container
Downloading app package...

...
</pre>

Now we want to consume our new MAriaDB from within our application. Add the respective driver to the `dependencies` using the command: 

<pre class="terminal">
$ dotnet add package MySqlConnector
</pre>

Also, we'll use ASP.NET Core MVC to create our API so we'll need to add that as well:

<pre class="terminal">
$ dotnet add package Microsoft.AspNetCore.Mvc
</pre>


Now add a `Models` folder and inside it, add a new class file of name `Kitten.cs` with the following code:

```c#
namespace CfSampleAppDotNetCore.Models
{
    public class Kitten
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
````

The class contains an Id property of the type Id. 

Next, we'll create a repository class and -interface to access our kittens. This is where all the access to MariaDB happens. First, let's create the class. Create a file `KittenRepository.cs` in the `Models` folder with the following content:

```c#
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace CfSampleAppDotNetCore.Models
{
    public class KittenRepository : IKittenRepository
    {
        public string ConnectionString;
        public MySqlConnection Connection;
        public KittenRepository()
        {
            if (Environment.GetEnvironmentVariable("VCAP_SERVICES") != null)
            {
                var vcapServices = JsonConvert.DeserializeObject<VcapServices>(Environment.GetEnvironmentVariable("VCAP_SERVICES"));
                ConnectionString = "server="+vcapServices.mariadbent[0].credentials.host
                                                         +";user="+vcapServices.mariadbent[0].credentials.username
                                                         +";database="+vcapServices.mariadbent[0].credentials.database
                                                         +";port="+vcapServices.mariadbent[0].credentials.port
                                                         +";password="+vcapServices.mariadbent[0].credentials.password;

            }
            else
            {
                Console.WriteLine("Using the local Mariadb");
                ConnectionString = "server=localhost;user=root;database=mysql;port=32769;password=test";;
            }
        }


        public List<string> Find()
        {
            List<String> columnData = new List<String>();
            Connection = new MySqlConnection(ConnectionString);
            try
            {
                Console.WriteLine("Connecting to MySQL...");
                Connection.Open();
                string sql = "SELECT Name FROM Kittens;";
                MySqlCommand cmd = new MySqlCommand(sql, Connection);
                MySql.Data.MySqlClient.MySqlDataReader reader = cmd.ExecuteReader();
                if (reader != null)
                {
                    while (reader.Read())
                    {
                      columnData.Add(reader.GetString(0));
                     }
                    Connection.Close();
                    Console.WriteLine("Done.");
                    return columnData; 

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            Connection.Close();
            Console.WriteLine("Done.");
            return columnData;
        }

        public Kitten Create(Kitten kitten)
        {
            Connection = new MySqlConnection(ConnectionString);
            try
            {
                Console.WriteLine("Connecting to MySQL...");
                Console.WriteLine(ConnectionString);
                Connection.Open();
                string sql = "INSERT INTO Kittens (Name) VALUES ('" + kitten.Name + "');";
                Console.WriteLine(sql);
                MySqlCommand cmd = new MySqlCommand(sql, Connection);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            Connection.Close();
            Console.WriteLine("Done.");
            return kitten;
        }
    }
}
```

The line

```c#
if (Environment.GetEnvironmentVariable("VCAP_SERVICES") != null)
```

checks if the app is running in the cloud. If not, it falls back to the default local MariadbDB connection string. This allows you to run your app locally as well as in the cloud without having to configure anything differently. To do so, it requires the `VcapServices` class, which we need to create. So let's create another file `VcapServices.cs` in the `Models` folder with the following content:

```c#
namespace CfSampleAppDotNetCore.Models
{
    public class VcapServices
    {
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
```

This class represents the JSON structure in which we get the credentials to our database in the environment variables of our app.

Now, let's create the interface to our new repository class. Create a file called `IKittenRepository.cs` in `Models` with the following content:

```c#
using System.Collections.Generic;

namespace CfSampleAppDotNetCore.Models
{
    public interface IKittenRepository
    {
        Kitten Create(Kitten kitten);
        List<string> Find();
    }
}
```

We still need a controller for the framework to interact with our new repository. Create a `Controllers` folder and inside a `KittenController.cs` file with the following content:

```c#
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using CfSampleAppDotNetCore.Models;

namespace CfSampleAppDotNetCore.Controllers
{
    [Route("[controller]")]
    public class KittenController : Controller
    {
        public KittenController(IKittenRepository kittens)
        {
            Kittens = kittens;
        }
        public IKittenRepository Kittens { get; set; }

        [HttpGet]
        public List<string>  Find()
        {
            return Kittens.Find();
        }

        [HttpPost]
        public IActionResult Create([FromBody] Kitten kitten)
        {
            if (kitten == null)
            {
                return BadRequest();
            }
            Kittens.Create(kitten);
            return StatusCode(201);
        }
    }
}
```

This controller interacts with the repository. We'll just need to hook it up to our `Startup.cs`. We need to change it to use the MVC architecture. Change the simple file we had to the following:

```c#
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using CfSampleAppDotNetCore.Models;

namespace CfSampleAppDotNetCore
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            services.AddSingleton<IKittenRepository, KittenRepository>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseMvc();
        }
    }
}
```

This removes the simple `app.Run` we had before and replaces it with our new kitten service.

We just added the `/kitten` route to our app. It will return all the kittens stored in your database. Currently there are no kittens so it will return an empty array. Sad...

But you can create new kittens by POST-ing to the `/kitten` endpoint with the kitten's name a the payload. You can do so using curl or any similar tool:

<pre class="terminal">
$ curl -X POST "http://localhost:5000/kitten" --header "Content-Type: application/json" -d '{"name":"garfield"}'
</pre>

and then retrieve your new kitten at the `/kitten` endpoint.

Now that the app is ready, let's push it to the cloud using

<pre class="terminal">
$ cf push my-dotnetcore-app
</pre>

You can access other services like Redis or MariaDB in a similar matter, simply by binding them to your app and accessing them through the environment variables.

<div style="text-align:center;padding:3em;">
  <a href="./manifest.html" class="btn btn-primary">I've bound a service to my App</a>
</div>
