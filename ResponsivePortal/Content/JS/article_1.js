
var clientId = document.getElementById("clientId").value;
var portalId = document.getElementById("portalId").value;
var searchText = "";

if (document.getElementById("SearchText") != null) {
    searchText = document.getElementById("SearchText").value;
}
var articleId = "";
if (document.getElementById("articleId") != null) {
    articleId = document.getElementById("articleId").value;
}

jQuery.fn.highlight = function (str, className) {
    var regex = new RegExp(str, "gi");
    return this.each(function () {
        $(this).contents().filter(function () {
            return this.nodeType == 3 && regex.test(this.nodeValue);
        }).replaceWith(function () {
            return (this.nodeValue || "").replace(regex, function (match) {
                return "<span class=\"" + className + "\">" + match + "</span>";
            });
        });
    });
};
    
$(".articlesContent *").highlight(searchText, "highlight");

var g_selectedWidget = null;
var g_action = "";

function executeMethod(params) {
    if (params.action == 'favorites') {
        HideOldSelection();
        $.ajax({
            url: params.url,
            data: JSON.stringify({ id: articleId }),
            dataType: "json",
            type: "POST",
            contentType: 'application/json; charset=utf-8',
            success: function (data) {
                if (data) {
                    if (data.Message) {
                        SetNotification(data.Message);
                        if (data.Result) {
                            $("#options-favorites").find('img').toggle();
                        }
                    }
                }
            },
            error: function (xhr, status, error) {
                var err = eval("(" + xhr.responseText + ")");
                alert(err.Message);
            }
        });
    }

    else if (params.action == 'subscribeConfirm') {

        var url = '/Article/ToggleArticleSubscription/' + clientId + '/' + portalId + "?email=" + $('#subscribeEmail').val() + "&id=" + articleId + "&isSubscribe=true";
        if (validateSubscriptionEmail()) {

            $.ajax({
                url: url,
                success: function (data) {
                    if (data) {
                        if (data.Message) {
                            SetNotification(data.Message);
                        }
                    }
                },
                error: function (xhr, status, error) {
                    var err = eval("(" + xhr.responseText + ")");
                    alert(err.Message);
                }
            });
        }
    }


    else if (params.action == 'unsubscribeConfirm') {


        var url = '/Article/ToggleArticleSubscription/' + clientId + '/' + portalId + "?email=" + $('#subscribeEmail').val() + "&id=" + articleId + "&isSubscribe=false";
        if (validateSubscriptionEmail()) {
            $.ajax({
                url: url,
                success: function (data) {
                    if (data) {
                        if (data.Message) {
                            SetNotification(data.Message);
                        }
                    }
                },
                error: function (xhr, status, error) {
                    var err = eval("(" + xhr.responseText + ")");
                    alert(err.Message);
                }
            });
        }
    }
    else if (params.action == 'feed') {
        var formExists = false;
        if ($('#divFeedbackForm').length) {
            formExists = true;
        }
        $.ajax({
            url: '/Article/InsertArticleRating/'+clientId+'/'+ portalId + "?id=" + articleId + "&positive="  + params.rate,
            success: function (data) {
                if (data) {
                    if (data.Result == 1) {
                        if (params.rate) {
                            $("#divFeedbackOptions").hide();
                            if (formExists) {
                                $("#divFeedbackForm").hide();
                            }
                            $("#divFeedbackSuccess").show();
                        }
                        else {
                            $("#divFeedbackOptions").hide();
                            if (formExists) {
                                $("#divFeedbackSuccess").hide();
                                $("#divFeedbackForm").show();
                                $('#txtName').val(data.Name);
                                $('#txtEmail').val(data.Email);
                                $('#txtName').keyup(function () { validateFeedback(); });
                                $('#txtEmail').keyup(function () { validateFeedback(); });
                                $('#txtSuggestion').keyup(function () { validateFeedback(); });
                            }
                            else {
                                $("#divFeedbackSuccess").show();
                            }
                        }
                    }
                }
            },
            error: function (xhr, status, error) {
                var err = eval("(" + xhr.responseText + ")");
                alert(err.Message);
            }
        });
    }
    else if (params.action == 'share') {
        HideOldSelection();
        if (g_action == params.action) {
            g_action = "";
            return;
        }
        g_action = params.action;
        //;  $("#tdArtContainer").css('width', "70%");
        $("#alertDetailsContainer").addClass("article_actionAlertContentPanel").removeClass("displayNone");
        g_selectedWidget = $("#ShareContainer");
        g_selectedWidget.css('display', 'block');
        SetIconSelection();
    }
    else if (params.action == 'subscribe') {
        HideOldSelection();
        if (g_action == params.action) {
            g_action = "";
            return;
        }
        g_action = params.action;
        //;  $("#tdArtContainer").css('width', "70%");
        $("#alertDetailsContainer").addClass("article_actionAlertContentPanel").removeClass("displayNone");
        g_selectedWidget = $("#SubscribeContainer");
        g_selectedWidget.css('display', 'block');
        SetIconSelection();
    }
    else if (params.action == 'sendMail') {


        g_action = "";
        var url = '/Article/ShareSendEmail/' + clientId + '/' + portalId;
        var email = $("#shareEmail").val();
        var subject = $("#shareSubject").val();
        var body = $("#shareBody").val();

        if (!validateShareSendEmail()) {

            return false;
        }

        HideOldSelection();
        $.ajax({
            url: url,
            data: JSON.stringify({ id: articleId, email: email, subject: subject, body: body }),
            dataType: "json",
            type: "POST",
            contentType: 'application/json; charset=utf-8',
            success: function (data) {
                if (data) {
                    if (data.Message) {
                        SetNotification(data.Message);
                    }
                }
            },
            error: function (xhr, status, error) {
                var err = eval("(" + xhr.responseText + ")");
                alert(err.Message);
            }
        });
    }
    else if (params.action == 'edit') {
        window.open(params.url + articleId);
    }
    else if (params.action == 'share_email') {
        HideOldSelection();
        if (g_action == params.action) {
            g_action = "";
            return;
        }
        //;$("#tdArtContainer").css('width', "70%");
        $("#alertDetailsContainer").addClass("article_actionAlertContentPanel").removeClass("displayNone");
        g_selectedWidget = $("#ShareMailContainer");
        g_selectedWidget.css('display', 'block');
        SetIconSelection();
    }
    else if (params.action == 'share_facebook') {
        window.open(params.url);
    }
    else if (params.action == 'share_twitter') {
        window.open(params.url);
    }
    else if (params.action == 'share_reddit') {
        window.open(params.url);
    }
    else if (params.action == 'feedsubmit') {
        if (!validateFeedback()) { return false; }
        $.ajax({
            url: '/Article/InsertArticleFeedback/' + clientId + '/' + portalId,
            data: JSON.stringify({ id: articleId, feedback: $("#txtSuggestion").val(), name: $("#txtName").val(), email: $("#txtEmail").val() }),
            dataType: "json",
            type: "POST",
            contentType: 'application/json; charset=utf-8',
            success: function (data) {
                if (data) {
                    $("#divFeedbackOptions").hide();
                    $("#divFeedbackForm").hide();
                    $("#divFeedbackSuccess").text(data.Message);
                    $("#divFeedbackSuccess").show();
                }
            },
            error: function (xhr, status, error) {
                var err = eval("(" + xhr.responseText + ")");
                alert(err.Message);
            }
        });
    }
    else {
        window.location = params.url;
    }
}
function validateFeedback() {

    var nameTitle = document.getElementById("nameTitle").value;
    var emailTitle = document.getElementById("emailTitle").value;
    var suggestionTitle = document.getElementById("suggestionTitle").value;
    var valid = true;
    var regex = /^[a-zA-Z0-9_.-]+@[a-zA-Z0-9]+[a-zA-Z0-9.-]+[a-zA-Z0-9]+.[a-z]{1,4}$/;
    if ($("#txtName").val().length < 3) { $("#txtName").css({ "border": "1px solid red", "background": "#FFCECE" }); $("#txtName").parent().next().text(nameTitle); valid = false; }
    else { $("#txtName").css({ "border": "", "background": "" }); $("#txtName").parent().next().text(''); }
    if ($("#txtEmail").val() == '' || !regex.test($("#txtEmail").val())) { $("#txtEmail").css({ "border": "1px solid red", "background": "#FFCECE" }); $("#txtEmail").parent().next().text(emailTitle); valid = false; }
    else { $("#txtEmail").css({ "border": "", "background": "" }); $("#txtEmail").parent().next().text(''); }
    if ($("#txtSuggestion").val().length < 3) { $("#txtSuggestion").css({ "border": "1px solid red", "background": "#FFCECE" }); $("#txtSuggestion").parent().next().text(suggestionTitle); valid = false; }
    else { $("#txtSuggestion").css({ "border": "", "background": "" }); $("#txtSuggestion").parent().next().text(''); }
    return valid;
}

