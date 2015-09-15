$(function () {
    var clientId = document.getElementById("clientId").value;
    var portalId = document.getElementById("portalId").value;
    var title = $('#title').val();
    var lastviewed = $('#lastviewed').val();
    var deletemsg = $('#delmsg').val();
    var deletetitle = $('#deltitle').val();
    var cancelbtntxt = $('#cancelbtn').val();
    var deletebtntxt = $('#delbtn').val();
    var totalviews = $('#totalviews').val();
    var action = $('#action').val();
    var recordcount = 0;
    jQuery("#grid").jqGrid({
        url: "/Favorites/BindGridData/" + clientId + "/" + portalId,
        datatype: "json",
        colNames: ['ArticleId', title, 'FavoriteOrder', 'LikeDate', lastviewed, totalviews, action],
        colModel: [
                    {
                        name: 'ArticleId', index: 'ArticleId', hidden: true, sortable: true,
                        editable: true, editoptions: { dataInit: ShowHint },
                        description: 'ArticleId tooltip goes here'
                    },
                    {
                        name: 'ArticleTitle', index: 'ArticleTitle', sortable: true, width: 250, 
                        editable: true, editoptions: { dataInit: ShowHint }, formatter: ArticlelinkFormat,
                        description: 'ArticleTitle tooltip goes here'
                    },
                    {
                        name: 'FavoriteOrder', index: 'FavoriteOrder', hidden: true, sortable: true,
                        editable: true, editoptions: { dataInit: ShowHint },
                        description: 'FavoriteOrder tooltip goes here'
                    },

                    {
                        name: 'ViewDate', index: 'ViewDate', hidden: true, sortable: true,
                        editable: true, editoptions: { dataInit: ShowHint },
                        description: 'ViewDate tooltip goes here'
                    },
                    {
                        name: 'LikeDate', index: 'LikeDate', sortable: true, width: 105, 
                        editable: true, editoptions: { dataInit: ShowHint }, formatter: CustomizeViewDate,
                        description: 'LikeDate tooltip goes here'
                    },
                    {
                        name: 'ViewCount', index: 'ViewCount', sortable: true, width: 105,align:"center",sorttype:'int',
                        editable: true, editoptions: { dataInit: ShowHint },
                        description: 'ViewCount tooltip goes here'
                    },

                    {
                        name: 'ActionColumn', sortable: false, width: 105, 
                        editable: false,
                        formatter: 'actions',

                        formatoptions: {
                            editbutton: false,

                             delOptions: {
                                
                                caption: deletetitle,
                                msg: deletemsg,
                                bSubmit: deletebtntxt,
                                bCancel: cancelbtntxt,
                                url: "/Favorites/DeleteFavorite/" + clientId + "/" + portalId,
                                
                                onclickSubmit: function (rp_ge, rowid) {
                                    
                                     var rowdata = jQuery('#grid').getRowData(rowid);
                                     rp_ge.url = "/Favorites/DeleteFavorite/" + clientId + "/" + portalId + "?" + jQuery.param({ articleid: rowdata.ArticleId });
                                    // alert(rp_ge.url + 'rp_ge.url ');
                                }
                            }
                        },
                        description: 'Delete Favorite'
                    }

        ],

        loadonce: true,
        rowList: [10, 20, 30],
        sortname: 'FavoriteOrder',
        sortable: true,
        viewrecords: true,
        scrollOffset: 0,
        sortorder: "desc",
        width:900,
        loadComplete: function (data) {
            var favoriteModifiedData = [];
            var i, rows = data.rows, l = rows.length;
            recordcount = rows.length;
            for (i = 0; i < l; i++) {
                if (typeof rows[i].id === "undefined") {
                    favoriteModifiedData[i] = rows[i]._id_;
                }
                else {
                    favoriteModifiedData[i] = rows[i].id;
                }
            }
            $.ajax({
                url: "/Favorites/SetFavoriteOrder/" + clientId + "/" + portalId,
                data: JSON.stringify({ "favoriteList": favoriteModifiedData }),
                contentType: "application/json; charset=utf-8",
                type: "POST",
                traditional: true,
                dataType: 'json'              
            });
        }
    });
    // jQuery("#grid").jqGrid('navGrid', '#pager', { edit: false, add: false, del: true });
    function ArticlelinkFormat(cellvalue, options, rowObject) {
        //s debugger;

        if (jQuery.isArray(rowObject)) {
            var artId = rowObject[0];
        }
        else {
            var artId = rowObject.ArticleId;
        }
        var arttitle = cellvalue;
        arttitle = arttitle.length < 34 ? arttitle : arttitle.substring(0, 34) + '...';
        //alert("id:" + artId + "arttitle:" + arttitle)
        return "<a href=" + "/Article/Index/" + clientId + "/" + portalId + "?id=" + artId + ">" + arttitle + "</a>";

    }
    function CustomizeViewDate(cellvalue, options, rowObject) {
        // debugger;
        var dateView = new Date(cellvalue);
        dateView.setHours(0, 0, 0, 0);
        var dateNow = new Date();
        dateNow.setHours(0, 0, 0, 0);
        var one_day = 1000 * 60 * 60 * 24;
        var retdate = cellvalue;
        if (Math.round((dateNow - dateView) / one_day) == 0) {
            //return String.format('{0}', $KB.LZ('Favorite_Today', 'Today'));
            retdate = "Today";
        }
        else if (Math.round((dateNow - dateView) / one_day) == 1) {
            // return String.format('{0}', $KB.LZ('Favorite_Yesterday', 'Yesterday'));
            retdate = "Yesterday";
        }
        return retdate;
    }

    jQuery("#grid").jqGrid('sortableRows', {
        update: function (e, ui) {           
            $.ajax({
                url: "/Favorites/UpdateOrder/" + clientId + "/" + portalId,
                data: { rowindex: ui.item.context.rowIndex, rowid: ui.item[0].id },
                contentType: "application/json; charset=utf-8",
                dataType: 'json',
                success: function (data) {                
                }
            });
        },

    });

});

