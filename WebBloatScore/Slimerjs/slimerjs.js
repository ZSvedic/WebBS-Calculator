"use strict";
var system = require('system');
var pako = require('pako_deflate');
var fs = require('fs');

/***************************/
/******** ARGUMENTS ********/
/***************************/
var url = system.args[1],
    screenshot = system.args[2],
    outputPattern = system.args[3],
    titlePlaceholder = system.args[4],
    countPlaceholder = system.args[5],
    sizePlaceholder = system.args[6];

var resources = [];

/***************************/
/********** TIME ***********/
/***************************/
var waitResource = 1000,
    maxWaitResource = 90000,
    startTime,
    renderTimeout,
    forcedRenderTimeout;

/***************************/
/********** PAGE ***********/
/***************************/
var page = require('webpage').create();
page.viewportSize = { width:1366, height:768 };
page.waitResourceCount = 0;

page.onLoadStarted = function () {
    if (!forcedRenderTimeout) {
        forcedRenderTimeout = setTimeout(function () {
            console.log('finish => forcedRenderTimeout');
            finish(1);
        }, maxWaitResource);
    }
};

//page.onResourceError = function (err) {
//    console.log('page.onResourceError => ERROR:' + err.errorString + ' - CODE:' + err.errorCode + ' - URL:' + err.url);
//};

//page.onError = function (msg) {
//    console.log('page.onError => ERROR:' + msg);
//};

page.onResourceRequested = function (req) {
    clearTimeout(renderTimeout);
    ++page.waitResourceCount;
};

page.onResourceReceived = function (res) {
    if (!res.stage || res.stage === 'end') {
        resources.push({
            url: res.url,
            type: res.contentType,
            size: getResponseSize(res)
        });

        --page.waitResourceCount;
        if (page.waitResourceCount === 0) {
            renderTimeout = setTimeout(function () {
                console.log('finish => renderTimeout');
                finish(0);
            }, waitResource);
        }
    }
};

page.open(url, function (status) {
    if (status === 'success') {
        console.log('page.open => SUCCESS - URL:' + url);
    } else {
        console.log('page.open => FAIL - STATUS:' + status + ' - URL:' + url);
        slimer.exit(3);
    }
});

function getResponseSize(res) {
    var lengthHeader, encodingHeader;
    res.headers.forEach(function (header) {
        if (header.name === 'Content-Length') {
            lengthHeader = header;
        } else if (header.name === 'Content-Encoding') {
            encodingHeader = header;
        }
    });
    
    if (lengthHeader) {
        return +lengthHeader.value;
    } else if (encodingHeader && encodingHeader.value === 'gzip') {
        if (res.body) {
            return pako.deflate(res.body).length;
        } else {
            return Math.round(res.bodySize * 0.4); // GZIP approximate compression ratio.
        }
    } else {
        return res.bodySize;
    }
}

function finish(exitCode) {
    page.render(screenshot, { format: 'png', quality: 0 });
    var size = 0, count = 0;

    // Write details to file in 'TAB' format.
    resources.forEach(function (resource) {
        ++count;
        size += resource.size;
        fs.write(screenshot.replace('.png', ''),
            resource.url + '\t' +
            (resource.size ? resource.size : '-') + '\t' +
            (resource.type ? resource.type : '-') + '\r\n', 'a');
    });

    // Write result to output in 'outputPattern' format.
    console.log(outputPattern.replace(titlePlaceholder, page.title)
                             .replace(countPlaceholder, count)
                             .replace(sizePlaceholder, size));

    slimer.exit(exitCode);
}