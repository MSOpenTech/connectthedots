//  ---------------------------------------------------------------------------------
//  Copyright (c) Microsoft Open Technologies, Inc.  All rights reserved.
// 
//  The MIT License (MIT)
// 
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
// 
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
// 
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.
//  ---------------------------------------------------------------------------------

var dataFlows = {};
var bulkMode = false;

function clearData() {
    for (var id in dataFlows) {
        if (id == 'dataSource') continue;
        if (dataFlows[id].hasOwnProperty('flows')) {
            for (var id2 in dataFlows[id].flows) {
                dataFlows[id].flows[id2].destroy();
                dataFlows[id].flows[id2] = null;
            }
        }
        if (dataFlows[id].hasOwnProperty('chart')) {
            dataFlows[id].chart.destroy();
            dataFlows[id].chart = null;
        }
    }
    dataFlows = { dataSource: dataFlows.dataSource };

    $('#chartsContainer').empty();
    $('#chartsContainer').height(0);
}

function onChangeSensors(isAll) {
    var newGUIDs = [];

    dataFlows.dataSource.onUpdating(true);

    $('#sensorList li').each(function () {
        if ($(this).hasClass('selected') && this.id) {
            if (!isAll) newGUIDs.push(this.id.slice(4));
        } else
            if (isAll) {
                $(this).addClass('selected');
            }
    });
    dataFlows.dataSource.changeDeviceGUIDs(isAll ? ['All'] : newGUIDs);

    clearData();

    dataFlows.dataSource.onUpdating(false);
}

function onLoading(evt) {
    $('#loading').show();
    if (evt.owner) {
        $('#loading-sensor').text(evt.owner);
    }
}

function onLoaded(evt) {
    $('#loading').hide();
}

function onError(evt) {
    $('#messages').prepend('<div>ERROR ' + evt.owner + '</div>');
}

function onOpen(evt) {
    $('#messages').prepend('<div>Connected.</div>');
}

function addNewDataFlow(eventObject) {
    var measurename = eventObject['measurename'];
    // create chart if necessary
    if (!dataFlows.hasOwnProperty(measurename)) {
        dataFlows[measurename] = {
            containerId: 'chart_' + measurename,
            controllerId: 'controller_' + measurename,
            dataSourceFilter: new d3CTDDataSourceFilter(dataFlows.dataSource, { measurename: measurename }),
            flows: {}
        };
        // create flows controller
        $('#controllersContainer').append('<ul id="' + dataFlows[measurename].controllerId + '" style="top: ' + (Object.keys(dataFlows).length - 2) * 300 + 'px;" class="controller"></ul>');
        dataFlows[measurename].controller = new d3ChartControl(dataFlows[measurename].controllerId)
                    .attachToDataSource(dataFlows[measurename].dataSourceFilter);

        // add new div object
        $('#chartsContainer').height((Object.keys(dataFlows).length - 1) * 300 + 'px');
        $('#chartsContainer').append('<div id="' + dataFlows[measurename].containerId + '" style="top: ' + (Object.keys(dataFlows).length - 2) * 300 + 'px;" class="chart"></div>');
        // create chart
        dataFlows[measurename].chart = (new d3Chart(dataFlows[measurename].containerId))
                    .addEventListeners({ 'loading': onLoading, 'loaded': onLoaded })
                    .attachToDataSource(dataFlows[measurename].dataSourceFilter)
                    .setFilter(dataFlows[measurename].controller)
                    .setBulkMode(bulkMode);

    };

    // add new flow
    var newFlow = new d3DataFlow(eventObject.guid);

    //addNewSensorOption(newFlow, eventObject);

    dataFlows[measurename].flows[eventObject.guid] = newFlow;

    dataFlows[measurename].chart.addFlow(newFlow, 0);

    $(window).resize();
}

