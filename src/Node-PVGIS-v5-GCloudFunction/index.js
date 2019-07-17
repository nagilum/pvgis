'use strict';

const request = require('request');

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
 * Format the input values into a human readable JSON.
 * @param {Object} input Input values.
 * @returns {Object}
 */
var FormatInputValues = (input) => {
    console.log('Function: QueryPvgisEuropeV5');
    console.log('input', input);

    return {
        database: {
            info: 'PVGIS database:',
            value: input.database
        },
        lat: {
            info: 'Latitude',
            value: parseFloat(input.lat)
        },
        lng: {
            info: 'Longitude',
            value: parseFloat(input.lon)
        },
        peakpower: {
            info: 'Nominal power of the PV system',
            value: parseFloat(input.peakpower)
        },
        loss: {
            info: 'System losses(%)',
            value: parseFloat(input.loss)
        },
        angle: {
            info: 'Fixed slope of modules (deg.)',
            value: parseFloat(input.angle)
        },
        aspect: {
            info: 'Orientation (azimuth) of modules',
            value: parseFloat(input.aspect)
        },
        mounting: {
            info: 'Mounting position',
            value: input.mountingplace
        },
        pvtech: {
            info: 'PV technology',
            value: input.pvtechchoice
        }
    };
};

/**
 * Get fixed system values from the array.
 * @param {Array} lines Lines to parse.
 * @returns {Object}
 */
var GetFixedSystemValues = (lines) => {
    let obj;

    lines.forEach((line) => {
        let parts = line
            .replaceAll('\t\t', '\t')
            .split('\t');

        if (!parts ||
            parts.length !== 5 ||
            parts[0] !== 'Fixed system:') {
                return;
            }

        let aoi = parseFloat(parts[1].trim()),
            spectral = parseFloat(parts[2].trim()),
            temp = parseFloat(parts[3].trim()),
            combined = parseFloat(parts[4].trim());
        
        if (!aoi) {
            aoi = parts[1].trim();
        }

        if (!spectral) {
            spectral = parts[2].trim();
        }

        if (!temp) {
            temp = parts[3].trim();
        }

        if (!combined) {
            combined = parts[4].trim();
        }

        obj = {
            aoi: {
                info: 'AOI loss (%)',
                value: aoi
            },
            spectral: {
                info: 'Spectral effects (%)',
                value: spectral
            },
            temp: {
                info: 'Temperature and low irradiance loss (%)',
                value: temp
            },
            combined: {
                info: 'Combined losses (%)',
                value: combined
            }
        };
    });

    return obj;
};

/**
 * Get array'd values, formatted.
 * @param {Array} lines Lines to parse.
 * @param {String} index First item in row.
 * @returns {Object}
 */
var GetRowValues = (lines, index) => {
    let obj;

    lines.forEach((line) => {
        let parts = line
            .replaceAll('\t\t', '\t')
            .split('\t');

        if (!parts ||
            parts.length !== 6 ||
            parts[0] !== index) {
                return;
            }

        obj = {
            'Ed': parseFloat(parts[1].trim()),
            'Em': parseFloat(parts[2].trim()),
            'Hd': parseFloat(parts[3].trim()),
            'Hm': parseFloat(parts[4].trim()),
            'SDm': parseFloat(parts[5].trim())
        };
    });

    return obj;
};

/**
 * Parse the incoming CSV and return usable values.
 * @param {Object} obj Input values and CSV from PVGIS Europe.
 * @returns {Promise}
 */
var ParsePvgisCsv = (obj) => {
    console.log('Function: QueryPvgisEuropeV5');
    console.log('obj', obj);

    return new Promise((resolve, reject) => {
        let lines = obj.csv
            .replaceAll('\n', '')
            .split('\r');

        if (!lines) {
            return reject('Invalid CSV from PVGIS.');
        }

        return resolve({
            input: FormatInputValues(obj.input),
            data: {
                fixedAngle: {
                    monthly: {
                        jan: GetRowValues(lines, '1'),
                        feb: GetRowValues(lines, '2'),
                        mar: GetRowValues(lines, '3'),
                        apr: GetRowValues(lines, '4'),
                        may: GetRowValues(lines, '5'),
                        jun: GetRowValues(lines, '6'),
                        jul: GetRowValues(lines, '7'),
                        aug: GetRowValues(lines, '8'),
                        sep: GetRowValues(lines, '9'),
                        oct: GetRowValues(lines, '10'),
                        nov: GetRowValues(lines, '11'),
                        dec: GetRowValues(lines, '12')
                    },
                    yearly: GetRowValues(lines, 'Year')
                },
                fixedSystem: GetFixedSystemValues(lines)
            },
            info: {
                Ed: 'Average daily energy production from the given system (kWh)',
                Em: 'Average monthly energy production from the given system (kWh)',
                Hd: 'Average daily sum of global irradiation per square meter received by the modules of the given system (kWh/m2)',
                Hm: 'Average monthly sum of global irradiation per square meter received by the modules of the given system (kWh/m2)',
                SDm: 'Standard deviation of the monthly energy production due to year-to-year variation (kWh)'
            }
        });
    });
};

/**
 * Query PVGIS for which database to use.
 * @param {String} lat Latutude
 * @param {String} lng Longitude
 * @param {Array} databases PVGIS databases.
 * @returns {Promise}
 */
