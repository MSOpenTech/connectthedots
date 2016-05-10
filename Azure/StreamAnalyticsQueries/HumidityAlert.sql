/*  ---------------------------------------------------------------------------------
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
//  ---------------------------------------------------------------------------------*/
WITH LongAverage AS
(
	SELECT 
		displayname as dspl,
		AVG(value) AS hmdt,
		MAX(timecreated) AS time
	FROM
		StreamInput TIMESTAMP BY timecreated
    WHERE
        measurename='Humidity'
	GROUP BY
		HoppingWindow(DURATION(ss, 5), HOP(ss, 2)),
                displayname
), ShortAverage AS
(
	SELECT
		displayname AS dspl,
		AVG(value) AS hmdt,
		MAX(timecreated) AS time,
		guid,
		measurename,
		unitofmeasure, 
    	location,
    	organization
	FROM
		StreamInput TIMESTAMP BY timecreated
    WHERE
        measurename='Humidity'
	GROUP BY
		TumblingWindow(ss, 2),
                displayname,
				guid,
				measurename,
				unitofmeasure, 
    			location,
    			organization
), Compare AS
(
	SELECT 
		ShortAverage.dspl AS dspl,
		ShortAverage.hmdt AS NewHumidity,
		LongAverage.hmdt AS OldHumidity,
		((ShortAverage.hmdt - LongAverage.hmdt)/ ShortAverage.hmdt) * 100 AS delta,
		ShortAverage.time AS time,
		ShortAverage.guid,
		ShortAverage.measurename,
		ShortAverage.unitofmeasure, 
    	ShortAverage.location,
    	ShortAverage.organization
	FROM
		LongAverage
		INNER JOIN ShortAverage
		On LongAverage.dspl = ShortAverage.dspl
		AND DATEDIFF(ss, LongAverage, ShortAverage) > 1
        AND DATEDIFF(ss, LongAverage, ShortAverage) < 5
)

SELECT
	'HumSpike' AS alerttype,
	'Sudden increase in Humidity' AS message,
	dspl as displayname,
	guid,
	measurename,
	unitofmeasure, 
    location,
    organization,
    NewHumidity as value,
	OldHumidity,
	delta,
	time AS timecreated
FROM
	Compare
WHERE
	delta >= 20