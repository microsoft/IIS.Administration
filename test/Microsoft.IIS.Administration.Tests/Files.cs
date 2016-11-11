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
        public void IsParentPath()
        {
            Assert.True(PathUtil.IsParentPath("/", "/a"));
            Assert.True(PathUtil.IsParentPath("/a/b", "/a/b/c"));
            Assert.False(PathUtil.IsParentPath("/", "/"));
            Assert.False(PathUtil.IsParentPath("/a/b", "/a/bc"));
        }
    }
}
