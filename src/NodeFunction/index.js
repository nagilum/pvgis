'use strict';

const express = require('express'),
      bodyParser = require('body-parser'),
      request = require('request'),
      app = express();

const hostname = 'localhost',
      port = 3001;

// parse application/json
app.use(bodyParser.json());

/**
 * Search and replace all instances.
 * @param {String} search Text to search for.
 * @param {String} replacement Text to replace with.
 * @returns {String} Fixed string.
 */
String.prototype.replaceAll = function(search, replacement) {
    return this.replace(new RegExp(search, 'g'), replacement);
};

/**
 * Query the actual Europa PVGIS server.
 * @param {Object} obj Parameters for call.
 * @returns {Promise}
 */
var QueryPvgisEurope = (obj) => {
    return new Promise((resolve, reject) => {
        let options = {
            url: 'http://re.jrc.ec.europa.eu/pvgis/apps4/PVcalc.php',
            formData: obj,
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
                'Content-Encoding': 'UTF8',
                'User-Agent': 'QueryPvgis'
            }
        };

        request.post(options, (err, res, body) => {
            return err
                ? reject(err)
                : resolve(body);
        });
    });
};

/**
 * Get values from a yearly row.
 * @param {String} html HTML to parse.
 * @param {String} sf Key to search for.
 * @returns {Object}
 */
var GetYearlyRowFromHTML = (html, sf) => {
    let key = '<td><b>' + sf + '</b></td>',
        sp = html.indexOf(key);

    if (sp === -1) {
        return null;
    }

    let temp = html.substr(sp + key.length);

    sp = temp.indexOf('</td> </tr>');

    if (sp === -1) {
        return null;
    }

    temp = temp
        .substr(0, sp)
        .replaceAll('<td align="right" colspan=2 >', ',')
        .replaceAll('<b>', '')
        .replaceAll('</b>', '')
        .replaceAll('</td>', '');

    let values = temp.split(',');

    if (values.length !== 3) {
        return null;
    }

    return {
        e: parseFloat(values[1].trim()),
        h: parseFloat(values[2].trim())
    };
};

/**
 * Get values from a monthly row.
 * @param {String} html HTML to parse.
 * @param {String} month Name of month to fetch data for.
 * @returns {Object}
 */
var GetMonthlyRowFromHTML = (html, month) => {
    let key = '<td> ' + month + ' </td>',
        sp = html.indexOf(key);

    if (sp === -1) {
        key = '<td><b> ' + month + ' </b></td>';
        sp = html.indexOf(key);
    }

    if (sp === -1) {
        return null;
    }

    let temp = html.substr(sp + key.length);

    sp = temp.indexOf('</td></tr>');

    if (sp === -1) {
        return null;
    }

    temp = temp
        .substr(0, sp)
        .replaceAll('</td>', '')
        .replaceAll('<b>', '')
        .replaceAll('</b>', '')
        .replaceAll('<td align="right">', ',');

    let values = temp.split(',');

    if (values.length !== 5) {
        return null;
    }

    return {
        ed: parseFloat(values[1].trim()),
        em: parseFloat(values[2].trim()),
        hd: parseFloat(values[3].trim()),
        hm: parseFloat(values[4].trim())
    };
};

/**
 * Parse the incoming HTML and return usable values.
 * @param {String} html HTML from PVGIS Europe.
 * @return {Promise}
 */
