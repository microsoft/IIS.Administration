// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Tests
{
    using Administration.Files;
    using System;
    using Web.Administration;
    using WebServer.Files;
    using Xunit;

    public class Files
    {
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

                app = FilesHelper.ResolveApplication(site, "/");
                Assert.True(app.Path == "/");

                app = FilesHelper.ResolveApplication(site, "/ac");
                Assert.True(app.Path == "/");

                app = FilesHelper.ResolveApplication(site, "/a");
                Assert.True(app.Path == "/a");

                app = FilesHelper.ResolveApplication(site, "/a/c");
                Assert.True(app.Path == "/a");

                app = FilesHelper.ResolveApplication(site, "/a/bc");
                Assert.True(app.Path == "/a");

                app = FilesHelper.ResolveApplication(site, "/a/b/c");
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

                vdir = FilesHelper.ResolveVdir(site, "/app1/vdir1");
                Assert.True(vdir.Path == "/vdir1");
                vdir = FilesHelper.ResolveVdir(site, "/app1/a_folder");
                Assert.True(vdir.Path == "/");
            }
        }

        [Fact]
        public void IsParentPath()
        {
            Assert.True(PathUtil.IsParentPath("/", "/a"));
            Assert.True(PathUtil.IsParentPath("/a/b", "/a/b/c"));
            Assert.False(PathUtil.IsParentPath("/", "/"));
            Assert.False(PathUtil.IsParentPath("/a/b", "/a/bc"));
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

                Assert.True(FilesHelper.IsExactVdirPath(site, app1, vdir1a, "/app1/"));
                Assert.True(FilesHelper.IsExactVdirPath(site, app1, vdir1a, "/app1"));
                Assert.True(FilesHelper.IsExactVdirPath(site, app1, vdir1b, "/app1/vdir1b"));
                Assert.True(FilesHelper.IsExactVdirPath(site, app1, vdir1b, "/app1/vdir1b/"));

                Assert.True(FilesHelper.IsExactVdirPath(site, app2, vdir2a, "/app2"));
                Assert.True(FilesHelper.IsExactVdirPath(site, app2, vdir2a, "/app2/"));
                Assert.True(FilesHelper.IsExactVdirPath(site, app2, vdir2b, "/app2/vdir2b"));
                Assert.True(FilesHelper.IsExactVdirPath(site, app2, vdir2b, "/app2/vdir2b/"));

                Assert.False(FilesHelper.IsExactVdirPath(site, app1, vdir1a, "/app1/folder"));
                Assert.False(FilesHelper.IsExactVdirPath(site, app1, vdir1b, "/app1/vdir1b/folder"));
                Assert.False(FilesHelper.IsExactVdirPath(site, app1, vdir1b, "/app2/vdir1b"));
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

                Assert.Equal(FilesHelper.GetPhysicalPath(site, "/"), rootVdirPhysicalPath);
                Assert.Equal(FilesHelper.GetPhysicalPath(site, "/abc/defg"), rootVdirPhysicalPath + @"\abc\defg");
                Assert.Equal(FilesHelper.GetPhysicalPath(site, "/app1"), vdir1aPhysicalPath);
                Assert.Equal(FilesHelper.GetPhysicalPath(site, "/app1/abc/defg"), vdir1aPhysicalPath + @"\abc\defg");
                Assert.Equal(FilesHelper.GetPhysicalPath(site, "/app1/vdir1bc/abc/defg"), vdir1aPhysicalPath + @"\vdir1bc\abc\defg");
                Assert.Equal(FilesHelper.GetPhysicalPath(site, "/app1/vdir1b"), vdir1bPhysicalPath);
                Assert.Equal(FilesHelper.GetPhysicalPath(site, "/app1/vdir1b/abc/defg"), vdir1bPhysicalPath + @"\abc\defg");
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
                Assert.True(FilesHelper.IsValidFileName(name));
            }

            foreach (var name in badFileNames) {
                Assert.False(FilesHelper.IsValidFileName(name));
            }
        }

        [Fact]
        public void PrefixSegments()
        {
            var path = "/";

            Assert.Equal(PathUtil.PrefixSegments("/", path), 1);
            Assert.Equal(PathUtil.PrefixSegments("/a", path), -1);

            path = "/abc/def/ghi";

            Assert.Equal(PathUtil.PrefixSegments("/", path), 1);
            Assert.Equal(PathUtil.PrefixSegments("/abc", path), 2);
            Assert.Equal(PathUtil.PrefixSegments("/abc/", path), 2);
            Assert.Equal(PathUtil.PrefixSegments("/abcd/", path), -1);
            Assert.Equal(PathUtil.PrefixSegments("/aBc/", path), 2);
            Assert.Equal(PathUtil.PrefixSegments("/aBc/", path, StringComparison.Ordinal), -1);
            Assert.Equal(PathUtil.PrefixSegments("/abc/def", path), 3);
            Assert.Equal(PathUtil.PrefixSegments("/abc/def/", path), 3);
            Assert.Equal(PathUtil.PrefixSegments("/abc/defg/", path), -1);
            Assert.Equal(PathUtil.PrefixSegments("/abca/def/", path), -1);

        }

        [Fact]
        public void RemoveLastSegment()
        {
            Assert.Equal(PathUtil.RemoveLastSegment("/a"), "/");
            Assert.Equal(PathUtil.RemoveLastSegment("/a/"), "/");
            Assert.Equal(PathUtil.RemoveLastSegment("/abc/def/ghi"), "/abc/def");
            Assert.Equal(PathUtil.RemoveLastSegment("/abc/def/ghi"), "/abc/def");
            Assert.Equal(PathUtil.RemoveLastSegment("/abc/def/ghi/"), "/abc/def");
            Assert.Equal(PathUtil.RemoveLastSegment(@"/abc/def\ghi"), "/abc/def");
            Assert.Equal(PathUtil.RemoveLastSegment(@"/abc/def\ghi/"), "/abc/def");
            Assert.Equal(PathUtil.RemoveLastSegment(@"/abc/def\ghi\"), "/abc/def");
        }
    }
}
