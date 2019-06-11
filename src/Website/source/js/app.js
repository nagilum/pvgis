"use strict";

/**
 * Syntax highlight the JSON output.
 * @param {String} json Input JSON.
 * @returns {String} Highlighted JSON.
 */
let syntaxHighlight = (json) => {
    if (typeof json != 'string') {
        json = JSON.stringify(json, undefined, 2);
    }

    json = json.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');

    return json.replace(/("(\\u[a-zA-Z0-9]{4}|\\[^u]|[^\\"])*"(\s*:)?|\b(true|false|null)\b|-?\d+(?:\.\d*)?(?:[eE][+\-]?\d+)?)/g, function (match) {
        let cls = 'number';

        if (/^"/.test(match)) {
            if (/:$/.test(match)) {
                cls = 'key';
            }
            else {
                cls = 'string';
            }
        }
        else if (/true|false/.test(match)) {
            cls = 'boolean';
        }
        else if (/null/.test(match)) {
            cls = 'null';
        }

        return '<span class="' + cls + '">' + match + '</span>';
    });
};

/**
 * Execute the API call.
 * @param {Event} e Click event.
 * @returns {Promise} Fetch API promise.
 */
let tryItExecute = (e) => {
    let id = e.target.getAttribute('data-id'),
        pre = document.querySelector('pre#' + id),
        json = pre.innerText;

    console.log('');
    console.log('PVGIS API Example');
    console.log('=================');

    console.log('POST https://api.pvgisjson.com/query');
    console.log('Request Body', JSON.parse(json));

    return fetch(
        'https://api.pvgisjson.com/query',
        {
            method: 'POST',
            mode: 'cors',
            cache: 'no-cache',
            headers: {
                'Content-Type': 'application/json'
            },
            body: json
        })
        .then((res) => {
            if (res.status !== 200) {
                throw new Error(res.statusText);
            }

            return res.json();
        })
        .then((data) => {
            console.log('Response from API:');
            console.log('Response Body', data);
        })
        .catch((err) => {
            console.log('Error while executing fetch()');
            console.error(err);
        });
};

/**
 * Init all the things..
 */
(() => {
    // Prettify JSON.
    document
        .querySelectorAll('pre.json-beautify')
        .forEach((pre) => {
            let html = syntaxHighlight(
                JSON.parse(pre.innerText))
                .replace(new RegExp(':', 'g'), '<span class="default">:</span>');

            pre.innerHTML = html;
        });

    // Try-it buttons.
    document
        .querySelectorAll('button')
        .forEach((button) => {
            button.addEventListener('click', tryItExecute);
        });
})();