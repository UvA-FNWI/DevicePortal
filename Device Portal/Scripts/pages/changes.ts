namespace DP {
    export function page_changes(parameters: string) {
        let state = ks.local_persist('page_changes', {
            devices: <Device[]>[],
            search: new DeviceSearchParams(),
        })

        header_breadcrumbs(['Changes'], function () {
            ks.navigate_to('Changes', pages[Page.Changes]);
        });

        if (isPageSwap) {
            GET_ONCE('fetch device changes', API.Devices('Changes')).done((result: Device[]) => {
                state.devices = result;
                state.devices.sort((a, b) => sort_string(b.dateEdit, a.dateEdit));
                Device.makeSearchableAndFormatted(state.devices);
                ks.refresh();
            });
        }
        if (!state.devices) { return; } // wait for get questions

        ks.row('device changes', function () {
            ks.set_next_item_class_name('mb-3');
            ks.column('devices', 12, function () {
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
                    let settings: DeviceTableSettings = ks.local_persist('####device_table_cols');
                    ks.table_head(function () {
                        ks.table_row(function () {
                            for (let i = 0; i < settings.columns.length; ++i) {
                                let c = settings.columns[i];
                                if (!c.active) { continue; }

                                ks.table_cell(function () {
                                    if (c.searchType === 'combo') {
                                        ks.set_next_item_class_name('custom-select-sm');
                                        ks.combo(c.label, function () {
                                            let keys = Object.keys(c.searchObj);
                                            let keyMax = 0;
                                            for (let key of keys) { keyMax = Math.max(keyMax, parseInt(key)); }
                                            let maskAll = keyMax * 2 - 1;

                                            ks.selectable(c.label, state.search[c.searchField] === maskAll);
                                            ks.is_item_clicked(function () {
                                                state.search[c.searchField] = maskAll;
                                                range.reset();
                                                ks.refresh();
                                            });

                                            for (let key of keys) {
                                                let type = parseInt(key);
                                                ks.selectable(c.searchObj[key], type === state.search[c.searchField]);
                                                ks.is_item_clicked(function () {
                                                    state.search[c.searchField] = type;
                                                    range.reset();
                                                    ks.refresh();
                                                });
                                            }
                                        });
                                    } else {
                                        ks.set_next_item_class_name('form-control-sm');
                                        ks.input_text(c.label, state.search[c.searchField], c.label, function (str) {
                                            state.search[c.searchField] = str;
                                            range.reset();
                                            ks.refresh();
                                        });
                                    }
                                    ks.is_item_clicked(function (_, ev) { ev.stopPropagation(); });
                                }, ks.Sort_Order.none);
                                sort_columns.push({ field: c.searchField, type: c.searchType });
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
                                sort_columns.push({ field: 'status', type: 'combo' });
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
                    });

                    ks.table_body(function () {
                        let countdown = range.i_end - range.i_start;
                        for (let i = range.i_start; countdown > 0; ++i) {
                            if (!device_search_match(search, state.devices[i])) { continue; }

                            ks.set_next_item_class_name('cursor-pointer');
                            device_row(state.devices[i], DTF.EditNote, '');
                            ks.is_item_clicked(function () {
                                deviceModal.show(state.devices[i], true);
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
                paginator('paginator', count, () => ks.refresh(this));
            });
        });
    }
}