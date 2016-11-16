// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Tests
{
    using Administration.Files;
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
    }
}
