﻿(function ($, document, LibraryBrowser, window) {

    var currentItem;

    function reload(page) {

        var id = getParameterByName('id');

        Dashboard.showLoadingMsg();

        ApiClient.getItem(Dashboard.getCurrentUserId(), id).done(function (item) {

            currentItem = item;

            var name = item.Name;

            $('#itemImage', page).html(LibraryBrowser.getDetailImageHtml(item));

            Dashboard.setPageTitle(name);

            $('#itemName', page).html(name);
            $('#seriesName', page).html('<a class="detailPageParentLink" href="tvseries.html?id=' + item.SeriesId + '">' + item.SeriesName + '</a>').trigger('create');

            setInitialCollapsibleState(page, item);
            renderDetails(page, item);

            if (LibraryBrowser.shouldDisplayGallery(item)) {
                $('#galleryCollapsible', page).show();
            } else {
                $('#galleryCollapsible', page).hide();
            }

            Dashboard.hideLoadingMsg();
        });
    }

    function setInitialCollapsibleState(page, item) {

        if (!item.People || !item.People.length) {
            $('#castCollapsible', page).hide();
        } else {
            $('#castCollapsible', page).show();
        }
    }

    function renderDetails(page, item) {

        if (item.Taglines && item.Taglines.length) {
            $('#itemTagline', page).html(item.Taglines[0]).show();
        } else {
            $('#itemTagline', page).hide();
        }

        if (item.Overview || item.OverviewHtml) {
            var overview = item.OverviewHtml || item.Overview;

            $('#itemOverview', page).html(overview).show();
            $('#itemOverview a').each(function () {
                $(this).attr("target", "_blank");
            });
        } else {
            $('#itemOverview', page).hide();
        }

        if (item.CommunityRating) {
            $('#itemCommunityRating', page).html(LibraryBrowser.getStarRatingHtml(item)).show().attr('title', item.CommunityRating);
        } else {
            $('#itemCommunityRating', page).hide();
        }

        $('#itemMiscInfo', page).html(LibraryBrowser.getMiscInfoHtml(item));

        LibraryBrowser.renderGenres($('#itemGenres', page), item);
        LibraryBrowser.renderStudios($('#itemStudios', page), item);
        renderUserDataIcons(page, item);
        LibraryBrowser.renderLinks($('#itemLinks', page), item);
    }

    function renderUserDataIcons(page, item) {
        $('#itemRatings', page).html(LibraryBrowser.getUserDataIconsHtml(item));
    }

    function renderSeasons(page) {

        ApiClient.getItems(Dashboard.getCurrentUserId(), {

            ParentId: getParameterByName('id'),
            SortBy: "SortName",
            Fields: "PrimaryImageAspectRatio,ItemCounts,DisplayMediaType,DateCreated,UserData"

        }).done(function (result) {

            var html = LibraryBrowser.getPosterDetailViewHtml({
                items: result.Items,
                useAverageAspectRatio: true
            });

            $('#episodesContent', page).html(html);
        });
    }

    function renderGallery(page, item) {

        var html = LibraryBrowser.getGalleryHtml(item);

        $('#galleryContent', page).html(html).trigger('create');
    }

    function renderCast(page, item) {
        var html = '';

        var casts = item.People || [];

        for (var i = 0, length = casts.length; i < length; i++) {

            var cast = casts[i];

            html += LibraryBrowser.createCastImage(cast);
        }

        $('#castContent', page).html(html);
    }

    $(document).on('pageshow', "#tvSeasonPage", function () {

        var page = this;

        reload(page);

        $('#episodesCollapsible', page).on('expand.lazyload', function () {

            renderSeasons(page);

            $(this).off('expand.lazyload');
        });

        $('#castCollapsible', page).on('expand.lazyload', function () {
            renderCast(page, currentItem);

            $(this).off('expand.lazyload');
        });

        $('#galleryCollapsible', page).on('expand.lazyload', function () {

            renderGallery(page, currentItem);

            $(this).off('expand.lazyload');
        });

    }).on('pagehide', "#tvSeasonPage", function () {

        currentItem = null;
        var page = this;

        $('#episodesCollapsible', page).off('expand.lazyload');
        $('#castCollapsible', page).off('expand.lazyload');
        $('#galleryCollapsible', page).off('expand.lazyload');
    });

})(jQuery, document, LibraryBrowser, window);
