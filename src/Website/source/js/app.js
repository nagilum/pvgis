"use strict";

(() => {
    // Prettify JSON.
    document
        .querySelectorAll('pre.json-beautify')
        .forEach((pre) => {
            pre.innerText = JSON.stringify(
                JSON.parse(pre.innerText),
                null,
                4);
        });

    // Try-it buttons.
    document
        .querySelectorAll('button')
        .forEach((button) => {
            button.addEventListener(
                'click',
                (e) => {
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
                });
        });
})();