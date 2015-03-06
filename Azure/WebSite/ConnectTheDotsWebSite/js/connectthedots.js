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

//var updateChart = true;
var websocket;
var isBulking = false;
var receivedFirstMessage = false;

// constants

var MAX_ARRAY_SIZE = 1000;

var MS_MIN_INTERVAL = 100;
var MS_PER_MINUTE = 60000;
var WINDOW_MINUTES = 10;

// globals

var D3_tmp = [];
var D3_hum = [];

// keep track of absolute freshest sample point

var freshestTime = [];
freshestTime["Temperature"] = null;
freshestTime["Humidity"] = null;

// globals used with D3

var line = null;
var path = {};
var legend = {};
var legend_r = {};
var x = null;
var y = null;
var height = {};
var width = {};
var svg = {};
var color = null;
var sensorNames = [];

// initialize color

sensorNames.push("avg")
color = d3.scale.category10();
color.domain(sensorNames);

// worker-related

var sss = null;

// Create the chart(s) that will be used to
// display the live data.  We use the D3.js
// library to establish SVG elements that will
// do the rendering

function chart(chart_name) {

    var margin = { top: 30, right: 200, bottom: 20, left: 50 };

    width = 800 - margin.right;
    height = 300 - margin.top - margin.bottom;

    // seed the axes with some dummy values

    x = d3.time.scale()
        .domain([new Date("2015-01-01T04:02:39.867841Z"), new Date("2015-01-01T04:07:39.867841Z")])
        .range([0, width]);

    y = d3.scale.linear()
        .domain([73.8, 74.2])
        .range([height, 0]);


    svg[chart_name] = d3.select("#" + chart_name)
        .append("p")
        .append("svg")
            .attr("width", width + margin.left + margin.right)
            .attr("height", height + margin.top + margin.bottom)
            .style("margin-left", margin.left + "px")
            .style("margin-bottom", margin.bottom + "px")
            .style("margin-right", margin.right + "px")
        .append("g")
            .attr("transform", "translate(" + margin.left + "," + margin.top + ")");


    svg[chart_name].append("text")
        .attr("x", (width / 2))
        .attr("y", 0 - (margin.top / 2))
        .attr("text-anchor", "middle")
        .style("font", "16px sans-serif")
        .text(chart_name);

    svg[chart_name].append("g")
        .attr("class", "y axis")
        .call(d3.svg.axis().scale(y).ticks(7).orient("left"));

    svg[chart_name].append("g")
        .attr("class", "x axis")
        .attr("transform", "translate(0," + (height) + ")")
        .call(d3.svg.axis().scale(x).orient("bottom").tickFormat(d3.time.format("%X")));

    path[chart_name] = {};
    legend[chart_name] = {};
    legend_r[chart_name] = {};
}

//
// MakeNewD3Series
//
// create a new D3 data series
//
//  each object in the array looks like this:
//
//  {
//      name: "<series name>"
//      data: [{
//                  datapoint (float)
//                  timestamp (Date object)
//            }],...N-1
//  }
//

function MakeNewD3Series(d3_data, series_name) {
    
    var obj = { name: series_name, data: [] };
    d3_data.push(obj);

    return obj;
}

//
// InsertNewDatapoint
//
// Push a new datapoint onto the queue.
// We want to keep the queue filled with WINDOW_MINUTES
// amount of data.  But we always keep it
// capped at MAX_ARRAY_SIZE number of items
//
// The datapoints come in out of order during
// bulk operations.  This necessitates a sorted
// insertion...which is NOT what we want here.
// Is there a way to get the bulk data to arrive
// sorted ??
//

function InsertNewDatapoint(data, time, val)
{
    var t = new Date(time);
    if (isNaN(t.getTime())) {
        //console.log("invalid date");
        return;
    }

   var now = new Date();
   var cutoff = new Date(now - WINDOW_MINUTES * MS_PER_MINUTE)

   if (t < cutoff) {
        //console.log("too old");
       return;
   }

    data.push({ data: val, time: new Date(time) });

    if (data.length >= MAX_ARRAY_SIZE) {  //should never be greater, but...
        data.shift();
        return;
    }
}

