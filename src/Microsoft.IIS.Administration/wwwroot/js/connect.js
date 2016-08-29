// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


(function () {
    $(document).ready(function () {
        $("#connect").click(function () {
            var token = $("#token").val();

            if (token) {
                $("#token").val("");
                accessToken(token, $("#rememberMe").prop('checked'));
                location.href = "/";
            }
        });

        $("#token").keypress(function (e) {
            if (e.which == 13) {
                $("#connect").click();
            }
        });

        if (location.href.indexOf("https://localhost") == 0) {
            $("#rememberMe").prop('checked', true);
        }
    });
}());
