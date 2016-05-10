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
// create dataFlow with
/*
containerId : string,
]*/
function d3Chart(containerId) {
    var self = this;
    // call base class contructor
    baseClass.call(self);
    // initialize object
    self._flows = {};
    self._flowsVisuals = {};
    self._containerId = containerId;
    self._CONSTANTS = {
        MS_PER_MINUTE: 60000,
        WINDOW_MINUTES: 10,
    }
    self._isBulking = false;
    self._colors = d3.scale.category10();

    self._onEventObjectHandler = function (event) {
        self._onMessageHandler.call(self, event);
    }

    self._onEventRemoveGuid = function (event) {
        self._onMessageRemoveGuid.call(self, event);
    }

    self._onEventAddGuid = function (event) {
        self._onMessageAddGuid.call(self, event);
    }

    // register update handler
    self.addEventListener('update', function (evt) {
        self.pruneOldData();
        self.updateChart();
    });

    self._wasResizeHandled = true;

    return self;
}

d3Chart.prototype = {
    constructor: d3Chart,
    destroy: function () {
        var self = this;

        self.clearDataFlows();
        self.removeChartVisual();

        if (self._dataSource) {
            self._dataSource.removeEventListener('eventObject', this._onEventObjectHandler);
        }

        if (self._filter) {
            self._filter.removeEventListener('removeGuid', this._onEventRemoveGuid);
            self._filter.removeEventListener('addGuid', this._onEventAddGuid);
        }

        window['resizeCallback@' + self._containerId] = false;
    },
    setBulkMode: function (newVal) {
        var self = this;
        self._isBulking = newVal;

        if (!newVal) {
            self.raiseEvent('update');
            self.raiseEvent('loaded');
        }
        return self;
    },
    addFlow: function (newFlow, yAxis) {
        var self = this;
        self._flows[newFlow.getGUID()] = newFlow;
        self._flowsVisuals[newFlow.getGUID()] = {
            alerts: {}
        };

        newFlow.yAxis(yAxis);
        newFlow.attachToChart(self);
        self._colors.domain(newFlow.getGUID());

        return self;
    },
    attachToDataSource: function (dataSource) {
        var self = this;
        // remebmer data source
        self._dataSource = dataSource;

        // register events handler
        dataSource.addEventListener('eventObject', self._onEventObjectHandler);

        return self;
    },
    setFilter: function (filter) {
        var self = this;
        // remebmer data source
        self._filter = filter;

        // register guid handlers
        filter.addEventListener('removeGuid', self._onEventRemoveGuid);
        filter.addEventListener('addGuid', self._onEventAddGuid);

        return self;
    },

    recalcFontSize: function () {
        //Standard height, for which the body font size is correct
        var preferredHeight = 768;
        //Base font size for the page
        var fontsize = 12;
        var displayHeight = $(window).height();
        var percentage = displayHeight / preferredHeight;

        // remember font size
        this._fontSize = Math.floor(fontsize * percentage) - 1;

        return self;
    },
    clearDataFlows: function () {
        var self = this;
        // remove visual elements
        for (var id in this._flowsVisuals) {
            self.removeFlowVisual(id);
        }
        // clear data
        for (var id in self._flows) {
            // clear data set
            self._flows[id].clearData();
        }

        return self;
    },
    removeChartVisual: function () {
        var self = this;
        if (self._x != null) {
            self._x = null;
        }
        if (self._y0 != null) {
            self._y0 = null;
        }
        if (self._y1 != null) {
            self._y1 = null;
        }
        if (self._y0Label != null) {
            self._y0Label = null;
        }
        if (self._svg != null) {
            self._svg.remove();
            self._svg = null;
        }

        return self;
    },
    removeFlowVisual: function (id) {
        if (!this._flowsVisuals.hasOwnProperty(id)) return;

        var dataFlowVisuals = this._flowsVisuals[id];

        for (var idAl in dataFlowVisuals.alerts) {
            var alert = dataFlowVisuals.alerts[idAl];
            if (alert.alertShowed) {
                alert.alertShowed.remove();
                alert.alertShowed = null;
            }
            if (alert.alertBarShowed) {
                alert.alertBarShowed.remove();
                alert.alertBarShowed = null;
            }
        }

        dataFlowVisuals.alerts = {};

        if (dataFlowVisuals.path) {
            dataFlowVisuals.path.remove();
            dataFlowVisuals.path = null;
        }
        if (dataFlowVisuals.legend) {
            dataFlowVisuals.legend.remove();
            dataFlowVisuals.legend = null;
        }
        if (dataFlowVisuals.legend_r) {
            dataFlowVisuals.legend_r.remove();
            dataFlowVisuals.legend_r = null;
        }

        return self;
    },
    registerResizeHandler: function (containerId) {
        var self = this;
        if (!window['resizeCallback@' + containerId]) {
            window['resizeCallback@' + containerId] = true;
            $(window).bind('resize', function () {
                console.log('rezise chart: ' + containerId);
                self._wasResizeHandled = false;

                // remove visual elements
                for (var id in self._flowsVisuals) {
                    self.removeFlowVisual(id);
                }
                // remove original one
                self.removeChartVisual();
                d3.select("#" + containerId).select('svg').remove();
                // create a new one w/ correct size
                self.updateChart();
            });
        }

        return self;
    },
    setY0Label: function () {
        var self = this;
        if (self._y0Label) return;
        for (var id in self._flows)
            if (self._flows[id].label()) {
                self._y0Label = self._svg.append("text")
                    .attr("transform", "rotate(-90)")
                    .attr("class", "y0 label")
                    .attr("text-anchor", "middle")
                    .attr("y", -50)
                    .attr("x", -self._height / 2)
                    .attr("dy", "1em")
                    .attr("font-size", self._fontSize + "px")
                    .text(self._flows[id].label());
                break;
            }
    },
    createChart: function () {
        var self = this;

        var margin = {
            top: 5,
            right: 250,
            bottom: 20,
            left: 50
        };

        // remember container
        self._container = $('#' + self._containerId);

        // recalc font size
        self.recalcFontSize();

        var dataFlowsArray = [];

        self._width = self._container.width() - margin.right;
        self._height = self._container.height() - margin.top - margin.bottom;

        // create dataFlows array
        for (var id in self._flows) {
            dataFlowsArray.push({
                id: id,
                yMin: self._flows[id].yMin(),
                yMax: self._flows[id].yMax()
            });
        }
        // seed the axes with some dummy values
        self._x = d3.time.scale()
			.domain([0, 1])
			.range([0, self._width]);

        self._y0 = d3.scale.linear()
			.range([self._height, 0]);

        if (dataFlowsArray.length > 0 && dataFlowsArray[0].yMax != null && dataFlowsArray[0].yMin != null)
            self._y0.domain([dataFlowsArray[0].yMin, dataFlowsArray[0].yMax]);

        self._svg = d3.select("#" + self._containerId)
			.append("p")
			.append("svg")
			.attr("width", self._width + margin.left + margin.right)
			.attr("height", self._height + margin.top + margin.bottom)
			.style("margin-bottom", margin.bottom + "px")
			.append("g")
			.attr("transform", "translate(" + margin.left + "," + margin.top + ")");

        self._svg.append("g")
			.attr("class", "y0 axis")
			.call(d3.svg.axis().scale(self._y0).ticks(7).orient("left"));

        // check y0 label
        self.setY0Label();

        self._svg.append("g")
			.attr("class", "x axis")
			.attr("transform", "translate(0," + (self._height) + ")")
			.call(d3.svg.axis().scale(self._x).orient("bottom").tickFormat(d3.time.format("%X")));

        // create tip
        self._tip = d3.tip()
			.attr('class', 'd3-tip')
			.offset([-10, 0])
			.html(function (d) {
			    return "<label class='time_header'>" + d.time + "</label><label class='value_circle'>&#x25cf;</label><label class='value'>" + d.data.toFixed(2) + "</label><label class='message'>" + d.alertData.message + "</label>";
			});
        self._svg.call(self._tip);

        // register resize handler
        self.registerResizeHandler(self._containerId);
    },
    pruneAlerts: function (flowId, cutoff) {
        var self = this;
        var alertsToRemove = [];
        // cut alerts
        for (var t in self._flowsVisuals[flowId].alerts) {
            if (new Date(t) < cutoff) alertsToRemove.push(t);
        }

        for (var t in alertsToRemove) {
            var alert = self._flowsVisuals[flowId].alerts[alertsToRemove[t]];

            if (alert.alertShowed) {
                alert.alertShowed.remove();
                alert.alertShowed = null;
            }
            if (alert.alertBarShowed) {
                alert.alertBarShowed.remove();
                alert.alertBarShowed = null;
            }

            delete alert;
        }
    },
    pruneOldData: function () {
        var self = this;
        var now = new Date();
        var cutoff = new Date(now - self._CONSTANTS.WINDOW_MINUTES * self._CONSTANTS.MS_PER_MINUTE)

        // cut data
        for (var id in self._flows) {
            if (self._flows[id].cutData(cutoff)) {
                self.pruneAlerts(id, cutoff);
                //self.removeFlowVisual(id);
            }
        }
    },

    updateChart: function () {

        var self = this;

        var minDate = new Date(3015, 1, 1);
        var maxDate = new Date(1915, 1, 1);

        var minVal = [Number.MAX_VALUE, Number.MAX_VALUE];
        var maxVal = [0, 0];

        var displayHeight = $(window).height();

        for (var id in self._flows) {
            var dataFlow = self._flows[id];
            if (dataFlow.visible == false) continue;
            var data = dataFlow.getData();
            if (data.length == 0 || !dataFlow.displayName()) continue;

            // sort data
            data.sort(function (a, b) {
                if (a.time < b.time) return -1;
                if (a.time > b.time) return 1;
                return 0;
            });

            var y = dataFlow.yAxis();

            for (var j = 0; j < data.length; j++) {

                var c = data[j].data;
                var t = data[j].time;

                if (c < minVal[y]) {
                    minVal[y] = c;
                }

                if (c > maxVal[y]) {
                    maxVal[y] = c;
                }

                if (t > maxDate) {
                    maxDate = t;
                }

                if (t < minDate) {
                    minDate = t;
                }
            }
        }

        // create chart on demand
        if (self._svg == null) {
            self.createChart();
        }

        // check y0 label
        self.setY0Label();

        var wasBoundsChanged = !self._previousBounds || self._previousBounds.maxVal0 !== maxVal[0] || self._previousBounds.minVal0 !== minVal[0];

        if (!self._wasResizeHandled || wasBoundsChanged && minVal[0] < Number.MAX_VALUE) {
            var diff = maxVal[0] - minVal[0];
            var scaleMargin =  diff * 10 / 100;
            
            if (!isFinite(scaleMargin)) {
                scaleMargin = 0;
            }
            
            var v1 = Math.abs(minVal[0] - scaleMargin);
            var v2 = Math.abs(maxVal[0] + scaleMargin);
            if (!isFinite(v1)) {
                v1 = minVal[0];
            }
            if (!isFinite(v2)) {
                v2 = maxVal[0];
            }
            if (v2 < v1) {
                v2 = v1;
            }
            
            self._y0 = self._y0
             		.domain([v1, v2]);
             		
            var yAxisLeft = d3.svg.axis()
				.scale(self._y0)
				.orient("left")
            self._svg.selectAll("g.y0.axis")
				.call(yAxisLeft);

            self._wasResizeHandled = true;
        }

        wasBoundsChanged = !self._previousBounds || self._previousBounds.maxVal1 !== maxVal[1] || self._previousBounds.minVal1 !== minVal[1];

        if (!self._wasResizeHandled || wasBoundsChanged && minVal[1] < Number.MAX_VALUE) {
            var diff = maxVal[1] - minVal[1];
            var scaleMargin = (diff) * 10 / 100;
            
	    if (!isFinite(scaleMargin)) {
                scaleMargin = 0;
            }
            
            var v1 = Math.abs(minVal[1] - scaleMargin);
            var v2 = Math.abs(maxVal[1] + scaleMargin);
            
            if (!isFinite(v1)) {
                v1 = minVal[1];
            }
            if (!isFinite(v2)) {
                v2 = maxVal[1];
            }
            if (v2 < v1) {
                v2 = v1;
            }
            
            self._y1 = self._y1
				.domain([v1,v2]);
            
            var yAxisRight = d3.svg.axis()
				.scale(self._y1)
				.orient("right")
            self._svg.selectAll("g.y1.axis")
				.call(yAxisRight);

            self._wasResizeHandled = true;
        }

        self._x = self._x
			.domain([minDate, maxDate]);

        var xAxis = d3.svg.axis()
			.scale(self._x)
			.tickFormat(d3.time.format("%X"))
			.orient("bottom");

        self._svg.selectAll("g.x.axis")
			.call(xAxis);

        self._previousBounds = {
            maxVal0: maxVal[0],
            maxVal1: maxVal[1],
            minVal0: minVal[0],
            minVal1: minVal[1],
        };

        if (!self._line) {
            self._line = [
                d3.svg.line()
                .interpolate("monotone")
                .x(function (d) {
                    return self._x(d.time);
                })
                .y(function (d) {
                    return self._y0(d.data);
                }),

                d3.svg.line()
                .interpolate("monotone")
                .x(function (d) {
                    return self._x(d.time);
                })
                .y(function (d) {
                    return self._y1(d.data);
                })
            ];
        }

        try {
            var pos = 0;
            for (var id in self._flows) {
                var dataGUID = id;
                var dataFlow = self._flows[id];
                if (dataFlow.visible == false) continue;
                var dataFlowVisuals = self._flowsVisuals[id];
                var data = dataFlow.getData();
                var yAxis = dataFlow.yAxis();

                if (dataFlowVisuals.path == null) {
                    dataFlowVisuals.path = self._svg.append("g")
						.append("path")
						.datum(data)
						.attr("class", "line")
						.attr("d", self._line[yAxis])
						.style("stroke", function (d) {
						    return self._colors(dataGUID);
						});
                }

                dataFlowVisuals.path.datum(data)
					.attr("d", self._line[yAxis]);

                // draw alert points
                for (var pnt in data) {
                    if (typeof data[pnt].alertData == 'object') {
                        if (!dataFlowVisuals.alerts.hasOwnProperty(data[pnt].time)) {
                            var transferData = JSON.stringify({
                                alertData: data[pnt].alertData,
                                time: data[pnt].time,
                                data: data[pnt].data
                            });
                            var alertVisual = dataFlowVisuals.alerts[data[pnt].time] = {};
                            alertVisual.alertBarShowed = self._svg.append("g").append("rect")
								.attr("class", "bar")
								.attr("x", self._x(data[pnt].time))
								.attr("y", 0)
								.attr("height", self._height)
								.attr("width", "2px")
								.style("fill", "#e6c9cd")

                            alertVisual.alertShowed = self._svg.append("g").append("circle")
								.attr("class", "d3-dot")
								.attr("cx", self._x(data[pnt].time))
								.attr("cy", yAxis == 0 ? self._y0(data[pnt].data) : self._y1(data[pnt].data))
								.style("fill", "#e93541")
								.attr("r", displayHeight / 200)
								.on('mouseover', function () {
								    d3.select(this).transition().attr("r", displayHeight / 130);
								    eval("self._tip.show(" + transferData + ");")
								})
								.on('mouseout', function () {
								    d3.select(this).transition().attr("r", displayHeight / 200);
								    self._tip.hide();
								});
                        } else {
                            var alertVisual = dataFlowVisuals.alerts[data[pnt].time];
                            alertVisual.alertShowed.attr("cx", self._x(data[pnt].time))
								.attr("cy", yAxis == 0 ? self._y0(data[pnt].data) : self._y1(data[pnt].data));

                            alertVisual.alertBarShowed
								.attr("x", self._x(data[pnt].time))
                        }
                    }
                }
                if (dataFlowVisuals.legend == null) {
                    dataFlowVisuals.legend_r = self._svg.append("rect")
						.attr("class", "legend")
						.attr("width", 10)
						.attr("height", 10)
						.attr("x", self._width + 50)
						.attr("y", 20 + (20 * pos))
						.style("fill", self._colors(dataGUID))
						.style("stroke", self._colors(dataGUID));

                    dataFlowVisuals.legend = self._svg.append("text")
						.attr("x", self._width + 65)
						.attr("y", 20 + (20 * pos) + 5)
						.attr("class", "legend")
						.style("fill", self._colors(dataGUID))
						.text(dataFlow.displayName());
                } else {
                    dataFlowVisuals.legend.text(dataFlow.displayName());
                }
                pos++;
            }
        } catch (e) {
            console.log(e);
        }
    },

    _onMessageAddGuid: function (evt) {
        var self = this;

        if (self._flows.hasOwnProperty(evt.owner)) {
            self._flows[evt.owner].visible = true;
        }
    },
    _onMessageRemoveGuid: function (evt) {
        var self = this;

        self.removeFlowVisual(evt.owner);
        if (self._flows.hasOwnProperty(evt.owner)) {
            self._flows[evt.owner].visible = false;
        }
    },
    // private members
    _onMessageHandler: function (eventObject) {
        var self = this;
        var evt = eventObject.owner;
        // the message is data for the charts. find chart for message
        if (evt.hasOwnProperty('guid') && self._flows.hasOwnProperty(evt.guid)) {
            // check filter
            //if (self._filter && !self._filter.checkGUID(evt.guid)) return;
            // check event time
            var now = new Date();
            var cutoff = new Date(now - self._CONSTANTS.WINDOW_MINUTES * self._CONSTANTS.MS_PER_MINUTE)

            if (evt.time < cutoff) {
                return;
            }

            // add event
            self.raiseEvent('newData', evt);

            // check if nessasary to update
            if (!self._isBulking) {
                self.raiseEvent('update');
            } else {
                self.raiseEvent('loading', evt.displayname);
            }
        }
    }
};

extendClass(d3Chart, baseClass);
