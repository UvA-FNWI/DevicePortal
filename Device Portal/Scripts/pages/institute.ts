class DeviceSearchParams {
    name = '';
    deviceId = '';
    serialNr = '';
    type = DeviceType.All;
    os_type = OSType.All;
    status = <DeviceStatus>-1;
}

function page_institute(parameters: string) {
    let state = ks.local_persist('page_device', {
        devices: <Device[]>null,
        search: new DeviceSearchParams(),
    });

    let institute: string;
    if (parameters) {
        let parts = parameters.split('/');
        institute = parts[0];
        if (!state.devices) {
            GET_ONCE('devices', API.Institute(institute) + '/Devices').done((devices: Device[]) => {
                for (let i = 0; i < devices.length; ++i) {
                    let d = devices[i];
                    d.nameLowerCase = d.name ? d.name.toLowerCase() : '';
                    d.deviceIdLowerCase = d.deviceId ? d.deviceId.toLowerCase() : '';
                    d.serialNumberLowerCase = d.serialNumber ? d.serialNumber.toLowerCase() : '';
                }
                devices.sort((a, b) => sort_string(a.name, b.name));
                state.devices = devices;
                ks.refresh();
            });
            return; // wait for devices
        }
    } else {
        ks.navigate_to('Home', '/');
        return;
    }

    header_breadcrumbs([institute], ks.no_op);

    let count = 0;
    let search = new DeviceSearchParams();
    search.name = state.search.name.toLowerCase();
    search.deviceId = state.search.deviceId.toLowerCase();
    search.serialNr = state.search.serialNr.toLowerCase();
    search.type = state.search.type;
    search.os_type = state.search.os_type;
    search.status = state.search.status;

    for (let i = 0; i < state.devices.length; ++i) {
        if (!device_search_match(search, state.devices[i])) { continue; }
        ++count;
    }

    let range = paginator_range('paginator', count);

    ks.set_next_item_class_name('bg-white border');
    ks.table('devices', function () {
        ks.table_head(function () {
            ks.table_row(function () {
                ks.table_cell(function () {
                    ks.set_next_item_class_name('form-control-sm');
                    ks.input_text('name', state.search.name, 'Name', function (str) {
                        state.search.name = str;
                        range.reset();
                        ks.refresh();
                    });
                    ks.is_item_clicked(function (_, ev) { ev.stopPropagation(); });
                }, ks.Sort_Order.asc);

                ks.table_cell(function () {
                    ks.set_next_item_class_name('form-control-sm');
                    ks.input_text('device id', state.search.deviceId, 'Device ID', function (str) {
                        state.search.deviceId = str;
                        range.reset();
                        ks.refresh();
                    });
                    ks.is_item_clicked(function (_, ev) { ev.stopPropagation(); });
                }, ks.Sort_Order.none);

                ks.table_cell(function () {
                    ks.set_next_item_class_name('form-control-sm');
                    ks.input_text('serial number', state.search.serialNr, 'Serial number', function (str) {
                        state.search.serialNr = str;
                        range.reset();
                        ks.refresh();
                    });
                    ks.is_item_clicked(function (_, ev) { ev.stopPropagation(); });
                }, ks.Sort_Order.none);

                ks.table_cell(function () {
                    ks.set_next_item_class_name('custom-select-sm');
                    ks.combo('device types', function () {
                        ks.selectable('Type', state.search.type === DeviceType.All);
                        ks.is_item_clicked(function () {
                            state.search.type = DeviceType.All;
                            range.reset();
                            ks.refresh();
                        });

                        let keys = Object.keys(deviceNames);
                        for (let key of keys) {
                            let type = parseInt(key);
                            ks.selectable(deviceNames[key], type === state.search.type);
                            ks.is_item_clicked(function () {
                                state.search.type = type;
                                range.reset();
                                ks.refresh();
                            });
                        }
                    });
                    ks.is_item_clicked(function (_, ev) { ev.stopPropagation(); });
                }, ks.Sort_Order.none);

                ks.table_cell(function () {
                    ks.set_next_item_class_name('custom-select-sm');
                    ks.combo('os types', function () {
                        ks.selectable('OS', state.search.os_type === OSType.All);
                        ks.is_item_clicked(function () {
                            state.search.os_type = OSType.All;
                            range.reset();
                            ks.refresh();
                        });

                        let keys = Object.keys(osNames);
                        for (let key of keys) {
                            let type = parseInt(key);
                            ks.selectable(osNames[key], type === state.search.os_type);
                            ks.is_item_clicked(function () {
                                state.search.os_type = type;
                                range.reset();
                                ks.refresh();
                            });
                        }
                    });
                    ks.is_item_clicked(function (_, ev) { ev.stopPropagation(); });
                }, ks.Sort_Order.none);

                ks.table_cell(function () {
                    ks.set_next_item_class_name('custom-select-sm');
                    ks.combo('status', function () {
                        ks.selectable('Status', state.search.status < 0);
                        ks.is_item_clicked(function () {
                            state.search.status = -1;
                            range.reset();
                            ks.refresh();
                        });

                        let keys = Object.keys(statusNames);
                        for (let key of keys) {
                            let type = parseInt(key);
                            ks.selectable(statusNames[key], type === state.search.status);
                            ks.is_item_clicked(function () {
                                state.search.status = type;
                                range.reset();
                                ks.refresh();
                            });
                        }
                    });
                    ks.is_item_clicked(function (_, ev) { ev.stopPropagation(); });
                }, ks.Sort_Order.none);
            });
        });


        ks.table_body(function () {
            let countdown = range.i_end - range.i_start;
            for (let i = range.i_start; countdown > 0; ++i) {
                if (!device_search_match(search, state.devices[i])) { continue; }

                device_row(state.devices[i], false, false);
                --countdown;
            }
        });
    }, function (i_head, order) {
            if (i_head === 0) { state.devices.sort((a, b) => order * sort_string(a.name, b.name)); }
            if (i_head === 1) { state.devices.sort((a, b) => order * sort_string(a.deviceId, b.deviceId)); }
            if (i_head === 2) { state.devices.sort((a, b) => order * sort_string(a.serialNumber, b.serialNumber)); }
            if (i_head === 3) { state.devices.sort((a, b) => order * (a.type - b.type)); }
            if (i_head === 4) { state.devices.sort((a, b) => order * (a.os_type - b.os_type)); }
            if (i_head === 5) { state.devices.sort((a, b) => order * (a.status - b.status)); }
    });

    ks.set_next_item_class_name('ml-1');
    paginator('paginator', state.devices.length, () => ks.refresh(this));
}

function device_search_match(p: DeviceSearchParams, d: Device) {
    return (!p.name || d.nameLowerCase.indexOf(p.name) >= 0) &&
           (!p.deviceId || d.deviceIdLowerCase.indexOf(p.deviceId) >= 0) &&
           (!p.serialNr || d.serialNumberLowerCase.indexOf(p.serialNr) >= 0) &&
           (p.type & d.type) &&
           (d.os_type === 0 || (p.os_type & d.os_type)) &&
           (p.status < 0 || p.status == d.status);
}