//
// AddToD3
//
// Add a new datapoint to the appropriate
// dataset.
//

function AddToD3(D3_set, chart_name, series_name, val, time) {

    var data = null;
    for (var i = 0; i < D3_set.length; i++) {
        if (D3_set[i].name == series_name) {
            data = D3_set[i].data;
            break;
        }
    }    

    if (data == null) {
        data = MakeNewD3Series(D3_set, series_name).data;        
    }

    // insert the new datapoint into the dataset

    InsertNewDatapoint(data, time, val);
}

//
// PruneOld3DData
//
//  Removes any datapoints that are older than 10 minutes.
//  If any datasets are completely empty afterwards, clear
//  that sensor entirely from the list.  This function is
//  needed to clear sensors that have gone offline and are
//  not producing any more data.
//

function PruneOldD3Data(D3_set, chart_name)
{    
    var now = new Date();
    var cutoff = new Date(now - WINDOW_MINUTES * MS_PER_MINUTE)

    console.log(now);
    console.log(cutoff);

    for (var i = 0; i < D3_set.length; i++) {
        var data = D3_set[i].data;        

        //for (var j = data.length - 1; j >= 0; j --){
        //    if (data[j].time < cutoff){
        //        data.splice(j,1);
        //    }
        //}

        while (data.length >= 1 && data[0].time < cutoff) {
            data.shift();
        }        
    }    

    var idxToRemove = [];

    for (var i = 0; i < D3_set.length ; i++) {

        if (D3_set[i].data.length == 0) {

            if (path[chart_name] == null) {
                continue;
            }

            if (path[chart_name][D3_set[i].name] != null) {
                path[chart_name][D3_set[i].name].remove();
                path[chart_name][D3_set[i].name] = null;
            }

            if (legend[chart_name][D3_set[i].name] != null) {
                legend[chart_name][D3_set[i].name].remove();
                legend[chart_name][D3_set[i].name] = null;
            }

            if (legend_r[chart_name][D3_set[i].name] != null) {
                legend_r[chart_name][D3_set[i].name].remove();
                legend_r[chart_name][D3_set[i].name] = null;
            }

            idxToRemove.push(i);
        }
    }

    for (var i = 0; i < idxToRemove.length; i++) {
        console.log("splicing");
        D3_set.splice(idxToRemove[i], 1);
    }
}

//
// UpdateD3Charts
//
//  Refreshes D3 charts with new data.  Rebinds data
//  arrays, recomputes ranges for x and y axes
//

function UpdateD3Charts(D3_set, chart_name)
{

    var minDate = new Date("3015-01-01T04:02:39.867841Z");
    var maxDate = new Date("1915-01-01T04:02:39.867841Z")

    var minVal = Number.MAX_VALUE;
    var maxVal = 0;

    for (var i = 0; i < D3_set.length; i++) {

        if (sensorNames.indexOf(D3_set[i].name) == -1) {
            sensorNames.push(D3_set[i].name);
        }        

        var data = D3_set[i].data;
        for (var j = 0; j < data.length; j++) {

            var c = data[j].data;
            var t = data[j].time;

            if (c < minVal) {
                minVal = c;
            }

            if (c > maxVal) {
                maxVal = c;
            }

            if (t > maxDate) {
                maxDate = t;
            }

            if (t < minDate) {
                minDate = t;
            }
        }
    }    

    color.domain(sensorNames);

    y = d3.scale.linear()
            .domain([minVal - 0.1, maxVal + 0.1])
            .range([height, 0]);

    var yAxis = d3.svg.axis()
                .scale(y)
                .orient("left")

    svg[chart_name].selectAll("g.y.axis")
        .call(yAxis);

    x = d3.time.scale()
        .domain([minDate, maxDate])
        .range([0, width]);

    var xAxis = d3.svg.axis()
                .scale(x)
                .tickFormat(d3.time.format("%X"))
                .orient("bottom");

    svg[chart_name].selectAll("g.x.axis")
            .call(xAxis);

    line = d3.svg.line()
            .interpolate("basis")
            .x(function (d) { return x(d.time); })
            .y(function (d) { return y(d.data); });    

    for (var i = 0; i < D3_set.length; i++) {

        try{

            var data = D3_set[i].data;
            var name = D3_set[i].name;

            if (path[chart_name][name] == null) {
                path[chart_name][name] = svg[chart_name].append("g")
                    .append("path")
                        .datum(data)
                        .attr("class", "line")
                        .attr("d", line)
                        .style("stroke", function (d) { return color(name); });

            }

            path[chart_name][name].datum(data)
                .attr("d", line);

            if (legend[chart_name][name] == null) {

                legend_r[chart_name][name] = svg[chart_name]
                        .append("rect")
                            .attr("class", "legend")
                            .attr("width", 10)
                            .attr("height", 10)
                            .attr("x", width + 10)
                            .attr("y", 20 + (20 * i))
                            .style("fill", color(name))
                            .style("stroke", color(name));

                legend[chart_name][name] = svg[chart_name].append("text")
                            .attr("x", width + 25)
                            .attr("y", 20 + (20 * i) + 5)
                            .attr("class", "legend")
                            .style("fill", color(name))
                            .text(name == 'avg' ? 'avg (of all sensors)' : name);
            }   
        }
        catch(e)
        {
            console.log(e);
        }
    }
}


