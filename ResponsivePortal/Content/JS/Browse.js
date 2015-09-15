
$(document).ready(function () {
    var clientId = document.getElementById("clientId").value;
    var portalId = document.getElementById("portalId").value;
    var catId = document.getElementById("LastcategoryIdSelected").value;
    var pcatId = document.getElementById("ParentcategorySelected").value;
    
    $('#catSearchButton').click(function (e) {
        var value = $('#catSearchTextBox').val();
        if (value.length > 0) {
            //value = htmlEncode(value);
            url = "/Browse/categoryBrowse/" + clientId + "/" + portalId + "?catId=" + catId + "&searchText=" + value+"&pcatId="+pcatId +"&search=true";
            window.location.href = url;
        }
    }); 
   
    function htmlEncode(value) {
        //create a in-memory div, set it's inner text(which jQuery automatically encodes)
        //then grab the encoded contents back out.  The div never exists on the page.
        return $('<div/>').text(value).html();
    }
    
    $('#catSearchTextBox').on("keypress", function (e) {
        if (e.keyCode == 13) {
            var value = $('#catSearchTextBox').val();
            if (value.length > 0) {
                //value = htmlEncode(value);
                url = "/Browse/categoryBrowse/" + clientId + "/" + portalId + "?catId=" + catId + "&searchText=" + value + "&pcatId=" + pcatId + "&search=true";
                window.location.href = url;
            }
            return false; // prevent the button click from happening.
        }
    });

    if (document.getElementById("leftcategoryBrowseWrapper").childNodes.length < 3) {
        $('#rightcategoryBrowseWrapper').removeClass("large-9 columns");
        $('#rightcategoryBrowseWrapper').addClass("large-12 columns");
    }


    var parentWidth = document.getElementById('SearchBoxWrapper').offsetWidth;
    $('#catSearchTextBox').css("max-width", parentWidth - 80 + "px");
         
});
   
$(window).resize(function () {
    var parentWidth = document.getElementById('SearchBoxWrapper').offsetWidth;
    $('#catSearchTextBox').css("max-width", parentWidth - 80 + "px");
});