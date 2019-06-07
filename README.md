
Microsoft IIS Administration API
--------------------------------

[![Build status](https://ci.appveyor.com/api/projects/status/l62ov4c6fbdi6vrq/branch/dev?svg=true)](https://ci.appveyor.com/project/jimmyca15/iis-administration-ed6b3/branch/dev)

To find the latest news for the IIS Administration api visit the blog at https://blogs.iis.net/adminapi.

Documentation is available at https://docs.microsoft.com/en-us/IIS-Administration 

### Installation: ###
* Supports 64 bit Windows Server 2008 R2 and above

The latest installer can be obtained from https://manage.iis.net/get. The installer will automatically download and install all dependencies.

### Nano Server Installation: ###
There is a blog post to get up and running on Nano Server located at https://blogs.iis.net/adminapi/microsoft-iis-administration-on-nano-server.

### Running Tests: ###
* Run the ConfigureDevEnvironment script with the test environment flag
`
C:\src\repos\IIS.Administration\scripts\Configure-DevEnvironment.ps1 -ConfigureTestEnvironment
`
* Open the project in Visual Studio as an Administrator and launch without debugging
* Open another instance of the project and run the tests located in the 'test' folder
* Tests can also be run with the CLI

### Publish and Install: ###
Publishing and installing can be done through a PowerShell script. This requires the .NET Core SDK and Bower.

```
# Replace the path to match your clone location
C:\src\repos\IIS.Administration\scripts\publish\publish.ps1
C:\src\repos\IIS.Administration\scripts\publish\bin\setup\setup.ps1 Install -Verbose
```

### Develop and Debug in Visual studio 2017: ###
* Clone this project
* Load the project in visual studio
* Try restoring all the NuGet packages
* Run PowerShell as an Administrator
* Run Configure-DevEnvironment.ps1 script in the scripts dir
* From the visual studio run profile menu select option Microsoft.IIS.Administration and run the application.
* If you are not able to browse the site or your getting generic browser error, most like SSL certificate is not configured for that. IIS   express installs SSL certificates on   port 44300-44399. Try changing the port to one of these in appsettings.json
  **ex: "urls":"https://*:44326"**

### Using the new API ###
1. Navigate to https://manage.iis.net
2. Click 'Get Access Token'
3. Generate an access token and copy it to the clipboard
4. Exit the access tokens window and return to the connection screen
5. Paste the access token into the Access Token field of the connection screen
6. Click 'Connect'

## Examples ##

### C# ### 

#### Intialize Api Client

```
var apiClient = new HttpClient(new HttpClientHandler() {
   UseDefaultCredentials = true
}, true);

// Set access token for every request
apiClient.DefaultRequestHeaders.Add("Access-Token", "Bearer {token}");

// Request HAL (_links)
apiClient.DefaultRequestHeaders.Add("Accept", "application/hal+json");
```

#### Get Web Sites ####
```
var res = await apiClient.GetAsync("https://localhost:55539/api/webserver/websites");

if (res.StatusCode != HttpStatusCode.OK) {
  HandleError(res);
  return;
}

JArray sites = JObject.Parse(res.Content.ReadAsStringAsync().Result).Value<JArray>("websites");
```

#### Create a Web Site ####
```

var newSite = new {
  name = "Contoso",
  physical_path = @"C:\inetpub\wwwroot",
  bindings = new object[] {
    new {
      port = 8080,
      protocol = "http",
      ip_address = "*"
    }
  }
};

res = await apiClient.PostAsync("https://localhost:55539/api/webserver/websites", 
    new StringContent(JsonConvert.SerializeObject(newSite), Encoding.UTF8, "application/json"));

if (res.StatusCode != HttpStatusCode.Created) {
    HandleError(res);
    return;
}

JObject site = JObject.Parse(res.Content.ReadAsStringAsync().Result);
```

#### Update a Web Site ####
```

var updateObject = new {
  bindings = new object[] {
    new {
      port = 8081,
      protocol = "http",
      ip_address = "*"
    }
  }
};

var updateRequest = new HttpRequestMessage(new HttpMethod("PATCH"),
    "https://localhost:55539" + site["_links"]["self"].Value<string>("href"));

updateRequest.Content = new StringContent(JsonConvert.SerializeObject(updateObject), Encoding.UTF8, "application/json");

res = await apiClient.SendAsync(updateRequest);

if (res.StatusCode != HttpStatusCode.OK) {
    HandleError(res);
    return;
}

site = JObject.Parse(res.Content.ReadAsStringAsync().Result);
```

#### Delete a Web Site ####
```
res = await apiClient.DeleteAsync("https://localhost:55539" + site["_links"]["self"].Value<string>("href"));
```

### PowerShell ###

There is a [utils.ps1](./scripts/utils/utils.ps1) script that demonstrates how to generate an access token from PowerShell.

```
# Replace the path to match your clone location
$accessToken = C:\src\repos\IIS.Administration\scripts\utils\utils.ps1 Generate-AccessToken -url "https://localhost:55539"
```

#### Get Web Sites ####

````
# Supply an access token to run the example

$accessToken = "{Some Access token}"

$headers = @{ "Access-Token" = "Bearer $accessToken"; "Accept" = "application/hal+json" }

$response = Invoke-RestMethod "https://localhost:55539/api/webserver/websites" -UseDefaultCredentials -Headers $headers

$response.websites
````