var QueryForPvgisDatabase = (lat, lng, databases) => {
    console.log('Function: QueryForPvgisDatabase');
    console.log('lat', lat);
    console.log('lng', lng);

    return new Promise((resolve, reject) => {
        let url =
            'https://re.jrc.ec.europa.eu/pvgis5/extent.php?' +
            'lat=' + lat + '&' +
            'lon=' + lng + '&' +
            'database=' + databases.join(',') + ',';
        
        let options = {
                url: url,
                headers: {
                    'User-Agent': 'QueryPvgis/1.0.0'
                }
            }
        
        console.log('options', options);
        
        request.get(
            options,
            (err, res, body) => {
                console.log('Function: QueryForPvgisDatabase => Response');
                console.log('err', err);
                console.log('body', body);

                if (!body) {
                    err = new Error('https://re.jrc.ec.europa.eu/pvgis5/extent.php did not return a valid response!');
                }

                return err
                    ? reject(err)
                    : resolve(body);
            });
    });
};

/**
 * Query the actual Europa PVGIS v5 server.
 * @param {Object} obj Parameters for call.
 * @returns {Promise}
 */
var QueryPvgisV5 = (obj) => {
    console.log('Function: QueryPvgisV5');
    console.log('obj', obj);

    return new Promise((resolve, reject) => {
        let url =
            'https://re.jrc.ec.europa.eu/pvgis5/PVcalc.php?' +
            'lat=' + obj.lat + '&' +
            'lon=' + obj.lon + '&' +
            'raddatabase=' + obj.database + '&' +
            'browser=1&' +
            'userhorizon=&' +
            'usehorizon=1&' +
            'select_database_grid=' + obj.database + '&' +
            'pvtechchoice=' + obj.pvtechchoice + '&' +
            'peakpower=' + obj.peakpower + '&' +
            'loss=' + obj.loss + '&' +
            'mountingplace=' + obj.mountingplace + '&' +
            'angle=' + obj.angle + '&' +
            'aspect=' + obj.aspect;

        let options = {
            url: url,
            headers: {
                'User-Agent': 'QueryPvgis/1.0.0'
            }
        }

        console.log('options', options);

        request.get(
            options,
            (err, res, body) => {
                console.log('Function: QueryPvgisV5 => Response');
                console.log('err', err);
                console.log('body', body);

                if (!body) {
                    err = new Error('https://re.jrc.ec.europa.eu/pvgis5/PVcalc.php did not return a valid response!');
                }

                return err
                    ? reject(err)
                    : resolve({
                        input: obj,
                        csv: body
                    });
            });
    });
};

/**
 * Get post data from client and re-query the PVGIS service.
 * @param {Object} req Cloud Function request context.
 * @param {Object} res Cloud Function response context.
 */
exports.pvgis = (req, res) => {
    let origin = req.headers['origin']
        ? req.headers['origin']
        : '*';

    res.set('Access-Control-Allow-Origin', origin);
    res.set('Access-Control-Allow-Headers', 'Content-Type');
    res.set('Access-Control-Max-Age', '3600');
    
    if (req.method === 'OPTIONS') {
        res.status(200).end();
        return;
    }
    else if (req.method !== 'POST') {
        res.status(405).end();
        return;
    }

    let lat = req.body.lat,
        lng = req.body.lng,
        peakpower = req.body.peakpower ? req.body.peakpower : 1,
        loss = req.body.loss ? req.body.loss : 14,
        angle = req.body.angle ? req.body.angle : 35,
        aspect = req.body.aspect ? req.body.aspect : 0,
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

    if (loss < 0 || loss > 100) {
        error = '\'loss\' must be between 0 (including) and 100 (including).';
    }

    if (angle < 0 || angle > 90) {
        error = '\'angle\' must be between 0 (including) and 90 (including).';
    }

    if (aspect < -180 || aspect > 180) {
        error = '\'aspect\' must be between -180 (including) and 180 (including).';
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

    console.log('Function: POST');

    if (error) {
        console.log('err', err);

        res
            .status(400)
            .json({
                message: error
            });

        return;
    }

    let databases = [
        'PVGIS-CMSAF',
        'PVGIS-SARAH',
        'PVGIS-NSRDB',
        'PVGIS-ERA5',
        'PVGIS-COSMO'
    ];

    let input = {
        database: null,
        lat: lat.toString(),
        lon: lng.toString(),
        pvtechchoice: pvtech,
        peakpower: peakpower.toString(),
        loss: loss.toString(),
        mountingplace: mounting,
        angle: angle.toString(),
        aspect: aspect.toString()
    };

    return new Promise((resolve, reject) => {
        return resolve(
            QueryForPvgisDatabase(
                input.lat,
                input.lon,
                databases));
    })
    .then((obj) => {
        let lines = obj
            .trim()
            .replaceAll('\r', '')
            .split('\n');

        if (!lines ||
            lines.length !== databases.length) {
                throw new Error('Unable to get PVGIS database list.');
            }

        if (lines[0] === '1') {
            input.database = databases[0];
        }
        else if (lines[1] === '1') {
            input.database = databases[1];
        }
        else if (lines[2] === '1') {
            input.database = databases[2];
        }
        else if (lines[3] === '1') {
            input.database = databases[3];
        }
        else if (lines[4] === '1') {
            input.database = databases[4];
        }

        return QueryPvgisV5(input);
    })
    .then((obj) => {
        console.log('Function: Promise => CSV');
        console.log('obj', obj);

        if (!obj.csv ||
            obj.csv.indexOf('There were no valid daily radiation data for the chosen location') > -1) {
                throw new Error('No valid data from PVGIS v5.');
            }

        return ParsePvgisCsv(obj);
    })
    .then((values) => {
        console.log('Function: Promise => VALUES');
        console.log('values', values);

        res.json(values);
    })
    .catch((err) => {
        console.log('Function: Promise => Error');
        console.log('err', err);

        res
            .status(400)
            .json({
                input: FormatInputValues(input),
                data: {},
                error: {
                    message: err.toString()
                }
            });
    });
};
