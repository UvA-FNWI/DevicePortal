﻿namespace DP {
    class DeviceSearchParams {
        user = '';
        name = '';
        deviceId = '';
        serialNr = '';
        type = DeviceType.All;
        category = DeviceCategory.All;
        os_type = OSType.All;
        costCentre = '';
        itracsBuilding = '';
        itracsRoom = '';
        itracsOutlet = '';
        status = <DeviceStatus>-1;
    }
    class Institute {
        name: string;
        users: number;
        usersAuthorized: number;
        usersApprover: number;
        devices: Device[];
    }
    export function page_institute(parameters: string) {
        let state = ks.local_persist('page_device', {
            institute: <Institute>null,
            devices: <Device[]>[],
            search: new DeviceSearchParams(),
        });
        if (isPageSwap) {
            state.institute = undefined;
            if (state.devices) { state.devices.length = 0; }
        }
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
                        if (d.user) { d.user.nameLowerCase = d.user.name ? d.user.name.toLowerCase() : ''; }
                        d.costCentreLowerCase = d.costCentre ? d.costCentre.toLowerCase() : '';
                        d.itracsBuildingLowerCase = d.itracsBuilding ? d.itracsBuilding.toLowerCase() : '';
                        d.itracsRoomLowerCase = d.itracsRoom ? d.itracsRoom.toLowerCase() : '';
                        d.itracsOutletLowerCase = d.itracsOutlet ? d.itracsOutlet.toLowerCase() : '';

                        Device.formatPurchaseDate(d);
                        Device.formatLastSeenDate(d);
                    }
                    institute.devices.sort((a, b) => sort_string(a.name, b.name));
                    ks.refresh();
                });
            }
        } else {
            ks.navigate_to('Home', '/');
            return;
        }

        if (state.institute) {
            header_breadcrumbs(['Faculty', state.institute.name], function () {
                ks.navigate_to('Faculty', pages[Page.Faculty]);
            });
        } else { ks.h5("Loading"); }

        ks.set_next_item_class_name('mx-n2');
        ks.row('stats', function () {
            ks.column('users', '12 col-sm-6 col-md-3 px-2', function () {
                ks.group('card', 'card mb-3', function () {
                    ks.group('body', 'card-body text-center d-flex flex-column justify-content-center', function () {
                        ks.icon('fa fa-users').style.fontSize = '1.5rem';
                        if (state.institute) {
                            ks.h4(state.institute.users.toString(), 'font-weight-bolder text-secondary mt-2 mb-2');
                        }
                        ks.text('Users', 'text-muted');
                    });
                });
            });

            ks.column('auth', '12 col-sm-6 col-md-3 px-2', function () {
                ks.group('card', 'card mb-3', function () {
                    ks.group('body', 'card-body text-center d-flex flex-column justify-content-center', function () {
                        ks.icon('fa fa-list-ol').style.fontSize = '1.5rem';
                        if (state.institute) {
                            ks.h4(state.institute.usersAuthorized.toString(), 'font-weight-bolder text-secondary mt-2 mb-2');
                        }
                        ks.text('Authorized', 'text-muted');
                    });
                });
            });

            ks.column('approvers', '12 col-sm-6 col-md-3 px-2', function () {
                ks.group('card', 'card mb-3', function () {
                    ks.group('body', 'card-body text-center d-flex flex-column justify-content-center', function () {
                        ks.icon('fa fa-gavel').style.fontSize = '1.5rem';
                        if (state.institute) {
                            ks.h4(state.institute.usersApprover.toString(), 'font-weight-bolder text-secondary mt-2 mb-2');
                        }
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
                                    { value: d.user?.name },
                                    { value: d.user?.userName },
                                    { value: d.user?.email, },
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
        search.costCentre = state.search.costCentre.toLowerCase();
        search.itracsBuilding = state.search.itracsBuilding.toLowerCase();
        search.itracsRoom = state.search.itracsRoom.toLowerCase();
        search.itracsOutlet = state.search.itracsOutlet.toLowerCase();
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
                    ks.table_cell('');

                    ks.table_cell(function () {
                        ks.set_next_item_class_name('form-control-sm');
                        ks.input_text('user', state.search.user, 'User', function (str) {
                            state.search.user = str;
                            range.reset();
                            ks.refresh();
                        });
                        ks.is_item_clicked(function (_, ev) { ev.stopPropagation(); });
                    }, ks.Sort_Order.none);

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
                        ks.set_next_item_class_name('form-control-sm');
                        ks.input_text('cost centre', state.search.costCentre, 'Cost centre', function (str) {
                            state.search.costCentre = str;
                            range.reset();
                            ks.refresh();
                        });
                        ks.is_item_clicked(function (_, ev) { ev.stopPropagation(); });
                    }, ks.Sort_Order.none);

                    ks.table_cell(function () {
                        ks.set_next_item_class_name('form-control-sm');
                        ks.input_text('building', state.search.itracsBuilding, 'Building', function (str) {
                            state.search.itracsBuilding = str;
                            range.reset();
                            ks.refresh();
                        });
                        ks.is_item_clicked(function (_, ev) { ev.stopPropagation(); });
                    }, ks.Sort_Order.none);

                    ks.table_cell(function () {
                        ks.set_next_item_class_name('form-control-sm');
                        ks.input_text('room', state.search.itracsRoom, 'Room', function (str) {
                            state.search.itracsRoom = str;
                            range.reset();
                            ks.refresh();
                        });
                        ks.is_item_clicked(function (_, ev) { ev.stopPropagation(); });
                    }, ks.Sort_Order.none);

                    ks.table_cell(function () {
                        ks.set_next_item_class_name('form-control-sm');
                        ks.input_text('outlet', state.search.itracsOutlet, 'Outlet', function (str) {
                            state.search.itracsOutlet = str;
                            range.reset();
                            ks.refresh();
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

                    ks.table_cell('');
                });
            });


            ks.table_body(function () {
                let countdown = range.i_end - range.i_start;
                for (let i = range.i_start; countdown > 0; ++i) {
                    if (!device_search_match(search, state.devices[i])) { continue; }

                    ks.set_next_item_class_name('cursor-pointer');
                    device_row(state.devices[i], DTF.EditNote | DTF.ShowSharedColumn, state.devices[i].user?.name || '_');
                    ks.is_item_clicked(function () {
                        deviceModal.show(state.devices[i]);
                    });

                    --countdown;
                }
            });
        }, function (i_head, order) {
            if (i_head === 0) { state.devices.sort((a, b) => order * sort_string(a.user?.name, b.user?.name)); }
            if (i_head === 1) { state.devices.sort((a, b) => order * sort_string(a.name, b.name)); }
            if (i_head === 2) { state.devices.sort((a, b) => order * sort_string(a.deviceId, b.deviceId)); }
            if (i_head === 3) { state.devices.sort((a, b) => order * sort_string(a.serialNumber, b.serialNumber)); }
            if (i_head === 4) { state.devices.sort((a, b) => order * (a.type - b.type)); }
            if (i_head === 5) { state.devices.sort((a, b) => order * (a.os_type - b.os_type)); }
            if (i_head === 6) { state.devices.sort((a, b) => order * sort_string(a.costCentre, b.costCentre)); }
            if (i_head === 7) { state.devices.sort((a, b) => order * sort_string(a.itracsBuilding, b.itracsBuilding)); }
            if (i_head === 8) { state.devices.sort((a, b) => order * sort_string(a.itracsRoom, b.itracsRoom)); }
            if (i_head === 9) { state.devices.sort((a, b) => order * sort_string(a.itracsOutlet, b.itracsOutlet)); }
            if (i_head === 10) { state.devices.sort((a, b) => order * (a.status - b.status)); }
        });

        ks.set_next_item_class_name('ml-1');
        paginator('paginator', state.devices.filter(d => device_search_match(search, d)).length, () => ks.refresh(this));
    }

    function device_search_match(p: DeviceSearchParams, d: Device): boolean {
        return (!p.user || d.user?.nameLowerCase.indexOf(p.user) >= 0) &&
            (!p.name || d.nameLowerCase.indexOf(p.name) >= 0) &&
            (!p.deviceId || d.deviceIdLowerCase.indexOf(p.deviceId) >= 0) &&
            (!p.serialNr || d.serialNumberLowerCase.indexOf(p.serialNr) >= 0) &&
            ((p.type === DeviceType.All && d.type === 0) || (p.type & d.type) > 0) &&
            ((p.os_type === OSType.All && d.os_type === 0) || (p.os_type & d.os_type) > 0) &&
            ((p.category === DeviceCategory.All && d.category === 0) || (p.category & d.category) > 0) &&
            (!p.costCentre || d.costCentreLowerCase.indexOf(p.costCentre) >= 0) &&
            (!p.itracsBuilding || d.itracsBuildingLowerCase.indexOf(p.itracsBuilding) >= 0) &&
            (!p.itracsRoom || d.itracsRoomLowerCase.indexOf(p.itracsRoom) >= 0) &&
            (!p.itracsOutlet || d.itracsOutletLowerCase.indexOf(p.itracsOutlet) >= 0) &&
            (p.status < 0 || p.status == d.status);
    }
}