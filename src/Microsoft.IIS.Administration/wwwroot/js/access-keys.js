// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


$(document).ready(function () {
    $("#cancel, #ok").click(function () {
        $("#modal, #newTokenForm, #tokenForm").hide();
        $("#purpose").val("");
        $("#key").html("");
        window.location.hash = "";
    });

    $("#showNewForm").click(function () {
        $("#tokenForm").hide();

        // show the newTokenForm
        $("#modal, #newTokenForm").show();

        // set input-focus to the first control of the newTokenForm
        $("#purpose").focus(); 
    });

    $("#tokenGenerator").submit(function () {
        $("#generate").attr("disabled", true);
    });

    $(".del-token").click(function (e) {
        if (confirm("The access key will be permanetly deleted!\nIt won't be able to access the API anymore.")) {
            $("#keysForm").attr("action", $("#keysForm").attr("action") + "delete");
            $(this).parents("li").hide();
            return true;
        }
        else {
            return false;
        }
    })

    $(".refresh-token").click(function (e) {
        if (confirm("The Access Token will be refreshed!\nPrevious tokens associated with that key won't be able to access the API anymore.")) {
            $("#keysForm").attr("action", $("#keysForm").attr("action") + "refreshtoken");
            return true;
        }
        else {
            return false;
        }
    })

    if ($("#key").val() != "") {
        $("#newTokenForm").hide();
        $("#modal, #tokenForm").show();
        $("#tokenForm #ok").focus();
    }

    $("#tokenGenerator").submit(function () {
        var $e = $("#newTokenForm .expiration .selected");

        var hh = parseInt($e.attr("date-hours")) || 0;
        var dd = parseInt($e.attr("date-days")) || 0;
        var mm = parseInt($e.attr("date-months")) || 0;
        var yy = parseInt($e.attr("date-years")) || 0;

        var now = new Date();

        var exp = Date.UTC(now.getFullYear() + yy, now.getMonth() + mm, now.getDate() + dd, now.getHours() + hh, now.getMinutes(), now.getSeconds());
        var utc_now = Date.UTC(now.getFullYear(), now.getMonth(), now.getDate(), now.getHours(), now.getMinutes(), now.getSeconds());

        var seconds = Math.ceil((exp - utc_now) / 1000);

        $("#newTokenForm input[name='expiration']").val(seconds || "");
    })

    $("#key").ready(function () {
        var copyText = document.getElementById("key");
        copyText.select();
    })

    $("#clipboardCopy").click(function () {
        var copyText = document.getElementById("key");
        copyText.select();

        /* Copy to clipboard */
        document.execCommand('copy');
    })

    //
    // Scroll
    $(document).scroll(function () {
        sessionStorage['page'] = document.URL;
        sessionStorage['scrollTop'] = $(document).scrollTop();
    });

    if (sessionStorage['page'] == document.URL) {
        $(document).scrollTop(sessionStorage['scrollTop']);
    }

    //
    // Open create form
    if (window.location.hash == '#new') {
        $("#showNewForm").click();
    }
});
