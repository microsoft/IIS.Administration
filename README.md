Microsoft IIS Administration API
--------------------------------

[![Build status](https://ci.appveyor.com/api/projects/status/l62ov4c6fbdi6vrq/branch/dev?svg=true)](https://ci.appveyor.com/project/jimmyca15/iis-administration-ed6b3/branch/dev)

### Requirements: ###
* IIS installed
* .NET Core installed (https://www.microsoft.com/net/download/core#/runtime)

### Nano Server Installation: ###
There is a blog post to get up and running on Nano Server located at https://blogs.iis.net/adminapi/microsoft-iis-administration-on-nano-server.

### Running Tests: ###
* Open the project in Visual Studio as an Administrator and launch without debugging
* Open another instance of the project and run the tests located in the 'test' folder
* Tests can also be run with the CLI

### Publish and Install: ###
* Run PowerShell as an Administrator
* Run the `Publish.ps1` script located in the scripts directory
* `(SolutionRoot)\scripts\publish\publish.ps1`
* `(SolutionRoot)\scripts\publish\bin\setup\setup.ps1 Install -Verbose`

### Using the new API ###
1. Navigate to https://manage.iis.net
2. Click 'Get Access Token'
3. Generate an access token and copy it to the clipboard
4. Exit the access tokens window and return to the connection screen
5. Paste the access token into the Access Token field of the connection screen
6. Click 'Connect'

## Examples ##

### Intialize Api Client ###
```
var httpClientHandler = new HttpClientHandler() {
    Credentials = new NetworkCredential(userName, password, domain)
};
var apiClient = new HttpClient(httpClientHandler);

// Set access token for every request
apiClient.DefaultRequestHeaders.Add("Access-Token", "Bearer {token}");
```

### Get Web Sites ###
```
var res = apiClient.GetAsync("https://localhost:55539/api/webserver/websites").Result;
if (res.StatusCode != HttpStatusCode.OK) {
  HandleError(res);
  return;
}

JArray sites = JObject.Parse(res.Content.ReadAsStringAsync().Result).Value<JArray>("websites");
```

### Create a Web Site ###
```
var newSite = new {
    name = "Contoso",
    physical_path = @"C:\sites\Contoso",
    bindings = new[] {
        new {
            port = 8080,
            is_https = false,
            ip_address = "*"
        }
    }
};
var res = apiClient.PostAsJsonAsync<object>("https://localhost:55539/api/webserver/websites", newSite).Result;
if (res.StatusCode != HttpStatusCode.Created) {
    HandleError(res);
    return;
}

JObject site = JObject.Parse(res.Content.ReadAsStringAsync().Result);
```
