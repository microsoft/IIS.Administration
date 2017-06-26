// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Tests
{
    using Administration.Files;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Web.Administration;
    using Xunit;

    public class Files
    {
        private const string TEST_FILE_NAME = "TEST_FILE.txt";
        private const string FILE_TEST_SITE_NAME = "File Test Site";
        private const string FILES_PATH = "/api/files";
        private const string WEBSERVER_FILES_PATH = "/api/webserver/files";

        [Fact]
        public void ResolveApplication()
        {
            using (var sm = new ServerManager()) {
                var site = sm.Sites.CreateElement();
                var app = site.Applications.CreateElement();
                app.Path = "/";
                site.Applications.Add(app);
                app = site.Applications.CreateElement();
                app.Path = "/a";
                site.Applications.Add(app);
                app = site.Applications.CreateElement();
                app.Path = "/a/b";
                site.Applications.Add(app);
                app = site.Applications.CreateElement();
                app.Path = "/b";
                site.Applications.Add(app);
                app = site.Applications.CreateElement();
                app.Path = "/ac/b";
                site.Applications.Add(app);

                app = WebServer.Files.FilesHelper.ResolveApplication(site, "/");
                Assert.True(app.Path == "/");

                app = WebServer.Files.FilesHelper.ResolveApplication(site, "/ac");
                Assert.True(app.Path == "/");

                app = WebServer.Files.FilesHelper.ResolveApplication(site, "/a");
                Assert.True(app.Path == "/a");

                app = WebServer.Files.FilesHelper.ResolveApplication(site, "/a/c");
                Assert.True(app.Path == "/a");

                app = WebServer.Files.FilesHelper.ResolveApplication(site, "/a/bc");
                Assert.True(app.Path == "/a");

                app = WebServer.Files.FilesHelper.ResolveApplication(site, "/a/b/c");
                Assert.True(app.Path == "/a/b");
            }
        }

        [Fact]
        public void ResolveVdir()
        {
            using (var sm = new ServerManager()) {
                var site = sm.Sites.CreateElement();
                var app1 = site.Applications.CreateElement();
                app1.Path = "/app1";
                site.Applications.Add(app1);
                var app2 = site.Applications.CreateElement();
                app2.Path = "/app2";
                site.Applications.Add(app2);

                var vdir = app1.VirtualDirectories.CreateElement();
                vdir.Path = "/";
                app1.VirtualDirectories.Add(vdir);
                vdir = app1.VirtualDirectories.CreateElement();
                vdir.Path = "/vdir1";
                app1.VirtualDirectories.Add(vdir);

                vdir = WebServer.Files.FilesHelper.ResolveVdir(site, "/app1/vdir1");
                Assert.True(vdir.Path == "/vdir1");
                vdir = WebServer.Files.FilesHelper.ResolveVdir(site, "/app1/a_folder");
                Assert.True(vdir.Path == "/");
            }
        }

        [Fact]
        public void IsExactVdirPath()
        {
            using (var sm = new ServerManager()) {
                var site = sm.Sites.CreateElement();

                var app1 = site.Applications.CreateElement();
                app1.Path = "/app1";

                var vdir1a = app1.VirtualDirectories.CreateElement();
                vdir1a.Path = "/";
                app1.VirtualDirectories.Add(vdir1a);
                var vdir1b = app1.VirtualDirectories.CreateElement();
                vdir1b.Path = "/vdir1b";
                app1.VirtualDirectories.Add(vdir1b);
                site.Applications.Add(app1);

                var app2 = site.Applications.CreateElement();
                app2.Path = "/app2";
                var vdir2a = app2.VirtualDirectories.CreateElement();
                vdir2a.Path = "/";
                app2.VirtualDirectories.Add(vdir2a);
                var vdir2b = app2.VirtualDirectories.CreateElement();
                vdir2b.Path = "/vdir2b";
                app2.VirtualDirectories.Add(vdir2b);
                site.Applications.Add(app2);

                Assert.True(WebServer.Files.FilesHelper.IsExactVdirPath(site, app1, vdir1a, "/app1/"));
                Assert.True(WebServer.Files.FilesHelper.IsExactVdirPath(site, app1, vdir1a, "/app1"));
                Assert.True(WebServer.Files.FilesHelper.IsExactVdirPath(site, app1, vdir1b, "/app1/vdir1b"));
                Assert.True(WebServer.Files.FilesHelper.IsExactVdirPath(site, app1, vdir1b, "/app1/vdir1b/"));

                Assert.True(WebServer.Files.FilesHelper.IsExactVdirPath(site, app2, vdir2a, "/app2"));
                Assert.True(WebServer.Files.FilesHelper.IsExactVdirPath(site, app2, vdir2a, "/app2/"));
                Assert.True(WebServer.Files.FilesHelper.IsExactVdirPath(site, app2, vdir2b, "/app2/vdir2b"));
                Assert.True(WebServer.Files.FilesHelper.IsExactVdirPath(site, app2, vdir2b, "/app2/vdir2b/"));

                Assert.False(WebServer.Files.FilesHelper.IsExactVdirPath(site, app1, vdir1a, "/app1/folder"));
                Assert.False(WebServer.Files.FilesHelper.IsExactVdirPath(site, app1, vdir1b, "/app1/vdir1b/folder"));
                Assert.False(WebServer.Files.FilesHelper.IsExactVdirPath(site, app1, vdir1b, "/app2/vdir1b"));
            }
        }

        [Fact]
        public void GetPhysicalPath()
        {
            using (var sm = new ServerManager()) {
                var site = sm.Sites.CreateElement();

                var rootApp = site.Applications.CreateElement();
                rootApp.Path = "/";
                var rootVdir = rootApp.VirtualDirectories.CreateElement();
                rootVdir.Path = "/";
                rootApp.VirtualDirectories.Add(rootVdir);
                site.Applications.Add(rootApp);

                var app1 = site.Applications.CreateElement();
                app1.Path = "/app1";
                var vdir1a = app1.VirtualDirectories.CreateElement();
                vdir1a.Path = "/";
                app1.VirtualDirectories.Add(vdir1a);
                var vdir1b = app1.VirtualDirectories.CreateElement();
                vdir1b.Path = "/vdir1b";
                app1.VirtualDirectories.Add(vdir1b);
                site.Applications.Add(app1);

                const string rootVdirPhysicalPath = @"c:\sites\physicalPathSite";
                const string vdir1aPhysicalPath = @"c:\storage\test_site\app1";
                const string vdir1bPhysicalPath = @"c:\storage\test_site\app1\vdir1b";
                rootVdir.PhysicalPath = rootVdirPhysicalPath;
                vdir1a.PhysicalPath = vdir1aPhysicalPath;
                vdir1b.PhysicalPath = vdir1bPhysicalPath;

                Assert.Equal(WebServer.Files.FilesHelper.GetPhysicalPath(site, "/"), rootVdirPhysicalPath);
                Assert.Equal(WebServer.Files.FilesHelper.GetPhysicalPath(site, "/abc/defg"), rootVdirPhysicalPath + @"\abc\defg");
                Assert.Equal(WebServer.Files.FilesHelper.GetPhysicalPath(site, "/app1"), vdir1aPhysicalPath);
                Assert.Equal(WebServer.Files.FilesHelper.GetPhysicalPath(site, "/app1/abc/defg"), vdir1aPhysicalPath + @"\abc\defg");
                Assert.Equal(WebServer.Files.FilesHelper.GetPhysicalPath(site, "/app1/vdir1bc/abc/defg"), vdir1aPhysicalPath + @"\vdir1bc\abc\defg");
                Assert.Equal(WebServer.Files.FilesHelper.GetPhysicalPath(site, "/app1/vdir1b"), vdir1bPhysicalPath);
                Assert.Equal(WebServer.Files.FilesHelper.GetPhysicalPath(site, "/app1/vdir1b/abc/defg"), vdir1bPhysicalPath + @"\abc\defg");
            }
        }

        [Fact]
        public void IsValidFileName()
        {
            var goodFileNames = new string[]
            {
                "abc",
                "abc.def",
                ".abc",
                "a.b.c.-g_r.z"
            };

            var badFileNames = new string[]
            {
                ".",
                "..",
                "/./",
                "/../",
                "../",
                "/..",
                "\\abc",
                "abc\\",
                "/abc",
                "*a",
                "abc/",
                "....",
                "abc.",
            };

            foreach (var name in goodFileNames) {
                Assert.True(PathUtil.IsValidFileName(name));
            }

            foreach (var name in badFileNames) {
                Assert.False(PathUtil.IsValidFileName(name));
            }
        }

        [Fact]
        public void CreateEditDeleteFile()
        {
            using (HttpClient client = ApiHttpClient.Create()) {

                JObject site = Sites.GetSite(client, "Default Web Site");

                // Create web file
                var webFile = CreateWebFile(client, site, TEST_FILE_NAME);

                Assert.True(webFile != null);

                try {
                    //
                    // Get physical file info
                    var fileInfo = Utils.FollowLink(client, webFile.Value<JObject>("file_info"), "self");

                    // Update content of file
                    var testContent = "Microsoft.IIS.Administration.Test.Files";
                    var res = client.PutAsync(Utils.GetLink(fileInfo, "content"), new StringContent(testContent)).Result;

                    Assert.True(res.StatusCode == HttpStatusCode.OK);

                    // Get updated content of file
                    string result = null;
                    Assert.True(client.Get(Utils.GetLink(fileInfo, "content"), out result));
                    Assert.True(result == testContent);

                    var downloadsHref = Utils.GetLink(fileInfo, "downloads");

                    var dl = new {
                        file = fileInfo
                    };

                    // Create download link for file
                    res = client.PostAsync(downloadsHref, new StringContent(JsonConvert.SerializeObject(dl), Encoding.UTF8, "application/json")).Result;
                    Assert.True(res.StatusCode == HttpStatusCode.Created);

                    IEnumerable<string> locationHeader;
                    Assert.True(res.Headers.TryGetValues("location", out locationHeader));
                    var location = locationHeader.First();

                    // Download file
                    Assert.True(client.Get($"{Configuration.TEST_SERVER_URL}{location}", out result));
                    Assert.True(result == testContent);

                    // Update file with empty content
                    res = client.PutAsync(Utils.GetLink(fileInfo, "content"), new ByteArrayContent(new byte[] { })).Result;

                    Assert.True(res.StatusCode == HttpStatusCode.OK);

                    // Assert file truncated
                    res = client.GetAsync(Utils.GetLink(fileInfo, "content")).Result;
                    Assert.True(res.Content.ReadAsByteArrayAsync().Result.Length == 0);
                }
                finally {
                    Assert.True(client.Delete(Utils.Self(webFile.Value<JObject>("file_info"))));
                }
            }
        }

        [Fact]
        public void CopyFile()
        {
            string copyName = "TEST_FILE_NAME_COPY.txt";
            string testContent = "Test content for copying files.";
            JObject copyInfo = null;

            using (HttpClient client = ApiHttpClient.Create()) {

                JObject site = Sites.GetSite(client, "Default Web Site");

                var webFile = CreateWebFile(client, site, TEST_FILE_NAME);

                var physicalPath = Environment.ExpandEnvironmentVariables(webFile["file_info"].Value<string>("physical_path"));
                File.WriteAllText(physicalPath, testContent);

                try {
                    var fileInfo = Utils.FollowLink(client, webFile.Value<JObject>("file_info"), "self");
                    var parent = fileInfo.Value<JObject>("parent");

                    var copy = new
                    {
                        name = copyName,
                        parent = parent,
                        file = fileInfo
                    };

                    copyInfo = client.Post(Utils.GetLink(fileInfo, "copy"), copy);

                    Assert.NotNull(copyInfo);

                    var copyParent = new DirectoryInfo(physicalPath).Parent.FullName;
                    var copyPhysicalPath = Environment.ExpandEnvironmentVariables(copyInfo["file"].Value<string>("physical_path"));

                    Assert.True(copyPhysicalPath.Equals(Path.Combine(copyParent, copyName), StringComparison.OrdinalIgnoreCase));

                    var copyContent = File.ReadAllText(copyPhysicalPath);

                    Assert.Equal(copyContent, testContent);
                }
                finally {
                    if (webFile != null && webFile["file_info"] != null) {
                        Assert.True(client.Delete(Utils.Self(webFile.Value<JObject>("file_info"))));
                    }
                    if (copyInfo != null) {
                        Assert.True(client.Delete(Utils.Self(copyInfo.Value<JObject>("file"))));
                    }
                }
            }
        }

        [Fact]
        public void MoveDirectory()
        {
            string startName = "move_dir_test";
            string destName = "move_dir_dest";
            var physicalPath = Path.Combine(Configuration.TEST_ROOT_PATH, startName);
            var destPhysicalPath = Path.Combine(Configuration.TEST_ROOT_PATH, destName);

            CreateTestDirectory(physicalPath);

            JObject site = null;
            using (HttpClient client = ApiHttpClient.Create()) {
                Sites.EnsureNoSite(client, FILE_TEST_SITE_NAME);
                site = Sites.CreateSite(client, FILE_TEST_SITE_NAME, Utils.GetAvailablePort(), physicalPath);

                try {
                    var rootDir = Utils.FollowLink(client, site, "files");
                    var rootDirFileInfo = Utils.FollowLink(client, rootDir.Value<JObject>("file_info"), "self");

                    object move = new {
                        name = destName,
                        parent = rootDirFileInfo.Value<JObject>("parent"),
                        file = rootDirFileInfo
                    };

                    var moveInfo = client.Post(Utils.GetLink(rootDirFileInfo, "move"), move);

                    // Wait for move to finish
                    HttpResponseMessage res = null;
                    while (res == null || res.StatusCode == HttpStatusCode.OK) {
                        res = client.GetAsync(Utils.Self(moveInfo)).Result;
                        Thread.Sleep(25);
                    }

                    Assert.True(!Directory.Exists(physicalPath));
                    Assert.True(VerifyTestDirectory(destPhysicalPath));
                }
                finally {
                    if (site != null) {
                        Sites.DeleteSite(client, Utils.Self(site));
                    }

                    if (Directory.Exists(destPhysicalPath)) {
                        Directory.Delete(destPhysicalPath, true);
                    }
                }
            }
        }

        [Fact]
        public void CopyDirectory()
        {
            string startName = "copy_dir_test";
            string destName = "copy_dir_dest";
            var physicalPath = Path.Combine(Configuration.TEST_ROOT_PATH, startName);
            var destPhysicalPath = Path.Combine(Configuration.TEST_ROOT_PATH, destName);

            CreateTestDirectory(physicalPath);

            JObject site = null;
            using (HttpClient client = ApiHttpClient.Create()) {
                Sites.EnsureNoSite(client, FILE_TEST_SITE_NAME);
                site = Sites.CreateSite(client, FILE_TEST_SITE_NAME, Utils.GetAvailablePort(), physicalPath);

                try {
                    var rootDir = Utils.FollowLink(client, site, "files");
                    var rootDirFileInfo = Utils.FollowLink(client, rootDir.Value<JObject>("file_info"), "self");

                    object copy = new {
                        name = destName,
                        parent = rootDirFileInfo.Value<JObject>("parent"),
                        file = rootDirFileInfo
                    };

                    var copyInfo = client.Post(Utils.GetLink(rootDirFileInfo, "copy"), copy);

                    //
                    // Wait for copy to finish
                    HttpResponseMessage res = null;
                    do {
                        res = client.GetAsync(Utils.Self(copyInfo)).Result;
                    } while (res.StatusCode == HttpStatusCode.OK);

                    // Don't add code between copy end and verification so we can make sure files aren't being held
                    Assert.True(VerifyTestDirectory(physicalPath));
                    Assert.True(VerifyTestDirectory(destPhysicalPath));
                }
                finally {
                    if (site != null) {
                        Sites.DeleteSite(client, Utils.Self(site));
                    }

                    if (Directory.Exists(physicalPath)) {
                        Directory.Delete(physicalPath, true);
                    }

                    if (Directory.Exists(destPhysicalPath)) {
                        Directory.Delete(destPhysicalPath, true);
                    }
                }
            }
        }

        [Fact]
        public void RangeUploadDownload()
        {
            using (HttpClient client = ApiHttpClient.Create()) {

                JObject site = Sites.GetSite(client, "Default Web Site");

                // Create web file
                var webFile = CreateWebFile(client, site, TEST_FILE_NAME);

                try {
                    //
                    // Get physical file info
                    var fileInfo = Utils.FollowLink(client, webFile.Value<JObject>("file_info"), "self");

                    var chunkSize = 1024 * 1024;
                    var totalFileSize = 1024 * 1024 * 10;
                    HttpRequestMessage req;
                    HttpResponseMessage res;

                    for (var i = 0; i < totalFileSize; i+= chunkSize) {
                        req = new HttpRequestMessage(HttpMethod.Put, Utils.GetLink(fileInfo, "content"));

                        var currentChunkSize = totalFileSize - i < chunkSize ? totalFileSize - i : chunkSize;
                        var slice = GetFileSlice(i, currentChunkSize);

                        req.Content = new ByteArrayContent(slice);

                        req.Content.Headers.Add("Content-Range", $"bytes {i}-{i + currentChunkSize - 1}/{totalFileSize}");

                        res = client.SendAsync(req).Result;

                        Assert.True(Globals.Success(res));
                    }

                    req = new HttpRequestMessage(HttpMethod.Get, Utils.GetLink(fileInfo, "content"));

                    res = client.SendAsync(req).Result;

                    Assert.True(Globals.Success(res));

                    var resultBytes = res.Content.ReadAsByteArrayAsync().Result;

                    Assert.True(resultBytes.SequenceEqual(GetFileSlice(0, totalFileSize)));

                    var download = new byte[totalFileSize];

                    //
                    // Range download
                    for (var i = 0; i < totalFileSize; i += chunkSize) {
                        req = new HttpRequestMessage(HttpMethod.Get, Utils.GetLink(fileInfo, "content"));

                        var currentChunkSize = totalFileSize - i < chunkSize ? totalFileSize - i : chunkSize;

                        req.Headers.Add("Range", $"bytes={i}-{i + currentChunkSize - 1}");

                        res = client.SendAsync(req).Result;

                        Assert.True(Globals.Success(res));

                        resultBytes = res.Content.ReadAsByteArrayAsync().Result;
                        resultBytes.CopyTo(download, i);
                    }

                    Assert.True(download.SequenceEqual(GetFileSlice(0, totalFileSize)));
                }
                finally {
                    Assert.True(client.Delete(Utils.Self(webFile.Value<JObject>("file_info"))));
                }
            }
        }

        [Fact]
        public void UploadMultipleFiles()
        {
            var mockFileNames = new List<string>();

            for (var i = 0; i < 15; i++) {
                mockFileNames.Add($"{TEST_FILE_NAME}{i}");
            }


            using (HttpClient client = ApiHttpClient.Create()) {

                JObject site = Sites.GetSite(client, "Default Web Site");

                var webFiles = new List<JObject>();
                var fileInfos = new List<JObject>();

                try {
                    foreach (var name in mockFileNames) {
                        webFiles.Add(CreateWebFile(client, site, name));
                    }
                    foreach (var webFile in webFiles) {
                        fileInfos.Add(Utils.FollowLink(client, webFile.Value<JObject>("file_info"), "self"));
                    }

                    var uploads = new List<Task<bool>>();
                    foreach (var fileInfo in fileInfos) {
                        uploads.Add(MockUploadFile(client, fileInfo, 1024 * 1024 * 5));
                    }

                    Task.WaitAll(uploads.ToArray());

                    foreach (var upload in uploads) {
                        Assert.True(upload.Result);
                    }
                }
                finally {
                    foreach (JObject webFile in webFiles) {
                        if (webFile != null) {
                            Assert.True(client.Delete(Utils.Self(webFile.Value<JObject>("file_info"))));
                        }
                    }
                }
                
            }
        }

        [Fact]
        public void TruncateOnCompleteRange()
        {
            var size = 1024 * 1024 * 5;
            var truncateSize = size / 2;

            using (HttpClient client = ApiHttpClient.Create()) {

                JObject site = Sites.GetSite(client, "Default Web Site");
                var webFile = CreateWebFile(client, site, TEST_FILE_NAME);
                var fileInfo = Utils.FollowLink(client, webFile.Value<JObject>("file_info"), "self");

                try {
                    Assert.True(MockUploadFile(client, fileInfo, size).Result);

                    fileInfo = Utils.FollowLink(client, fileInfo, "self");

                    Assert.True(fileInfo.Value<int>("size") == size);

                    Assert.True(MockUploadFile(client, fileInfo, truncateSize).Result);

                    fileInfo = Utils.FollowLink(client, fileInfo, "self");

                    Assert.True(fileInfo.Value<int>("size") == truncateSize);
                }
                finally {
                    if (webFile != null) {
                        Assert.True(client.Delete(Utils.Self(webFile.Value<JObject>("file_info"))));
                    }
                }
            }
        }

        [Fact]
        public void CreateRenameFile()
        {
            using (HttpClient client = ApiHttpClient.Create()) {
                JObject target = null;
                string updatedName = "updated_test_file_name.txt";
                JObject site = Sites.GetSite(client, "Default Web Site");

                try {
                    var webFile = CreateWebFile(client, site, TEST_FILE_NAME);

                    var physicalPath = Path.Combine(Environment.ExpandEnvironmentVariables(site.Value<string>("physical_path")), updatedName);

                    if (File.Exists(physicalPath)) {
                        File.Delete(physicalPath);
                    }

                    target = RenameSitesFile(client, site, webFile, updatedName);
                }
                finally {
                    Assert.True(client.Delete(Utils.Self(target.Value<JObject>("file_info"))));
                }
            }
        }

        [Fact]
        public void CreateRenameDirectory()
        {
            using (HttpClient client = ApiHttpClient.Create()) {
                JObject target = null;
                string updatedName = "updated_test_folder_name";
                JObject site = Sites.GetSite(client, "Default Web Site");

                try {
                    var webFile = CreateWebFile(client, site, TEST_FILE_NAME, "directory");
                    var physicalPath = Path.Combine(Environment.ExpandEnvironmentVariables(site.Value<string>("physical_path")), updatedName);

                    if (Directory.Exists(physicalPath)) {
                        Directory.Delete(physicalPath);
                    }

                    target = RenameSitesFile(client, site, webFile, updatedName);
                }
                finally {
                    Assert.True(client.Delete(Utils.Self(target.Value<JObject>("file_info"))));
                }
            }
        }

        [Fact]
        public void CoreFileRange()
        {
            var physicalPath = Path.Combine(Configuration.TEST_ROOT_PATH, "api_file_range_test");
            if (Directory.Exists(physicalPath)) {
                Directory.Delete(physicalPath, true);
            }

            using (HttpClient client = ApiHttpClient.Create()) {
                try {
                    var dirs = new List<string>() { "dir1", "dir2", "dir3" };
                    var files = new List<string>() { "file1.txt", "file2.txt", "file3.txt" };

                    foreach (var dir in dirs) {
                        Directory.CreateDirectory(Path.Combine(physicalPath, dir));
                    }

                    foreach (var file in files) {
                        File.Create(Path.Combine(physicalPath, file)).Dispose();
                    }

                    JObject folder = client.Get($"{Configuration.TEST_SERVER_URL}/api/files?physical_path={physicalPath}");

                    HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, Utils.GetLink(folder, "files"));
                    req.Headers.Add("Range", "files=1-3");

                    var res = client.SendAsync(req).Result;

                    Assert.True(res.Content.Headers.Contains("Content-Range"));
                    Assert.True(res.Content.Headers.GetValues("Content-Range").First().Equals("1-3/6"));

                    var children = JObject.Parse(res.Content.ReadAsStringAsync().Result)["files"].ToObject<IEnumerable<JObject>>();
                    Assert.True(children.Count() == 3);
                }
                finally {
                    Directory.Delete(physicalPath, true);
                }
            }
        }

        [Fact]
        public void WebFileRange()
        {
            var physicalPath = Path.Combine(Configuration.TEST_ROOT_PATH, "web_file_range_test");
            if (Directory.Exists(physicalPath)) {
                Directory.Delete(physicalPath, true);
                Directory.CreateDirectory(physicalPath);
            }

            JObject site = null;
            using (HttpClient client = ApiHttpClient.Create()) {
                try {
                    var dirs = new List<string>() { "dir1", "dir2", "dir3", "dir4" };
                    var files = new List<string>() { "file1.txt", "file2.txt", "file3.txt", "file4.txt" };

                    Sites.EnsureNoSite(client, FILE_TEST_SITE_NAME);
                    site = Sites.CreateSite(client, FILE_TEST_SITE_NAME, Utils.GetAvailablePort(), physicalPath);

                    Assert.NotNull(site);

                    foreach (var dir in dirs) {
                        Directory.CreateDirectory(Path.Combine(physicalPath, dir));
                    }

                    foreach (var file in files) {
                        File.Create(Path.Combine(physicalPath, file)).Dispose();
                    }

                    JObject folder = Utils.FollowLink(client, site, "files");

                    HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, Utils.GetLink(folder, "files"));
                    req.Headers.Add("Range", "files=2-5");

                    var res = client.SendAsync(req).Result;

                    Assert.True(res.Content.Headers.Contains("Content-Range"));
                    Assert.True(res.Content.Headers.GetValues("Content-Range").First().Equals("2-5/8"));

                    var children = JObject.Parse(res.Content.ReadAsStringAsync().Result)["files"].ToObject<IEnumerable<JObject>>();
                    Assert.True(children.Count() == 4);
                }
                finally {
                    Directory.Delete(physicalPath, true);
                    if (site != null) {
                        client.Delete(Utils.Self(site));
                    }
                }
            }
        }





        private JObject RenameSitesFile(HttpClient client, JObject site, JObject file, string newName)
        {
            var target = file;
            var originalName = target.Value<string>("name");

            var rootVdir = Utils.FollowLink(client, site, "files");

            var files = Utils.FollowLink(client, rootVdir, "files")["files"].ToObject<IEnumerable<JObject>>();

            target = files.FirstOrDefault(f => f.Value<string>("name").Equals(originalName, StringComparison.OrdinalIgnoreCase));

            Assert.NotNull(target);

            var targetFileInfo = Utils.FollowLink(client, Utils.FollowLink(client, target, "self").Value<JObject>("file_info"), "self");

            var alteredName = newName;
            targetFileInfo["name"] = alteredName;

            targetFileInfo = client.Patch(Utils.Self(targetFileInfo), targetFileInfo);

            Assert.True(targetFileInfo != null);
            Assert.True(targetFileInfo.Value<string>("name").Equals(alteredName));

            files = Utils.FollowLink(client, rootVdir, "files")["files"].ToObject<IEnumerable<JObject>>();
            target = files.FirstOrDefault(f => f.Value<string>("name").Equals(alteredName, StringComparison.OrdinalIgnoreCase));

            Assert.NotNull(target);

            return Utils.FollowLink(client, target, "self");
        }

        private async Task<bool> MockUploadFile(HttpClient client, JObject file, int fileSize)
        {
            const int chunkSize = 1024 * 1024 / 2;

            var initialSize = chunkSize < fileSize ? chunkSize : fileSize;

            return await UploadFileChunked(client, Utils.GetLink(file, "content"), GetFileSlice(0, initialSize), 0, chunkSize, fileSize);
        }

        private async Task<bool> UploadFileChunked(HttpClient client, string url, byte[] content, int start, int chunkSize, int totalSize)
        {
            var length = content.Length;
            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Put, url);

            req.Content = new ByteArrayContent(content);

            req.Content.Headers.Add("Content-Range", $"bytes {start}-{start + length - 1}/{totalSize}");

            var task = client.SendAsync(req);

            HttpResponseMessage res = await task;

            bool success = Globals.Success(res);

            if (!success) {
                return false;
            }

            start = start + length;

            if (start >= totalSize) {
                return true;
            }

            var l = totalSize - start < chunkSize ? totalSize - start : chunkSize;
            content = GetFileSlice(start, l);

            return await UploadFileChunked(client, url, content, start, chunkSize, totalSize);
        }

        private byte[] GetFileSlice(int start, int length)
        {
            var mod = byte.MaxValue + 1;
            var ret = new byte[length];
            byte initial = (byte)(start % mod);

            for (var i = 0; i < length; i++) {
                ret[i] = (byte)((initial + i) % mod);
            }

            return ret;
        }

        private JObject CreateWebFile(HttpClient client, JObject site, string fileName, string fileType = "file", bool deleteIfExists = true)
        {
            var physicalPath = site.Value<string>("physical_path");
            physicalPath = Path.Combine(Environment.ExpandEnvironmentVariables(physicalPath), TEST_FILE_NAME);

            if (deleteIfExists) {
                if (File.Exists(physicalPath)) {
                    File.Delete(physicalPath);
                }
                if (Directory.Exists(physicalPath)) {
                    Directory.Delete(physicalPath);
                }
            }

            var rootDir = Utils.FollowLink(client, site, "files");
            var rootDirFileInfo = Utils.FollowLink(client, rootDir.Value<JObject>("file_info"), "self");

            object newFile = new
            {
                type = fileType,
                parent = rootDirFileInfo,
                name = fileName
            };

            // Create web file
            var file = client.Post($"{Configuration.TEST_SERVER_URL}{FILES_PATH}", newFile);

            Assert.True(file != null);

            var siteFiles = Utils.FollowLink(client, rootDir, "files");

            file = siteFiles.Value<JArray>("files").ToObject<IEnumerable<JObject>>().FirstOrDefault(f =>
                f.Value<string>("name").Equals(file.Value<string>("name"))
            );

            Assert.True(file != null);

            return Utils.FollowLink(client, file, "self");
        }

        private void CreateTestDirectory(string path)
        {
            if (Directory.Exists(path)) {
                Directory.Delete(path, true);
                Directory.CreateDirectory(path);
            }

            //
            // Create directory with large amount of files to move
            var tasks = new List<Task>();
            var kb = GetFileSlice(0, 1000);
            for (var i = 0; i < 50; i++) {
                string dir = Path.Combine(path, i.ToString());

                tasks.Add(Task.Run(() => {
                    Directory.CreateDirectory(dir);
                    for (var j = 0; j < 50; j++) {
                        using (var stream = File.Create(Path.Combine(dir, j.ToString()))) {
                            for (var k = 0; k < 10; k++) {
                                stream.Write(kb, 0, kb.Length);
                            }
                        }
                    }
                }));
            }
            Task.WaitAll(tasks.ToArray());
        }

        private bool VerifyTestDirectory(string path)
        {
            if (!Directory.Exists(path)) {
                return false;
            }

            var kb = GetFileSlice(0, 1000);
            for (var i = 0; i < 50; i++) {
                string dir = Path.Combine(path, i.ToString());

                if (!Directory.Exists(dir)) {
                    return false;
                }

                for (var j = 0; j < 50; j++) {
                    var bytes = File.ReadAllBytes(Path.Combine(dir, j.ToString()));

                    for (var k = 0; k < 10; k++) {
                        for (var l = 0; l < kb.Length; l++) {
                            if (bytes[(k * kb.Length) + l] != kb[l]) {
                                return false;
                            }
                        }
                    }
                }
            }

            return true;
        }
    }
}