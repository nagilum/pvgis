"use strict";

(() => {
    document
        .querySelectorAll('pre.json-beautify')
        .forEach((pre) => {
            let json = JSON.parse(pre.innerText);
            pre.innerHTML = JSON.stringify(json, null, 4);
        });
})();