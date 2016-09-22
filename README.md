Microsoft IIS Administration API
--------------------------------

### Requirements: ###
* IIS installed
* Windows authentication enabled
* Hostable Web Core enabled
* ASP.NET Core Module installed (https://go.microsoft.com/fwlink/?LinkId=817246)

### Running Tests: ###
* Open the project in Visual Studio as an Administrator and launch without debugging
* Open another instance of the project and run the tests located in the 'test' folder
* Tests can also be run with the CLI

### Installing: ###
* Run PowerShell as an Administrator
* Run the Publish.ps1 script located in the scripts directory
* \<OutputDirectory>\setup\setup.ps1 Install -verbose

### Using the new API ###
1. Navigate to https://manage.iis.net?api_url=localhost
2. Click 'Get Access Token'
3. Generate an access token and copy it to the clipboard
4. Exit the access tokens window and return to the connection screen
5. Paste the access token into the Access Token field of the connection screen
6. Click 'Connect'

#### Updating ####
Running the install script will perform an in place update and will preserve user files.

#### Dev Setup ####
1. Open the solution in VS 2015, which will automatically trigger a package restore
2. Build the solution
3. Run the solution (F5) so Visual Studio automatically generates required IIS Express files
4. Using PowerShell, run the **Configure-DevEnvironment.ps1** script in the scripts directory

## Examples ##

### Intialize Api Client ###
```
var apiClient = new HttpClient();
// Set access token for every request
apiClient.DefaultRequestHeaders.Add("Access-Token", "Bearer {token}");
```

### Get Web Sites ###
```
var res = apiClient.GetAsync("https://localhost:55539/api/webserver/websites").Result;
if (res.StatusCode != HttpStatusCode.OK) {
  HandleError(res);
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
}
JObject site = JObject.Parse(res.Content.ReadAsStringAsync().Result);
```
