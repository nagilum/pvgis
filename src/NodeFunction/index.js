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
 * Query the actual Europa PVGIS server.
 * @param {Object} obj Parameters for call.
 * @returns {Promise}
 */
var QueryPvgisEurope = (obj) => {
    console.log('=======================================================');
    console.log('Function: QueryPvgisEurope');
    console.log('obj', obj);

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

        console.log('options', options);

        request.post(options, (err, res, body) => {
            console.log('err', err);
            console.log('body', body);

            return err
                ? reject(err)
                : resolve(body);
        });
    });
};

/**
 * Parse the incoming HTML and return usable values.
 * @param {String} html HTML from PVGIS Europe.
 * @return {Promise}
 */
var ParsePvgisHtml = (html) => {
    console.log('=======================================================');
    console.log('Function: ParsePvgisHtml');
    console.log('html', html);

    return new Promise((resolve, reject) => { return reject(null); });
};

/**
 * Get post data from client and re-query the PVGIS service.
 */
app.post('/', (req, res) => {
    let lat = req.body.lat,
        lng = req.body.lng,
        peakpower = req.body.peakpower ? req.body.peakpower : 1,
        losses = req.body.losses ? req.body.losses : 14,
        slope = req.body.slope ? req.body.slope : 35,
        azimuth = req.body.azimuth ? req.body.azimuth : 0,
        mounting = req.body.mounting ? req.body.mounting.toLowerCase() : 'free',
        pvtech = req.body.pvtech ? req.body.pvtech.toLowerCase() : 'crystsi',
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
        case 'crystsi':
        case 'cis':
        case 'cdte':
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
                horizonfile: '',
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
        console.log('=======================================================');
        console.log('Function: Then1');
        console.log('html', html);

        if (!html) {
            throw new Error('No valid daily radiation data.');
        }

        return ParsePvgisHtml(html);
    })
    .then((values) => {
        console.log('=======================================================');
        console.log('Function: Then2');
        console.log('values', values);

        // TODO: Output a formatted PVGIS JSON object.
        res.json(values);
    })
    .catch((err) => {
        console.log('=======================================================');
        console.log('Function: PromiseErrorHandler');
        console.log('err', err);

        res
            .status(400)
            .json(err);
    });
});

// Done, let's fire up the app.
app.listen(port, hostname, () => {
    console.log('Server running at http://' + hostname + ':' + port + '/');
});