function validateShareSendEmail() {

    var emailTitle = document.getElementById("ShareEmailTitle").value;
    var subjectTitle = document.getElementById("ShareSubjectTitle").value;
    var bodyTitle = document.getElementById("ShareBodyTitle").value;
    var objshareEmailerr = document.getElementById("shareEmailerr");
    var objshareSuberr = document.getElementById("shareSuberr");
    var objshareBodyerr = document.getElementById("shareBodyerr");
    var valid = true;
    var regex = /^[a-zA-Z0-9_.-]+@[a-zA-Z0-9]+[a-zA-Z0-9.-]+[a-zA-Z0-9]+.[a-z]{1,4}$/;
    if ($("#shareSubject").val().length < 3) { $("#shareSubject").css({ "border": "1px solid red", "background": "#FFCECE" }); objshareSuberr.textContent = subjectTitle; valid = false; }
    else { $("#shareSubject").css({ "border": "", "background": "" }); objshareSuberr.textContent = ''; }
    if ($("#shareEmail").val() == '' || !regex.test($("#shareEmail").val())) { $("#shareEmail").css({ "border": "1px solid red", "background": "#FFCECE" }); objshareEmailerr.textContent = emailTitle; valid = false; }
    else { $("#shareEmail").css({ "border": "", "background": "" }); objshareEmailerr.textContent = ''; }
    if ($("#shareBody").val().length < 3) { $("#shareBody").css({ "border": "1px solid red", "background": "#FFCECE" }); objshareBodyerr.textContent = bodyTitle; valid = false; }
    else { $("#shareBody").css({ "border": "", "background": "" }); objshareBodyerr.textContent = ''; }
    return valid;
}

