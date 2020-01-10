var $elements = {
    $urlTxt: document.getElementById('page'),
    $timeoutSelect: document.getElementById('timeout'),
    $calcBtn: document.getElementById('calculator'),
    $scoresTable: document.getElementById('scores-table'),
    $scores: document.getElementById('scores'),
    $notifier: document.getElementById('notifier'),
    $loader: document.getElementById('loader')
};

var calculating = false;

function callCalculate(url) {
    $elements.$urlTxt.value = url;
    $elements.$calcBtn.click();
}

[].forEach.call(document.getElementsByClassName("example"), function ($example) {
    $example.onclick = function (event) {
        if (!calculating) {
            callCalculate($example.href);
        }
        event.preventDefault();
        return false;
    };
});

$elements.$urlTxt.onkeypress = function (e) {
    if (e.keyCode === 13) {
        $elements.$calcBtn.click();
    }
};

$elements.$calcBtn.onclick = function () {
    $elements.$scoresTable.style.display = 'table';

    var url = $elements.$urlTxt.value;
    if (url === '') {
        notifyUser('The URL is required.');
        return;
    }

    if (!url.startsWith('http://') &&
        !url.startsWith('https://') &&
        !url.startsWith('ftp://') &&
        !url.startsWith('ftps://')) {
        url = 'http://' + url;
    }

    if (!(/^(https?|s?ftp):\/\/(((([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(%[\da-f]{2})|[!\$&'\(\)\*\+,;=]|:)*@)?(((\d|[1-9]\d|1\d\d|2[0-4]\d|25[0-5])\.(\d|[1-9]\d|1\d\d|2[0-4]\d|25[0-5])\.(\d|[1-9]\d|1\d\d|2[0-4]\d|25[0-5])\.(\d|[1-9]\d|1\d\d|2[0-4]\d|25[0-5]))|((([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.)+(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.?)(:\d*)?)(\/((([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(%[\da-f]{2})|[!\$&'\(\)\*\+,;=]|:|@)+(\/(([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(%[\da-f]{2})|[!\$&'\(\)\*\+,;=]|:|@)*)*)?)?(\?((([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(%[\da-f]{2})|[!\$&'\(\)\*\+,;=]|:|@)|[\uE000-\uF8FF]|\/|\?)*)?(#((([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(%[\da-f]{2})|[!\$&'\(\)\*\+,;=]|:|@)|\/|\?)*)?$/i.test(url))) {
        notifyUser('The provided URL is invalid.');
        return;
    }

    var timeout = $elements.$timeoutSelect.options[$elements.$timeoutSelect.selectedIndex].value;

    calculatePage(url, timeout, calculateSuccess, calculateError);
};

document.addEventListener("DOMContentLoaded", function (event) {
    var query = window.location.search;
    if (query.startsWith('?url=')) {
        var queryIndex = window.location.href.indexOf(query);
        var queryUrl = window.location.href.substring(queryIndex + '?url='.length);
        callCalculate(decodeURIComponent(queryUrl));
    }
});

function calculatePage(url, timeout, success, error) {
    disableUser(true);
    notifyUser('Waiting for all requests and generating a screenshot …', '#8D8D8D');

    var request = new XMLHttpRequest();
    request.open('post', '/Capture', true);
    request.setRequestHeader('Content-Type', 'application/json; charset=utf-8');

    request.onreadystatechange = function () {
        if (this.readyState === 4) {
            if (this.status === 200 && this.getResponseHeader('Content-Type') === 'application/json; charset=utf-8') {
                var result = JSON.parse(this.responseText);
                if (result.Exit === 1) {
                    notifyUser('Calculator stopped measuring after timeout.');
                } else if (result.Exit === 2) {
                    notifyUser('Calculator stopped screenshot optimization after timeout.');
                } else {
                    notifyUser('Done.', '#8D8D8D');
                }
                success(result);
            } else {
                error(this.status);
            }
            disableUser(false);
        }
    };

    request.send('{ url: "' + url + '", timeout : "' + timeout + '" }');
}

function calculateSuccess(result) {
    var encodedPageUrl = encodeURI(result.Page);
    var sharePageUrl = 'http://www.webbloatscore.com?url=' + encodedPageUrl;
    var bloatScore = formatToKilobytes(result.BS * 1000);

    var resultRow = $elements.$scores.insertRow($elements.$scores.rows.length);
    resultRow.innerHTML =
        '<td>' + escapeText(result.PageTitle) + '<a href="' + result.Page + '" class="icon icon-new-tab" rel="nofollow" target="_blank"></a></td>' +
        '<td><a href="' + result.PageDetails + '" target="_blank">' + formatToKilobytes(result.PageSize) + ' kB and ' + result.PageCount + ' request' + (result.PageCount > 1 ? 's' : '') + '</a></td>' +
        '<td><a href="' + result.Image + '" target="_blank">' + formatToKilobytes(result.ImageSize) + ' kB</a></td>' +
        '<td>' + bloatScore + '</td>' +
        '<td><div class="tip"><a class="icon icon-share"><span class="share-txt">Share</span></a><div class="tip-content">' +
            '<a class="icon icon-facebook" href="https://www.facebook.com/sharer.php?u=' +
                sharePageUrl + '" target="_blank">Facebook</a><br/>' +
            '<a class="icon icon-twitter" href="https://twitter.com/intent/tweet?text=I%20got%20the%20Bloat%20Score%20of%20' +
                bloatScore + '%20for%20' +
                encodedPageUrl + '%2C%20check%20it%20out%3A%0A' +
                sharePageUrl + '" target="_blank">Twitter</a><br/>' +
            '<a class="icon icon-googleplus" href="https://plus.google.com/share?url=' +
                sharePageUrl + '" target="_blank">Google+</a>' +
        '</div></div></td>';
}

function calculateError(status) {
    switch (status) {
        case 400:
            notifyUser('The provided URL is invalid.');
            break;
        case 429:
            location.href = "/TooManyRequests.html";
            break;
        case 500:
            notifyUser('Problem occurred while processing requested website.');
            break;
        case 504:
            notifyUser('Timeout occurred while processing requested website.');
            break;
        default:
            notifyUser('Unexpected error occurred while processing requested website.');
    }
}

function disableUser(disable) {
    calculating = disable;
    $elements.$urlTxt.disabled = disable;
    $elements.$calcBtn.disabled = disable;
    $elements.$loader.style.visibility = disable ? 'visible' : 'hidden';
}

function notifyUser(message, color) {
    color = color || '#ECA33A';
    $elements.$notifier.innerHTML = message;
    $elements.$notifier.style.color = color;
}

function formatToKilobytes(bytes) {
    var kilobytes = bytes / 1000;
    var decimalNotation = 0;

    if (kilobytes < 1) {
        decimalNotation = 1000;
    } else if (kilobytes < 10) {
        decimalNotation = 100;
    } else if (kilobytes < 100) {
        decimalNotation = 10;
    } else {
        decimalNotation = 1;
    }
    return (Math.round(kilobytes * decimalNotation) / decimalNotation).toLocaleString('en-US');
}

function escapeText(unsafe) {
    return unsafe.replace(/&/g, "&amp;")
                 .replace(/</g, "&lt;")
                 .replace(/>/g, "&gt;")
                 .replace(/"/g, "&quot;")
                 .replace(/'/g, "&#039;");
}

String.prototype.startsWith = function (value) {
    return this.substr(0, value.length).toLowerCase() === value.toLowerCase();
};