function addNewSensorOption(newFlow, eventObject) {
    var found = false;

    for (var id in dataFlows) {
        if (dataFlows[id].hasOwnProperty('flows')) {
            for (var id2 in dataFlows[id].flows) {
                if (id2 == eventObject.guid)
                    found = true;
            }
        }
    }
    if (!found) {
        // check old
        var oldOpt = document.getElementById('flow' + eventObject.guid);
        if (!oldOpt) {
            // add new
            $('#sensorList').append("<li id='flow" + eventObject.guid + "' class='selected'>loading...</li>");
        }

        document.getElementById('flow' + eventObject.guid)
            .onclick = function () {
                if ($(this).hasClass('selected')) {
                    $(this).removeClass('selected');
                } else {
                    $(this).addClass('selected');
                }

                onChangeSensors();
            };

        newFlow.addEventListener('change', function (evt) {
            document.getElementById('flow' + eventObject.guid).innerHTML = evt.owner.displayName();
        });
    }
}

function checkBulkMode(evt) {
    if (evt.bulkData != undefined) {
        bulkMode = evt.bulkData;

        // alert all charts
        for (var id in dataFlows) {
            if (dataFlows[id].chart)
                dataFlows[id].chart.setBulkMode(bulkMode);
        }
    }
}
function onNewEvent(evt) {
    var eventObject = evt.owner;
    var flowCnt = dataFlows.length;

    // check bulk mode
    checkBulkMode(eventObject);

    // check object necessary properties
    if (!eventObject.hasOwnProperty('guid') || !eventObject.hasOwnProperty('measurename')) return;

    // auto add flows
    if (!dataFlows.hasOwnProperty(eventObject['measurename']) || !dataFlows[eventObject['measurename']].flows.hasOwnProperty(eventObject['guid']))
        addNewDataFlow(eventObject);

    if (eventObject.alerttype != null) {
        var table = $('#alertTable').DataTable();
        var time = new Date(eventObject.timecreated);

        // Check if we already have this one in the table already to prevent duplicates
        var indexes = table.rows().eq(0).filter(function (rowIdx) {
            if (
                table.cell(rowIdx, 0).data().getTime() == time.getTime() && table.cell(rowIdx, 1).data() == eventObject.displayname && table.cell(rowIdx, 2).data() == eventObject.alerttype) {
                return true;
            }
            return false;
        });

        // The alert is a new one, lets display it
        if (indexes.length == 0) {
            // For performance reasons, we want to limit the number of items in the table to a max of 20. 
            // We will remove the oldest from the list
            if (table.data().length > 19) {
                // Search for the oldest time in the list of alerts
                var minTime = table.data().sort(

                    function (a, b) {
                        return (a[0] > b[0]) - (a[0] < b[0])
                    })[0][0];
                // Delete the oldest row
                table.rows(

                    function (idx, data, node) {
                        return data[0].getTime() == minTime.getTime();
                    }).remove();
            }

            // Add the new alert to the table
            var message = 'message';
            if (eventObject.message != null) message = eventObject.message;
            table.row.add([
                time,
                eventObject.displayname,
                eventObject.alerttype,
                message
            ]).draw();

        }
    }
}

//
// JQuery ready function
//

$(document).ready(function () {

    // create datasource
    var sss = (window.location.protocol.indexOf('s') > 0 ? "s" : "");
    var uri = 'ws' + sss + '://' + window.location.host + '/api/websocketconnect?clientId=none';

    $('#messages').prepend('<div> Connecting to ' + uri + '<div>');
    dataFlows.dataSource = new d3CTDDataSourceSocket(uri).addEventListeners({ 'eventObject': onNewEvent, 'error': onError, 'open': onOpen });

    $('#selectAllOpt').on('click', function () {
        onChangeSensors(true);
    });

    // create alerts table
    var table = $('#alertTable').DataTable({
        "bAutoWidth": false,
        "bFilter": true,
        "bInfo": true,
        "paging": true,
        "order": [
            [0, "desc"]
        ],
        "columnDefs": [{
            "targets": "timeFromDate",
            "data": function (row, type, val, meta) {
                if (type === 'set') {
                    row[meta.col] = val;
                    return;
                } else if (type === 'display') {
                    return row[meta.col].toLocaleTimeString();
                }
                return row[meta.col];
            }
        }, {
            "targets": "numberFixed",
            "data": function (row, type, val, meta) {
                if (type === 'set') {
                    row[meta.col] = val;
                    return;
                } else if (type === 'display') {
                    return row[meta.col].toFixed(1);
                }
                return row[meta.col];
            }
        }, ]
    });
});