function validateSubscriptionEmail() {


    var emailTitle = document.getElementById("emailTitle").value;

    var valid = true;
    var regex = /^[a-zA-Z0-9_.-]+@[a-zA-Z0-9]+[a-zA-Z0-9.-]+[a-zA-Z0-9]+.[a-z]{1,4}$/;

    if ($("#subscribeEmail").val() == '' || !regex.test($("#subscribeEmail").val())) { $("#subscribeEmail").css({ "border": "1px solid red", "background": "#FFCECE" }); $("#subscribeEmail").prop('title', emailTitle); valid = false; }
    else { $("#subscribeEmail").css({ "border": "", "background": "" }); $("#subscribeEmail").prop('title', ''); }

    return valid;
}
function HideOldSelection() {
    if (g_selectedWidget == null) {
        return;
    }
    g_selectedWidget.css('display', 'none');
    g_selectedWidget = null;
    //;  $("#tdArtContainer").css('width', "100%");
    $("#alertDetailsContainer").removeClass("article_actionAlertContentPanel").addClass("displayNone");
    $("#imgNormal-" + g_action).css('display', "inline");
    $("#imgToggle-" + g_action).css('display', "none");
}
function SetIconSelection() {
    if (g_selectedWidget == null) {
        return;
    }
    $("#imgNormal-" + g_action).css('display', "none");
    $("#imgToggle-" + g_action).css('display', "inline");
}
function SetNotification(message) {
    $("#divActionAlert").text(message);
    $("#divActionAlert").css('display', 'block');
    $("#divActionAlert").fadeOut(5000, function () { $("#divActionAlert").css('display', 'none') });

}

$(window).resize(function () {
    var l = $(window).width();
    if (l < 270) {
        $('#alertDetailsContainer').addClass("article_SocialLeft").removeClass("article_SocialRight");
    }
    else {
        $('#alertDetailsContainer').addClass("article_SocialRight").removeClass("article_SocialLeft");
    }
});

//// srcdoc-polyfill
(function (window, document, undefined) {
    var idx, iframes;
    var _srcDoc = window.srcDoc;
    var isCompliant = !!("srcdoc" in document.createElement("iframe"));
    var implementations = {
        compliant: function (iframe, content) {
            if (content) {
                iframe.setAttribute("srcdoc", content);
            }
        },
        legacy: function (iframe, content) {
            var jsUrl;
            if (!iframe || !iframe.getAttribute) {
                return;
            }
            if (!content) {
                content = iframe.getAttribute("srcdoc");
            } else {
                iframe.setAttribute("srcdoc", content);
            }
            if (content) {
                // The value returned by a script-targeted URL will be used as
                // the iFrame's content. Create such a URL which returns the
                // iFrame element's `srcdoc` attribute.
                jsUrl = "javascript: window.frameElement.getAttribute('srcdoc');";
                iframe.setAttribute("src", jsUrl);
                // Explicitly set the iFrame's window.location for
                // compatability with IE9, which does not react to changes in
                // the `src` attribute when it is a `javascript:` URL, for
                // some reason
                if (iframe.contentWindow) {
                    iframe.contentWindow.location = jsUrl;
                }
            }
        }
    };
    var srcDoc = window.srcDoc = {
        // Assume the best
        set: implementations.compliant,
        noConflict: function () {
            window.srcDoc = _srcDoc;
            return srcDoc;
        }
    };
    // If the browser supports srcdoc, no shimming is necessary
    if (isCompliant) {
        return;
    }
    srcDoc.set = implementations.legacy;
    // Automatically shim any iframes already present in the document
    iframes = document.getElementsByTagName("iframe");
    idx = iframes.length;
    while (idx--) {
        srcDoc.set(iframes[idx]);
    }
}(this, this.document));
