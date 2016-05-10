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

// Using HTTP Rest connection to Azure event Hubs
var https = require('https');

// Using a json settings file for Events Hub connectivity
var devicesettings;

// Variables
var connectionstring;
var sastoken;
var fixedsastoken = false;

// ---------------------------------------------------------------
// validate settings from JSON file  passed as a parameter to the app
function validate_settings(settings, options) {
    console.log("Validating device settings");
    var missing = [];
    for (var idx in options) {
        if (settings[options[idx]] === undefined) missing.push(options[idx]);
    }
    if (missing.length > 0) {
        // app is terminated if settings are missing
        throw new Error('Required settings ' + (missing.join(', ')) + ' missing.');
    }
}

// ---------------------------------------------------------------
// Force the SAS token
function set_sas_token(token)
{
    fixedsastoken = true;
    sastoken = token;
}

// ---------------------------------------------------------------
// Create a SAS token
// See http://msdn.microsoft.com/library/azure/dn170477.aspx
function create_sas_token(uri, key_name, key) {
    // We do the require in the function so that devices using fixed SAS Tokens don't need to add the libs as dependencies
    var crypto = require('crypto');
    var moment = require('moment');

    // Token expires in one hour
    var expiry = moment().add(1, 'hours').unix();
    var string_to_sign = encodeURIComponent(uri) + '\n' + expiry;
    var hmac = crypto.createHmac('sha256', key);
    hmac.update(string_to_sign);
        
    var signature = hmac.digest('base64');
        
    var token = 'SharedAccessSignature sr=' + encodeURIComponent(uri) + '&sig=' + encodeURIComponent(signature) + '&se=' + expiry + '&skn=' + key_name;
    
    return token;

}

// ---------------------------------------------------------------
// Update the sas token
function update_sas_token()
{
    if (!fixedsastoken)
        sastoken = create_sas_token(connectionstring, devicesettings.keyname, devicesettings.key);
    console.log("New SAS token generated: " + sastoken);        
}

// ---------------------------------------------------------------
// Format sensor data into JSON
function format_sensor_data(guid, displayname, organization, location, measurename, unitofmeasure, timecreated, value) {
    var JSON_obj = {
        "guid": guid,
        "displayname": displayname,
        "organization": organization,
        "location": location,
        "measurename": measurename,
        "unitofmeasure": unitofmeasure,
        "timecreated": timecreated,
        "value": value
    };
    
    return JSON.stringify(JSON_obj);
}

// ---------------------------------------------------------------
// Initializes connection settings to Event Hub
exports.init_connection = function(settings, token)
{
    devicesettings = settings;
    
    // Validate settings
    validate_settings(devicesettings, ['namespace', 'keyname', 'key', 'eventhubname', 'displayname', 'guid', 'organization', 'location']);

    // ---------------------------------------------------------------
    // Get the full Event Hub publisher URI
    connectionstring = 'https://' + devicesettings.namespace + '.servicebus.windows.net' + '/' + devicesettings.eventhubname + '/publishers/' + devicesettings.displayname + '/messages';
    console.log("Event Hub connection string: " + connectionstring);
 
    if (arguments.length >1)
    {
        fixedsastoken = true;
        sastoken = token;
    } else
    {
        // we need to recreate the sas token regularly as it has a limited validity in time
        update_sas_token();
        setInterval(function(){
            update_sas_token();
        }, 3600000);
    }
 };

function send_raw_message(raw_message)
{
    // Send the request to the Event Hub
    var http_options = {
            
        hostname: devicesettings.namespace + '.servicebus.windows.net',
        port: 443,
        path: '/' + devicesettings.eventhubname + '/publishers/' + devicesettings.displayname + '/messages',
        method: 'POST',
        headers: {
            'Authorization': sastoken,
            'Content-Length': raw_message.length,
            'Content-Type': 'application/atom+xml;type=entry;charset=utf-8'
        }
    };
        
    var req = https.request(http_options, function (res) {
        console.log("statusCode: ", res.statusCode);
        console.log("headers: ", res.headers);
            
        res.on('data', function (d) {
            console.log(d);
            if (res.statusCode == 401) update_sas_token();
        });
    });
        
    req.on('error', function (e) {
        console.error(e);
        // When we get an error, chances are our SAS token is not valid any more
        update_sas_token();
    });
        
    req.write(raw_message);
        
    req.end();    
}

// ---------------------------------------------------------------
// Send message to Event Hub
exports.send_message = function(measurename, unitofmeasure, value)
{
    var currentTime = new Date().toISOString();
    var message = format_sensor_data(devicesettings.guid, devicesettings.displayname, devicesettings.organization, devicesettings.location, measurename, unitofmeasure, currentTime, value);
	console.log("Sending message: " + message);
    
    send_raw_message(message);
}

// ---------------------------------------------------------------
// Send bulk messages to Event Hub
exports.send_bulk_message = function(messages)
{
    var currentTime = new Date().toISOString();
    var message;
    
    messages.forEach( function(element, index, array){
        message+= format_sensor_data(devicesettings.guid, devicesettings.displayname, devicesettings.organization, devicesettings.location, element.measurename, element.unitofmeasure, currentTime, element.value);
    }); 
	console.log("Sending bulk message: " + message);
    
    send_raw_message(message);
}



