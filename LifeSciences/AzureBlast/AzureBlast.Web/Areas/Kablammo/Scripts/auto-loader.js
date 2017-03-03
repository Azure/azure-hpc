"use strict";

function Interface(grapher, loader) {
    this._grapher = grapher;
    this._loader = loader;
    this._configure_hsp_outline_controls();

    this._searchId = $("#Id").val();
    this._filename = $("#Filename").val();
    this._queryId = $("#QueryId").val();

    // make sure we have everything we need.
    if (!this._searchId || !this._queryId || !this._filename) {
        Interface.error("a required parameter is missing. Id: '" + this._searchId + "', QueryId: '" + this._queryId + "', Filename: '" + this._filename + "'");
    } else {
        this.autoload_and_parse_results(this._searchId, this._queryId, this._filename);
    }
}

Interface.prototype._configure_hsp_outline_controls = function () {
    var get_grapher = function (elem) {
        var container = $(elem).parents('.subject');
        return container[0]._grapher;
    }

    $('#results-container').on('click', '.view-alignment', function () {
        get_grapher(this).view_alignments();
    });

    $('#results-container').on('click', '.export-alignment', function () {
        get_grapher(this).export_alignments();
    });

    $('#results-container').on('click', '.deselect-all-hsps', function () {
        get_grapher(this).deselect_all_alignments();
    });

    $('#results-container').on('change', '.toggle-hsp-outline', function () {
        var grapher = get_grapher(this);
        if (this.checked) {
            grapher.enable_hsp_outlines();
        } else {
            grapher.disable_hsp_outlines();
        }
    });
}

Interface.prototype.create_query_header = function (container, label, query_index, num_filtered_queries, num_hidden_queries) {
    // Don't show label if no valid one present.
    if (label === 'No definition line') {
        label = '';
    }

    var header = $('#example-query-header').clone().removeAttr('id');
    header.find('.query-name').text(label);

    var count_label = 'Query ' + query_index + ' of ' + num_filtered_queries;
    if (num_hidden_queries > 0) {
        count_label += ' (' + num_hidden_queries + ' hidden)';
    }

    header.find('.query-index').text(count_label);
    $(container).append(header);
}

Interface.prototype.autoload_and_parse_results = function (search_id, query_id, blast_filename, on_complete) {
    var self = this;
    Interface.show_curtain(function () {
        self._loader.load_from_api(search_id, query_id, blast_filename, function (results) {
            self._display_results(results, on_complete);
        });
    });
}

Interface.error = function (msg) {
    // Hide curtain in case it is showing, which would obscure error.
    Interface.hide_curtain();

    var container = $('#errors');
    var error = $('#example-error').clone().removeAttr('id');
    error.find('.message').text(msg);
    container.append(error);
}

Interface.show_curtain = function (on_done) {
    $('#curtain').fadeIn(500, on_done);
}

Interface.hide_curtain = function (on_done) {
    $('#curtain').fadeOut(500, on_done);
}

Interface.prototype._display_results = function (results, on_complete) {
    this._grapher.display_blast_results(results, '#results-container', this);
    Interface.hide_curtain();
    $('html, body').scrollTop(0);

    if (typeof on_complete !== 'undefined') {
        on_complete();
    }
}
