<!--
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
-->

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="ConnectTheDotsWebSite.Default" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Connect The Dots</title>

    <!-- general styles -->

    <style>
        body {
            font: 16px sans-serif;
            color: #333;
        }

        #header {
            width: 100%;
            border-bottom: 1px #cdcdcd solid;
        }

        h3 {
            width: 1000px;
            background-color: #f7f7f7;
            border-bottom: 1px #ddd solid;
            padding: 10px;
        }



        .big-block {
            margin-left: 100px;
        }
    </style>

    <!-- for device selection list -->

    <style>
        ul {
            list-style-type: none;
            padding-left: 0;
            font: 12px sans-serif;
            color: #666;
        }

        li {
            cursor: pointer;
            font-weight: normal;
        }

        .selected {
            font-weight: bold;
        }


        li.selected:before {
            content: "\25BA \0020";
        }
    </style>

    <!-- styles for D3 charts -->

    <style>
        .axis {
            shape-rendering: crispEdges;
        }

            .axis text {
                font: 10px sans-serif;
                font-weight: normal;
                fill: #787878;
            }

            .axis path,
            .axis line {
                fill: none;
                stroke: #787878;
                shape-rendering: crispEdges;
            }

        .y.axis {
        }

        .x.axis {
        }

        path.line {
            fill: none;
            stroke: steelblue;
            stroke-width: 1.5px;
        }

        .legend {
            font: 10px sans-serif;
        }
    </style>

    <!-- for "loading" gif -->

    <style>
        #loading {
            width: 100%;
            height: 100%;
            top: 0px;
            left: 0px;
            position: fixed;
            display: block;
            background-color: #777;
            background-color: rgba(155, 155, 155, 0.4);
            z-index: 99;
            text-align: center;
        }

        #loading-inner {
            background-color: #fff;
            border-style: solid;
            border-width: 1px;
            width: 400px;
            height: 200px;
            position: absolute;
            top: 50%;
            left: 50%;
            margin-left: -200px;
            margin-top: -100px;
        }

        #loading-image {
            position: relative;
            top: 10px;
            left: 10px;
            z-index: 100;
        }

        #loading-text {
            position: relative;
            top: 10px;
            left: 10px;
            z-index: 100;
        }
    </style>

    <link rel="stylesheet" type="text/css" href="https://cdn.datatables.net/1.10.3/css/jquery.dataTables.css">

    <script type="text/javascript" src="https://code.jquery.com/jquery-1.11.1.min.js"></script>
    <script type="text/javascript" src="https://cdn.datatables.net/1.10.3/js/jquery.dataTables.min.js"></script>
    <script src="http://d3js.org/d3.v3.min.js" charset="utf-8"></script>
    <script src="Scripts/jquery.signalR-2.2.0.min.js"></script>
    <script src="signalr/hubs"></script>
    <script type="text/javascript" src="js/connectthedots.js"></script>
</head>
<body>

    <div id="loading">
        <div id="loading-inner">
            <p id="loading-text">Loading last 10 minutes of data...</p>
            <p id="loading-sensor"></p>
            <img id="loading-image" src="img/ajax-loader.gif" />
        </div>
    </div>

    <div id="header">
        <div>
            <img src="img/ConnectTheDotsLogo.png" />
        </div>
    </div>

    <form id="form2" runat="server">

        <div class="big-block">
            <h3>Live Sensor Data</h3>

            <div style="float: left; width: 200px">

                <p><strong>Select Sensor/R-PI:</strong></p>

                <ul id="sensorList">
                    <li class="selected">All</li>
                </ul>

            </div>


            <div style="margin-left: 200px">
                <div>
                    <div id="Temperature">
                        <script>
                            (function () {
                                chart("Temperature");  // make sure chart name matches the div id
                            })();
                        </script>
                    </div>

                    <div id="Humidity">
                        <script>

                            (function () {
                                chart("Humidity");  // make sure chart name matches the div id
                            })();

                        </script>
                    </div>
                </div>
            </div>
        </div>

        <div class="big-block" style="width: 1000px">
            <h3>Real Time Events</h3>

            <div>
                <input type="button" onclick="ShowHide(alerts)" value="Show/Hide Alerts" />

            </div>


            <div id="alerts">
                <table id="alertTable">
                    <thead>
                        <tr>
                            <th class="timeFromDate">Time</th>
                            <th>Device</th>
                            <th>Alert</th>
                            <th>Message</th>
                        </tr>
                    </thead>
                    <tbody>
                    </tbody>
                </table>

            </div>
        </div>

        <div class="big-block">

            <h3>Raw Alerts</h3>

            <input type="button" onclick="ShowHide(rawalerts)" value="Show/Hide Raw Alerts" />

            <div id="rawalerts" style="display: none"></div>

            <div id="messages"></div>
        </div>
    </form>
</body>
</html>
