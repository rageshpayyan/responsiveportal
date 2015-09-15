
$(document).ready(function () {
    var clientId = document.getElementById("clientId").value;
    var portalId = document.getElementById("portalId").value;
    $('#searchbutton').click(function (e) {
        var value = $('#searchtextbox').val();
        if (value.length > 0) {
            //value = htmlEncode(value);
            url = "/Search/GetSearch/" + clientId + "/" + portalId + "?searchFrom=SearchPage&text=" + value;
            window.location.href = url;
        }
    });

    $('#searchtextbox').autocomplete({
        source: function (pattern, response) {
            if (pattern.term.length < 3 || pattern.term.length > 20) {
                //don't do anything
                return;
            }
            var fullurl = "/Search/GetAutoCompletes/" + clientId + "/" + portalId
            $.ajax({
                type: 'Get',
                url: fullurl,
                dataType: 'json',
                data: { "pattern": pattern.term, "clientId": clientId, "portalId": portalId },
                success: function (data) {
                    response(data);
                }
            });
        }
    });

    $('#searchtextbox').on("keypress", function (e) {
        if (e.keyCode == 13) {
            var value = $('#searchtextbox').val();
            if (value.length > 0) {
               // value = htmlEncode(value);
                url = "/Search/GetSearch/" + clientId + "/" + portalId + "?searchFrom=SearchPage&fromwidget=false&text=" + value;
                window.location.href = url;
            }
            return false; // prevent the button click from happening
        }
    });

    function htmlEncode(value) {
        //create a in-memory div, set it's inner text(which jQuery automatically encodes)
        //then grab the encoded contents back out.  The div never exists on the page.
        return $('<div/>').text(value).html();
    }
    $(window).resize(function () {
        $("#searchtextbox").autocomplete("search");
    });
    if (document.getElementById("searchfilterswrapper").childElementCount < 2) {
        $('#searchcontentwrapper').removeClass("large-9 columns");
        $('#searchcontentwrapper').addClass("large-12 columns");
    }

    var parentWidth = document.getElementById('searchboxwrapper').offsetWidth;
    $('#searchtextbox').css("max-width", parentWidth - 80 + "px");
});

$(window).resize(function () {
    var parentWidth = document.getElementById('searchboxwrapper').offsetWidth;
    $('#searchtextbox').css("max-width", parentWidth - 80 + "px");
});