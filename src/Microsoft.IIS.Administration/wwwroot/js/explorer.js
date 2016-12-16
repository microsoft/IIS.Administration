// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


(function () {
    var _json;
    var _editableFile = false;

    $(document).ready(function () {
        //
        // Go button
        $("#go-button").click(function () {
            var url = $("#urlgo").val();
            if (url == window.location.hash.substring(1)) {
                _load(url); // reload
            }
            else {
                window.location.useVerb = true;
                window.location.hash = url;
            }
        });

        //
        // Request entity body textarea
        $("#request-panel > textarea").on("focus blur", function () {
            if ($(this).val() == "") {
                $(this).val("{\r\r\r\r}").trigger("input");
                $(this)[0].setSelectionRange(3, 3);
            }
        });

        // 
        // Verbs
        $("#nav-panel .verbs a").click(function () {
            var verb = $(this).attr("id").replace("verb-", "").toUpperCase();

            if (verb != "POST" && verb != "PUT" && _json && _json._links && _json._links.self) {
                window.location.hash = _json._links.self.href;
            }
            
            // Schedule set because hash change event may fire
            setTimeout(function () { _selectVerb(verb); }, 1);
            
            return false;
        });

        $("#urlgo").keyup(function (event) {
            if (event.keyCode == 13) {
                $("#go-button").click();
            }
        });

        $("#files input").change(function (evt) {

            var self = $(this).parent('.file-upload');

            self.find('.state').html('');
            self.find(".name").html('');

            var file = evt.target.files[0]
            var reader = new FileReader();

            if (!file) {
                return;
            }

            self.find('.state').html("In Progress");
            uploadFile(file, $("#urlgo").val(),
                function (response, statusText, jqhxr) {
                    _setStatus(jqhxr);
                    self.find('.state').html("Success");
                },
                function (jqhxr) {
                    _setStatus(jqhxr);
                    self.find('.state').html("Failed");
                });
        });

        function uploadFile(file, url, success, error) {
            var chunkSize = 1024 * 1024 / 2; // Half a megabyte;

            var initialLength = file.size < chunkSize ? file.size : chunkSize;

            readFile(file, 0, initialLength, function (data) {
                _uploadFileChunked(file, url, 0, initialLength, data, chunkSize, file.size, success, error);
            });

        }

        function _uploadFileChunked(file, url, start, length, data, chunkSize, totalSize, success, error) {
            $.ajax({
                type: "PUT",
                url: url,
                data: data,
                dataType: 'text',
                processData: false,
                contentType: 'text/plain;',
                xhrFields: {
                    withCredentials: true
                },
                beforeSend: function (request) {
                    request.setRequestHeader('Access-Token', 'Bearer ' + accessToken());
                    request.setRequestHeader('Content-Range', 'bytes ' + start + '-' + (start + length - 1) + '/' + totalSize);
                },
                success: function (response, statusText, jqhxr) {
                    start = start + length;

                    if (start >= totalSize) {
                        success(response, statusText, jqhxr);
                    }
                    else {
                        var l = totalSize - start < chunkSize ? totalSize - start : chunkSize;

                        readFile(file, start, l, function (d) {
                            _uploadFileChunked(file, url, start, l, d, chunkSize, totalSize, success, error);
                        })
                    }
                },
                error: function (jqhxr) {
                    error(jqhxr);
                }
            })
        }

        function readFile(file, start, length, callback) {
            var reader = new FileReader();

            var slice = file.slice(start, start + length);

            reader.readAsArrayBuffer(slice);
            reader.onload = function (e) {
                callback(e.target.result);
            }
        }

        $(window).on('hashchange', function (e) {
            var hash = window.location.hash.substring(1);
            if (hash && hash != "/") {
                loadUrl = window.location.hash.substring(1);
            }
            else {
                loadUrl = '/api';
                window.location.hash = loadUrl;
            }

            if (!window.location.useVerb) {
                _selectVerb("get");
            }

            window.location.useVerb = false;
            _load(loadUrl);
        });

        $(window).trigger("hashchange");
    });


    function _selectVerb(verb) {
        verb = verb.toUpperCase();
        var $elem = $("#verb-" + verb.toLowerCase());

        $elem.addClass("selected").siblings().removeClass("selected");

        if (verb == "PUT" && _editableFile) {
            showFileUpload(true);
            $("#request-panel").hide();
        }
        else if (verb == "POST" || verb == "PUT" || verb == "PATCH") {
            // POST, PUT and PATCH require body
            $("#request-panel").show();
            $("#request-panel > textarea").trigger("input").focus()[0].setSelectionRange(3, 3);
            showFileUpload(false);
        }
        else {
            // Hide request entity body panel for GET and DELETE
            $("#request-panel").hide();
            showFileUpload(false);
        }
    }


    function _load(api_url) {
        var verb = $("#nav-panel .verbs .selected").attr("id").replace("verb-", "").toUpperCase().trim();
        var body = (verb == "GET" || verb == "DELETE") ? null : $("#request-panel #payload").val();

        $("#urlgo").val(api_url);

        // Clear the result area
        $('#resultWrapper').html("<div id='result'></div>");

        $.ajax({
            type: verb,
            url: api_url,
            data: body,
            dataType: 'text',
            processData: false,
            xhrFields: {
                withCredentials: true
            },
            beforeSend: function (request) {
                _setStatus({});

                if (body) {
                    request.setRequestHeader('Content-Type', 'application/json');
                }
                request.setRequestHeader('Accept', 'application/hal+json');
                request.setRequestHeader('Access-Token', 'Bearer ' + accessToken());
            },
            success: function (response, statusText, jqhxr) {
                _setStatus(jqhxr);
                _editableFile = false;
                $("#nav-panel").show();

                var contentType = jqhxr.getResponseHeader("Content-Type");
                var contentDisposition = jqhxr.getResponseHeader("Content-Disposition");
                var location = jqhxr.getResponseHeader("Location");
                var pragma = jqhxr.getResponseHeader("Pragma");

                if (location && pragma && pragma.toLowerCase() === "attachment") {
                    window.location = window.location.origin + location;
                }
                else if (contentType && contentType.indexOf("json") != -1) {
                    var json = JSON.parse(response);

                    document.getElementById('result').innerHTML = json2Html(json);

                    _json = json;
                }

                if (contentDisposition && filesSupported()) {
                    _editableFile = true;
                    _json = null;
                }
            },
            error: function (xhr) {
                _json = null;

                if (isAccessTokenError(xhr)) {
                    signOut();
                    return;
                }
                
                $("#nav-panel").show();
                _ajaxError(xhr);
            }
        })
    }

    function _setStatus(jqhxr) {
        $("#response").removeClass("error");

        if (jqhxr.status >= 400) {
            $("#response").addClass("error");
        }

        $("#response .status").html(jqhxr.status || "");
        $("#response .status-text").html(jqhxr.statusText || "");
    }


    function _ajaxError(xhr) {
        _setStatus(xhr);

        if (xhr.responseText) {
            var json = $.parseJSON(xhr.responseText);
            $("#result").html(json2Html(json));
        }
    }

    function json2Html(o) {
        var json = jQuery.extend(true, {}, o);

        if (jQuery.isEmptyObject(json)) {
            return "";
        }

        json = _json2Html(json);

        // Use regex to help format the stringified json object
        // We replace line breaks with the html <br />
        // We replace spaces with the html &nbsp;
        return JSON.stringify(json, null, 4).replace(/\n/g, "<br/>").replace(/\ \ /g, "&nbsp;&nbsp;");
    }

    function wrapResult(val) {
        val ? $('#result').css('white-space', 'pre-wrap') : $('#result').css('white-space', '');
    }

    function sanitizeHtml(unsafe) {
        return unsafe
             .replace(/&/g, "&amp;")
             .replace(/</g, "&lt;")
             .replace(/>/g, "&gt;")
             .replace(/"/g, "&quot;")
             .replace(/'/g, "&#039;");
    }

    function _json2Html(o) {
        for (var i in o) {
            if (!(o instanceof Array)) {
                if (i == "_links") {
                    var j = "<z class='links'>" + i + "</z>";
                    o[j] = o[i];
                    delete o[i];
                    i = j;
                }
                else {
                    var j = (typeof (o[i]) == "object") ? "<b class='obj'>" + i + "</b>" : "<b>" + i + "</b>";
                    o[j] = o[i];
                    delete o[i];
                    i = j;
                }
            }
            if (i.match(/href/g)) {
                o[i] = "<a class='href' title='" + o[i] + "' href='" + window.location.pathname + "#" + o[i] + "'>" + o[i] + "</a>";
            } else if (i == "<b>id</b>" && typeof (o[i]) == "string") {
                o[i] = "<z class='id'>" + o[i] + "</z>";
            } else if (i == "<b>_links</b>" && typeof (o[i]) == "object") {
                o[i] = "<z class='links'>" + o[i] + "</z>";
            } else if (o[i] !== null && typeof (o[i]) == "object") {
                _json2Html(o[i]);
            } else if (!(o[i] === null) && typeof (o[i]) == "boolean") {
                o[i] = "<z class='boolean'>" + o[i] + "</z>";
            } else if (o[i] !== null && (typeof (o[i]) == "number")) {
                o[i] = "<z class='number'>" + o[i] + "</z>";
            } else if (!(o[i] === null) && !isNaN(Date.parse(o[i]))) {
                o[i] = "<z class='datetime'>" + o[i] + "</z>";
            } else if (o[i] !== null && isTimeSpan(o[i])) {
                o[i] = "<z class='timespan'>" + o[i] + "</z>";
            } else if (o[i] !== null && typeof (o[i]) == "string") {
                o[i] = "<z class='string'>" + o[i] + "</z>";
            }
        }

        return o;
    }

    function filesSupported() {
        return window.File && window.FileReader && window.FileList && window.Blob;
    }

    function showFileUpload(val) {
        var self = $("#files");

        if (val) {
            self.show();
        }
        else {
            self.hide();
            self.find('.state').html('');
            self.find(".name").html('');
            var input = self.find("input");
            input.replaceWith(input.val('').clone(true));
        }
    }
}());
