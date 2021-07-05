namespace DP {
    export class DeviceSearchParams {
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
        labnet = '';
        ipv4 = '';
        ipv6 = '';
        status = <DeviceStatus>-1;

        static copyToLower(params: DeviceSearchParams) {
            let result = new DeviceSearchParams();
            result.user = params.user.toLowerCase();
            result.name = params.name.toLowerCase();
            result.deviceId = params.deviceId.toLowerCase();
            result.serialNr = params.serialNr.toLowerCase();
            result.type = params.type;
            result.category = params.category;
            result.os_type = params.os_type;
            result.costCentre = params.costCentre.toLowerCase();
            result.itracsBuilding = params.itracsBuilding.toLowerCase();
            result.itracsRoom = params.itracsRoom.toLowerCase();
            result.itracsOutlet = params.itracsOutlet.toLowerCase();
            result.status = params.status;
            result.labnet = params.labnet;
            result.ipv4 = params.ipv4;
            result.ipv6 = params.ipv6;
            return result;
        }
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
                    Device.makeSearchableAndFormatted(state.devices);
                    state.devices.sort((a, b) => sort_string(a.name, b.name));
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
        let search = DeviceSearchParams.copyToLower(state.search);

        for (let i = 0; i < state.devices.length; ++i) {
            if (device_search_match(search, state.devices[i])) { ++count; }
        }

        let range = paginator_range('paginator', count);

        let sort_columns = [];
        // TODO: remove on fix
        let workaround: { counter: number } = ks.local_persist('####table_settings_workaround');
        ks.set_next_item_class_name('bg-white border');
        ks.table('devices##' + workaround.counter, function () {
            ks.table_head(function () {
                let settings: DeviceTableSettings = ks.local_persist('####device_table_cols');
                ks.table_row(function () {
                    ks.table_cell('');
                    sort_columns.push(null);

                    ks.table_cell(function () {
                        ks.set_next_item_class_name('form-control-sm');
                        ks.input_text('user', state.search.user, 'User', function (str) {
                            state.search.user = str;
                            range.reset();
                            ks.refresh();
                        });
                        ks.is_item_clicked(function (_, ev) { ev.stopPropagation(); });
                    }, ks.Sort_Order.none);
                    sort_columns.push(null);

                    if (settings.columns[0].active) {
                        ks.table_cell(function () {
                            ks.set_next_item_class_name('form-control-sm');
                            ks.input_text('name', state.search.name, 'Name', function (str) {
                                state.search.name = str;
                                range.reset();
                                ks.refresh();
                            });
                            ks.is_item_clicked(function (_, ev) { ev.stopPropagation(); });
                        }, ks.Sort_Order.asc);
                        sort_columns.push({ field: 'name', type: 'string' });
                    }

                    if (settings.columns[1].active) {
                        ks.table_cell(function () {
                            ks.set_next_item_class_name('form-control-sm');
                            ks.input_text('device id', state.search.deviceId, 'Device ID', function (str) {
                                state.search.deviceId = str;
                                range.reset();
                                ks.refresh();
                            });
                            ks.is_item_clicked(function (_, ev) { ev.stopPropagation(); });
                        }, ks.Sort_Order.none);
                        sort_columns.push({ field: 'deviceId', type: 'string' });
                    }

                    if (settings.columns[2].active) {
                        ks.table_cell(function () {
                            ks.set_next_item_class_name('form-control-sm');
                            ks.input_text('serial number', state.search.serialNr, 'Serial number', function (str) {
                                state.search.serialNr = str;
                                range.reset();
                                ks.refresh();
                            });
                            ks.is_item_clicked(function (_, ev) { ev.stopPropagation(); });
                        }, ks.Sort_Order.none);
                        sort_columns.push({ field: 'serialNr', type: 'string' });
                    }

                    if (settings.columns[3].active) {
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
                        sort_columns.push({ field: 'type', type: 'number' });
                    }

                    if (settings.columns[4].active) {
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
                        sort_columns.push({ field: 'category', type: 'number' });
                    }

                    if (settings.columns[5].active) {
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
                        sort_columns.push({ field: 'os_type', type: 'number' });
                    }

                    if (settings.columns[6].active) {
                        ks.table_cell(function () {
                            ks.set_next_item_class_name('form-control-sm');
                            ks.input_text('cost centre', state.search.costCentre, 'Cost centre', function (str) {
                                state.search.costCentre = str;
                                range.reset();
                                ks.refresh();
                            });
                            ks.is_item_clicked(function (_, ev) { ev.stopPropagation(); });
                        }, ks.Sort_Order.none);
                        sort_columns.push({ field: 'costCentre', type: 'string' });
                    }

                    if (settings.columns[7].active) {
                        ks.table_cell(function () {
                            ks.set_next_item_class_name('form-control-sm');
                            ks.input_text('building', state.search.itracsBuilding, 'Building', function (str) {
                                state.search.itracsBuilding = str;
                                range.reset();
                                ks.refresh();
                            });
                            ks.is_item_clicked(function (_, ev) { ev.stopPropagation(); });
                        }, ks.Sort_Order.none);
                        sort_columns.push({ field: 'itracsBuilding', type: 'string' });
                    }

                    if (settings.columns[8].active) {
                        ks.table_cell(function () {
                            ks.set_next_item_class_name('form-control-sm');
                            ks.input_text('room', state.search.itracsRoom, 'Room', function (str) {
                                state.search.itracsRoom = str;
                                range.reset();
                                ks.refresh();
                            });
                            ks.is_item_clicked(function (_, ev) { ev.stopPropagation(); });
                        }, ks.Sort_Order.none);
                        sort_columns.push({ field: 'itracsRoom', type: 'string' });
                    }

                    if (settings.columns[9].active) {
                        ks.table_cell(function () {
                            ks.set_next_item_class_name('form-control-sm');
                            ks.input_text('outlet', state.search.itracsOutlet, 'Outlet', function (str) {
                                state.search.itracsOutlet = str;
                                range.reset();
                                ks.refresh();
                            });
                            ks.is_item_clicked(function (_, ev) { ev.stopPropagation(); });
                        }, ks.Sort_Order.none);
                        sort_columns.push({ field: 'itracsOutlet', type: 'string' });
                    }

                    if (settings.columns[10].active) {
                        ks.table_cell(function () {
                            ks.set_next_item_class_name('form-control-sm');
                            ks.input_text('Labnet', state.search.labnet, 'Labnet', function (str) {
                                state.search.labnet = str;
                                range.reset();
                                ks.refresh();
                            });
                            ks.is_item_clicked(function (_, ev) { ev.stopPropagation(); });
                        }, ks.Sort_Order.none);
                        sort_columns.push({ field: 'labnetId', type: 'number' });
                    }

                    if (settings.columns[11].active) {
                        ks.table_cell(function () {
                            ks.set_next_item_class_name('form-control-sm');
                            ks.input_text('ipv4', state.search.ipv4, 'IPv4', function (str) {
                                state.search.ipv4 = str;
                                range.reset();
                                ks.refresh();
                            });
                            ks.is_item_clicked(function (_, ev) { ev.stopPropagation(); });
                        }, ks.Sort_Order.none);
                        sort_columns.push({ field: 'ipv4', type: 'string' });
                    }

                    if (settings.columns[12].active) {
                        ks.table_cell(function () {
                            ks.set_next_item_class_name('form-control-sm');
                            ks.input_text('ipv6', state.search.ipv6, 'IPv6', function (str) {
                                state.search.ipv6 = str;
                                range.reset();
                                ks.refresh();
                            });
                            ks.is_item_clicked(function (_, ev) { ev.stopPropagation(); });
                        }, ks.Sort_Order.none);
                        sort_columns.push({ field: 'ipv6', type: 'string' });
                    }

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
                        sort_columns.push({ field: 'status', type: 'number' });
                    }, ks.Sort_Order.none);

                    ks.table_cell(function () {
                        ks.group('dropdown', 'dropdown', function () {
                            ks.set_next_item_class_name('dropdown-toggle btn-sm');
                            ks.button('##btn', ks.no_op, 'outline-secondary').setAttribute('data-toggle', 'dropdown');
                            ks.group('menu', 'dropdown-menu dropdown-menu-right', function () {
                                for (let i = 0; i < settings.columns.length; ++i) {
                                    let c = settings.columns[i];
                                    dropdown_item(c.label, c.active, function () {
                                        c.active = !c.active;
                                        // TODO: remove when fixed
                                        (<any>ks.local_persist('####table_settings_workaround')).counter++;
                                        window.localStorage.setItem('device_table_cols', JSON.stringify(settings));
                                        ks.refresh();
                                    });
                                }
                            });
                        });
                    }).style.width = '1%';
                });
                sort_columns.push(null);
            });


            ks.table_body(function () {
                let countdown = range.i_end - range.i_start;
                for (let i = range.i_start; countdown > 0; ++i) {
                    if (!device_search_match(search, state.devices[i])) { continue; }

                    ks.set_next_item_class_name('cursor-pointer');
                    device_row(state.devices[i], DTF.EditNote | DTF.ShowSharedColumn, state.devices[i].user?.name || '_');
                    ks.is_item_clicked(function () {
                        deviceModal.show(state.devices[i], false);
                    });

                    --countdown;
                }
            });
        }, function (i_head, order) {
            let sc = sort_columns[i_head];
            if (sc) {
                if (sc.type === 'string') {
                    state.devices.sort((a, b) => order * sort_string(a[sc.field], b[sc.field]));
                } else {
                    state.devices.sort((a, b) => order * (a[sc.field] - b[sc.field]));
                }
            }
            if (i_head === 1) { state.devices.sort((a, b) => order * sort_string(a.user?.name, b.user?.name)); }
        });

        ks.set_next_item_class_name('ml-1');
        paginator('paginator', state.devices.count(d => device_search_match(search, d)), () => ks.refresh(this));
    }

    export function device_search_match(p: DeviceSearchParams, d: Device): boolean {
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
            (!p.labnet || p.labnet === ('' + d.labnetId)) &&
            (!p.ipv4 || !d.ipv4 || d.ipv4.indexOf(p.ipv4)) >= 0 &&
            (!p.ipv6 || !d.ipv6 || d.ipv6.indexOf(p.ipv6)) >= 0 &&
            (p.status < 0 || p.status == d.status);
    }
}