var ParsePvgisHtml = (html) => {
    return new Promise((resolve, reject) => {
        return resolve({
            monthlyAverage: {
                jan: GetMonthlyRowFromHTML(html, 'Jan'),
                feb: GetMonthlyRowFromHTML(html, 'Feb'),
                mar: GetMonthlyRowFromHTML(html, 'Mar'),
                apr: GetMonthlyRowFromHTML(html, 'Apr'),
                may: GetMonthlyRowFromHTML(html, 'May'),
                jun: GetMonthlyRowFromHTML(html, 'Jun'),
                jul: GetMonthlyRowFromHTML(html, 'Jul'),
                aug: GetMonthlyRowFromHTML(html, 'Aug'),
                sep: GetMonthlyRowFromHTML(html, 'Sep'),
                oct: GetMonthlyRowFromHTML(html, 'Oct'),
                nov: GetMonthlyRowFromHTML(html, 'Nov'),
                dec: GetMonthlyRowFromHTML(html, 'Dec')
            },
            yearlyAverage: GetMonthlyRowFromHTML(html, 'Yearly average'),
            yearlyTotal: GetYearlyRowFromHTML(html, 'Total for year')
        });
    });
};

/**
 * Get post data from client and re-query the PVGIS service.
 */
app.post('/', (req, res) => {
    let origin = req.headers['origin']
        ? req.headers['origin']
        : '*';

    let lat = req.body.lat,
        lng = req.body.lng,
        peakpower = req.body.peakpower ? req.body.peakpower : 1,
        losses = req.body.losses ? req.body.losses : 14,
        slope = req.body.slope ? req.body.slope : 35,
        azimuth = req.body.azimuth ? req.body.azimuth : 0,
        mounting = req.body.mounting ? req.body.mounting.toLowerCase() : 'free',
        pvtech = req.body.pvtech ? req.body.pvtech : 'crystSi',
        error;

    // Verify payload data.
    if (!lat || !lng) {
        error = 'Both \'lat\' and \'lng\' are required.';
    }

    if (peakpower < 1) {
        error = '\'peakpower\' must be 1 or greater.';
    }

    if (losses < 0 || losses > 100) {
        error = '\'losses\' must be between 0 (including) and 100 (including).';
    }

    if (slope < 0 || slope > 90) {
        error = '\'slope\' must be between 0 (including) and 90 (including).';
    }

    if (azimuth < -180 || azimuth > 180) {
        error = '\'azimuth\' must be between -180 (including) and 180 (including).';
    }

    switch (mounting) {
        case 'free':
        case 'building':
            break;

        default:
            error = '\'mounting\' must be either \'free\' or \'building\'.';
            break;
    }

    switch (pvtech) {
        case 'crystSi':
        case 'CIS':
        case 'CdTe':
            break;

        default:
            error = '\'pvtech\' must be one of the following: \'crystSi\', \'CIS\', or \'CdTe\'.';
            break;
    }

    if (error) {
        res
            .status(400)
            .json({
                message: error
            });

        return;
    }

    return new Promise((resolve, reject) => {
        return resolve(
            QueryPvgisEurope({
                MAX_FILE_SIZE: '10000',
                pv_database: 'PVGIS-classic',
                pvtechchoice: pvtech,
                peakpower: peakpower.toString(),
                efficiency: losses.toString(),
                mountingplace: mounting,
                angle: slope.toString(),
                aspectangle: azimuth.toString(),
                outputchoicebuttons: 'window',
                sbutton: 'Calculate',
                outputformatchoice: 'window',
                optimalchoice: '',
                latitude: lat.toString(),
                longitude: lng.toString(),
                regionname: 'europe',
                language: 'en_en'
            }));
    })
    .then((html) => {
        if (!html) {
            throw new Error('No valid daily radiation data.');
        }

        return ParsePvgisHtml(html);
    })
    .then((values) => {
        res
            .set('Access-Control-Allow-Origin', origin)
            .json(values);
    })
    .catch((err) => {
        res
            .set('Access-Control-Allow-Origin', origin)
            .status(400)
            .json(err);
    });
});

/**
 * Handle CORS.
 */
app.options('/', (req, res) => {
    let origin = req.headers['origin']
        ? req.headers['origin']
        : '*';

    res
        .set('Access-Control-Allow-Origin', origin)
        .status(200)
        .end();
});

// Done, let's fire up the app.
app.listen(port, hostname, () => {
    console.log('Server running at http://' + hostname + ':' + port + '/');
});