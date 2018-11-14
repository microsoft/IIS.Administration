// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


$(document).ready(function () {
    //
    // Auto-resize
    $(".auto-resize")
    .on("input", function () {
        this.style.height = 'auto';
        this.style.height = (this.scrollHeight) + 'px';
    })
    .trigger("input");

    $(window).resize(function () {
        $(".auto-resize").trigger("input");
    })

    //
    // Sign-out
    $("#signOut > a").click(function () {
        signOut();
    });

    if (!accessToken()) {
        $("#signOut").addClass('disabled');
    }
    else {
        $("#signOut").show();
    }

    //
    // radio-row
    $(".radio-row li").click(function () {
        $(this).siblings().removeClass("selected");
        $(this).addClass("selected");
    });

    //
    // handle Enter and Space keyup event to select the input-focused <li> element
    $(".radio-row li").keyup(function (event) {
        if (event.which === 13 || event.which === 32) {
            $(this).siblings().removeClass("selected");
            $(this).addClass("selected");
        }
    });
});


function isTimeSpan(o) {
    return /^\d{2,}:\d{2}:\d{2}(?:\.\d+)?$/.test(o);
}

var ACCESS_TOKEN_KEY = "accessToken";

function accessToken(token, persist) {
    if (!sessionStorage) {
        console.error("Session Storage is not available");
        return "";
    }

    try {
        //
        // Set
        if (token) {
            sessionStorage.setItem(ACCESS_TOKEN_KEY, token);

            if (persist && localStorage) {
                localStorage.setItem(ACCESS_TOKEN_KEY, token);
            }
        }

        //
        // Get

        // Try the session storage first
        var tkn = sessionStorage.getItem(ACCESS_TOKEN_KEY);

        if (!tkn && localStorage) {
            // Try the local storage
            tkn = localStorage.getItem(ACCESS_TOKEN_KEY);

            if (tkn) {
                // Save to the session
                sessionStorage.setItem(ACCESS_TOKEN_KEY, tkn);
            }
        }

        return tkn || "";
    }
    catch (e) {
        console.error("Storage Error:" + e);
        return "";
    }
}

function signOut() {
    if (sessionStorage) {
        sessionStorage.removeItem(ACCESS_TOKEN_KEY);
    }

    if (localStorage) {
        localStorage.removeItem(ACCESS_TOKEN_KEY);
    }

    // Redirect to login (async)
    setTimeout(function () {
        location.href = "/connect";
    }, 10);
}


function isAccessTokenError(xhr) {
    if ((xhr.status == "401" || xhr.status == "403") && xhr.responseText) {
        try {
            var json = $.parseJSON(xhr.responseText);
            return json.authentication_scheme == "Bearer";
        }
        catch (err) {
        }
    }

    return false;
}
