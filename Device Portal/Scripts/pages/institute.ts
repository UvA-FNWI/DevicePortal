namespace DP {
    class DeviceSearchParams {
        user = '';
        name = '';
        deviceId = '';
        serialNr = '';
        type = DeviceType.All;
        category = DeviceCategory.All;
        os_type = OSType.All;
        status = <DeviceStatus>-1;
    }
    class Institute {
        name: string;
        users: number;
        usersAuthorized: number;
        usersApprover: number;
        devices: DeviceUser[];
    }
    class DeviceUser extends Device {
        user: string;
        userLowerCase: string;
        userName: string;
        email: string;
    }
    export function page_institute(parameters: string) {
        let state = ks.local_persist('page_device', {
            institute: <Institute>null,
            devices: <DeviceUser[]>null,
            search: new DeviceSearchParams(),
        });

        let instituteId: number;
        if (parameters) {
            let parts = parameters.split('/');
            instituteId = parseInt(parts[0]);
        }
        if (!isNaN(instituteId)) {
            if (!state.institute) {
                GET_ONCE('devices', API.Institutes(instituteId) + '/Overview').done((institute: Institute) => {
                    state.institute = institute;
                    state.devices = institute.devices;
                    for (let d of institute.devices) {
                        d.nameLowerCase = d.name ? d.name.toLowerCase() : '';
                        d.deviceIdLowerCase = d.deviceId ? d.deviceId.toLowerCase() : '';
                        d.serialNumberLowerCase = d.serialNumber ? d.serialNumber.toLowerCase() : '';
                        d.userLowerCase = d.user ? d.user.toLowerCase() : '';
                    }
                    institute.devices.sort((a, b) => sort_string(a.name, b.name));
                    ks.refresh();
                });
                return; // wait for devices
            }
        } else {
            ks.navigate_to('Home', '/');
            return;
        }

        if (state.institute) {
            header_breadcrumbs(['Faculty', state.institute.name], function () {
                ks.navigate_to('Faculty', pages[Page.Faculty]);
            });
        }

        ks.set_next_item_class_name('mx-n2');
        ks.row('stats', function () {
            ks.column('users', '12 col-sm-6 col-md-3 px-2', function () {
                ks.group('card', 'card mb-3', function () {
                    ks.group('body', 'card-body text-center d-flex flex-column justify-content-center', function () {
                        ks.icon('fa fa-users').style.fontSize = '1.5rem';
                        ks.h4(state.institute.users.toString(), 'font-weight-bolder text-secondary mt-2 mb-2');
                        ks.text('Users', 'text-muted');
                    });
                });
            });

            ks.column('auth', '12 col-sm-6 col-md-3 px-2', function () {
                ks.group('card', 'card mb-3', function () {
                    ks.group('body', 'card-body text-center d-flex flex-column justify-content-center', function () {
                        ks.icon('fa fa-list-ol').style.fontSize = '1.5rem';
                        ks.h4(state.institute.usersAuthorized.toString(), 'font-weight-bolder text-secondary mt-2 mb-2');
                        ks.text('Authorized', 'text-muted');
                    });
                });
            });

            ks.column('approvers', '12 col-sm-6 col-md-3 px-2', function () {
                ks.group('card', 'card mb-3', function () {
                    ks.group('body', 'card-body text-center d-flex flex-column justify-content-center', function () {
                        ks.icon('fa fa-gavel').style.fontSize = '1.5rem';
                        ks.h4(state.institute.usersApprover.toString(), 'font-weight-bolder text-secondary mt-2 mb-2');
                        ks.text('Approvers', 'text-muted');
                    });
                });
            });

            ks.column('devices', '12 col-sm-6 col-md-3 px-2', function () {
                ks.group('card', 'card mb-3', function () {
                    ks.group('body', 'card-body text-center d-flex flex-column justify-content-center', function () {
                        ks.icon('fa fa-microchip').style.fontSize = '1.5rem';
                        let count = state.devices.length;
                        ks.h4(count.toString(), 'font-weight-bolder text-secondary mt-2 mb-2');

                        ks.anchor('excel', '#', function () {
                            ks.set_next_item_class_name('d-inline mr-2');
                            ks.icon('fa fa-file-excel-o');
                            ks.set_next_item_class_name('d-inline');
                            ks.text('Devices', 'text-muted');
                        });
                        ks.is_item_clicked(function () {
                            let config: Zipcelx.Config = {
                                filename: `${state.institute.name} - devices`,
                                sheet: {
                                    data: [[
                                        { value: 'User' },
                                        { value: 'UserName' },
                                        { value: 'Email' },
                                        { value: 'Device' },
                                        { value: 'DeviceId' },
                                        { value: 'SerialNumber' },
                                        { value: 'Type' },
                                        { value: 'Category' },
                                        { value: 'OS' },
                                        { value: 'Status' },
                                    ]]
                                }
                            };
                            for (let d of state.institute.devices) {
                                config.sheet.data.push([
                                    { value: d.user },
                                    { value: d.userName },
                                    { value: d.email, },
                                    { value: d.name, },
                                    { value: d.deviceId, },
                                    { value: d.serialNumber, },
                                    { value: deviceTypes[d.type] },
                                    { value: deviceCategories[d.category]},
                                    { value: osNames[d.os_type] },
                                    { value: statusNames[d.status] },
                                ]);
                            }
                            zipcelx(config);
                        });
                    });
                });
            });
        });

        let count = 0;
        let search = new DeviceSearchParams();
        search.user = state.search.user.toLowerCase();
        search.name = state.search.name.toLowerCase();
        search.deviceId = state.search.deviceId.toLowerCase();
        search.serialNr = state.search.serialNr.toLowerCase();
        search.type = state.search.type;
        search.category = state.search.category;
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
                        ks.input_text('user', state.search.user, 'User', function (str) {
                            state.search.user = str;
                            range.reset();
                            ks.refresh();
                        });
                        ks.is_item_clicked(function (_, ev) { ev.stopPropagation(); });
                    }, ks.Sort_Order.asc);

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

                            let keys = Object.keys(deviceTypes);
                            for (let key of keys) {
                                let type = parseInt(key);
                                ks.selectable(deviceTypes[key], type === state.search.type);
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
                        ks.combo('device categories', function () {
                            ks.selectable('Category', state.search.category === DeviceCategory.All);
                            ks.is_item_clicked(function () {
                                state.search.category = DeviceCategory.All;
                                range.reset();
                                ks.refresh();
                            });

                            let keys = Object.keys(deviceCategories);
                            for (let key of keys) {
                                let category = parseInt(key);
                                ks.selectable(deviceCategories[key], category === state.search.category);
                                ks.is_item_clicked(function () {
                                    state.search.category = category;
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
                    device_row(state.devices[i], false, false, state.devices[i].user);
                    --countdown;
                }
            });
        }, function (i_head, order) {
            if (i_head === 0) { state.devices.sort((a, b) => order * sort_string(a.user, b.user)); }
            if (i_head === 1) { state.devices.sort((a, b) => order * sort_string(a.name, b.name)); }
            if (i_head === 2) { state.devices.sort((a, b) => order * sort_string(a.deviceId, b.deviceId)); }
            if (i_head === 3) { state.devices.sort((a, b) => order * sort_string(a.serialNumber, b.serialNumber)); }
            if (i_head === 4) { state.devices.sort((a, b) => order * (a.type - b.type)); }
            if (i_head === 5) { state.devices.sort((a, b) => order * (a.os_type - b.os_type)); }
            if (i_head === 6) { state.devices.sort((a, b) => order * (a.status - b.status)); }
        });

        ks.set_next_item_class_name('ml-1');
        paginator('paginator', state.devices.filter(d => device_search_match(search, d)).length, () => ks.refresh(this));
    }

    function device_search_match(p: DeviceSearchParams, d: DeviceUser): boolean {
        return (!p.user || d.userLowerCase.indexOf(p.user) >= 0) &&
            (!p.name || d.nameLowerCase.indexOf(p.name) >= 0) &&
            (!p.deviceId || d.deviceIdLowerCase.indexOf(p.deviceId) >= 0) &&
            (!p.serialNr || d.serialNumberLowerCase.indexOf(p.serialNr) >= 0) &&
            ((p.type === DeviceType.All && d.type === 0) || p.type & d.type) &&
            ((p.os_type === OSType.All && d.os_type === 0) || (p.os_type & d.os_type)) &&
            ((p.category === DeviceCategory.All && d.category === 0) || (p.category & d.category)) &&
            (p.status < 0 || p.status == d.status);
    }
}