﻿(function ($, document) {

    var view = "Backdrop";

    // The base query options
    var query = {

        SortBy: "SortName",
        SortOrder: "Ascending",
        IncludeItemTypes: "Series",
        Recursive: true,
        Fields: "PrimaryImageAspectRatio,DisplayMediaType,SeriesInfo,ItemCounts,DateCreated,UserData",
        Limit: LibraryBrowser.getDetaultPageSize(),
        StartIndex: 0
    };

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getItems(Dashboard.getCurrentUserId(), query).done(function (result) {

            var html = '';

            var showPaging = result.TotalRecordCount > query.Limit;

            if (showPaging) {
                html += LibraryBrowser.getPagingHtml(query, result.TotalRecordCount, true);
            }

            if (view == "Backdrop") {
                html += LibraryBrowser.getPosterDetailViewHtml({
                    items: result.Items,
                    useAverageAspectRatio: true,
                    preferBackdrop: true
                });
            }
            else if (view == "Poster") {
                html += LibraryBrowser.getPosterDetailViewHtml({
                    items: result.Items,
                    useAverageAspectRatio: true
                });
            }

            if (showPaging) {
                html += LibraryBrowser.getPagingHtml(query, result.TotalRecordCount);
            }

            var elem = $('#items', page).html(html).trigger('create');

            $('select', elem).on('change', function () {
                query.StartIndex = (parseInt(this.value) - 1) * query.Limit;
                reloadItems(page);
            });

            $('.btnNextPage', elem).on('click', function () {
                query.StartIndex += query.Limit;
                reloadItems(page);
            });

            $('.btnPreviousPage', elem).on('click', function () {
                query.StartIndex -= query.Limit;
                reloadItems(page);
            });

            Dashboard.hideLoadingMsg();
        });
    }

    $(document).on('pageinit', "#tvShowsPage", function () {

        var page = this;

        $('.radioSortBy', this).on('click', function () {
            query.SortBy = this.getAttribute('data-sortby');
            query.StartIndex = 0;
            reloadItems(page);
        });

        $('.radioSortOrder', this).on('click', function () {
            query.SortOrder = this.getAttribute('data-sortorder');
            query.StartIndex = 0;
            reloadItems(page);
        });

        $('.chkStandardFilter', this).on('change', function () {

            var filterName = this.getAttribute('data-filter');
            var filters = query.Filters || "";

            filters = (',' + filters).replace(',' + filterName, '').substring(1);

            if (this.checked) {
                filters = filters ? (filters + ',' + filterName) : filterName;
            }

            query.Filters = filters;
            query.StartIndex = 0;
            reloadItems(page);
        });

        $('.chkStatus', this).on('change', function () {

            var filterName = this.getAttribute('data-filter');
            var filters = query.SeriesStatus || "";

            filters = (',' + filters).replace(',' + filterName, '').substring(1);

            if (this.checked) {
                filters = filters ? (filters + ',' + filterName) : filterName;
            }

            query.SeriesStatus = filters;
            query.StartIndex = 0;
            reloadItems(page);
        });

        $('.chkAirDays', this).on('change', function () {

            var filterName = this.getAttribute('data-filter');
            var filters = query.AirDays || "";

            filters = (',' + filters).replace(',' + filterName, '').substring(1);

            if (this.checked) {
                filters = filters ? (filters + ',' + filterName) : filterName;
            }

            query.AirDays = filters;
            query.StartIndex = 0;
            reloadItems(page);
        });

        $('#selectView', this).on('change', function () {

            view = this.value;

            reloadItems(page);
        });

    }).on('pagebeforeshow', "#tvShowsPage", function () {

        reloadItems(this);

    }).on('pageshow', "#tvShowsPage", function () {

        // Reset form values using the last used query
        $('.radioSortBy', this).each(function () {

            this.checked = query.SortBy == this.getAttribute('data-sortby');

        }).checkboxradio('refresh');

        $('.chkStatus', this).each(function () {

            var filters = "," + (query.SeriesStatus || "");
            var filterName = this.getAttribute('data-filter');

            this.checked = filters.indexOf(',' + filterName) != -1;

        }).checkboxradio('refresh');

        $('.radioSortOrder', this).each(function () {

            this.checked = query.SortOrder == this.getAttribute('data-sortorder');

        }).checkboxradio('refresh');

        $('.chkStandardFilter', this).each(function () {

            var filters = "," + (query.Filters || "");
            var filterName = this.getAttribute('data-filter');

            this.checked = filters.indexOf(',' + filterName) != -1;

        }).checkboxradio('refresh');

        $('.chkAirDays', this).each(function () {

            var filters = "," + (query.AirDays || "");
            var filterName = this.getAttribute('data-filter');

            this.checked = filters.indexOf(',' + filterName) != -1;

        }).checkboxradio('refresh');

        $('#selectView', this).val(view).selectmenu('refresh');
    });

})(jQuery, document);