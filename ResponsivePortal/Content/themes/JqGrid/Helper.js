

// this function show tool tip dialog for a certain element of jqgrid and assumes that jqgrid name is #grid
function ShowHint(elem) {
    var selector = '';
 
    if (this.gridName != null)
        selector = '#' + this.gridName;
    else if ($('#grid') != null) {
        selector = '#grid';

    }

    if (selector == '') {
        alert('jqgrid name is not "grid" or gridName "Edit Option" not set');
        return;

    }

    if (elem == 0)
        return;

    jQuery(elem).qtip({
        content: getColDescription(this.name, selector),
        show: 'focusin',
        hide: 'focusout',
        style:
    {

        name: 'red',
        tip: 'leftTop',
        textAlign: 'left',
        fontWeight: '500',
        fontSize: '11px'

    },
        position:
    {
        corner:
        {
            target: 'rightTop',
            tooltip: 'LeftTop'

        }
    }
    });
}

function getColDescription(colName, jqGridSelector) {
    var description = '';
    if (colName != '') {
        var colModel = $(jqGridSelector).getGridParam('colModel');
        $.each(colModel, function (index, model) {
            if (model.name == colName) {
                description = model.description;
                return false;
            }

        });
    }


    return description;
}

