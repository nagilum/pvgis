'use strict';

const url = 'https://europe-west2-pvgisjson-api.cloudfunctions.net/pvgis';

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
    let pre = document.querySelector('pre#try-it-output');

    let lat = document.querySelector('input#try-it-lat'),
        lng = document.querySelector('input#try-it-lng'),
        peakpower = document.querySelector('input#try-it-peakpower'),
        loss = document.querySelector('input#try-it-loss'),
        angle = document.querySelector('input#try-it-angle'),
        aspect = document.querySelector('input#try-it-aspect'),
        mounting = document.querySelector('select#try-it-mounting'),
        pvtech = document.querySelector('select#try-it-pvtech');

    let payload = {
        lat: parseFloat(lat.value),
        lng: parseFloat(lng.value),
        peakpower: parseInt(peakpower.value, 10),
        loss: parseInt(loss.value, 10),
        angle: parseInt(angle.value, 10),
        aspect: parseInt(aspect.value, 10),
        mounting: mounting.value,
        pvtech: pvtech.value
    };

    pre.classList.add('loading');

    return fetch(
        url,
        {
            method: 'POST',
            mode: 'cors',
            cache: 'no-cache',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(payload)
        })
        .then((res) => {
            if (res.status !== 200) {
                throw new Error(res.statusText);
            }

            return res.json();
        })
        .then((json) => {
            pre.innerHTML = syntaxHighlight(json)
                .replace(new RegExp(':', 'g'), '<span class="default">:</span>');

            pre.classList.remove('loading');
        })
        .catch((err) => {
            console.log('err', err);
            pre.classList.remove('loading');
            alert('Error. Check console!');
        });
};

/**
 * Init all the things..
 */
(() => {
    // Prettify JSON.
    document
        .querySelectorAll('.json-beautify')
        .forEach((pre) => {
            pre.innerHTML = syntaxHighlight(
                JSON.parse(pre.innerText))
                    .replace(new RegExp(':', 'g'), '<span class="default">:</span>');
        });
    
    // Try-it button.
    document
        .querySelector('button#try-it-execute')
        .addEventListener('click', tryItExecute);
})();