function RemoveFromChart(chart_name, series_name) {

    path[chart_name][series_name].remove();
    path[chart_name][series_name] = null;

    legend[chart_name][series_name].remove();
    legend[chart_name][series_name] = null;

    legend_r[chart_name][series_name].remove();
    legend_r[chart_name][series_name] = null;
}

function ResetChart(chart_name)
{
    path[chart_name] = {};
    legend[chart_name] = {};
    legend_r[chart_name] = {};
}

//
// ClearD3Charts
//
//  Removes all graphical elements from the SVG objects
//  representing the charts.  Clears all arrays holding
//  chart data
//

function ClearD3Charts() {

    for (var i = 0; i < D3_tmp.length; i++) {
        RemoveFromChart("Temperature", D3_tmp[i].name);
    }

    ResetChart("Temperature");    

    for (var i = 0; i < D3_hum.length; i++) {
        RemoveFromChart("Humidity", D3_hum[i].name);
    }

    ResetChart("Humidity");

    D3_tmp = [];
    D3_hum = [];
}

//
// JQuery ready function
//

$(document).ready(function () {

    // 
    //  Handle a sensor selection change
    // 'All' means all dataset are shown.
    //  Anything else toggles that particular
    //  dataset
    //

    $('#sensorList').on('click', 'li', function () {
        var device = $(this).text();
        if (websocket != null) {            

            $('#loading').show();
            ClearD3Charts();

            if (device == 'All') {                

                var c = { MessageType: "LiveDataSelection", DeviceName: "clear" };
                websocket.send(JSON.stringify(x));

                var x = { MessageType: "LiveDataSelection", DeviceName: device };
                websocket.send(JSON.stringify(x));

                $('#sensorList li').each(function () {
                    //this now refers to each li
                    //do stuff to each

                    var d = $(this).text();

                    if (d == device) {
                        $(this).addClass('selected');
                        $(this).css('color', color(d == 'All' ? 'avg' : d));
                        $(this).css('font-weight', 'bold');
                    }
                    else {
                        $(this).removeClass('selected');
                        $(this).css('color', '');
                        $(this).css('font-weight', 'normal');
                    }
                });

            }
            else {

                // not 'All' so general case

                var j = $('#sensorList li').eq(0);
                j.removeClass('selected');
                j.css('color', '');
                j.css('font-weight', 'normal');

                var d = $(this).text();
                if ($(this).hasClass('selected')) {
                    $(this).removeClass('selected');
                    $(this).css('color', '');
                    $(this).css('font-weight', 'normal');
                }
                else {
                    $(this).addClass('selected');
                    $(this).css('color', color(d == 'All' ? 'avg' : d));
                    $(this).css('font-weight', 'bold');
                }

                var x = { MessageType: "LiveDataSelection", DeviceName: "clear" };
                websocket.send(JSON.stringify(x));

                $('#sensorList li').each(function () {

                    var d = $(this).text();

                    if ($(this).hasClass('selected')) {
                        var x = { MessageType: "LiveDataSelection", DeviceName: $(this).text() };
                        websocket.send(JSON.stringify(x));
                    }
                });
            }

        }
    });

    // 
    //  Mouseover: highlight the sensor with its color
    //  and make the text bold.
    //

    $('#sensorList').on('mouseover', 'li', function(e) {            
        var device = $(this).text();

        if (device == 'All') {
            device = 'avg';
        }

        if ($(this).hasClass('selected') == false)
        {
            $(this).css('color', color(device));
            $(this).css('font-weight', 'bold');
        }        
        
    }).on('mouseout', 'li', function (e) {
        if ($(this).hasClass('selected') == false) {
            $(this).css('color', '');
            $(this).css('font-weight', 'normal');
        }
    });
    
    $('#loading').hide();

    //
    // Set up jQuery DataTable to show alerts
    //

    var table = $('#alertTable').DataTable(
    {
        "bAutoWidth": false,
        "columnDefs":
        [
            {
                "targets": "timeFromDate",
                "data":
                    function (row, type, val, meta) {
                        if (type === 'set') {
                            row[meta.col] = val;
                            return;
                        }
                        else if (type === 'display') {
                            return row[meta.col].toLocaleTimeString();
                        }
                        return row[meta.col];
                    }
            },
            {
                "targets": "numberFixed",
                "data":
                    function (row, type, val, meta) {
                        if (type === 'set') {
                            row[meta.col] = val;
                            return;
                        }
                        else if (type === 'display') {
                            return row[meta.col].toFixed(1);
                        }
                        return row[meta.col];
                    }
            },
        ]
    });

    table.order([0, 'desc']);

    // Set up websocket client
    var sss = (window.location.protocol.indexOf('s') > 0 ? "s" : "");    
    var uri = 'ws' + sss + '://' + window.location.host + '/api/WebSocketConnect/Connect?clientId=none';
    websocket = new WebSocket(uri);

    $('#messages').prepend('<div> Connecting to ' + uri + '<div>');

    websocket.onopen = function () {
        $('#messages').prepend('<div>Connected.</div>');
    }

    websocket.onerror = function (event) {
        console.log(event);
        $('#messages').prepend('<div>ERROR ' + event.error + '</div>');
    }

    // Deal with message received on WebSocket
    websocket.onmessage = function (event) {
        try {
            // Parse the JSON package
            var eventObject = JSON.parse(event.data);
        }
        catch (e) {
            $('#messages').prepend('<div>Malformed message: ' + event.data + "</div>");
        }

        // initialize the page with all sensors

        if (receivedFirstMessage == false) {

            ClearD3Charts();
            var x = { MessageType: "LiveDataSelection", DeviceName: 'All' };

            websocket.send(JSON.stringify(x));
            receivedFirstMessage = true;

            // make 'All' the active sensor

            var j = $('#sensorList li').eq(0);
            j.css('color', color('avg'));
            j.css('font-weight', 'bold');
        }

        // Seems like we have valid data
        try {

            if (eventObject.dspl != null) {

                // Remove any sensors that have gone stale
                /*$('#sensorList li').each(function () {

                    var t = $(this).text();
                    var found = false;
                    for (var i = 0; i < D3_tmp.length; i++) {
                        if (D3_tmp[i].name == t) {
                            found = true;
                            break;
                        }
                    }

                    if (found == false) {
                        for (var i = 0; i < D3_hum.length; i++) {
                            if (D3_hum[i].name == t) {
                                found = true;
                                break;
                            }
                        }
                    }

                    if (found == false) {
                        $(this).remove();
                    }
                    
                });*/

                // if we have a new sensor, add it to the list
                var exists = true;
                if ($('#sensorList').data(eventObject.dspl) == undefined) {
                    exists = false;
                }

                if (exists == false){

                    var ul = document.getElementById("sensorList");
                    var li = document.createElement("li");
                    li.appendChild(document.createTextNode(eventObject.dspl));
                    ul.appendChild(li);

                    $('#sensorList').data(eventObject.dspl, eventObject.dspl);

                }
            }

            // If the message is an alert, we need to display it in the datatable
            if (eventObject.alerttype != null){ // && isBulking == false) {
                var table = $('#alertTable').DataTable();
                var time = new Date(eventObject.timestart);

                // Check if we already have this one in the table already to prevent duplicates
                var indexes = table.rows().eq(0).filter(function (rowIdx) {
                    if (
                        table.cell(rowIdx, 0).data().getTime() == time.getTime()
                        && table.cell(rowIdx, 1).data() == eventObject.dsplalert
                        && table.cell(rowIdx, 2).data() == eventObject.alerttype
                    ) {
                        return true;
                    }
                    return false;
                });

                // The alert is a new one, lets display it
                if (indexes.length == 0)
                {
                    // For performance reasons, we want to limit the number of items in the table to a max of 20. 
                    // We will remove the oldest from the list
                    if (table.data().length > 19) {
                        // Search for the oldest time in the list of alerts
                        var minTime = table.data().sort(
                            function (a, b) {
                                return (a[0] > b[0]) - (a[0] < b[0])
                            }
                            )[0][0];
                        // Delete the oldest row
                        table.rows(
                            function (idx, data, node) {
                                return data[0].getTime() == minTime.getTime();
                            }
                        ).remove();
                    }

                    // Add the new alert to the table
                    var message = 'message';
                    if (eventObject.message != null) message = eventObject.message;
                    table.row.add([
                        time,
                        eventObject.dsplalert,
                        eventObject.alerttype,
                        message
                    ]).draw();
                }
            }
            else {

                // Message received is not an alert. let's display it in the charts

                if (eventObject.tempavg != null) {                    

                    if (eventObject.time != null) {
                        AddToD3(D3_tmp, "Temperature", "avg", eventObject.tempavg, eventObject.time);
                    }
                    if (!isBulking) {
                        PruneOldD3Data(D3_tmp, "Temperature");
                        UpdateD3Charts(D3_tmp, "Temperature");
                    }
                    else {
                        
                        $('#loading-sensor').text("avg");
                    }
                }
                else if (eventObject.bulkData != null) {

                    // Don't update while receiving bulk data.  It will
                    // cause the browser to (usually) freeze

                    if (eventObject.bulkData == true) {
                        $('#loading').show();
                        isBulking = true;
                        ClearD3Charts();
                    }
                    else {

                        PruneOldD3Data(D3_tmp, "Temperature");
                        PruneOldD3Data(D3_hum, "Humidity");

                        UpdateD3Charts(D3_tmp, "Temperature");
                        UpdateD3Charts(D3_hum, "Humidity");

                        $('#loading').hide();

                        isBulking = false;
                    }
                }
                else {
                    // the message is data for the charts
                    // we make sure the dspl field is in the message, meaning the data is coming from a known device or service
                    if (eventObject.dspl != null) {
                        
                        if (eventObject.time != null) {
                            AddToD3(D3_tmp, "Temperature", eventObject.dspl, eventObject.temp, eventObject.time);
                            AddToD3(D3_hum, "Humidity", eventObject.dspl, eventObject.hmdt, eventObject.time);
                        }
                        if (!isBulking) {
                            PruneOldD3Data(D3_tmp, "Temperature");
                            PruneOldD3Data(D3_hum, "Humidity");

                            UpdateD3Charts(D3_tmp, "Temperature");
                            UpdateD3Charts(D3_hum, "Humidity");
                        } else {
                            $('#loading-sensor').text(eventObject.dspl);
                        }
                    }
                }
            }
        }
        catch (e) {           
        
            $('#messages').prepend('<div>Error processing message: ' + e.message + "</div>");
        }
    }

});


function SensorSelectionChanged(dropDown) {
    var newSensor = dropDown.value;
    if (websocket != null) {

        ClearD3Charts();

        var x = { MessageType: "LiveDataSelection", DeviceName: newSensor };
        websocket.send(JSON.stringify(x));        
    }
    
}


