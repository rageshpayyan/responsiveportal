if (!window.Controls) window.Controls = {};

//=========================== LIST Control ==================================
Controls.ResizeHelper = function (maxWidth, minWidth, maxImageWidth, prefix, imagePrefix, tabContainerPrefix, mainContainer) {
    this.g_screenWidth = 0.0;
    this.g_maxWidth = maxWidth;
    this.g_maxImageWidth = maxImageWidth;
    this.g_minWidth = minWidth;
    this.g_prefix = prefix;
    this.g_imagePrefix = imagePrefix;
    this.g_tabContainerPrefix = tabContainerPrefix;
    this.g_mainContainer = mainContainer;
}
Controls.ResizeHelper.prototype =
{
    dispose: function () {
        //this.root = null;
    },
    ProcessScreenChange: function () {
        if (this.g_screenWidth == window.innerWidth) {
            return;
        }
        this.g_screenWidth = window.innerWidth;
        var mainContainer = $("#" + this.g_mainContainer);
        var children = mainContainer.children();
        var itemsId = '[oid="' + this.g_prefix + '"]';
        var count = $(itemsId).length;
        if (count > 0) {
            var itemWidth = mainContainer.width() / count;
            var actWidth = itemWidth;
            if (itemWidth > this.g_maxWidth) {
                actWidth = this.g_maxWidth;
            }
            if (itemWidth < this.g_minWidth) {
                actWidth = this.g_minWidth;
            }
            $(itemsId).each(function () {
                $(this).width(actWidth);
            });
            var imageId = '[oid="' + this.g_imagePrefix + '"]';
            var imageWidth = actWidth - 20;
            if (imageWidth > this.g_maxImageWidth)
            {
                imageWidth = this.g_maxImageWidth;
            }
            $(imageId).each(function () {
                $(this).width(imageWidth);
            });
            var maxHeight = 0.0;
            $(imageId).each(function () {
                if($(this).height() > maxHeight)
                {
                    maxHeight = $(this).height();
                }
            });
            maxHeight += 20.0;
            $('[oid="' + this.g_tabContainerPrefix + '"]').each(function () {
                $(this).height(maxHeight);
            });
        }
    }
}

//--------------------------- Update footer position -----------------------------
Controls.LocateFooterHelper = function () {
    this.g_screenWidth = 0.0;
    this.g_footerHeight = 0.0;
}
Controls.LocateFooterHelper.prototype =
{
    dispose: function () {
        //this.root = null;
    },
    ProcessScreenChange: function () {
        if (this.g_screenHeight == window.innerHeight) {
            return;
        }
        this.g_screenHeight = window.innerHeight;
        if (this.g_footerHeight == 0.0) {
            this.g_footerHeight = $("#footerContainer").height();
        }
        var top = $("#offsetDiv").offset().top;
        if (top + this.g_footerHeight < this.g_screenHeight) {
            $("#offsetDiv").height(this.g_screenHeight - top - this.g_footerHeight);
        }
        else {
            $("#offsetDiv").height(0);
        }
    }
}